using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Globalization;
using System.Threading;

namespace MarkerParser
{
    public partial class FrmMainForm : Form
    {
        /* Arrays of Controls */
        CheckBox[] MealCheckBox;
        ComboBox[] StartComboBox;
        ComboBox[] EndComboBox;
        Label[] MealMinutes;
        TextBox[] Descriptions;
        TextBox[] Activities;
        ComboBox[] Locations;
        CheckBox[] Seconds;
        CheckBox[] Company;
        CheckBox[] CEating;
        TextBox[] MealNames;

        object FormObject;

        String InputFileName, OutputFileName, MarkerFileName, InterviewFileName, StartTimeString, EndTimeString;
        DateTime StartTime = new DateTime(0), EndTime;
        List<string> Markers = new List<string>();

        ManualResetEvent runThread = new ManualResetEvent(false);
        Thread t;
        UInt64 BytesPerLine = 306;  // Average number of bytes each line in the original file has
        UInt64 TotalLines = 0;
        UInt64 CurrentLine = 0;
        double Percentage = 0;
        UInt16 UPercentage = 0;
        UInt64 ExpSamples = 0;
        UInt64 LinesWritten = 0;
        UInt64 ReadAndDiscarded = 0;

        double sampleFreq = 15.0f;
        double beta = 0.1f;
        double q0 = 1.0f, q1 = 0.0f, q2 = 0.0f, q3 = 0.0f;	// quaternion of sensor frame relative to auxiliary frame


        double TicksPerSample = (TimeSpan.TicksPerSecond / 15.00f);

        double ax = 0, ay = 0, az = 0, Gx = 0, Gy = 0, Gz = 0, gx = 0, gy = 0, gz = 0, lx = 0, ly = 0, lz = 0, mx = 0, my = 0, mz = 0;

        
        double[,] R = new double[,] { { 0, 0, 0 }, { 0, 0, 0 }, { 0, 0, 0} };

        bool EndThread = true;
        bool ReadAndDiscard = false;

        /* Column Numbers */
        int COL_TIMESTRING = 16;    /* Default Consensys 0.2.0 = 16 */
        int COL_AX = 10;            /* Default Consensys 0.2.0 = 10 */
        int COL_GX = 7;             /* Default Consensys 0.2.0 = 7 */
        int COL_MX = 12;            /* Default Consensys 0.2.0 = 12 */
        int COL_Q0 = 3;             /* Default Consensys 0.2.0 = 3 */
        int COL_BUTTON = 2;         /* Default Consensys 0.2.0 = 2 */


        StreamWriter MarkerWriter;
        BinaryWriter DataWriter;
        StreamReader reader;


        private void WorkerThread()
        {

            while (!EndThread)
            {
                runThread.WaitOne(Timeout.Infinite);

                while (!EndThread)
                {
                    try
                    {
                        String FileDateTimeString; // String from the file which contains time and date
                        DateTime FileDateTime; // DateTime generated from the File data
                        DateTime CurrentEstimate = new DateTime(0);
                        DateTime Time = StartTime;
                        DateTime CompTime = StartTime, LongCompTime = StartTime;

                        LinesWritten = 0;
                        Percentage = 0;
                        UPercentage = 0;
                        ReadAndDiscarded = 0;

                        /* Modify the button so it can't be pressed again */
                        lbStatus.Parent.Invoke((MethodInvoker)delegate {
                            lbStatus.Text = @"Working";
                            lbStatus.ForeColor = Color.YellowGreen;
                            lbStatus.Refresh();
                            btnReadData.Enabled = false;
                        });
                        

                        
                        MarkerWriter = new StreamWriter(File.OpenWrite(MarkerFileName));
                        DataWriter = new BinaryWriter(File.OpenWrite(OutputFileName));

                        reader = new StreamReader(File.OpenRead(InputFileName));

                        FileInfo FI = new FileInfo(InputFileName);
                        TotalLines = (UInt64) FI.Length / BytesPerLine;

                        Percentage = (CurrentLine * 100 / TotalLines);
                        UPercentage = (UInt16)Percentage;

                        /* Read first line with seperator information */
                        var line = reader.ReadLine();
                        var values = line.Split('\t');

                       

                        /* Read first line with column titles */
                        line = reader.ReadLine();

                        /* Read first line with column units */
                        line = reader.ReadLine();
                        values = line.Split('\t');

                        if (values.Length < 18)
                        {
                            lbStatus.Parent.Invoke((MethodInvoker)delegate
                            {
                                btnReadData.Enabled = true;
                                lbStatus.Text = "Data is corrupted. Open CSV file in Excel. Column Q should have date.";
                                lbStatus.ForeColor = Color.Red;
                            });
                            MarkerWriter.Close();
                            DataWriter.Close();
                            EndThread = true;
                            return;

                        }

                        CurrentLine = 3;

                        /* Force reading of 3 lines */
                        /* The Quaternion data for the first few lines is bad, so we ignore it */

                        line = reader.ReadLine();
                        CurrentLine++;
                        line = reader.ReadLine();
                        CurrentLine++;
                        line = reader.ReadLine();
                        CurrentLine++;

                        values = line.Split('\t');

                        /* This the start of the data */
                        /* Write the START Marker Details */
                        /* Time / Marker / Missed Samples */

                        var time = values[COL_TIMESTRING].Split('_');

                        GetTimeString(time, out FileDateTimeString);
                        FileDateTime = Convert.ToDateTime(FileDateTimeString);

                        StartTime = FileDateTime;
                        Time = StartTime;
                        MarkerWriter.WriteLine("START\t" + FileDateTime.Date.ToString("u").Substring(0, 10) + "\t" + time[3].ToString().Substring(0, 8));
                        StartTimeString = "START\t" + FileDateTime.Date.ToString("u").Substring(0, 10) + "\t" + time[3].ToString().Substring(0, 8);

                        /* Read actual lines */
                        while (true)
                        {
                            /* Increment Time only if Time is not more than half a second ahead of FileTime*/
                            CompTime = FileDateTime.AddMilliseconds(267);
                            if (Time.CompareTo(CompTime) <= 0)
                                Time = Time.AddTicks((long)TicksPerSample);
                            else ReadAndDiscard = true; /* Otherwise we read from the file and discard the data instead of writing it */
                            
                            CompTime = FileDateTime;
                            LongCompTime = FileDateTime.AddSeconds(5);
                            if((FileDateTime-Time).TotalSeconds > 5)
                            {
                                lx = ly = lz = Gx = Gy = Gz = 0; // Pad with zeros
                            }
                            else if (Time.CompareTo(CompTime) >= 0) /* Is Time later than FileTime + 66ms ? */
                            {
                                /* Read the line from the file */
                                line = reader.ReadLine();
                                if (reader.EndOfStream)
                                    break;
                                CurrentLine++;                                  /* Increment the counter tracking progress */
                                /* Calculate and display progress */
                                Percentage = (CurrentLine * 100 / TotalLines);
                                if ((UInt16)Percentage > UPercentage)       /* Failsafe to check that percentage isn't greater than 100 - this happens if we miscalculate file size */
                                {
                                    ProgressBar.Parent.Invoke((MethodInvoker)delegate
                                    {
                                        ProgressBar.ForeColor = Color.YellowGreen;
                                        ProgressBar.Value = ((UInt16)Percentage <= 100) ? (UInt16)Percentage : 100;
                                    });

                                    lbStatus.Parent.Invoke((MethodInvoker)delegate
                                    {
                                        lbStatus.Text = @"Working " + Percentage.ToString() + "%";
                                        lbStatus.ForeColor = Color.YellowGreen;
                                        lbStatus.Refresh();
                                    });

                                }

                                UPercentage = (UInt16)Percentage;

                                /* Split the line so it can be parsed */
                                values = line.Split('\t');


                                /* Time / Marker / Missed Samples */
                                time = values[COL_TIMESTRING].Split('_');
                                GetTimeString(time, out FileDateTimeString);

                                CurrentEstimate = FileDateTime;
                                FileDateTime = Convert.ToDateTime(FileDateTimeString);
                                
                                /* Is this a Marker (Button Press), if so, write the time to the marker file */
                                if ((Double.Parse(values[COL_BUTTON])) > 0)
                                {
                                    MarkerWriter.WriteLine("MARKER\t" + time[3].ToString().Substring(0, 8));
                                    Markers.Add(time[3].ToString().Substring(0, 8));
                                }

                                /* Calculate the raw acceleration from Quaternion data */
                                CalculateLinear(values, out lx, out ly, out lz);
                            }

                            if (ReadAndDiscard == true)
                            {
                                ReadAndDiscard = false;
                                ReadAndDiscarded++;
                                continue;
                            }


                                DataWriter.Write((float)lx);
                                DataWriter.Write((float)ly);
                                DataWriter.Write((float)lz);
                                DataWriter.Write((float)Gx);
                                DataWriter.Write((float)Gy);
                                DataWriter.Write((float)Gz);
                                LinesWritten++;
                                
                        }


                        /* Write the END Marker details */
                        time = values[COL_TIMESTRING].Split('_');
                        GetTimeString(time, out FileDateTimeString);
                        FileDateTime = Convert.ToDateTime(FileDateTimeString);
                        EndTime = FileDateTime;
                        MarkerWriter.WriteLine("END\t" + FileDateTime.Date.ToString("u").Substring(0, 10) + "\t" + time[3].ToString().Substring(0, 8)+"\n\n");
                        EndTimeString = "END\t" + FileDateTime.Date.ToString("u").Substring(0, 10) + "\t" + time[3].ToString().Substring(0, 8) + "\n\n";
                        
                        /* Close the Writer streams */
                        MarkerWriter.Close();
                        DataWriter.Close();

                        ExpSamples = (UInt64)((EndTime.Subtract(StartTime)).TotalSeconds * 15);
                        DateTimeFormatInfo fmt = (new CultureInfo("hr-HR")).DateTimeFormat;

                        lbStatus.Parent.Invoke((MethodInvoker)delegate {
                            lbStatus.Text = "Finished reading data. Please interview\nparticipant and write events.";//  "Expected: " + ExpSamples.ToString() + " Written: " + LinesWritten.ToString() + " E - W: " + ((long)(ExpSamples - LinesWritten)).ToString() + "\nFileEndTime: " + FileDateTime.ToLongTimeString() +"\nCalcEndTime" + Time.ToLongTimeString()+ "\nSamples Added " + ((long)(LinesWritten-CurrentLine)).ToString() + " Discarded " + ReadAndDiscarded.ToString();
                            lbStatus.ForeColor = Color.Green;
                            lbStatus.Refresh();
                            btnReadData.Enabled = false;
                            ProgressBar.ForeColor = Color.Green;
                            ProgressBar.Value = 100;
                            btnOpenIFile.Enabled = true;
                            lbStartTime.Text = StartTime.ToString("T", fmt);
                            lbEndTime.Text = EndTime.ToString("T", fmt);
                            FrmMainForm.ActiveForm.Size = new System.Drawing.Size(1391, 464);
                            
                        });

                        MarkerWriter.Close();
                        DataWriter.Close();
                        reader.Close();
                        
                        StartTime1.Parent.Invoke((MethodInvoker)delegate
                        {
                        /* Populate Combo boxes */
                        for(int i = 0; i < 10; i++ )
                        {
                            foreach (String S in Markers)
                            {
                                StartComboBox[i].Items.Add(S);
                                EndComboBox[i].Items.Add(S);
                            }
                        }
                        });

                        EndThread = true;
                    }

                    catch (Exception ex)
                    {
                        
                        lbStatus.Parent.Invoke((MethodInvoker)delegate {
                            btnReadData.Enabled = false;
                            lbStatus.Text = "An error has occured. Close this program and try again.";
                            lbStatus.ForeColor = Color.Red;
                        });
                        MarkerWriter.Close();
                        DataWriter.Close();
                        EndThread = true;
                        return;
                    }
                }
            }
        }


        public FrmMainForm()
        {
            InitializeComponent();
            InitItems();
        }

        private void InitItems()
        {
            Int32 unixTimestamp = (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
            TBTime.Text = unixTimestamp.ToString();
            
            MealCheckBox = new CheckBox[] { checkBox1, checkBox2, checkBox3, checkBox4, checkBox5, checkBox6, checkBox7, checkBox8, checkBox9, checkBox10 };
            StartComboBox = new ComboBox[] { StartTime1, StartTime2, StartTime3, StartTime4, StartTime5, StartTime6, StartTime7, StartTime8, StartTime9, StartTime10 };
            EndComboBox = new ComboBox[] { EndTime1, EndTime2, EndTime3, EndTime4, EndTime5, EndTime6,  EndTime7, EndTime8, EndTime9, EndTime10 };
            MealMinutes = new Label[] { Length1, Length2, Length3, Length4, Length5, Length6, Length7, Length8, Length9, Length10 };
            Descriptions = new TextBox[] {Desc1, Desc2, Desc3, Desc4, Desc5,  Desc6, Desc7,  Desc8, Desc9, Desc10};
            Activities = new TextBox[] {Activity1, Activity2, Activity3, Activity4, Activity5, Activity6, Activity7, Activity8, Activity9, Activity10};
            Locations = new ComboBox[] {Location1, Location2, Location3, Location4, Location5, Location6, Location7, Location8, Location9, Location10};
            Seconds = new CheckBox[] {Seconds1, Seconds2, Seconds3, Seconds4, Seconds5, Seconds6, Seconds7, Seconds8, Seconds9, Seconds10};
            Company = new CheckBox[] {Company1, Company2, Company3, Company4, Company5, Company6, Company7, Company8, Company9, Company10};
            CEating  = new CheckBox[] {CEating1, CEating2, CEating3, CEating4, CEating5, CEating6, CEating7, CEating8, CEating9, CEating10};
            MealNames = new TextBox[] { MealName1, MealName2, MealName3, MealName4, MealName5, MealName6, MealName7, MealName8, MealName9, MealName10 };
            for (int i = 0; i < 10; i++)
            {
                MealCheckBox[i].Checked = false;
                StartComboBox[i].Items.Clear();
                StartComboBox[i].SelectedIndex = -1;
                EndComboBox[i].Items.Clear();
                EndComboBox[i].SelectedIndex = -1;
                MealMinutes[i].Text = "";
                Descriptions[i].Text = "";
                Activities[i].Text = "";
                Locations[i].Items.Clear();
                Locations[i].SelectedIndex = -1;
                Locations[i].Items.Add("Home");
                Locations[i].Items.Add("Restaurant");
                Locations[i].Items.Add("Office");
                Locations[i].Items.Add("Other");
                Seconds[i].Checked = false;
                Company[i].Checked = false;
                CEating[i].Checked = false;
            }

        }


        private void button2_Click(object sender, EventArgs e)
        {
            InputFileName = TBInputFile.Text;
            ExpSamples = 0;

            if (TBOutputFile.Text.Equals("", StringComparison.Ordinal))
            {
                lbStatus.Parent.Invoke((MethodInvoker)delegate
                {
                    btnReadData.Enabled = true;
                    lbStatus.Text = "Please pick an output File";
                    lbStatus.ForeColor = Color.Red;
                });
            }
            else
            {
                lbStartTimeWarning.Text = "";
                lbStartTimeWarning.ForeColor = Color.Green;
                lbEndTimeWarning.Text = "";
                lbEndTimeWarning.ForeColor = Color.Green;
                EndThread = false;
                t = new Thread(WorkerThread);
                t.Start();
                runThread.Set();
            }

            btnReadData.Enabled = false;
        }
        
        private void button1_Click(object sender, EventArgs e)
        {
            // Create an instance of the open file dialog box.
            OpenFileDialog openFileDialog1 = new OpenFileDialog();

            // Set filter options and filter index.
            openFileDialog1.Filter = "CSV Files (.csv)|*.csv|All Files (*.*)|*.*";
            openFileDialog1.FilterIndex = 1;

            // Call the ShowDialog method to show the dialog box.
            DialogResult userClickedOK = openFileDialog1.ShowDialog();

             // Process input if the user clicked OK.
            if (userClickedOK == DialogResult.OK)
            {
                btnReadData.Enabled = true;
                InputFileName = openFileDialog1.FileName;
                TBInputFile.Text = InputFileName;
                OutputFileName = new DirectoryInfo(InputFileName).Parent.Parent.Name;
                
                OutputFileName = new DirectoryInfo(InputFileName).Parent.Parent.FullName +"\\"+ OutputFileName + ".txt";
                TBOutputFile.Text = OutputFileName;
                MarkerFileName = OutputFileName.Substring(0, OutputFileName.Length - 4) + "-markers.txt";
                InterviewFileName = OutputFileName.Substring(0, OutputFileName.Length - 4) + "-events.txt";

                lbStatus.Text = @"Ready";
                lbStatus.ForeColor = Color.Green;
                lbStatus.Refresh();

                if(File.Exists(MarkerFileName)) //Ask if the previous files need to be deleted or you want to abort the operation.
                {
                    DialogResult dialogResult = MessageBox.Show("Shimmerview Files for this Participant already exist. Do you want to delete the previous files and create new ones? Make sure you backup any previous interview information.", "Duplicate files", MessageBoxButtons.YesNo);
                    if (dialogResult == DialogResult.Yes)
                    {
                        string[] filePaths = Directory.GetFiles(new DirectoryInfo(InputFileName).Parent.Parent.FullName);
                        foreach (string filePath in filePaths)
                        File.Delete(filePath);
                        lbStatus.Text = @"Previous Files Deleted";
                        lbStatus.ForeColor = Color.Red;
                        lbStatus.Refresh();
                    }
                    else if (dialogResult == DialogResult.No)
                    {
                        lbStatus.Text = @"Check Files in folder and restart MarkerParser";
                        lbStatus.ForeColor = Color.Red;
                        lbStatus.Refresh();
                        btnReadData.Enabled = false;
                    }
                }

                

                
            }



        }

        private void button4_Click(object sender, EventArgs e)
        {
            Int32 unixTimestamp = (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
            TBTime.Text = unixTimestamp.ToString();
        }

        private void CalculateLinear(String[] values, out double lx, out double ly, out double lz)
        {
            
            Gx = double.Parse(values[COL_GX]);
            Gy = double.Parse(values[COL_GX + 1]);
            Gz = double.Parse(values[COL_GX + 2]);

            ax = double.Parse(values[COL_AX]);
            ay = double.Parse(values[COL_AX + 1]);
            az = double.Parse(values[COL_AX + 2]);

            mx = double.Parse(values[COL_MX]);
            my = double.Parse(values[COL_MX + 1]);
            mz = double.Parse(values[COL_MX + 2]);

            /* Even if you enable this, it changes q0,q1,...q3 not _q0,_q1,...q3 */
            /*
            This part of the code was added to consider usning AHRS algorithms instead of
            the Quaternion supplied by Invensenses DMP. It is unused. A program called AHRSTEST was
            created instead.
            */
            //MadgwickAHRSupdate(Gx, Gy, Gz, ax, ay, az, mx, my, mz);

            double _q0 = double.Parse(values[COL_Q0]);
            double _q1 = double.Parse(values[COL_Q0 + 1]);
            double _q2 = double.Parse(values[COL_Q0 + 2]);
            double _q3 = double.Parse(values[COL_Q0 + 3]);

            double sq__q1 = 2 * _q1 * _q1;
            double sq__q2 = 2 * _q2 * _q2;
            double sq__q3 = 2 * _q3 * _q3;
            double _q1__q2 = 2 * _q1 * _q2;
            double _q3__q0 = 2 * _q3 * _q0;
            double _q1__q3 = 2 * _q1 * _q3;
            double _q2__q0 = 2 * _q2 * _q0;
            double _q2__q3 = 2 * _q2 * _q3;
            double _q1__q0 = 2 * _q1 * _q0;

            R[0, 0] = 1 - sq__q2 - sq__q3;
            R[0, 1] = _q1__q2 - _q3__q0;
            R[0, 2] = _q1__q3 + _q2__q0;
            R[1, 0] = _q1__q2 + _q3__q0;
            R[1, 1] = 1 - sq__q1 - sq__q3;
            R[1, 2] = _q2__q3 - _q1__q0;
            R[2, 0] = _q1__q3 - _q2__q0;
            R[2, 1] = _q2__q3 + _q1__q0;
            R[2, 2] = 1 - sq__q1 - sq__q2;

            /* Seperating gravity */
            /* This is [gx gy gz] = [0 0 1] * R, where R is the Rotation Matrix above */
            gx = R[2, 0];
            gy = R[2, 1];
            gz = R[2, 2];

            /* Adjust signs for Data Port Facing Outside (towards the fingers) */
            lx = -(ax - gx);
            ly = -(ay - gy);
            lz = -(az - gz);

            /* Move data around for correct axes orientation in Shimmer Vs the iPhone*/
            // If data port pointing away from hand. Swap X and Y
            gx = lx;
            lx = ly;
            ly = gx;
        }

        void GetTimeString(string[] time, out string FileDateTimeString)
        {
            if (time[0].Length < 4)
                FileDateTimeString = time[0].Substring(0, 1) + " " + time[1].ToString().Substring(0, 3) + " " + time[2].ToString() + " " + time[3].ToString();
            else
                FileDateTimeString = time[0].Substring(0, 2) + " " + time[1].ToString().Substring(0, 3) + " " + time[2].ToString() + " " + time[3].ToString();

        }

        private void button4_Click_1(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start("C:\\Shimmerview.exe",OutputFileName);
        }

        private void EndTime1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (StartTime1.SelectedIndex > -1)
            {
                try
                {
                    Length1.Text = ((Convert.ToDateTime(EndTime1.SelectedItem.ToString()) - Convert.ToDateTime(StartTime1.SelectedItem.ToString())).TotalMinutes.ToString()).Split('.')[0];
                }
                catch
                {

                }
            }
        }

        private void StartTime1_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                Length1.Text = ((Convert.ToDateTime(EndTime1.SelectedItem.ToString()) - Convert.ToDateTime(StartTime1.SelectedItem.ToString())).TotalMinutes.ToString()).Split('.')[0];
            }
            catch
            {

            }
        }

        private void EndTime2_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                Length2.Text = ((Convert.ToDateTime(EndTime2.SelectedItem.ToString()) - Convert.ToDateTime(StartTime2.SelectedItem.ToString())).TotalMinutes.ToString()).Split('.')[0];
            }
            catch
            {

            }
        }

        private void StartTime2_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                Length2.Text = ((Convert.ToDateTime(EndTime2.SelectedItem.ToString()) - Convert.ToDateTime(StartTime2.SelectedItem.ToString())).TotalMinutes.ToString()).Split('.')[0];
            }
            catch
            {

            }
        }

        private void StartTime3_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                Length3.Text = ((Convert.ToDateTime(EndTime3.SelectedItem.ToString()) - Convert.ToDateTime(StartTime3.SelectedItem.ToString())).TotalMinutes.ToString()).Split('.')[0];
            }
            catch
            {

            }
        }

        private void StartTime4_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                Length4.Text = ((Convert.ToDateTime(EndTime4.SelectedItem.ToString()) - Convert.ToDateTime(StartTime4.SelectedItem.ToString())).TotalMinutes.ToString()).Split('.')[0];
            }
            catch
            {

            }
        }

        private void StartTime5_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                Length5.Text = ((Convert.ToDateTime(EndTime5.SelectedItem.ToString()) - Convert.ToDateTime(StartTime5.SelectedItem.ToString())).TotalMinutes.ToString()).Split('.')[0];
            }
            catch
            {

            }
        }

        private void StartTime6_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                Length6.Text = ((Convert.ToDateTime(EndTime6.SelectedItem.ToString()) - Convert.ToDateTime(StartTime6.SelectedItem.ToString())).TotalMinutes.ToString()).Split('.')[0];
            }
            catch
            {

            }
        }

        private void StartTime7_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                Length7.Text = ((Convert.ToDateTime(EndTime7.SelectedItem.ToString()) - Convert.ToDateTime(StartTime7.SelectedItem.ToString())).TotalMinutes.ToString()).Split('.')[0];
            }
            catch
            {

            }
        }

        private void StartTime8_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                Length8.Text = ((Convert.ToDateTime(EndTime8.SelectedItem.ToString()) - Convert.ToDateTime(StartTime8.SelectedItem.ToString())).TotalMinutes.ToString()).Split('.')[0];
            }
            catch
            {

            }
        }

        private void StartTime9_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                Length9.Text = ((Convert.ToDateTime(EndTime9.SelectedItem.ToString()) - Convert.ToDateTime(StartTime9.SelectedItem.ToString())).TotalMinutes.ToString()).Split('.')[0];
            }
            catch
            {

            }
        }

        private void StartTime10_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                Length10.Text = ((Convert.ToDateTime(EndTime10.SelectedItem.ToString()) - Convert.ToDateTime(StartTime10.SelectedItem.ToString())).TotalMinutes.ToString()).Split('.')[0];
            }
            catch
            {

            }
        }

        private void EndTime3_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                Length3.Text = ((Convert.ToDateTime(EndTime3.SelectedItem.ToString()) - Convert.ToDateTime(StartTime3.SelectedItem.ToString())).TotalMinutes.ToString()).Split('.')[0];
            }
            catch
            {

            }
        }

        private void EndTime4_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                Length4.Text = ((Convert.ToDateTime(EndTime4.SelectedItem.ToString()) - Convert.ToDateTime(StartTime4.SelectedItem.ToString())).TotalMinutes.ToString()).Split('.')[0];
            }
            catch
            {

            }
        }

        private void EndTime5_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                Length5.Text = ((Convert.ToDateTime(EndTime5.SelectedItem.ToString()) - Convert.ToDateTime(StartTime5.SelectedItem.ToString())).TotalMinutes.ToString()).Split('.')[0];
            }
            catch
            {

            }
        }

        private void EndTime6_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                Length6.Text = ((Convert.ToDateTime(EndTime6.SelectedItem.ToString()) - Convert.ToDateTime(StartTime6.SelectedItem.ToString())).TotalMinutes.ToString()).Split('.')[0];
            }
            catch
            {

            }
        }

        private void EndTime7_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                Length7.Text = ((Convert.ToDateTime(EndTime7.SelectedItem.ToString()) - Convert.ToDateTime(StartTime7.SelectedItem.ToString())).TotalMinutes.ToString()).Split('.')[0];
            }
            catch
            {

            }
        }

        private void EndTime8_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                Length8.Text = ((Convert.ToDateTime(EndTime8.SelectedItem.ToString()) - Convert.ToDateTime(StartTime8.SelectedItem.ToString())).TotalMinutes.ToString()).Split('.')[0];
            }
            catch
            {

            }
        }

        private void EndTime9_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                Length9.Text = ((Convert.ToDateTime(EndTime9.SelectedItem.ToString()) - Convert.ToDateTime(StartTime9.SelectedItem.ToString())).TotalMinutes.ToString()).Split('.')[0];
            }
            catch
            {

            }
        }

        private void EndTime10_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                Length10.Text = ((Convert.ToDateTime(EndTime10.SelectedItem.ToString()) - Convert.ToDateTime(StartTime10.SelectedItem.ToString())).TotalMinutes.ToString()).Split('.')[0];
            }
            catch
            {

            }
        }

        private void btnWriteEvents_Click(object sender, EventArgs e)
        {
            String FirstMealStartTime = "" , LastMealEndTime = "";
            bool first = true;
            /* First create the events file */
            InterviewFileName = OutputFileName.Substring(0, OutputFileName.Length - 4) + "-events.txt";
            MarkerWriter = new StreamWriter(File.OpenWrite(InterviewFileName));
            MarkerWriter.WriteLine(StartTimeString);
            
            for (int i = 0; i<10; i++)
            {
                if(MealCheckBox[i].Checked == true)
                {
                    if (first)
                    {
                        FirstMealStartTime = StartComboBox[i].Text;
                        first = false;
                    }

                    MarkerWriter.WriteLine(MealNames[i].Text + "\t" + StartComboBox[i].Text + "\t" + EndComboBox[i].Text + "\t" + Locations[i].Text + "\t" + (Seconds[i].Checked?"Seconds":"NoSeconds") + "\t" + (Company[i].Checked?"InCompany":"Alone") + "\t" + (CEating[i].Checked?"CompanyEating":"CompanyNotEating") + "\t" + Descriptions[i].Text + " | " + Activities[i].Text);
                    LastMealEndTime = EndComboBox[i].Text;
                }
            }
            
            MarkerWriter.WriteLine(EndTimeString +"\n");
            
            MarkerWriter.Close();

            try
            {
                /* Calculate time before first meal and after last meal */
                StartTime = DateTime.Today + StartTime.TimeOfDay;
                EndTime = DateTime.Today + EndTime.TimeOfDay;
                TimeSpan BeforeFirstMeal = Convert.ToDateTime(FirstMealStartTime) - StartTime;
                TimeSpan AfterLastMeal = EndTime - Convert.ToDateTime(LastMealEndTime);
                lbStatus.Text = "Events file updated. Check in ShimmerView.";
                lbStartTimeWarning.Text = "Time before first meal: " + Math.Floor(BeforeFirstMeal.TotalMinutes).ToString() + " minutes.";
                lbEndTimeWarning.Text = "Time after last meal: " + Math.Floor(AfterLastMeal.TotalMinutes).ToString() + " minutes.";
                if (AfterLastMeal.TotalMinutes < 8) lbEndTimeWarning.ForeColor = Color.Red;
                if (BeforeFirstMeal.TotalMinutes < 8) lbStartTimeWarning.ForeColor = Color.Red;
                lbStatus.ForeColor = Color.Green;
            }
            catch {

                lbStatus.Text = "An error has occured. Please check if Event times are selected correctly.";
                lbStatus.ForeColor = Color.Red;
            }

            
        }

        private void btnReset_Click(object sender, EventArgs e)
        {
            InitItems();
            this.Invoke((MethodInvoker)delegate
                {
                FrmMainForm.ActiveForm.Width = 381;
                }
            );
            lbStatus.Text = "";
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            this.Size = new System.Drawing.Size(381, 464);
            FormObject = this;
        }

        private void button1_Click_1(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start(new DirectoryInfo(InputFileName).Parent.Parent.FullName);
        }

        private void lbSTText_Click(object sender, EventArgs e)
        {

        }

        void MadgwickAHRSupdate(double gx, double gy, double gz, double ax, double ay, double az, double mx, double my, double mz)
        {
            double recipNorm;
            double s0, s1, s2, s3;
            double qDot1, qDot2, qDot3, qDot4;
            double hx, hy, _8bx, _8bz;
            double _2q0mx, _2q0my, _2q0mz, _2q1mx, _2bx, _2bz, _4bx, _4bz, _2q0, _2q1, _2q2, _2q3, _2q0q2, _2q2q3, q0q0, q0q1, q0q2, q0q3, q1q1, q1q2, q1q3, q2q2, q2q3, q3q3;

            /* Data in Shimmer is degrees / sec. Convert to Rad/sec for the algorithm */
            gx = gx * Math.PI / 180;
            gy = gy * Math.PI / 180;
            gz = gz * Math.PI / 180;

            // Use IMU algorithm if magnetometer measurement invalid (avoids NaN in magnetometer normalisation)
            if ((mx == 0.0f) && (my == 0.0f) && (mz == 0.0f))
            {
                MadgwickAHRSupdateIMU(gx, gy, gz, ax, ay, az);
                return;
            }

            // Rate of change of quaternion from gyroscope
            qDot1 = 0.5f * (-q1 * gx - q2 * gy - q3 * gz);
            qDot2 = 0.5f * (q0 * gx + q2 * gz - q3 * gy);
            qDot3 = 0.5f * (q0 * gy - q1 * gz + q3 * gx);
            qDot4 = 0.5f * (q0 * gz + q1 * gy - q2 * gx);

            // Compute feedback only if accelerometer measurement valid (avoids NaN in accelerometer normalisation)
            if (!((ax == 0.0f) && (ay == 0.0f) && (az == 0.0f)))
            {

                // Normalise accelerometer measurement
                recipNorm = 1 / Math.Sqrt(ax * ax + ay * ay + az * az);
                ax *= recipNorm;
                ay *= recipNorm;
                az *= recipNorm;

                // Normalise magnetometer measurement
                recipNorm = 1 / Math.Sqrt(mx * mx + my * my + mz * mz);
                mx *= recipNorm;
                my *= recipNorm;
                mz *= recipNorm;

                // Auxiliary variables to avoid repeated arithmetic
                _2q0mx = 2.0f * q0 * mx;
                _2q0my = 2.0f * q0 * my;
                _2q0mz = 2.0f * q0 * mz;
                _2q1mx = 2.0f * q1 * mx;
                _2q0 = 2.0f * q0;
                _2q1 = 2.0f * q1;
                _2q2 = 2.0f * q2;
                _2q3 = 2.0f * q3;
                _2q0q2 = 2.0f * q0 * q2;
                _2q2q3 = 2.0f * q2 * q3;
                q0q0 = q0 * q0;
                q0q1 = q0 * q1;
                q0q2 = q0 * q2;
                q0q3 = q0 * q3;
                q1q1 = q1 * q1;
                q1q2 = q1 * q2;
                q1q3 = q1 * q3;
                q2q2 = q2 * q2;
                q2q3 = q2 * q3;
                q3q3 = q3 * q3;

                // Reference direction of Earth's magnetic field
                hx = mx * q0q0 - _2q0my * q3 + _2q0mz * q2 + mx * q1q1 + _2q1 * my * q2 + _2q1 * mz * q3 - mx * q2q2 - mx * q3q3;
                hy = _2q0mx * q3 + my * q0q0 - _2q0mz * q1 + _2q1mx * q2 - my * q1q1 + my * q2q2 + _2q2 * mz * q3 - my * q3q3;
                _2bx = Math.Sqrt(hx * hx + hy * hy);
                _2bz = -_2q0mx * q2 + _2q0my * q1 + mz * q0q0 + _2q1mx * q3 - mz * q1q1 + _2q2 * my * q3 - mz * q2q2 + mz * q3q3;
                _4bx = 2.0f * _2bx;
                _4bz = 2.0f * _2bz;
                _8bx = 2.0f * _4bx;
                _8bz = 2.0f * _4bz;
                // Gradient decent algorithm corrective step
                /*
                s0 = -_2q2 * (2.0f * (q1q3 - q0q2) - ax) + _2q1 * (2.0f * (q0q1 + q2q3) - ay) + -_4bz * q2 * (_4bx * (0.5 - q2q2 - q3q3) + _4bz * (q1q3 - q0q2) - mx) + (-_4bx * q3 + _4bz * q1) * (_4bx * (q1q2 - q0q3) + _4bz * (q0q1 + q2q3) - my) + _4bx * q2 * (_4bx * (q0q2 + q1q3) + _4bz * (0.5 - q1q1 - q2q2) - mz);
                s1 = _2q3 * (2.0f * (q1q3 - q0q2) - ax) + _2q0 * (2.0f * (q0q1 + q2q3) - ay) + -4.0f * q1 * (2.0f * (0.5 - q1q1 - q2q2) - az) + _4bz * q3 * (_4bx * (0.5 - q2q2 - q3q3) + _4bz * (q1q3 - q0q2) - mx) + (_4bx * q2 + _4bz * q0) * (_4bx * (q1q2 - q0q3) + _4bz * (q0q1 + q2q3) - my) + (_4bx * q3 - _8bz * q1) * (_4bx * (q0q2 + q1q3) + _4bz * (0.5 - q1q1 - q2q2) - mz);
                s2 = -_2q0 * (2.0f * (q1q3 - q0q2) - ax) + _2q3 * (2.0f * (q0q1 + q2q3) - ay) + (-4.0f * q2) * (2.0f * (0.5 - q1q1 - q2q2) - az) + (-_8bx * q2 - _4bz * q0) * (_4bx * (0.5 - q2q2 - q3q3) + _4bz * (q1q3 - q0q2) - mx) + (_4bx * q1 + _4bz * q3) * (_4bx * (q1q2 - q0q3) + _4bz * (q0q1 + q2q3) - my) + (_4bx * q0 - _8bz * q2) * (_4bx * (q0q2 + q1q3) + _4bz * (0.5 - q1q1 - q2q2) - mz);
                s3 = _2q1 * (2.0f * (q1q3 - q0q2) - ax) + _2q2 * (2.0f * (q0q1 + q2q3) - ay) + (-_8bx * q3 + _4bz * q1) * (_4bx * (0.5 - q2q2 - q3q3) + _4bz * (q1q3 - q0q2) - mx) + (-_4bx * q0 + _4bz * q2) * (_4bx * (q1q2 - q0q3) + _4bz * (q0q1 + q2q3) - my) + (_4bx * q1) * (_4bx * (q0q2 + q1q3) + _4bz * (0.5 - q1q1 - q2q2) - mz);
                */
                s0 = -_2q2 * (2.0f * q1q3 - _2q0q2 - ax) + _2q1 * (2.0f * q0q1 + _2q2q3 - ay) - _2bz * q2 * (_2bx * (0.5f - q2q2 - q3q3) + _2bz * (q1q3 - q0q2) - mx) + (-_2bx * q3 + _2bz * q1) * (_2bx * (q1q2 - q0q3) + _2bz * (q0q1 + q2q3) - my) + _2bx * q2 * (_2bx * (q0q2 + q1q3) + _2bz * (0.5f - q1q1 - q2q2) - mz);
                s1 = _2q3 * (2.0f * q1q3 - _2q0q2 - ax) + _2q0 * (2.0f * q0q1 + _2q2q3 - ay) - 4.0f * q1 * (1 - 2.0f * q1q1 - 2.0f * q2q2 - az) + _2bz * q3 * (_2bx * (0.5f - q2q2 - q3q3) + _2bz * (q1q3 - q0q2) - mx) + (_2bx * q2 + _2bz * q0) * (_2bx * (q1q2 - q0q3) + _2bz * (q0q1 + q2q3) - my) + (_2bx * q3 - _4bz * q1) * (_2bx * (q0q2 + q1q3) + _2bz * (0.5f - q1q1 - q2q2) - mz);
                s2 = -_2q0 * (2.0f * q1q3 - _2q0q2 - ax) + _2q3 * (2.0f * q0q1 + _2q2q3 - ay) - 4.0f * q2 * (1 - 2.0f * q1q1 - 2.0f * q2q2 - az) + (-_4bx * q2 - _2bz * q0) * (_2bx * (0.5f - q2q2 - q3q3) + _2bz * (q1q3 - q0q2) - mx) + (_2bx * q1 + _2bz * q3) * (_2bx * (q1q2 - q0q3) + _2bz * (q0q1 + q2q3) - my) + (_2bx * q0 - _4bz * q2) * (_2bx * (q0q2 + q1q3) + _2bz * (0.5f - q1q1 - q2q2) - mz);
                s3 = _2q1 * (2.0f * q1q3 - _2q0q2 - ax) + _2q2 * (2.0f * q0q1 + _2q2q3 - ay) + (-_4bx * q3 + _2bz * q1) * (_2bx * (0.5f - q2q2 - q3q3) + _2bz * (q1q3 - q0q2) - mx) + (-_2bx * q0 + _2bz * q2) * (_2bx * (q1q2 - q0q3) + _2bz * (q0q1 + q2q3) - my) + _2bx * q1 * (_2bx * (q0q2 + q1q3) + _2bz * (0.5f - q1q1 - q2q2) - mz);
                

                recipNorm = 1 / Math.Sqrt(s0 * s0 + s1 * s1 + s2 * s2 + s3 * s3); // normalise step magnitude
                s0 *= recipNorm;
                s1 *= recipNorm;
                s2 *= recipNorm;
                s3 *= recipNorm;

                // Apply feedback step
                qDot1 -= beta * s0;
                qDot2 -= beta * s1;
                qDot3 -= beta * s2;
                qDot4 -= beta * s3;
            }

            // Integrate rate of change of quaternion to yield quaternion
            q0 += qDot1 * (1.0f / sampleFreq);
            q1 += qDot2 * (1.0f / sampleFreq);
            q2 += qDot3 * (1.0f / sampleFreq);
            q3 += qDot4 * (1.0f / sampleFreq);

            // Normalise quaternion
            recipNorm = 1 / Math.Sqrt(q0 * q0 + q1 * q1 + q2 * q2 + q3 * q3);
            q0 *= recipNorm;
            q1 *= recipNorm;
            q2 *= recipNorm;
            q3 *= recipNorm;
        }

        //---------------------------------------------------------------------------------------------------
        // IMU algorithm update

        void MadgwickAHRSupdateIMU(double gx, double gy, double gz, double ax, double ay, double az)
        {
            double recipNorm;
            double s0, s1, s2, s3;
            double qDot1, qDot2, qDot3, qDot4;
            double _2q0, _2q1, _2q2, _2q3, _4q0, _4q1, _4q2, _8q1, _8q2, q0q0, q1q1, q2q2, q3q3;

            // Rate of change of quaternion from gyroscope
            qDot1 = 0.5f * (-q1 * gx - q2 * gy - q3 * gz);
            qDot2 = 0.5f * (q0 * gx + q2 * gz - q3 * gy);
            qDot3 = 0.5f * (q0 * gy - q1 * gz + q3 * gx);
            qDot4 = 0.5f * (q0 * gz + q1 * gy - q2 * gx);

            // Compute feedback only if accelerometer measurement valid (avoids NaN in accelerometer normalisation)
            if (!((ax == 0.0f) && (ay == 0.0f) && (az == 0.0f)))
            {

                // Normalise accelerometer measurement
                recipNorm = 1 / Math.Sqrt(ax * ax + ay * ay + az * az);
                ax *= recipNorm;
                ay *= recipNorm;
                az *= recipNorm;

                // Auxiliary variables to avoid repeated arithmetic
                _2q0 = 2.0f * q0;
                _2q1 = 2.0f * q1;
                _2q2 = 2.0f * q2;
                _2q3 = 2.0f * q3;
                _4q0 = 4.0f * q0;
                _4q1 = 4.0f * q1;
                _4q2 = 4.0f * q2;
                _8q1 = 8.0f * q1;
                _8q2 = 8.0f * q2;
                q0q0 = q0 * q0;
                q1q1 = q1 * q1;
                q2q2 = q2 * q2;
                q3q3 = q3 * q3;

                // Gradient decent algorithm corrective step
                s0 = _4q0 * q2q2 + _2q2 * ax + _4q0 * q1q1 - _2q1 * ay;
                s1 = _4q1 * q3q3 - _2q3 * ax + 4.0f * q0q0 * q1 - _2q0 * ay - _4q1 + _8q1 * q1q1 + _8q1 * q2q2 + _4q1 * az;
                s2 = 4.0f * q0q0 * q2 + _2q0 * ax + _4q2 * q3q3 - _2q3 * ay - _4q2 + _8q2 * q1q1 + _8q2 * q2q2 + _4q2 * az;
                s3 = 4.0f * q1q1 * q3 - _2q1 * ax + 4.0f * q2q2 * q3 - _2q2 * ay;
                recipNorm = 1 / Math.Sqrt(s0 * s0 + s1 * s1 + s2 * s2 + s3 * s3); // normalise step magnitude
                s0 *= recipNorm;
                s1 *= recipNorm;
                s2 *= recipNorm;
                s3 *= recipNorm;

                // Apply feedback step
                qDot1 -= beta * s0;
                qDot2 -= beta * s1;
                qDot3 -= beta * s2;
                qDot4 -= beta * s3;
            }

            // Integrate rate of change of quaternion to yield quaternion
            q0 += qDot1 * (1.0f / sampleFreq);
            q1 += qDot2 * (1.0f / sampleFreq);
            q2 += qDot3 * (1.0f / sampleFreq);
            q3 += qDot4 * (1.0f / sampleFreq);

            // Normalise quaternion
            recipNorm = 1 / Math.Sqrt((q0 * q0 + q1 * q1 + q2 * q2 + q3 * q3));
            q0 *= recipNorm;
            q1 *= recipNorm;
            q2 *= recipNorm;
            q3 *= recipNorm;
        }



    }
}

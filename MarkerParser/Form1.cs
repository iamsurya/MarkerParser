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
    public partial class Form1 : Form
    {
        String InputFileName, OutputFileName, MarkerFileName, InterviewFileName;

        ManualResetEvent runThread = new ManualResetEvent(false);
        Thread t;
        UInt64 BytesPerLine = 306;  // Average number of bytes each line in the original file has
        UInt64 TotalLines = 0;
        UInt64 CurrentLine = 0;
        double Percentage = 0;
        UInt16 UPercentage = 0;
        UInt64 ExpSamples = 0;
        UInt64 LinesWritten = 0;
        

        double ax = 0, ay = 0, az = 0, Gx = 0, Gy = 0, Gz = 0, gx = 0, gy = 0, gz = 0, lx = 0, ly = 0, lz = 0;

        double[,] R = new double[,] { { 0, 0, 0 }, { 0, 0, 0 }, { 0, 0, 0} };

        double quatmag = 0, gravmag = 0, accmag = 0;

        bool EndThread = true;

        StreamWriter MarkerWriter;
        BinaryWriter DataWriter;
        StreamReader reader;
        double NumSamples;

        private void WorkerThread()
        {

            while (!EndThread)
            {
                runThread.WaitOne(Timeout.Infinite);

                while (!EndThread)
                {
                    try
                    {

                        bool First = true; // Bool To get the starting time
                        String FileDateTimeString; // String from the file which contains time and date
                        DateTime FileDateTime; // DateTime generated from the File data
                        DateTime StartTime = new DateTime(0), EndTime, CurrentEstimate = new DateTime(0);
                        LinesWritten = 0;
                        Percentage = 0;
                        UPercentage = 0;


                        /* Modify the button so it can't be pressed again */
                        label2.Parent.Invoke((MethodInvoker)delegate {
                            label2.Text = @"Working";
                            label2.ForeColor = Color.YellowGreen;
                            label2.Refresh();
                            button2.Enabled = false;
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
                            label2.Parent.Invoke((MethodInvoker)delegate
                            {
                                button2.Enabled = true;
                                label2.Text = "Data is corrupted. Open CSV file in Excel. Column Q should have date.";
                                label2.ForeColor = Color.Red;
                            });
                            MarkerWriter.Close();
                            DataWriter.Close();
                            EndThread = true;
                            return;

                        }

                        var time = values[16].Split('_');
                        

                        /* Force reading of 3 lines */
                        /* The Quaternion data for the first few lines is bad, so we ignore it */

                        line = reader.ReadLine();
                        line = reader.ReadLine();
                        line = reader.ReadLine();
                        CurrentLine = 6;

                        /* Read actual lines */
                        while (!reader.EndOfStream)
                        {
                            /* Read the line from the file */
                            line = reader.ReadLine();
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

                                label2.Parent.Invoke((MethodInvoker)delegate {
                                    label2.Text = @"Working " + Percentage.ToString() + "%";
                                    label2.ForeColor = Color.YellowGreen;
                                    label2.Refresh();
                                });

                            }

                            UPercentage = (UInt16) Percentage;

                            /* Split the line so it can be parsed */
                            values = line.Split('\t');


                            /* Time / Marker / Missed Samples */

                            time = values[16].Split('_');
                            if (time[0].Length < 4)
                                FileDateTimeString = time[0].Substring(0, 1) + " " + time[1].ToString().Substring(0, 3) + " " + time[2].ToString() + " " + time[3].ToString();
                            else
                                FileDateTimeString = time[0].Substring(0, 2) + " " + time[1].ToString().Substring(0, 3) + " " + time[2].ToString() + " " + time[3].ToString();
                            FileDateTime = Convert.ToDateTime(FileDateTimeString);


                            /* Is this the start of the data ? */
                            /* Write the START Marker Details */
                            if (First == true)
                            {            
                                StartTime = Convert.ToDateTime(FileDateTimeString);
                                MarkerWriter.WriteLine("START\t" + FileDateTime.Date.ToString("u").Substring(0, 10) + "\t" + time[3].ToString().Substring(0, 8));
                                First = false;
                            }
                            

                            /* Is this a Marker (Button Press) ? */
                            if ((Double.Parse(values[2])) > 0)
                            {
                                MarkerWriter.WriteLine("MARKER\t" + time[3].ToString().Substring(0, 8));
                            }

                            
                           
                           


                            double q0 = double.Parse(values[3]);
                            double q1 = double.Parse(values[4]);
                            double q2 = double.Parse(values[5]);
                            double q3 = double.Parse(values[6]);

                            Gx = double.Parse(values[7]);
                            Gy = double.Parse(values[8]);
                            Gz = double.Parse(values[9]);

                            ax = double.Parse(values[10]);
                            ay = double.Parse(values[11]);
                            az = double.Parse(values[12]);

                            double sq_q1 = 2 * q1 * q1;
                            double sq_q2 = 2 * q2 * q2;
                            double sq_q3 = 2 * q3 * q3;
                            double q1_q2 = 2 * q1 * q2;
                            double q3_q0 = 2 * q3 * q0;
                            double q1_q3 = 2 * q1 * q3;
                            double q2_q0 = 2 * q2 * q0;
                            double q2_q3 = 2 * q2 * q3;
                            double q1_q0 = 2 * q1 * q0;

                            R[0,0] = 1 - sq_q2 - sq_q3;
                            R[0,1] = q1_q2 - q3_q0;
                            R[0,2] = q1_q3 + q2_q0;
                            R[1,0] = q1_q2 + q3_q0;
                            R[1,1] = 1 - sq_q1 - sq_q3;
                            R[1,2] = q2_q3 - q1_q0;
                            R[2,0] = q1_q3 - q2_q0;
                            R[2,1] = q2_q3 + q1_q0;
                            R[2,2] = 1 - sq_q1 - sq_q2;

                            /* Seperating gravity */
                            gx =  R[2,0];
                            gy =  R[2,1];
                            gz =  R[2,2];

                            /* Removing gravity from Smoothed Signal */
                            /* Data Port facing inside */
                            /* RawData[0][Total_Data] = -(RawData[0][Total_Data] - gx);
                            RawData[1][Total_Data] = -(RawData[1][Total_Data] - gy);
                            RawData[2][Total_Data] = -(RawData[2][Total_Data] - gz); */

                            /* Data Port Facing Outside */
                            lx = -(ax -  gx);
                            ly = -(ay -  gy);
                            lz = -(az -  gz);

                            /* Move data around for correct axes orientation in Shimmer Vs the iPhone*/
                            // If data port pointing away from hand. Swap X and Y
                            gx = lx;
                            lx = ly;
                            ly = gx;

                            DataWriter.Write((float)lx);
                            DataWriter.Write((float)ly);
                            DataWriter.Write((float)lz);
                            DataWriter.Write((float)Gx);
                            DataWriter.Write((float)Gy);
                            DataWriter.Write((float)Gz);


                            
                        }


                        /* Write the END Marker details */
                        time = values[16].Split('_');
                        if (time[0].Length < 4)
                            FileDateTimeString = time[0].Substring(0, 1) + " " + time[1].ToString().Substring(0,3) + " " + time[2].ToString()+ " " + time[3].ToString();
                        else
                            FileDateTimeString = time[0].Substring(0, 2) + " " + time[1].ToString().Substring(0,3) + " " + time[2].ToString() + " " + time[3].ToString();
                        FileDateTime = Convert.ToDateTime(FileDateTimeString);
                        EndTime = FileDateTime;
                        MarkerWriter.WriteLine("END\t" + FileDateTime.Date.ToString("u").Substring(0, 10) + "\t" + time[3].ToString().Substring(0, 8));

                        /* Close the Writer streams */
                        MarkerWriter.Close();
                        DataWriter.Close();

                        ExpSamples = (UInt64)((EndTime.Subtract(StartTime)).TotalSeconds * 15);

                        label2.Parent.Invoke((MethodInvoker)delegate {
                            label2.Text = "Expected: " + ExpSamples.ToString() + " Written: ";
                            label2.ForeColor = Color.Green;
                            label2.Refresh();
                            button2.Enabled = true;
                        });

                        ProgressBar.Parent.Invoke((MethodInvoker)delegate {
                            ProgressBar.ForeColor = Color.Green;
                            ProgressBar.Value = 100;
                        });

                        MarkerWriter.Close();
                        DataWriter.Close();
                        reader.Close();

                        System.IO.File.Copy(MarkerFileName, InterviewFileName, true);

                        EndThread = true;
                    }

                    catch (Exception ex)
                    {
                        
                        label2.Parent.Invoke((MethodInvoker)delegate {
                            button2.Enabled = true;
                            label2.Text = ex.Message;
                            label2.ForeColor = Color.Red;
                        });
                        MarkerWriter.Close();
                        DataWriter.Close();
                        EndThread = true;
                        return;
                    }
                }
            }
        }


        public Form1()
        {
            InitializeComponent();
            Int32 unixTimestamp = (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
            TBTime.Text = unixTimestamp.ToString();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            InputFileName = TBInputFile.Text;
            ExpSamples = 0;

            if (TBOutputFile.Text.Equals("", StringComparison.Ordinal))
            {
                label2.Parent.Invoke((MethodInvoker)delegate
                {
                    button2.Enabled = true;
                    label2.Text = "Please pick an output File";
                    label2.ForeColor = Color.Red;
                });
            }
            else
            {
                EndThread = false;
                t = new Thread(WorkerThread);
                t.Start();
                runThread.Set();
            }
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
                InputFileName = openFileDialog1.FileName;
                TBInputFile.Text = InputFileName;
                OutputFileName = new DirectoryInfo(InputFileName).Parent.Name;
                TBOutputFile.Text = "";
                TBMarkerFile.Text = "";
                InterviewFileName = "";
                MarkerFileName = "";

            }



        }

        private void button3_Click(object sender, EventArgs e)
        {
            SaveFileDialog SaveFileDialog1 = new SaveFileDialog();

            SaveFileDialog1.Filter = "Shimmer Data Files (.txt)|*.txt|All Files (*.*)|*.*";
            SaveFileDialog1.FilterIndex = 1;
            SaveFileDialog1.FileName = OutputFileName + ".txt";
            

            DialogResult userClickedOK = SaveFileDialog1.ShowDialog();

            // Process input if the user clicked OK.
            if (userClickedOK == DialogResult.OK)
            {
                OutputFileName = SaveFileDialog1.FileName;
                TBOutputFile.Text = OutputFileName;
                MarkerFileName = OutputFileName.Substring(0, OutputFileName.Length - 4) + "-markers.txt";
                InterviewFileName = OutputFileName.Substring(0, OutputFileName.Length - 4) + "-interview.txt";
                TBMarkerFile.Text = MarkerFileName;

                label2.Parent.Invoke((MethodInvoker)delegate
                {
                    label2.Text = @"Ready";
                    label2.ForeColor = Color.Green;
                    label2.Refresh();
                });

                ProgressBar.Value = 0;

            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            Int32 unixTimestamp = (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
            TBTime.Text = unixTimestamp.ToString();
        }


    }
}

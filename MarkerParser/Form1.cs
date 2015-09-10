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
        UInt64 BytesPerLine = 306;
        UInt64 TotalLines = 0;
        UInt64 CurrentLine = 0;
        double Percentage = 0;
        UInt16 UPercentage = 0;

        float ax = 0, ay = 0, az = 0, lx = 0, ly = 0, lz = 0, Gx = 0, Gy = 0, Gz = 0, gx = 0, gy = 0, gz = 0;

        float[,] R = new float[,] { { 0, 0, 0 }, { 0, 0, 0 }, { 0, 0, 0} };

        

        bool EndThread = true;

        StreamWriter MarkerWriter;
        StreamWriter DataWriter;
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

                        bool First = true; // Bool To get the starting time
                        String SomeDate;
                        DateTime SomeDateTime;

                        // Console.WriteLine("Hello!");
                        label2.Parent.Invoke((MethodInvoker)delegate {
                            label2.Text = @"Working";
                            label2.ForeColor = Color.YellowGreen;
                            label2.Refresh();
                            button2.Enabled = false;
                        });
                        
                        //Application.DoEvents();

                        MarkerWriter = new StreamWriter(File.OpenWrite(MarkerFileName));
                        DataWriter = new StreamWriter(File.OpenWrite(OutputFileName));

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
                        var time = values[16].Split('_');
                        CurrentLine = 3;

                        /* Read actual lines */
                        while (!reader.EndOfStream)
                        {
                            line = reader.ReadLine();
                            CurrentLine++;
                            Percentage = (CurrentLine * 100 / TotalLines);

                            if ((UInt16)Percentage > UPercentage)
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

                            values = line.Split('\t');

                            float q0 = float.Parse(values[3]);
                            float q1 = float.Parse(values[4]);
                            float q2 = float.Parse(values[5]);
                            float q3 = float.Parse(values[6]);

                            Gx = float.Parse(values[7]);
                            Gy = float.Parse(values[8]);
                            Gz = float.Parse(values[9]);

                            ax = float.Parse(values[10]);
                            ay = float.Parse(values[11]);
                            az = float.Parse(values[12]);



                            float sq_q1 = 2 * q1 * q1;
                            float sq_q2 = 2 * q2 * q2;
                            float sq_q3 = 2 * q3 * q3;
                            float q1_q2 = 2 * q1 * q2;
                            float q3_q0 = 2 * q3 * q0;
                            float q1_q3 = 2 * q1 * q3;
                            float q2_q0 = 2 * q2 * q0;
                            float q2_q3 = 2 * q2 * q3;
                            float q1_q0 = 2 * q1 * q0;

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
                            //gz = (q0*q0) - (q1*q1) - (q2*q2) + (q3*q3);

                            /* Removing gravity from Smoothed Signal */
                            /* Data Port facing inside */
                            /* RawData[0][Total_Data] = -(RawData[0][Total_Data] - gx);
                            RawData[1][Total_Data] = -(RawData[1][Total_Data] - gy);
                            RawData[2][Total_Data] = -(RawData[2][Total_Data] - gz); */

                            /* Data Port Facing Outside */
                            ax = -(ax -  gx);
                            ay = -(ay -  gy);
                            az = -(az -  gz);



                            /* Move data around for correct axes orientation in Shimmer Vs the iPhone*/
                            // If data port pointing away from hand. Swap X and Y
                            gx = ax;
                            ax = ay;
                            ay = gx;



                            DataWriter.WriteLine(ax + "\t" + ay + "\t" + az + "\t" + Gx + "\t" + Gy + "\t" + Gz);


                            //ay = ay;

                            /* Is this the start of the data ? */
                            /* Write the START Marker Details */
                            if (First == true)
                            {
                                time = values[16].Split('_');
                                if (time[0].Length < 4)
                                    SomeDate = time[0].Substring(0, 1) + " " + time[1].ToString().Substring(0,3) + " " + time[2].ToString();
                                else
                                    SomeDate = time[0].Substring(0, 2) + " " + time[1].ToString().Substring(0,3) + " " + time[2].ToString();
                                SomeDateTime = Convert.ToDateTime(SomeDate);
                                MarkerWriter.WriteLine("START\t" + SomeDateTime.Date.ToString("u").Substring(0, 10) + "\t" + time[3].ToString().Substring(0, 8));

                                First = false;
                            }

                            /* Is this a Marker ? */
                            if ((Double.Parse(values[2])) > 0)
                            {

                                time = values[16].Split('_');
                                //Console.WriteLine(time[3].ToString().Substring(0,8));
                                MarkerWriter.WriteLine("MARKER\t" + time[3].ToString().Substring(0, 8));

                            }


                        }


                        /* Write the END Marker details */
                        time = values[16].Split('_');
                        if (time[0].Length < 4)
                            SomeDate = time[0].Substring(0, 1) + " " + time[1].ToString().Substring(0,3) + " " + time[2].ToString();
                        else
                            SomeDate = time[0].Substring(0, 2) + " " + time[1].ToString().Substring(0,3) + " " + time[2].ToString();
                        SomeDateTime = Convert.ToDateTime(SomeDate);
                        MarkerWriter.WriteLine("END\t" + SomeDateTime.Date.ToString("u").Substring(0, 10) + "\t" + time[3].ToString().Substring(0, 8));

                        /* Close the Writer streams */
                        MarkerWriter.Close();
                        DataWriter.Close();

                        label2.Parent.Invoke((MethodInvoker)delegate {
                            label2.Text = @"Finished";
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
                        //MessageBox.Show(ex.Message, "", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
        }

        private void button2_Click(object sender, EventArgs e)
        {
            InputFileName = TBInputFile.Text;

            EndThread = false;
            t = new Thread(WorkerThread);
            t.Start();
            runThread.Set();    
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

            }
        }
    }
}

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
        String InputFileName, OutputFileName, MarkerFileName;

        ManualResetEvent runThread = new ManualResetEvent(false);
        Thread t;
        UInt64 BytesPerLine = 306;
        UInt64 TotalLines = 0;
        UInt64 CurrentLine = 0;
        double Percentage = 0;
        UInt16 UPercentage = 0;

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

                            DataWriter.WriteLine(values[0] + "\t" + values[3] + "\t" + values[4] + "\t" + values[5] + "\t" + values[6] + "\t" + values[7] + "\t" + values[8] + "\t" + values[9] + "\t" + values[10] + "\t" + values[11] + "\t" + values[12] + "\t" + values[13] + "\t" + values[14] + "\t" + values[15]);

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
                TBMarkerFile.Text = MarkerFileName;

            }
        }
    }
}

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


namespace MarkerParser
{
    public partial class Form1 : Form
    {
        String InputFileName, OutputFileName, MarkerFileName;

        public Form1()
        {
            InitializeComponent();
        }

        private void button2_Click(object sender, EventArgs e)
        {

            bool First = true; // Bool To get the starting time
            String SomeDate;
            DateTime SomeDateTime;

            // Console.WriteLine("Hello!");
            label2.Text = @"Working";
            label2.ForeColor = Color.Red;
            label2.Refresh();
            //Application.DoEvents();

            StreamWriter MarkerWriter = new StreamWriter(File.OpenWrite(MarkerFileName));
            StreamWriter DataWriter = new StreamWriter(File.OpenWrite(OutputFileName));

            var reader = new StreamReader(File.OpenRead(InputFileName));

            /* Read first line with seperator information */
            var line = reader.ReadLine();
            var values = line.Split('\t');
            

            /* Read first line with column titles */
            line = reader.ReadLine();

            /* Read first line with column units */
            line = reader.ReadLine();
            values = line.Split('\t');
            var time = values[16].Split('_');


            /* Read actual lines */
            while (!reader.EndOfStream)
            {
                line = reader.ReadLine();

                values = line.Split('\t');

                DataWriter.WriteLine(values[0] + "\t" + values[3] + "\t" + values[4] + "\t" + values[5] + "\t" + values[6] + "\t" + values[7] + "\t" + values[8] + "\t" + values[9] + "\t" + values[10] + "\t" + values[11] + "\t" + values[12] + "\t" + values[13] + "\t" + values[14] + "\t" + values[15]);
                
                /* Is this the start of the data ? */
                /* Write the START Marker Details */
                if(First == true)
                {
                    time = values[16].Split('_');
                    if(time[0].Length < 4)
                        SomeDate = time[0].Substring(0,1) + " " + time[1].ToString() + " " + time[2].ToString();
                    else
                        SomeDate = time[0].Substring(0, 2) + " " + time[1].ToString() + " " + time[2].ToString();
                    SomeDateTime = Convert.ToDateTime(SomeDate);
                    MarkerWriter.WriteLine("START\t" + SomeDateTime.Date.ToString("u").Substring(0,10) + "\t" + time[3].ToString().Substring(0, 8));

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
                SomeDate = time[0].Substring(0, 1) + " " + time[1].ToString() + " " + time[2].ToString();
            else
                SomeDate = time[0].Substring(0, 2) + " " + time[1].ToString() + " " + time[2].ToString();
            SomeDateTime = Convert.ToDateTime(SomeDate);
            MarkerWriter.WriteLine("END\t" + SomeDateTime.Date.ToString("u").Substring(0, 10) + "\t" + time[3].ToString().Substring(0, 8));

            /* Close the Writer streams */
            MarkerWriter.Close();
            DataWriter.Close();
            label2.Text = @"Finished";
            label2.ForeColor = Color.Green;
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

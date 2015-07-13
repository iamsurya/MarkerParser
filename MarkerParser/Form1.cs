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
        public Form1()
        {
            InitializeComponent();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            // Console.WriteLine("Hello!");
            label2.Text = @"Started";
            label2.Refresh();
            //Application.DoEvents();
            


            String MarkerOutputFName = @"C:\temp\data1-markers.txt";
            String DataOutputFName = @"C:\temp\data1.txt";
            // String Fname = @"C:\temp\data1-markers.txt";

            var MarkerWriter = new StreamWriter(File.OpenWrite(MarkerOutputFName));
            var DataWriter = new StreamWriter(File.OpenWrite(DataOutputFName));

            var reader = new StreamReader(File.OpenRead(@"C:\temp\data1.csv"));

            /* Read first line with seperator information */
            var line = reader.ReadLine();
            var values = line.Split('\t');
            

            /* Read first line with column titles */
            line = reader.ReadLine();

            /* Read first line with column units */
            line = reader.ReadLine();

            /* Read actual line */
            line = reader.ReadLine();
            values = line.Split('\t');
            var time = values[16].Split('_');
            
            while (!reader.EndOfStream)
            {

                values = line.Split('\t');

                DataWriter.WriteLine(values[0] + "\t" + values[3] + "\t" + values[4] + "\t" + values[5] + "\t" + values[6] + "\t" + values[7] + "\t" + values[8] + "\t" + values[9] + "\t" + values[10] + "\t" + values[11] + "\t" + values[12] + "\t" + values[13] + "\t" + values[14] + "\t" + values[15]);
                if ((Double.Parse(values[2])) > 0)
                {
                    
                    time = values[16].Split('_');
                    //Console.WriteLine(time[3].ToString().Substring(0,8));
                    MarkerWriter.WriteLine("MARKER\t" + time[3].ToString().Substring(0, 8));

                }
                line = reader.ReadLine();

            }

            MarkerWriter.Close();
            DataWriter.Close();
            label2.Text = @"Finished";
        }
    }
}

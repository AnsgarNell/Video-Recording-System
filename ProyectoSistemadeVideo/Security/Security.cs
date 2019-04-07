using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;

namespace Security
{
    public partial class Security : Form
    {
        public Security()
        {
            InitializeComponent();
        }

        private void btnGenerate_Click(object sender, EventArgs e)
        {
            string value = FingerPrint.GenerateMachineIdentification();

            // create a writer and open the file in append mode
            string strCurrentDirectory = System.Environment.CurrentDirectory + "\\";
            string strFile = strCurrentDirectory + "boot.cry";
            TextWriter tw = new StreamWriter(strFile, false);

            // write a line of text to the file
            tw.WriteLine(value);

            // close the stream
            tw.Close();
            MessageBox.Show(value);
        }
    }
}

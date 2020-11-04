using System;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PuppetMaster {
    public partial class PuppetMasterForm : Form {

        private PuppetMasterParser parser;
        private PuppetMaster puppetMaster;
        public PuppetMasterForm() {
            InitializeComponent();
            puppetMaster = new PuppetMaster();
            parser = new PuppetMasterParser(puppetMaster);
        }

        private void btnOpenScript_Click(object sender, EventArgs e) {

            if (openScriptDialog.ShowDialog() == DialogResult.OK) {
                try {
                    string path = openScriptDialog.FileName;
                    StreamReader file = new StreamReader(path);
                    StreamReader fileConfig = new StreamReader(path);
                    string fileString = fileConfig.ReadToEnd();
                    parser.generateConfig(fileString);
                    fileConfig.Close();
                    string line;
                    while ((line = file.ReadLine()) != null) {
                        parser.parse(line);
                    }
                    file.Close();

                } catch (Exception ex) {
                    MessageBox.Show("Error when opening file. " + ex.StackTrace);
                }
            }
        }

        private void btnKill_Click(object sender, EventArgs e)
        {
            puppetMaster.Crash(txtKill.Text);
        }

        private void btnStatus_Click(object sender, EventArgs e)
        {
            puppetMaster.Status();
        }

        private void btnFreeze_Click(object sender, EventArgs e)
        {
            puppetMaster.Freeze(txtFreeze.Text);
        }

        private void btnUnfreeze_Click(object sender, EventArgs e)
        {
            puppetMaster.Unfreeze(txtUnfreeze.Text);
        }
    }
}

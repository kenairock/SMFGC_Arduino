using MySql.Data.MySqlClient;
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
using System.Xml;

namespace SMFGC {
    public partial class dbSettings : Form {
        public dbSettings() {
            InitializeComponent();
            this.Text += " - " + pVariables.Project_Name;
        }

        private void dbSettings_Load(object sender, EventArgs e) {
            string config_file = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.xml");
            if (File.Exists(config_file)) {
                XmlDocument doc = new XmlDocument();
                doc.Load(config_file);
                XmlNode node = doc.DocumentElement.SelectSingleNode("/configuration/databaseSettings");

                txtHost.Text = node.ChildNodes[0].InnerText;
                txtPort.Text = node.ChildNodes[1].InnerText;
                txtDB.Text = node.ChildNodes[2].InnerText;
                txtUser.Text = node.ChildNodes[3].InnerText;
                txtPass.Text = node.ChildNodes[4].InnerText;

                // pVariables.sConn = string.Format(pVariables.sConn, node.ChildNodes[0].InnerText;,
                //node.ChildNodes[1].InnerText, node.ChildNodes[2].InnerText, node.ChildNodes[3].InnerText, node.ChildNodes[4].InnerText);
            }
            else {
                MessageBox.Show("Configuration not found!", this.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnCancel_Click(object sender, EventArgs e) {
            this.Close();
        }

        private void btnSave_Click(object sender, EventArgs e) {
            MySqlConnection conn = null;
            string connstr = string.Format(pVariables.sConn, txtHost.Text, txtPort.Text, txtDB.Text, txtUser.Text, txtPass.Text);

            if (btnSave.Text == "TEST") {
                try {
                    conn = new MySqlConnection(connstr);
                    conn.Open();
                    if (conn.State == ConnectionState.Open) {
                        MessageBox.Show("Connection sucessfull! \nMySQL Version : " + conn.ServerVersion, this.Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
                        btnSave.Text = "SAVE";
                    }
                    conn.Close();
                }
                catch (Exception ex) {
                    MessageBox.Show("Database Error: " + ex.Message, this.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                finally {
                    if (conn != null && conn.State == ConnectionState.Open) conn.Close();
                }
            }
            else if (btnSave.Text == "SAVE") {

                string config_file = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.xml");
                if (File.Exists(config_file)) {

                    pVariables.sConn = connstr;

                    XmlDocument doc = new XmlDocument();
                    doc.Load(config_file);
                    XmlNode node = doc.DocumentElement.SelectSingleNode("/configuration/databaseSettings");
                    node.ChildNodes[0].InnerText = txtHost.Text;
                    node.ChildNodes[1].InnerText = txtPort.Text;
                    node.ChildNodes[2].InnerText = txtDB.Text;
                    node.ChildNodes[3].InnerText = txtUser.Text;
                    node.ChildNodes[4].InnerText = txtPass.Text;
                    doc.Save(config_file);

                    this.DialogResult = DialogResult.OK;
                    this.Close();
                    // pVariables.sConn = string.Format(pVariables.sConn, node.ChildNodes[0].InnerText;,
                    //node.ChildNodes[1].InnerText, node.ChildNodes[2].InnerText, node.ChildNodes[3].InnerText, node.ChildNodes[4].InnerText);
                }
                else {
                    MessageBox.Show("Configuration not found!", this.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
                }

            }
        }

        private void txtHost_TextChanged(object sender, EventArgs e) {
            btnSave.Text = "TEST";
        }

        private void txtUser_TextChanged(object sender, EventArgs e) {
            btnSave.Text = "TEST";
        }

        private void txtPass_TextChanged(object sender, EventArgs e) {
            btnSave.Text = "TEST";
        }

        private void txtDB_TextChanged(object sender, EventArgs e) {
            btnSave.Text = "TEST";
        }

        private void txtPort_TextChanged(object sender, EventArgs e) {
            btnSave.Text = "TEST";
        }
    }
}

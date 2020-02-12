using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SMFGC {
    public partial class Main : Form {

        mServer server = new mServer();
        pingClient clientPinger = new pingClient();

        bool confirmExit = true;

        public Main() {
            InitializeComponent();
            if (pVariables.AdminMode) this.Text = "Administrator - " + pVariables.Project_Name;
            if (pVariables.DeptMode) this.Text = "Dashboard - " + pVariables.Project_Name;
        }

        private void Main_Load(object sender, EventArgs e) {
            try {
                server.startServer();
                clientPinger.startPing();
            }
            catch (Exception ex) {
                confirmExit = false;
                MessageBox.Show(ex.Message, this.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
                Environment.Exit(0);
            }
        }

        private void Main_FormClosing(object sender, FormClosingEventArgs e) {
            if (confirmExit && MessageBox.Show("Do you really want to Exit?", this.Text, MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.No) {
                e.Cancel = true;
            }
        }

        private void Main_FormClosed(object sender, FormClosedEventArgs e) {
            Environment.Exit(0);
        }

    }
}

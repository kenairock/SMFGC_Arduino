using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SMFGC {
    public partial class dbSettings : Form {
        public dbSettings() {
            InitializeComponent();
            this.Text += " - " + pVariables.Project_Name;
        }

        private void dbSettings_Load(object sender, EventArgs e) {

        }

        private void btnCancel_Click(object sender, EventArgs e) {
            this.Close();
        }
    }
}

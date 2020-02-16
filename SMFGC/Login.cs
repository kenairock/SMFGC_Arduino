using System;
using System.Data;
using System.Windows.Forms;
using MySql.Data.MySqlClient;

namespace SMFGC {
    public partial class Login : Form {
        readonly MySqlConnection conn = new MySqlConnection(pVariables.sConn);
        MySqlCommand cmd;
        MySqlDataReader reader;

        Main frm_main = new Main();
        public Login() {
            InitializeComponent();
            this.Text += " - " + pVariables.Project_Name;
        }

        private void tmrClock_Tick(object sender, EventArgs e) {
            txtTime.Text = DateTime.Now.ToString("ddd, dd MMMM yyyy - hh:mm tt");
        }

        private void btnLogin_Click(object sender, EventArgs e) {
            try {
                conn.Open();
                cmd = conn.CreateCommand();
                cmd.CommandText = pVariables.qLogin;
                cmd.Parameters.Add("@user", MySqlDbType.VarChar).Value = txt_username.Text.Trim();
                cmd.Parameters.Add("@pass", MySqlDbType.VarChar).Value = txt_password.Text.Trim();
                reader = cmd.ExecuteReader();
                if (reader.Read()) {
                    switch (reader["role"].ToString()) {
                        case "admin": {
                                pVariables.AdminMode = true;
                                pVariables.DeptMode = false;
                                pVariables.AcctMode = false;
                                break;
                            }
                        case "depthead": {
                                pVariables.AdminMode = false;
                                pVariables.DeptMode = true;
                                pVariables.AcctMode = false;

                                pVariables.DeptID = Convert.ToInt32(reader["dept_id"]);
                                break;
                            }
                        case "acct": {
                                pVariables.AdminMode = false;
                                pVariables.DeptMode = false;
                                pVariables.AcctMode = true;
                                break;
                            }
                        default: {
                                MessageBox.Show("You don't have permission!", this.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
                                break;
                            }
                    }

                    if (pVariables.AdminMode || pVariables.DeptMode || pVariables.AcctMode) {
                        pVariables.confirmExit = true;
                        txt_username.Text = "Username";
                        txt_password.Text = "Password";
                        txt_password.PasswordChar = '\0';
                        this.Hide();
                        frm_main.setMode();
                        frm_main.Show();
                    }

                }
                else {
                    MessageBox.Show("You have entered wrong username and password!", this.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                conn.Close();
            }
            catch (Exception ex) {
                MessageBox.Show(ex.Message);
            }
            finally {
                if (conn.State == ConnectionState.Open) conn.Close();
            }
        }

        private void Login_Load(object sender, EventArgs e) {
            try {
                conn.Open();
                if (conn.State == ConnectionState.Open) {
                    frm_main.Activate();
                    checkConnection.Text = "Connected!";
                    conn.Close();
                }
            }
            catch (Exception ex) {
                MessageBox.Show("Database Error: " + ex.Message);
            }
            finally {
                if (conn.State == ConnectionState.Open) conn.Close();
            }
            txt_username.Text = "francis";
            txt_password.Text = "admin";
            txt_password.PasswordChar = '●';
        }

        private void txt_username_Click(object sender, EventArgs e) {
            if (txt_username.Text == "Username") { txt_username.Clear(); }
        }

        private void txt_password_Click(object sender, EventArgs e) {
            if (txt_password.Text == "Password") {
                txt_password.Clear();
            }
            txt_password.PasswordChar = '●';
        }

        private void Login_FormClosing(object sender, FormClosingEventArgs e) {
            pVariables.confirmExit = false;
            frm_main.Close();
        }

        private void Login_FormClosed(object sender, FormClosedEventArgs e) {
            Environment.Exit(0);
        }
    }
}

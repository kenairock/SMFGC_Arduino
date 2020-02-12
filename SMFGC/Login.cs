using System;
using System.Data;
using System.Windows.Forms;
using MySql.Data.MySqlClient;

namespace SMFGC {
    public partial class Login : Form {
        readonly MySqlConnection conn = new MySqlConnection(pVariables.sConn);
        MySqlCommand cmd;
        MySqlDataReader reader;

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
                cmd.Parameters.Add("@user", MySqlDbType.VarChar).Value = txt_username.Text;
                cmd.Parameters.Add("@pass", MySqlDbType.VarChar).Value = txt_password.Text;
                reader = cmd.ExecuteReader();
                if (reader.Read()) {
                    switch (reader["Role"].ToString()) {
                        case "Admin": {
                                pVariables.AdminMode = true;
                                pVariables.DeptMode = false;
                                break;
                            }
                        case "Dean": {
                                pVariables.AdminMode = false;
                                pVariables.DeptMode = true;
                                break;
                            }
                        default: {
                                MessageBox.Show("You have entered wrong username and password!", this.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
                                break;
                            }
                    }

                    if (pVariables.AdminMode || pVariables.DeptMode) {
                        this.Hide();
                        new Main().Show();
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
                txt_password.PasswordChar = '●';
            }
        }
    }
}

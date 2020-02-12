using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SMFGC {
    public partial class Main : Form {

        readonly MySqlConnection conn = new MySqlConnection(pVariables.sConn);
        MySqlCommand cmd;
        MySqlDataReader reader;

        mServer server = new mServer();
        pingClient clientPinger = new pingClient();

        bool confirmExit = true;
        int room_index = -1;

        public Main() {
            InitializeComponent();
            if (pVariables.AdminMode) this.Text = "Administrator - " + pVariables.Project_Name;
            if (pVariables.DeptMode) {
                this.Text = "Department - " + pVariables.Project_Name;
                toplabel.Text = "College of Arts, Science and Engineering";

                btnFaculty.Hide();
                btnReports.Hide();
            }
            this.tabMain.ItemSize = new System.Drawing.Size(0, 1);
            lvRooms.Items.Clear();
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

        private void tmrClock_Tick(object sender, EventArgs e) {
            lblDate.Text = DateTime.Now.ToString("dddd, dd MMMM yyyy");
            lblTime.Text = DateTime.Now.ToString("hh:mm tt");
        }

        private void btnHome_Click(object sender, EventArgs e) {
            tabMain.SelectedIndex = 0;
            //RefreshClassrooms();
        }

        private void btnFaculty_Click(object sender, EventArgs e) {
            tabMain.SelectedIndex = 1;
            InitRFID();
        }

        private void btnReports_Click(object sender, EventArgs e) {
            tabMain.SelectedIndex = 2;
        }

        private void btnISmall_Click(object sender, EventArgs e) {
            if (btnISmall.ImageIndex == 0) {
                lvRooms.View = View.LargeIcon;
                btnISmall.ImageIndex = 1;
            }
            else {
                lvRooms.View = View.SmallIcon;                
                btnISmall.ImageIndex = 0;
            }
        }

        private void Exit_Click(object sender, EventArgs e) {
            this.Close();
        }

        private void Minimize_Click(object sender, EventArgs e) {
            this.WindowState = FormWindowState.Minimized;
        }

        private void btnClearSearch_Click(object sender, EventArgs e) {
            txtSearch.Clear();
            txtSearch.Focus();
            btnClearSearch.Hide();
        }

        private void RefreshClassrooms() {
            try {
                var listOfItems = new List<ListViewItem>();

                cmd = conn.CreateCommand();
                cmd.CommandText = @"SELECT `room_id`, `classroom`, `ip_add`, `mac_add` , `port`, `uptime`,`status` FROM `classroom_tb`";

                conn.Open();
                reader = cmd.ExecuteReader();
                while (reader.Read()) {
                    ListViewItem lvI = new ListViewItem();

                    lvI.ImageIndex = Convert.ToInt32(reader["status"]);
                    lvI.Text = reader["classroom"].ToString();
                    lvI.SubItems.Add(reader["room_id"].ToString());
                    lvI.SubItems.Add(reader["ip_add"].ToString());
                    lvI.SubItems.Add(reader["mac_add"].ToString());
                    lvI.SubItems.Add(reader["port"].ToString());
                    lvI.SubItems.Add(reader["uptime"].ToString());
                    listOfItems.Add(lvI);
                }
                conn.Close();
                lvRooms.BeginUpdate();
                lvRooms.Items.Clear();
                lvRooms.Items.AddRange(listOfItems.ToArray());
                lvRooms.EndUpdate();

                if (room_index > -1 && lvRooms.Items.Count > 0) {
                    lvRooms.Items[room_index].Selected = true;
                }
                
            }
            catch (Exception e) {
                Console.WriteLine(e.ToString());
            }
            finally {
                if (conn.State == ConnectionState.Open) conn.Close();
            }
        }

        private void clear_RMInfo() {
            lblRmName.Text = "SELECT ROOM";
            txtIP.Clear();
            txtMAC.Clear();
            txtPort.Clear();
            lblStatus.Text = "";
            txtSubjDesc.Clear();
            txtFaculty.Clear();
            txtCourse.Clear();
            txtDay.Clear();
            txtTStart.Clear();
            txtTEnd.Clear();
            lblUpTime.Text = "00H : 00M : 00S";
            txtVolt.Text = "0.0";
            txtCurr.Text = "0.0";
            txtPower.Text = "0.0";
            txtEnergy.Text = "0.0";
            txtFreq.Text = "0.0";
            txtPF.Text = "0.0";
            txtFT.Text = "0.0";
            txtMS.Text = "0.0";
            txtVat.Text = "0.0";
            txtExpense.Text = "0.0";
            lblStatus.Text = "...";
        }

        private void RefreshDatagrid() {
            try {
                conn.Open();
                string query = "SELECT * FROM ";

                if (tabFaculty.SelectedTab.Text == "Information") query += "users_view";
                else if (tabFaculty.SelectedTab.Text == "Schedule") query += "class_sched_view";
                else if (tabFaculty.SelectedTab.Text == "Classroom") query += "classroom_view";
                else if (tabFaculty.SelectedTab.Text == "Subject") query += "subject_view";
                else if (tabFaculty.SelectedTab.Text == "Department") query += "department_view";

                if (txtSearch.Text.Length > 0) query += " WHERE `" + cbSearch.Items[cbSearch.SelectedIndex].ToString() + "` LIKE @search";

                DataTable dt = new DataTable();
                MySqlDataAdapter oda = new MySqlDataAdapter(query, conn);
                oda.SelectCommand.Parameters.AddWithValue("@search", "%" + txtSearch.Text + "%");
                oda.Fill(dt);
                dataGrid.DataSource = dt;
                conn.Close();

                if (tabFaculty.SelectedTab.Text == "Information" || tabFaculty.SelectedTab.Text == "Subject") dataGrid.Columns[2].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                if (tabFaculty.SelectedTab.Text == "Department") dataGrid.Columns[1].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                if (dataGrid.RowCount == 1 && txtSearch.Text != "") {
                    dataGrid.Rows[0].Selected = true;
                    dataGrid_CellClick(dataGrid, new DataGridViewCellEventArgs(dataGrid.CurrentCell.ColumnIndex, dataGrid.CurrentCell.RowIndex));
                }
                else { dataGrid.ClearSelection(); }

                if (tabFaculty.SelectedTab.Text == "Schedule") {
                    FillComboBox(cbSHCourse, "course_view", "course_id", "cs_name");
                    FillComboBox(cbSHSubj, "subject_tb", "subject_id", "code");
                    FillComboBox(cbSHRoom, "classroom_tb", "room_id", "classroom");
                    FillComboBox(cbSHFaculty, "users_view", "id", "fullname");
                }
                else if (tabFaculty.SelectedTab.Text == "Classroom") {
                    FillComboBox(cbRMDept, "department_tb", "dept_id", "dept_name");
                }
                btnDel.Enabled = false;
                btnSave.Enabled = false;
                btnCancel.Enabled = false;
            }
            catch (Exception ex) {
                MessageBox.Show(ex.Message);
            }
            finally {
                if (conn.State == ConnectionState.Open) conn.Close();
            }
        }

        private void dataGrid_CellClick(object sender, DataGridViewCellEventArgs e) {

        }

        private void FillComboBox(ComboBox cb, string table, string id, string col) {
            try {
                cb.DataSource = null;
                cb.Items.Clear();

                conn.Open();
                MySqlDataAdapter oda = new MySqlDataAdapter("SELECT `" + id + "`,`" + col + "` FROM " + table, conn);
                DataTable dt = new DataTable();
                oda.Fill(dt);

                DataRow dr = dt.NewRow();
                dr[0] = 0;
                dr[1] = "";
                dt.Rows.InsertAt(dr, 0);

                cb.DataSource = dt;
                cb.DisplayMember = col;
                cb.ValueMember = id;

                conn.Close();
            }
            catch (Exception ex) {
                MessageBox.Show(ex.Message);
            }
            finally {
                if (conn.State == ConnectionState.Open) conn.Close();
            }
        }

        private void InitRFID() {
            try {
                lblRFIDStatus.Text = "Not Ready!";
                spRFID.PortName = cboPort.Items[cboPort.SelectedIndex].ToString();
                spRFID.Open();
                if (spRFID.IsOpen) {
                    lblRFIDStatus.Text = "Ready!";
                }
                else {
                    spRFID.Close();
                }
                spRFID.DataReceived += new SerialDataReceivedEventHandler(spRFID_DataReceived);
            }
            catch (Exception ex) {
                spRFID.Close();
                Console.WriteLine("Error Opening port: {0}", ex.Message);
            }
        }

        private void btnReloadPort_Click(object sender, EventArgs e) {
            cboPort.DataSource = SerialPort.GetPortNames();
        }

        private void spRFID_DataReceived(object sender, SerialDataReceivedEventArgs e) {

        }

        private void cboPort_Click(object sender, EventArgs e) {
            InitRFID();
        }

        private void btnNew_Click(object sender, EventArgs e) {
            btnDel.Enabled = false;
            btnSave.Enabled = true;
            btnCancel.Enabled = true;
            btnSave.Tag = "new";

            switch (tabFaculty.SelectedTab.Text) {
                case "Information":
                    txtUid.Clear();
                    txtUTag.Clear();
                    cbtitle.SelectedIndex = 0;
                    txtULN.Clear();
                    txtUFN.Clear();
                    txtUMI.Clear();
                    profpic.Image = new Bitmap(Properties.Resources.user);
                    profpic.Tag = null;
                    txtULN.Focus();
                    btnImgBrowse.Enabled = true;
                    btnImgRemove.Enabled = false;
                    break;

                case "Schedule":
                    txtSHid.Clear();
                    cbSHCourse.SelectedIndex = 0;
                    cbSHSubj.SelectedIndex = 0;
                    clearRBDay();
                    cbSHRoom.SelectedIndex = 0;
                    cbSHFaculty.SelectedIndex = 0;
                    break;

                case "Classroom":
                    txtRMid.Clear();
                    txtRMno.Clear();
                    cbRMDept.SelectedIndex = 0;
                    txtRMdev.Clear();
                    txtRMip.Clear();
                    txtRMport.Clear();
                    txtRMmac.Clear();
                    txtRMclass.Clear();
                    ckRelay1.Checked = false;
                    ckRelay2.Checked = false;
                    break;

                case "Subject":
                    txtSubid.Clear();
                    txtSubcode.Clear();
                    txtSubDesc.Clear();
                    break;

                case "Department":
                    txtDeptid.Clear();
                    txtDeptname.Clear();
                    txtDeptflr.Clear();
                    break;
            }
        }

        string getRBDay() {
            foreach (Control c in gbDay.Controls) {
                if (c.GetType() == typeof(RadioButton)) {
                    RadioButton rb = c as RadioButton;
                    if (rb.Checked) return rb.Tag.ToString();
                }
            }
            return null;
        }

        private void setRBDay(string day) {
            foreach (Control c in gbDay.Controls) {
                if (c.GetType() == typeof(RadioButton)) {
                    RadioButton rb = c as RadioButton;
                    if (rb.Tag.ToString() == day) { rb.Checked = true; break; }
                }
            }
        }

        string clearRBDay() {
            foreach (Control c in gbDay.Controls) {
                if (c.GetType() == typeof(RadioButton)) {
                    RadioButton rb = c as RadioButton;
                    rb.Checked = false;
                }
            }
            return null;
        }

        private void btnDel_Click(object sender, EventArgs e) {
            try {
                switch (tabFaculty.SelectedTab.Text) {
                    case "Information":
                        cmd = new MySqlCommand("DELETE FROM users_tb WHERE users_id = " + txtUid.Text, conn);
                        break;
                    case "Schedule":
                        cmd = new MySqlCommand("DELETE FROM class_sched_tb WHERE sched_id = " + txtSHid.Text, conn);
                        break;
                    case "Classroom":
                        cmd = new MySqlCommand("DELETE FROM classroom_tb WHERE room_id = " + txtRMid.Text, conn);
                        break;
                    case "Subject":
                        cmd = new MySqlCommand("DELETE FROM subject_tb WHERE subject_id = " + txtSubid.Text, conn);
                        break;
                    case "Department":
                        cmd = new MySqlCommand("DELETE FROM department_tb WHERE dept_id = " + txtDeptid.Text, conn);
                        break;
                }
                if (MessageBox.Show("Are you sure you want to delete?", " ", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes) {
                    conn.Open();
                    cmd.ExecuteNonQuery();
                    conn.Close();
                    RefreshDatagrid();
                    MessageBox.Show("Record Deleted.");
                    btnNew_Click(sender, e);
                }
            }
            catch (Exception ex) {
                MessageBox.Show(ex.Message);
            }
            finally {
                if (conn.State == ConnectionState.Open) conn.Close();
            }
        }

        private void btnSave_Click(object sender, EventArgs e) {
            try {
                cmd = conn.CreateCommand();
                bool form = true;

                switch (tabFaculty.SelectedTab.Text) {
                    case "Information":
                        byte[] ImageData = null;

                        if (btnSave.Tag.ToString() == "new")
                            cmd.CommandText = @"INSERT INTO users_tb(uid,title,last_name,first_name,m_i,picture) 
                                VALUES(@tag, @title, UC_WORDS(@ln), UC_WORDS(@fn), UC_WORDS(@mi), @pic);";
                        else if (btnSave.Tag.ToString() == "edit")
                            cmd.CommandText = @"UPDATE users_tb SET uid=@tag, title=@title, last_name=UC_WORDS(@ln), 
                                first_name=UC_WORDS(@fn), m_i=UC_WORDS(@mi), picture=@pic WHERE users_id=@id;";
                        if (profpic.Tag != null) {
                            MemoryStream ms = new MemoryStream();
                            profpic.Image.Save(ms, ImageFormat.Jpeg);
                            ImageData = new byte[ms.Length];
                            ms.Position = 0;
                            ms.Read(ImageData, 0, ImageData.Length);
                        }

                        cmd.Parameters.Add("@id", MySqlDbType.Int32).Value = txtUid.Text;
                        cmd.Parameters.Add("@tag", MySqlDbType.VarChar).Value = txtUTag.Text;
                        cmd.Parameters.Add("@title", MySqlDbType.VarChar).Value = cbtitle.Items[cbtitle.SelectedIndex].ToString();
                        cmd.Parameters.Add("@ln", MySqlDbType.VarChar).Value = txtULN.Text;
                        cmd.Parameters.Add("@fn", MySqlDbType.VarChar).Value = txtUFN.Text;
                        cmd.Parameters.Add("@mi", MySqlDbType.VarChar).Value = txtUMI.Text.Substring(0, 1) + ".";
                        cmd.Parameters.Add("@pic", MySqlDbType.Blob).Value = ImageData;
                        break;

                    case "Schedule":
                        cmd.CommandText = @"SELECT `sched_id` FROM `class_sched_tb`
                                WHERE `room_id` = @rmid AND `day` = @day 
                                AND ((`start_time` BETWEEN @stime AND @etime) OR (`end_time` BETWEEN @stime AND @etime)) LIMIT 1;";
                        cmd.Parameters.Add("@rmid", MySqlDbType.Int32).Value = cbSHRoom.SelectedValue;
                        cmd.Parameters.Add("@day", MySqlDbType.VarChar).Value = getRBDay();
                        cmd.Parameters.Add("@stime", MySqlDbType.VarChar).Value = dtTimeStart.Value.ToString("HH:mm");
                        cmd.Parameters.Add("@etime", MySqlDbType.VarChar).Value = dtTimeEnd.Value.ToString("HH:mm");
                        conn.Open();
                        reader = cmd.ExecuteReader();
                        if (reader.Read()) {
                            if (btnSave.Tag.ToString() == "new" || btnSave.Tag.ToString() == "edit" && reader["sched_id"].ToString() != txtSHid.Text) {
                                MessageBox.Show("Data already exist!", "", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                form = false;
                            }
                        }
                        conn.Close();

                        if (form) {
                            cmd = conn.CreateCommand();
                            if (btnSave.Tag.ToString() == "new")
                                cmd.CommandText = @"INSERT INTO `class_sched_tb` (`course_id`,`subject_id`,`day`,`start_time`,`end_time`,`room_id`,`faculty`) 
                                    VALUES(@courseid, @subjectid, @day, @stime, @etime, @rmid, @faculty);";
                            else if (btnSave.Tag.ToString() == "edit")
                                cmd.CommandText = @"UPDATE `class_sched_tb` SET `course_id`=@courseid, `subject_id`=@subjectid, `day`=@day, 
                                    `start_time`=@stime, `end_time`=@etime, `room_id`=@rmid, `faculty`=@faculty WHERE `sched_id`=@id;";

                            cmd.Parameters.Add("@id", MySqlDbType.Int32).Value = txtSHid.Text;
                            cmd.Parameters.Add("@courseid", MySqlDbType.Int32).Value = cbSHCourse.SelectedValue;
                            cmd.Parameters.Add("@subjectid", MySqlDbType.Int32).Value = cbSHSubj.SelectedValue;
                            cmd.Parameters.Add("@day", MySqlDbType.VarChar).Value = getRBDay();
                            cmd.Parameters.Add("@stime", MySqlDbType.VarChar).Value = dtTimeStart.Value.ToString("HH:mm");
                            cmd.Parameters.Add("@etime", MySqlDbType.VarChar).Value = dtTimeEnd.Value.ToString("HH:mm");
                            cmd.Parameters.Add("@rmid", MySqlDbType.Int32).Value = cbSHRoom.SelectedValue;
                            cmd.Parameters.Add("@faculty", MySqlDbType.Int32).Value = cbSHFaculty.SelectedValue;
                        }
                        break;

                    case "Classroom":
                        if (btnSave.Tag.ToString() == "new")
                            cmd.CommandText = @"INSERT INTO `classroom_tb` (`number`, `dept`, `dev_id`, `ip_add`, `port`, `mac_add`, `classroom`, `relay1`, `relay2`)  
                                VALUES(@num, @dept, @dev, @ip, @port, @mac, @class, @relay1, @relay2);";
                        else if (btnSave.Tag.ToString() == "edit")
                            cmd.CommandText = @"UPDATE `classroom_tb` SET `number`=@num, `dept`=@dept, `dev_id`=@dev, `ip_add`=@ip,
                                `port`=@port, `mac_add`=@mac, `classroom`=@class, `relay1`=@relay1, `relay2`=@relay2 WHERE `room_id`=@id;";

                        cmd.Parameters.Add("@id", MySqlDbType.Int32).Value = txtRMid.Text;
                        cmd.Parameters.Add("@num", MySqlDbType.VarChar).Value = txtRMno.Text;
                        cmd.Parameters.Add("@dept", MySqlDbType.Int32).Value = cbRMDept.SelectedValue;
                        cmd.Parameters.Add("@dev", MySqlDbType.Int32).Value = txtRMdev.Text;
                        cmd.Parameters.Add("@ip", MySqlDbType.VarChar).Value = txtRMip.Text;
                        cmd.Parameters.Add("@port", MySqlDbType.VarChar).Value = txtRMport.Text;
                        cmd.Parameters.Add("@mac", MySqlDbType.VarChar).Value = txtRMmac.Text;
                        cmd.Parameters.Add("@class", MySqlDbType.VarChar).Value = txtRMclass.Text;
                        cmd.Parameters.Add("@relay1", MySqlDbType.Int32).Value = (int)ckRelay1.CheckState;
                        cmd.Parameters.Add("@relay2", MySqlDbType.Int32).Value = (int)ckRelay2.CheckState;
                        break;

                    case "Subject":
                        if (btnSave.Tag.ToString() == "new")
                            cmd.CommandText = @"INSERT INTO `subject_tb` (`code`,`descpt`) VALUES(@code, @desc);";
                        else if (btnSave.Tag.ToString() == "edit")
                            cmd.CommandText = @"UPDATE `subject_tb` SET `code`=@code, `descpt`=@desc WHERE `subject_id`=@id;";

                        cmd.Parameters.Add("@id", MySqlDbType.Int32).Value = txtSubid.Text;
                        cmd.Parameters.Add("@code", MySqlDbType.VarChar).Value = txtSubcode.Text;
                        cmd.Parameters.Add("@desc", MySqlDbType.VarChar).Value = txtSubjDesc.Text;
                        break;

                    case "Department":
                        if (btnSave.Tag.ToString() == "new")
                            cmd.CommandText = @"INSERT INTO `department_tb` (`dept_name`,`floor`) VALUES(@name, @floor);";
                        else if (btnSave.Tag.ToString() == "edit")
                            cmd.CommandText = @"UPDATE `department_tb` SET `dept_name`=@name, `floor`=@floor WHERE `dept_id`=@id;";

                        cmd.Parameters.Add("@id", MySqlDbType.Int32).Value = txtSubid.Text;
                        cmd.Parameters.Add("@name", MySqlDbType.VarChar).Value = txtSubcode.Text;
                        cmd.Parameters.Add("@floor", MySqlDbType.VarChar).Value = txtSubjDesc.Text;
                        break;
                }

                if (form && MessageBox.Show("Are you sure you want to save?", " ", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes) {
                    conn.Open();
                    cmd.ExecuteNonQuery();
                    conn.Close();

                    int nRow = 0;

                    if (btnSave.Tag.ToString() == "edit") {
                        nRow = dataGrid.CurrentCell.RowIndex;
                        MessageBox.Show("Data Saved.");
                    }

                    RefreshDatagrid();

                    if (btnSave.Tag.ToString() == "new") {
                        //select newly inserted row
                        nRow = dataGrid.Rows.Count - 1;
                        MessageBox.Show("Data Inserted.");
                    }
                    dataGrid.Rows[nRow].Selected = true;
                    dataGrid_CellClick(dataGrid, new DataGridViewCellEventArgs(0, nRow));
                }
                if (conn.State == ConnectionState.Open) conn.Close();
            }
            catch (Exception ex) {
                MessageBox.Show(ex.Message);
            }
            finally {
                if (conn.State == ConnectionState.Open) conn.Close();
            }
        }

        private void btnCancel_Click(object sender, EventArgs e) {
            btnNew_Click(sender, e);
            btnSave.Enabled = false;
            btnCancel.Enabled = false;
            btnImgBrowse.Enabled = false;
            if (dataGrid.SelectedRows.Count > 0) dataGrid_CellClick(dataGrid, new DataGridViewCellEventArgs(dataGrid.CurrentCell.ColumnIndex, dataGrid.CurrentCell.RowIndex));

        }

        private void tmrRooms_Tick(object sender, EventArgs e) {
            if (tabMain.SelectedIndex == 0) RefreshClassrooms();
        }

        private void lvRooms_SelectedIndexChanged(object sender, EventArgs e) {
            try {
                if (lvRooms.SelectedItems.Count > 0) {
                    room_index = lvRooms.SelectedIndices[0];

                    lblRmName.Text = "ROOM " + lvRooms.Items[room_index].Text;
                    txtIP.Text = lvRooms.Items[room_index].SubItems[2].Text;
                    txtMAC.Text = lvRooms.Items[room_index].SubItems[3].Text;
                    txtPort.Text = lvRooms.Items[room_index].SubItems[4].Text;

                    switch (lvRooms.Items[room_index].ImageIndex) {
                        case 0:
                            lblStatus.Text = "Offline!";
                            lblStatus.ForeColor = System.Drawing.Color.Red;
                            break;
                        case 1:
                            lblStatus.Text = "Disconnected!";
                            lblStatus.ForeColor = System.Drawing.Color.Orange;
                            break;
                        case 2:
                            lblStatus.Text = "Connected.";
                            lblStatus.ForeColor = System.Drawing.Color.Lime;
                            break;
                        case 3:
                            lblStatus.Text = "In-Use.";
                            lblStatus.ForeColor = System.Drawing.Color.LightBlue;
                            break;
                    }

                    cmd = conn.CreateCommand();
                    cmd.CommandText = @"SELECT `day`,
                            TIME_FORMAT(`start_time`, '%h:%i %p') AS `start`,
                            TIME_FORMAT(`end_time`, '%h:%i %p') AS `end`,
                            st.`code`, st.`descpt`, crt.`course_name`, 
                            CONCAT(ut.`title`, ' ', ut.`last_name`, ' ', ut.`first_name`) AS faculty
                        FROM
                            class_sched_tb ct
                            INNER JOIN subject_tb st
                                ON st.`subject_id` = ct.`subject_id` 
                            INNER JOIN course_tb crt
                                ON crt.`course_id` = ct.`course_id` 
                            INNER JOIN users_tb ut
                                ON ut.`users_id` = ct.`faculty`
                        WHERE room_id=@id AND (`start_time` < NOW() AND `end_time` > NOW())";

                    string rm_id = lvRooms.Items[room_index].SubItems[1].Text;
                    cmd.Parameters.AddWithValue("@id", rm_id);

                    conn.Open();
                    reader = cmd.ExecuteReader();
                    if (reader.Read()) {
                        txtSubjDesc.Text = reader["code"].ToString() + " - " + reader["descpt"].ToString();
                        txtFaculty.Text = reader["faculty"].ToString();
                        txtCourse.Text = reader["course_name"].ToString();
                        txtDay.Text = reader["day"].ToString();
                        txtTStart.Text = reader["start"].ToString();
                        txtTEnd.Text = reader["end"].ToString();
                        TimeSpan tdiff = DateTime.Now.Subtract(DateTime.Parse(lvRooms.Items[room_index].SubItems[5].Text));
                        lblUpTime.Text = tdiff.Hours.ToString() + "H : " + tdiff.Minutes.ToString() + "M : " + tdiff.Seconds.ToString() + "S";
                    }
                    conn.Close();
                }
                else {
                    clear_RMInfo();
                    room_index = -1;
                }
            }
            catch (Exception ex) {
                Console.WriteLine(ex.ToString());
            }
            finally {
                if (conn.State == ConnectionState.Open) conn.Close();
            }
        }
    }
}

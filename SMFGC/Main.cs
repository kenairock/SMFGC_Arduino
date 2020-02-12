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

using static SMFGC.Program;

namespace SMFGC {
    public partial class Main : Form {

        readonly MySqlConnection conn = new MySqlConnection(pVariables.sConn);
        MySqlCommand cmd;
        MySqlDataReader reader;

        mServer server = new mServer();
        pingClient clientPinger = new pingClient();

        bool confirmExit = true;
        int room_index = -1;

        string RFIDTag = "";

        public Main() {
            InitializeComponent();
            setMode();
            this.tabMain.ItemSize = new Size(0, 1);
            lvRooms.Items.Clear();
        }

        public void setMode() {
            if (pVariables.AdminMode) this.Text = "Administrator - " + pVariables.Project_Name;
            if (pVariables.DeptMode) {
                this.Text = "Department - " + pVariables.Project_Name;
                toplabel.Text = "College of Arts, Science and Engineering";

                btnFaculty.Hide();
                btnReports.Hide();
            }
            tabMain.SelectedIndex = 0;
        }

        private void Main_Load(object sender, EventArgs e) {
            try {
                server.startServer();
                clientPinger.startPing();

                sysLog(0, "", "system", "Server started.", 64);
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
            else {
                sysLog(0, "", "system", "Server closed.", 64);
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
            RefreshFacultyDatagrid();
            FillSearchCombo();
        }

        private void btnReports_Click(object sender, EventArgs e) {
            tabMain.SelectedIndex = 2;
            RefreshLogDatagrid();
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
                cmd.CommandText = @"SELECT `room_id`, `classroom`, `dev_id`, 
                    `ip_add`, `mac_add` , `port`, `uptime`,`status` FROM `classroom_tb`";

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
                    lvI.SubItems.Add(reader["dev_id"].ToString());
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
                else {
                    lblRmName.Text = "SELECT ROOM";
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
            txtVat.Text = "0.0";
            txtExpense.Text = "0.0";
            lblStatus.Text = "...";
        }

        private void RefreshFacultyDatagrid() {
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
                MessageBox.Show(ex.Message, this.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally {
                if (conn.State == ConnectionState.Open) conn.Close();
            }
        }

        private void dataGrid_CellClick(object sender, DataGridViewCellEventArgs e) {
            try {
                if (tabFaculty.SelectedIndex < 0) return;

                string s_id = dataGrid.Rows[e.RowIndex].Cells["ID"].Value.ToString();
                btnSave.Tag = "edit";
                conn.Open();
                cmd = conn.CreateCommand();

                switch (tabFaculty.SelectedTab.Text) {
                    case "Information":
                        cmd.CommandText = @"SELECT * FROM users_tb WHERE users_id=@id";
                        cmd.Parameters.AddWithValue("@id", s_id);
                        reader = cmd.ExecuteReader();
                        if (reader.Read()) {
                            txtUid.Text = s_id;
                            txtUTag.Text = reader["uid"].ToString();
                            cbtitle.SelectedItem = reader["title"].ToString();
                            txtULN.Text = reader["last_name"].ToString();
                            txtUFN.Text = reader["first_name"].ToString();
                            txtUMI.Text = reader["m_i"].ToString();

                            if (Convert.IsDBNull(reader["picture"])) {
                                profpic.Image = new Bitmap(Properties.Resources.user);
                                profpic.Tag = null;
                                btnImgRemove.Hide();
                            }
                            else {
                                btnImgRemove.Show();
                                profpic.Image = byteArrayToImage((byte[])reader["picture"]);
                            }
                            btnImgBrowse.Enabled = true;
                        }
                        break;

                    case "Schedule":
                        cmd.CommandText = @"SELECT * FROM class_sched_tb WHERE sched_id=@id";
                        cmd.Parameters.AddWithValue("@id", s_id);
                        reader = cmd.ExecuteReader();
                        if (reader.Read()) {
                            txtSHid.Text = s_id;
                            cbSHCourse.SelectedValue = reader["course_id"];
                            cbSHSubj.SelectedValue = reader["subject_id"];
                            setRBDay(reader["day"].ToString());
                            dtTimeStart.Value = DateTime.Parse(reader["start_time"].ToString());
                            dtTimeEnd.Value = DateTime.Parse(reader["end_time"].ToString());
                            cbSHRoom.SelectedValue = reader["room_id"];
                            cbSHFaculty.SelectedValue = reader["faculty"];

                        }
                        break;

                    case "Classroom":
                        cmd.CommandText = @"SELECT * FROM classroom_tb WHERE room_id=@id";
                        cmd.Parameters.AddWithValue("@id", s_id);
                        reader = cmd.ExecuteReader();
                        if (reader.Read()) {
                            txtRMid.Text = s_id;
                            txtRMno.Text = reader["number"].ToString();
                            cbRMDept.SelectedValue = reader["dept"];
                            txtRMdev.Text = reader["dev_id"].ToString();
                            txtRMip.Text = reader["ip_add"].ToString();
                            txtRMport.Text = reader["port"].ToString();
                            txtRMmac.Text = reader["mac_add"].ToString();
                            txtRMclass.Text = reader["classroom"].ToString();
                            ckRelay1.Checked = Convert.ToBoolean((int)reader["relay1"]);
                            ckRelay2.Checked = Convert.ToBoolean((int)reader["relay2"]);
                        }
                        break;

                    case "Subject":
                        cmd.CommandText = @"SELECT * FROM subject_tb WHERE subject_id=@id";
                        cmd.Parameters.AddWithValue("@id", s_id);
                        reader = cmd.ExecuteReader();
                        if (reader.Read()) {
                            txtSubid.Text = s_id;
                            txtSubcode.Text = reader["code"].ToString();
                            txtSubDesc.Text = reader["descpt"].ToString();
                        }
                        break;

                    case "Department":
                        cmd.CommandText = @"SELECT * FROM department_tb WHERE dept_id=@id";
                        cmd.Parameters.AddWithValue("@id", s_id);
                        reader = cmd.ExecuteReader();
                        if (reader.Read()) {
                            txtDeptid.Text = s_id;
                            txtDeptname.Text = reader["dept_name"].ToString();
                            txtDeptflr.Text = reader["floor"].ToString();
                        }
                        break;
                }

                btnDel.Enabled = true;
                btnSave.Enabled = true;
                btnCancel.Enabled = true;
                conn.Close();
            }
            catch (Exception ex) {
                MessageBox.Show(ex.Message, this.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally {
                if (conn.State == ConnectionState.Open) conn.Close();
            }
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
                MessageBox.Show(ex.Message, this.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
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
            if (txtUTag.Text.Length >= 12) {
                spRFID.Close();
            }
            else {
                RFIDTag = spRFID.ReadExisting();
                this.Invoke(new EventHandler(DisplayText));
            }
        }

        private void DisplayText(object sender, EventArgs e) {
            if (btnSave.Tag != null) txtUTag.Text = RFIDTag;
            if (txtSearch.Text == "" && txtSearch.Focused && cbSearch.Items[cbSearch.SelectedIndex].ToString() == "Tag") txtSearch.Text = RFIDTag;
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
                    btnImgRemove.Hide();
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

        private void FillSearchCombo() {
            cbSearch.Items.Clear();
            for (int i = 0; i < dataGrid.Columns.Count; i++) {
                cbSearch.Items.Add(dataGrid.Columns[i].HeaderText);
            }
            cbSearch.SelectedIndex = 0;
        }

        private void btnDel_Click(object sender, EventArgs e) {
            try {
                if (dataGrid.SelectedRows.Count > 0) dataGrid_CellClick(dataGrid, new DataGridViewCellEventArgs(dataGrid.CurrentCell.ColumnIndex, dataGrid.CurrentCell.RowIndex));

                string tmp_res = "ID: ";
                switch (tabFaculty.SelectedTab.Text) {
                    case "Information":
                        cmd = new MySqlCommand("DELETE FROM users_tb WHERE users_id = " + txtUid.Text, conn);
                        tmp_res += txtUid.Text;
                        tmp_res += ", Name: " + cbtitle.SelectedItem + " " + txtULN.Text + ", " + txtUFN.Text + " " + txtUMI.Text;
                        break;

                    case "Schedule":
                        cmd = new MySqlCommand("DELETE FROM class_sched_tb WHERE sched_id = " + txtSHid.Text, conn);
                        tmp_res += txtSHid.Text;
                        tmp_res += ", Course: " + cbSHCourse + ", Subject: " + cbSHSubj.Text;
                        break;

                    case "Classroom":
                        cmd = new MySqlCommand("DELETE FROM classroom_tb WHERE room_id = " + txtRMid.Text, conn);
                        tmp_res += txtRMid.Text;
                        tmp_res += ", Number: " + txtRMno.Text + ", Device ID: " + txtRMdev.Text;
                        break;

                    case "Subject":
                        cmd = new MySqlCommand("DELETE FROM subject_tb WHERE subject_id = " + txtSubid.Text, conn);
                        tmp_res += txtSubid.Text;
                        tmp_res += ", Code: " + txtSubcode.Text + ", Description: " + txtSubDesc.Text;
                        break;

                    case "Department":
                        cmd = new MySqlCommand("DELETE FROM department_tb WHERE dept_id = " + txtDeptid.Text, conn);
                        tmp_res += txtDeptid.Text;
                        tmp_res += ", Name: " + txtDeptname.Text;
                        break;
                }
                if (MessageBox.Show("Are you sure you want to delete?", " ", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes) {
                    conn.Open();
                    cmd.ExecuteNonQuery();
                    conn.Close();
                    RefreshFacultyDatagrid();
                    sysLog(0, "", "information", "Record deleted on: [" + tabFaculty.SelectedTab.Text + "] Ref: (" + tmp_res + ")", 64);
                    btnNew_Click(sender, e);
                    MessageBox.Show("Record Deleted.");
                }
            }
            catch (Exception ex) {
                MessageBox.Show(ex.Message, this.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                            Bitmap bm = ResizeImage(profpic.Image, 150, 150);
                            ImageData = imageToByteArray(bm);
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
                                MessageBox.Show("Data already exist!", this.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
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

                if (form && MessageBox.Show("Are you sure you want to save?", this.Text, MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes) {
                    conn.Open();
                    cmd.ExecuteNonQuery();
                    conn.Close();

                    int nRow = 0;

                    if (btnSave.Tag.ToString() == "edit") {
                        nRow = dataGrid.CurrentCell.RowIndex;
                        MessageBox.Show("Changes Saved.", this.Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }

                    RefreshFacultyDatagrid();

                    if (btnSave.Tag.ToString() == "new") {
                        //select newly inserted row
                        nRow = dataGrid.Rows.Count - 1;
                        MessageBox.Show("Data Inserted.", this.Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    dataGrid.Rows[nRow].Selected = true;
                    dataGrid_CellClick(dataGrid, new DataGridViewCellEventArgs(0, nRow));
                }
                if (conn.State == ConnectionState.Open) conn.Close();
            }
            catch (Exception ex) {
                MessageBox.Show(ex.Message, this.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                    bool has_sched = false;
                    string orig_start = "", orig_end = "";

                    room_index = lvRooms.SelectedIndices[0];

                    lblRmName.Text = "ROOM " + lvRooms.Items[room_index].Text;
                    txtIP.Text = lvRooms.Items[room_index].SubItems[2].Text;
                    txtMAC.Text = lvRooms.Items[room_index].SubItems[3].Text;
                    txtPort.Text = lvRooms.Items[room_index].SubItems[4].Text;

                    switch (lvRooms.Items[room_index].ImageIndex) {
                        case 0:
                            lblStatus.Text = "Offline!";
                            lblStatus.ForeColor = Color.Red;
                            break;
                        case 1:
                            lblStatus.Text = "Disconnected!";
                            lblStatus.ForeColor = Color.Orange;
                            break;
                        case 2:
                            lblStatus.Text = "Connected.";
                            lblStatus.ForeColor = Color.Lime;
                            break;
                        case 3:
                            lblStatus.Text = "In-Use.";
                            lblStatus.ForeColor = Color.LightBlue;
                            break;
                    }

                    cmd = conn.CreateCommand();
                    cmd.CommandText = @"SELECT `day`,
                            TIME_FORMAT(`start_time`, '%h:%i %p') AS `start`,
                            TIME_FORMAT(`end_time`, '%h:%i %p') AS `end`,
                            `start_time` AS `o_start`,
                            `end_time` AS `o_end`,
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
                        WHERE `room_id`=@p1 AND `day`=@p2 AND (`start_time` < NOW() AND `end_time` > NOW());";

                    string rm_id = lvRooms.Items[room_index].SubItems[1].Text;
                    cmd.Parameters.AddWithValue("@p1", rm_id);
                    cmd.Parameters.AddWithValue("@p2", DateTime.Now.DayOfWeek.ToString());

                    conn.Open();
                    reader = cmd.ExecuteReader();
                    if (reader.Read()) {
                        txtSubjDesc.Text = reader["code"].ToString() + " - " + reader["descpt"].ToString();
                        txtFaculty.Text = reader["faculty"].ToString();
                        txtCourse.Text = reader["course_name"].ToString();
                        txtDay.Text = reader["day"].ToString();
                        txtTStart.Text = reader["start"].ToString();
                        txtTEnd.Text = reader["end"].ToString();
                        TimeSpan tdiff = DateTime.Parse(reader["end"].ToString()) - DateTime.Now;
                        lblUpTime.Text = tdiff.Hours.ToString() + "H : " + tdiff.Minutes.ToString() + "M : " + tdiff.Seconds.ToString() + "S";
                        has_sched = true;
                        orig_start = reader["o_start"].ToString();
                        orig_end = reader["o_end"].ToString();
                    }
                    conn.Close();

                    if (has_sched) {
                        cmd = conn.CreateCommand();
                        cmd.CommandText = @"SELECT
                                              FORMAT(AVG(`pzem_tb`.`volt`),2) AS `volt`,
                                              FORMAT(AVG(`pzem_tb`.`current`),3) AS `current`,
                                              FORMAT(AVG(`pzem_tb`.`power`),2) AS `power`,
                                              MAX(`pzem_tb`.`energy`) AS `energy`,
                                              FORMAT(AVG(`pzem_tb`.`frequency`),2) AS `frequency`,
                                              FORMAT(AVG(`pzem_tb`.`pf`),2) AS `pf`
                                            FROM `pzem_tb`
                                            WHERE `dev_id` = @p1 AND 
                                                (`pzem_tb`.`t_stamp` BETWEEN CAST(@p2 AS DATETIME) 
                                                AND CAST(@p3 AS DATETIME));";

                        cmd.Parameters.AddWithValue("@p1", lvRooms.Items[room_index].SubItems[6].Text);
                        cmd.Parameters.AddWithValue("@p2", DateTime.Now.ToString("yyyy-MM-dd " + orig_start));
                        cmd.Parameters.AddWithValue("@p3", DateTime.Now.ToString("yyyy-MM-dd " + orig_end));

                        conn.Open();
                        reader = cmd.ExecuteReader();
                        if (reader.Read()) {
                            txtVolt.Text = reader["volt"].ToString();
                            txtCurr.Text = reader["current"].ToString();
                            txtPower.Text = reader["power"].ToString();
                            txtEnergy.Text = reader["energy"].ToString();
                            txtFreq.Text = reader["frequency"].ToString();
                            txtPF.Text = reader["pf"].ToString();
                            Console.WriteLine(lvRooms.Items[room_index].SubItems[6].Text);
                            calcEnergy();
                        }
                        conn.Close();
                    }
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

        private void calcEnergy() {
            if (!txtFT.Text.Equals("") && !txtMS.Text.Equals("")) {
                float ft = 0, ms = 0;

                if (float.TryParse(txtFT.Text, out ft) && float.TryParse(txtMS.Text, out ms)) {
                    float fVat = (float.Parse(txtEnergy.Text) * ft + ms) * 12 / 100;
                    txtVat.Text = fVat.ToString("#0.##");
                    txtExpense.Text = ((float.Parse(txtEnergy.Text) * ft + ms) + fVat).ToString("#,##0.00");
                }
            }
        }

        private void tabFaculty_SelectedIndexChanged(object sender, EventArgs e) {
            RefreshFacultyDatagrid();
            FillSearchCombo();
            btnCancel_Click(sender, e);
        }

        private void txtSearch_TextChanged(object sender, EventArgs e) {
            RefreshFacultyDatagrid();
            if (txtSearch.Text.Length > 0) btnClearSearch.Show(); else btnClearSearch.Hide();
        }

        private void btnImgBrowse_Click(object sender, EventArgs e) {
            try {
                // open file dialog   
                OpenFileDialog open = new OpenFileDialog();
                // image filters  
                open.Filter = "Image Files(*.jpg; *.jpeg; *.gif; *.bmp)|*.jpg; *.jpeg; *.gif; *.bmp";
                if (open.ShowDialog() == DialogResult.OK) {
                    // display image in picture box  
                    profpic.Image = new Bitmap(open.FileName);
                    // image file path  
                    profpic.Tag = open.FileName;
                }
            }
            catch (Exception ex) {
                MessageBox.Show(ex.Message, this.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnImgRemove_Click(object sender, EventArgs e) {
            profpic.Image = new Bitmap(Properties.Resources.user);
            profpic.Tag = null;
            btnImgRemove.Hide();
        }

        private void btnLogout_Click(object sender, EventArgs e) {
            foreach (Form oForm in Application.OpenForms) {
                if (oForm is Login) {
                    oForm.Show();
                    this.Hide();
                    break;
                }
            }
        }

        private void cbSearch_SelectedIndexChanged(object sender, EventArgs e) {
            RefreshFacultyDatagrid();
            txtSearch.Focus();
        }

        public void RefreshLogDatagrid() {
            try {
                conn.Open();

                DataTable dt = new DataTable();
                MySqlDataAdapter oda = new MySqlDataAdapter(pVariables.qLogReport, conn);

                if (tabReport.SelectedTab.Text == "Attendance / User-Auth") oda.SelectCommand.Parameters.AddWithValue("@p1", "userauth");
                else if (tabReport.SelectedTab.Text == "Device") oda.SelectCommand.Parameters.AddWithValue("@p1", "devinfo");
                else if (tabReport.SelectedTab.Text == "Information") oda.SelectCommand.Parameters.AddWithValue("@p1", "information");
                else if (tabReport.SelectedTab.Text == "System") oda.SelectCommand.Parameters.AddWithValue("@p1", "system");

                oda.Fill(dt);
                dgSysLogs.DataSource = dt;
                conn.Close();

                lblLogCount.Text = string.Format("Last: {0}, {1} Log Entries. (Maximum 100)", dt.Rows.Count, tabReport.SelectedTab.Text);

                dgSysLogs.Columns[4].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            }
            catch (Exception ex) {
                MessageBox.Show(ex.Message, this.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally {
                if (conn.State == ConnectionState.Open) conn.Close();
            }

        }

        private void tabReport_SelectedIndexChanged(object sender, EventArgs e) {
            RefreshLogDatagrid();
        }

        private void btnAbout_Click(object sender, EventArgs e) {
            Console.WriteLine(DateTime.Now.ToString("yyyy-MM-dd 04:00:00"));
        }

        private void bExport_Click(object sender, EventArgs e) {
            try {
                // creating Excel Application  
                Microsoft.Office.Interop.Excel._Application app = new Microsoft.Office.Interop.Excel.Application();
                // creating new WorkBook within Excel application  
                Microsoft.Office.Interop.Excel._Workbook workbook = app.Workbooks.Add(Type.Missing);
                // creating new Excelsheet in workbook  
                Microsoft.Office.Interop.Excel._Worksheet worksheet = null;
                // see the excel sheet behind the program  
                app.Visible = true;
                // get the reference of first sheet. By default its name is Sheet1.  
                // store its reference to worksheet  
                worksheet = workbook.Sheets["Sheet1"];
                worksheet = workbook.ActiveSheet;
                // changing the name of active sheet  
                worksheet.Name = "Exported from gridview";
                // storing header part in Excel  
                for (int i = 1; i < dgSysLogs.Columns.Count + 1; i++) {
                    worksheet.Cells[1, i] = dgSysLogs.Columns[i - 1].HeaderText;
                }
                // storing Each row and column value to excel sheet  
                for (int i = 0; i < dgSysLogs.Rows.Count - 1; i++) {
                    for (int j = 0; j < dgSysLogs.Columns.Count; j++) {
                        worksheet.Cells[i + 2, j + 1] = dgSysLogs.Rows[i].Cells[j].Value.ToString();
                    }
                }
                // save the application  
                workbook.SaveAs(Environment.CurrentDirectory + "\\logs.xls", Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Microsoft.Office.Interop.Excel.XlSaveAsAccessMode.xlExclusive, Type.Missing, Type.Missing, Type.Missing, Type.Missing);
                // Exit from the application  
                app.Quit();
            }
            catch (Exception ex) {
                MessageBox.Show(ex.Message, this.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

        }

        private void txtFT_TextChanged(object sender, EventArgs e) {
            calcEnergy();
        }

        private void txtMS_TextChanged(object sender, EventArgs e) {
            calcEnergy();
        }
    }
}

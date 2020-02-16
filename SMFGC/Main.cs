using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.IO.Ports;
using System.Windows.Forms;

using static SMFGC.Program;

namespace SMFGC {
    public partial class Main : Form {

        readonly MySqlConnection conn = new MySqlConnection(pVariables.sConn);
        MySqlCommand cmd;
        MySqlDataReader reader;
        SerialPort RFID;

        int room_index = -1;
        string RFIDTag = "";

        mServer server = new mServer();
        pingClient client = new pingClient();

        public Main() {
            InitializeComponent();
            setMode();
            this.tabMain.ItemSize = new Size(0, 1);
            lvRooms.Items.Clear();
        }

        public void setMode() {
            if (pVariables.AdminMode) {
                this.Text = "Administrator - " + pVariables.Project_Name;
                toplabel.Text = pVariables.Project_Name;

                btnFaculty.Show();
                btnReports.Show();
            }
            if (pVariables.DeptMode) {
                this.Text = "Department Mode - " + pVariables.Project_Name;
                toplabel.Text = "College of Arts, Science and Engineering";

                btnFaculty.Hide();
                btnReports.Hide();
            }
            tabMain.SelectedIndex = 0;
            RefreshClassrooms();
        }

        private void Main_Load(object sender, EventArgs e) {
            try {
                if (pVariables.AdminMode) {
                    //server.startServer();
                    //client.startPing();
                }
                sysLog("sys", "Server started.", 64);
            }
            catch (Exception ex) {
                pVariables.confirmExit = false;
                MessageBox.Show(ex.Message, this.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
                Environment.Exit(0);
            }
        }

        private void Main_FormClosing(object sender, FormClosingEventArgs e) {
            if (pVariables.confirmExit && MessageBox.Show("Do you really want to Exit?", this.Text, MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.No) {
                e.Cancel = true;
            }
            else {
                sysLog("sys", "Server closed.", 64);
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
        }

        private void btnFaculty_Click(object sender, EventArgs e) {
            tabMain.SelectedIndex = 1;
            InitRFID();
            RefreshFacultyDatagrid();
            FillSearchCombo();
            btnReloadPort_Click(sender, e);
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
                var rm_list = new List<ListViewItem>();


                cmd = conn.CreateCommand();
                cmd.CommandText = pVariables.qRoom + ((pVariables.DeptMode) ? String.Format(" WHERE dept_id = {0} ORDER BY `number`;", pVariables.DeptID) : "ORDER BY `number`;");

                conn.Open();
                reader = cmd.ExecuteReader();
                while (reader.Read()) {
                    ListViewItem lvI = new ListViewItem();

                    lvI.ImageIndex = Convert.ToInt32(reader["status"]);
                    lvI.Text = reader["number"].ToString();
                    lvI.SubItems.Add(reader["id"].ToString());
                    lvI.SubItems.Add(reader["dev_id"].ToString());
                    lvI.SubItems.Add(reader["name"].ToString());
                    lvI.SubItems.Add(reader["ip_addr"].ToString());
                    lvI.SubItems.Add(reader["mac_addr"].ToString());
                    lvI.SubItems.Add(reader["port"].ToString());
                    rm_list.Add(lvI);
                }
                conn.Close();
                lvRooms.BeginUpdate();
                lvRooms.Items.Clear();
                lvRooms.Items.AddRange(rm_list.ToArray());
                lvRooms.EndUpdate();

                if (room_index > -1 && lvRooms.Items.Count > 0 && room_index < lvRooms.Items.Count) {
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
            txtSubjDesc.Clear();
            txtFaculty.Clear();
            txtCourse.Clear();
            txtDay.Clear();
            txtTStart.Clear();
            txtTEnd.Clear();
            lblUpTime.Text = "00H : 00M : 00S";
            lblStatus.Text = "...";

            //value
            Volt.Text = Curr.Text = Power.Text = Energy.Text = Freq.Text = PF.Text = "0.0";
            //min
            mVolt.Text = mCurr.Text = mPower.Text = mEnergy.Text = mFreq.Text = mPF.Text = "0.0";
            //max
            xVolt.Text = xCurr.Text = xPower.Text = xEnergy.Text = xFreq.Text = xPF.Text = "0.0";
            // avarage
            aVolt.Text = aCurr.Text = aPower.Text = aEnergy.Text = aFreq.Text = aPF.Text = "0.0";
        }

        private void RefreshFacultyDatagrid() {
            try {
                conn.Open();
                string query = "SELECT * FROM ";

                if (tabFaculty.SelectedTab.Text == "Information") query += "faculty_v";
                else if (tabFaculty.SelectedTab.Text == "Schedule") query += "schedule_v";
                else if (tabFaculty.SelectedTab.Text == "Classroom") query += "classroom_v";
                else if (tabFaculty.SelectedTab.Text == "Subject") query += "subject_v";
                else if (tabFaculty.SelectedTab.Text == "Department") query += "department_v";

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
                    FillComboBox(cbSHCourse, "course_tb", "id", "name");
                    FillComboBox(cbSHSubj, "subject_tb", "id", "code");
                    FillComboBox(cbSHRoom, "classroom_tb", "id", "number");
                    FillComboBox(cbSHFaculty, "faculty_v", "id", "fullname");
                }
                else if (tabFaculty.SelectedTab.Text == "Classroom") {
                    FillComboBox(cbRMDept, "department_tb", "id", "name");
                    FillComboBox(cbRMDev, "device_tb", "id", "serial_no");
                }
                else if (tabFaculty.SelectedTab.Text == "Information") {
                    dgProfSched.DataSource = null;
                    dgProfSched.Show();
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
            if (e.RowIndex == -1) return;

            try {
                if (tabFaculty.SelectedIndex < 0) return;

                string s_id = dataGrid.Rows[e.RowIndex].Cells["ID"].Value.ToString();
                btnSave.Tag = "edit";
                conn.Open();
                cmd = conn.CreateCommand();

                switch (tabFaculty.SelectedTab.Text) {
                    case "Information":
                        cmd.CommandText = @"SELECT * FROM `faculty_tb` WHERE `id`=@id";
                        cmd.Parameters.AddWithValue("@id", s_id);
                        reader = cmd.ExecuteReader();
                        if (reader.Read()) {
                            txtUid.Text = s_id;
                            cbLevel.SelectedIndex = Convert.ToInt32(reader["level"]);
                            txtUTag.Text = reader["uidtag"].ToString();
                            cbtitle.SelectedItem = reader["title"].ToString();
                            txtULN.Text = reader["last_name"].ToString();
                            txtUFN.Text = reader["first_name"].ToString();
                            txtUMI.Text = reader["mi"].ToString();

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
                            conn.Close();

                            string query = @"SELECT `Room`,`Course`,`Subject Code`,`Day`,`Start Time`,`End Time` FROM `schedule_v` WHERE `faculty` = @p1;";

                            conn.Open();
                            DataTable dt = new DataTable();
                            MySqlDataAdapter oda = new MySqlDataAdapter(query, conn);
                            oda.SelectCommand.Parameters.AddWithValue("@p1", String.Format("{0} {1} {2} {3}", cbtitle.SelectedItem, txtULN.Text, txtUFN.Text, txtUMI.Text));
                            oda.Fill(dt);
                            dgProfSched.DataSource = dt;
                            conn.Close();

                            //datagrid has calculated it's widths so we can store them
                            for (int i = 0; i <= dgProfSched.Columns.Count - 1; i++) {
                                //store autosized widths
                                dgProfSched.Columns[i].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
                            }

                            if (dgProfSched.Rows.Count > 0) dgProfSched.Show(); else dgProfSched.Hide();
                        }
                        break;

                    case "Schedule":
                        cmd.CommandText = @"SELECT * FROM `schedule_tb` WHERE `id`=@id";
                        cmd.Parameters.AddWithValue("@id", s_id);
                        reader = cmd.ExecuteReader();
                        if (reader.Read()) {
                            txtSHid.Text = s_id;
                            cbSHCourse.SelectedValue = reader["course_id"];
                            cbSHSubj.SelectedValue = reader["subject_id"];
                            setRBDay(FirstCharToUpper(reader["day"].ToString()));
                            dtTimeStart.Value = DateTime.Parse(reader["start_time"].ToString());
                            dtTimeEnd.Value = DateTime.Parse(reader["end_time"].ToString());
                            cbSHRoom.SelectedValue = reader["room_id"];
                            cbSHFaculty.SelectedValue = reader["faculty_id"];

                        }
                        break;

                    case "Classroom":
                        cmd.CommandText = @"SELECT * FROM `classroom_tb` WHERE `id`=@id";
                        cmd.Parameters.AddWithValue("@id", s_id);
                        reader = cmd.ExecuteReader();
                        if (reader.Read()) {
                            txtRMid.Text = s_id;
                            txtRMname.Text = reader["name"].ToString();
                            txtRMno.Text = reader["number"].ToString();
                            cbRMDept.SelectedValue = reader["dept_id"];
                            cbRMDev.SelectedValue = reader["dev_id"].ToString();
                            ckRelay1.Checked = Convert.ToBoolean((int)reader["relay_1"]);
                            ckRelay2.Checked = Convert.ToBoolean((int)reader["relay_2"]);
                        }
                        break;

                    case "Subject":
                        cmd.CommandText = @"SELECT * FROM `subject_tb` WHERE `id`=@id";
                        cmd.Parameters.AddWithValue("@id", s_id);
                        reader = cmd.ExecuteReader();
                        if (reader.Read()) {
                            txtSubid.Text = s_id;
                            txtSubcode.Text = reader["code"].ToString();
                            txtSubDesc.Text = reader["desc"].ToString();
                        }
                        break;

                    case "Department":
                        cmd.CommandText = @"SELECT * FROM `department_tb` WHERE `id`=@id";
                        cmd.Parameters.AddWithValue("@id", s_id);
                        reader = cmd.ExecuteReader();
                        if (reader.Read()) {
                            txtDeptid.Text = s_id;
                            txtDeptname.Text = reader["name"].ToString();
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
                RFID = new SerialPort();
                RFID.PortName = cboPort.Items[cboPort.SelectedIndex].ToString();
                RFID.BaudRate = 9600;
                RFID.DataBits = 8;
                RFID.Parity = Parity.None;
                RFID.StopBits = StopBits.One;
                RFID.Open();
                RFID.ReadTimeout = 200;
                if (RFID.IsOpen) {
                    lblRFIDStatus.Text = "Ready!";
                }
                else {
                    RFID.Close();
                }
                RFID.DataReceived += new SerialDataReceivedEventHandler(RFID_DataReceived);
            }
            catch (Exception ex) {
                lblRFIDStatus.Text = "Not Ready!";
                Console.WriteLine("Error Opening port: {0}", ex.Message);
            }
        }

        private void RFID_DataReceived(object sender, SerialDataReceivedEventArgs e) {
            if (txtUTag.Text.Length >= 12) {
                RFID.Close();
            }
            else {
                RFIDTag = RFID.ReadExisting();
                this.Invoke(new EventHandler(DisplayText));
            }

        }

        private void btnReloadPort_Click(object sender, EventArgs e) {
            cboPort.DataSource = SerialPort.GetPortNames();
        }

        private void DisplayText(object sender, EventArgs e) {
            if (btnSave.Tag != null) txtUTag.Text = RFIDTag;
            if (txtSearch.Text == "" && txtSearch.Focused && cbSearch.Items[cbSearch.SelectedIndex].ToString() == "Tag") txtSearch.Text = RFIDTag;
        }

        private void cboPort_Click(object sender, EventArgs e) {
            InitRFID();
        }

        private void btnNew_Click(object sender, EventArgs e) {
            dgProfSched.Hide();
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
                    txtRMname.Clear();
                    txtRMno.Clear();
                    cbRMDept.SelectedIndex = 0;
                    cbRMDev.SelectedIndex = 0;
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
                        dgProfSched.Hide();

                        cmd = new MySqlCommand("DELETE FROM `faculty_tb` WHERE `id` = " + txtUid.Text, conn);
                        tmp_res += txtUid.Text;
                        tmp_res += ", Name: " + cbtitle.SelectedItem + " " + txtULN.Text + ", " + txtUFN.Text + " " + txtUMI.Text;
                        break;

                    case "Schedule":
                        cmd = new MySqlCommand("DELETE FROM `schedule_tb` WHERE `id` = " + txtSHid.Text, conn);
                        tmp_res += txtSHid.Text;
                        tmp_res += ", Course: " + cbSHCourse + ", Subject: " + cbSHSubj.Text;
                        break;

                    case "Classroom":
                        cmd = new MySqlCommand("DELETE FROM `classroom_tb` WHERE `id` = " + txtRMid.Text, conn);
                        tmp_res += txtRMid.Text;
                        tmp_res += ", Number: " + txtRMno.Text + ", Device ID: " + cbRMDev.SelectedItem;
                        break;

                    case "Subject":
                        cmd = new MySqlCommand("DELETE FROM `subject_tb` WHERE `id` = " + txtSubid.Text, conn);
                        tmp_res += txtSubid.Text;
                        tmp_res += ", Code: " + txtSubcode.Text + ", Description: " + txtSubDesc.Text;
                        break;

                    case "Department":
                        cmd = new MySqlCommand("DELETE FROM `department_tb` WHERE `id` = " + txtDeptid.Text, conn);
                        tmp_res += txtDeptid.Text;
                        tmp_res += ", Name: " + txtDeptname.Text;
                        break;
                }
                if (MessageBox.Show("Are you sure you want to delete?", " ", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes) {
                    conn.Open();
                    cmd.ExecuteNonQuery();
                    conn.Close();
                    RefreshFacultyDatagrid();
                    sysLog("data", "Record deleted on: [" + tabFaculty.SelectedTab.Text + "] Ref: (" + tmp_res + ")", 64);
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

                        if (btnSave.Tag.ToString() == "new") {
                            cmd.CommandText = @"INSERT INTO `faculty_tb` (`level`,`uidtag`,`title`,`last_name`,`first_name`,`mi`,`picture`) 
                                VALUES(@lvl, @tag, @title, UC_WORDS(@ln), UC_WORDS(@fn), UC_WORDS(@mi), @pic);";
                        }
                        else if (btnSave.Tag.ToString() == "edit") {
                            cmd.CommandText = @"UPDATE `faculty_tb` SET `level`@lvl, `uidtag`=@tag, `title`=@title, `last_name`=UC_WORDS(@ln), 
                                `first_name`=UC_WORDS(@fn), `mi`=UC_WORDS(@mi), `picture`=@pic WHERE `id`=@id;";
                            cmd.Parameters.Add("@id", MySqlDbType.Int32).Value = txtUid.Text;
                        }
                        if (profpic.Tag != null) {
                            Bitmap bm = ResizeImage(profpic.Image, 150, 150);
                            ImageData = imageToByteArray(bm);
                        }

                        cmd.Parameters.Add("@lvl", MySqlDbType.Int32).Value = cbLevel.SelectedIndex;
                        cmd.Parameters.Add("@tag", MySqlDbType.VarChar).Value = txtUTag.Text;
                        cmd.Parameters.Add("@title", MySqlDbType.VarChar).Value = cbtitle.Items[cbtitle.SelectedIndex].ToString();
                        cmd.Parameters.Add("@ln", MySqlDbType.VarChar).Value = txtULN.Text;
                        cmd.Parameters.Add("@fn", MySqlDbType.VarChar).Value = txtUFN.Text;
                        cmd.Parameters.Add("@mi", MySqlDbType.VarChar).Value = txtUMI.Text.Substring(0, 1) + ".";
                        cmd.Parameters.Add("@pic", MySqlDbType.Blob).Value = ImageData;
                        break;

                    case "Schedule":
                        cmd.CommandText = @"SELECT `id` FROM `schedule_tb`
                                WHERE `room_id` = @rmid AND `day` = @day AND `faculty_id` = @fac
                                AND ((`start_time` BETWEEN @stime AND @etime) OR (`end_time` BETWEEN @stime AND @etime)) LIMIT 1;";
                        cmd.Parameters.Add("@rmid", MySqlDbType.Int32).Value = cbSHRoom.SelectedValue;
                        cmd.Parameters.Add("@fac", MySqlDbType.Int32).Value = cbSHFaculty.SelectedValue;
                        cmd.Parameters.Add("@day", MySqlDbType.VarChar).Value = getRBDay().ToLower();
                        cmd.Parameters.Add("@stime", MySqlDbType.VarChar).Value = dtTimeStart.Value.ToString("HH:mm");
                        cmd.Parameters.Add("@etime", MySqlDbType.VarChar).Value = dtTimeEnd.Value.ToString("HH:mm");
                        conn.Open();
                        reader = cmd.ExecuteReader();
                        if (reader.Read()) {
                            if (btnSave.Tag.ToString() == "new" || btnSave.Tag.ToString() == "edit" && reader["id"].ToString() != txtSHid.Text) {
                                MessageBox.Show("Data already exist!", this.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
                                form = false;
                            }
                        }
                        conn.Close();

                        if (form) {
                            cmd = conn.CreateCommand();
                            if (btnSave.Tag.ToString() == "new") {
                                cmd.CommandText = @"INSERT INTO `schedule_tb` (`course_id`,`subject_id`,`day`,`start_time`,`end_time`,`room_id`,`faculty_id`) 
                                    VALUES(@courseid, @subjectid, @day, @stime, @etime, @rmid, @faculty);";
                            }
                            else if (btnSave.Tag.ToString() == "edit") {
                                cmd.CommandText = @"UPDATE `schedule_tb` SET `course_id`=@courseid, `subject_id`=@subjectid, `day`=@day, 
                                    `start_time`=@stime, `end_time`=@etime, `room_id`=@rmid, `faculty_id`=@faculty WHERE `id`=@id;";
                                cmd.Parameters.Add("@id", MySqlDbType.Int32).Value = txtSHid.Text;
                            }
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
                        if (btnSave.Tag.ToString() == "new") {
                            cmd.CommandText = @"INSERT INTO `classroom_tb` (`name`, `number`, `dept`, `dev_id`, `relay1`, `relay2`)  
                                VALUES(@name, @num, @dept, @dev, @r1, @r2);";
                        }
                        else if (btnSave.Tag.ToString() == "edit") {
                            cmd.CommandText = @"UPDATE `classroom_tb` SET `name`,=@name `number`=@num, `dept`=@dept, `dev_id`=@dev,
                                `relay_1`=@r1, `relay_2`=@r2 WHERE `id`=@id;";
                            cmd.Parameters.Add("@id", MySqlDbType.Int32).Value = txtRMid.Text;
                        }
                        cmd.Parameters.Add("@name", MySqlDbType.VarChar).Value = txtRMname.Text;
                        cmd.Parameters.Add("@num", MySqlDbType.VarChar).Value = txtRMno.Text;
                        cmd.Parameters.Add("@dept", MySqlDbType.Int32).Value = cbRMDept.SelectedValue;
                        cmd.Parameters.Add("@dev", MySqlDbType.Int32).Value = cbRMDev.SelectedValue;
                        cmd.Parameters.Add("@r1", MySqlDbType.Int32).Value = (int)ckRelay1.CheckState;
                        cmd.Parameters.Add("@r2", MySqlDbType.Int32).Value = (int)ckRelay2.CheckState;
                        break;

                    case "Subject":
                        if (btnSave.Tag.ToString() == "new") {
                            cmd.CommandText = @"INSERT INTO `subject_tb` (`code`,`desc`) VALUES(@code, @desc);";
                        }
                        else if (btnSave.Tag.ToString() == "edit") {
                            cmd.CommandText = @"UPDATE `subject_tb` SET `code`=@code, `desc`=UC_WORDS(@desc) WHERE `id`=@id;";
                            cmd.Parameters.Add("@id", MySqlDbType.Int32).Value = txtSubid.Text;
                        }
                        cmd.Parameters.Add("@code", MySqlDbType.VarChar).Value = txtSubcode.Text;
                        cmd.Parameters.Add("@desc", MySqlDbType.VarChar).Value = txtSubDesc.Text;
                        break;

                    case "Department":
                        if (btnSave.Tag.ToString() == "new") {
                            cmd.CommandText = @"INSERT INTO `department_tb` (`name`,`floor`) VALUES(@name, @floor);";
                        }
                        else if (btnSave.Tag.ToString() == "edit") {
                            cmd.CommandText = @"UPDATE `department_tb` SET `name`=@name, `floor`=@floor WHERE `id`=@id;";
                            cmd.Parameters.Add("@id", MySqlDbType.Int32).Value = txtSubid.Text;
                        }
                        cmd.Parameters.Add("@name", MySqlDbType.VarChar).Value = txtDeptname.Text;
                        cmd.Parameters.Add("@floor", MySqlDbType.VarChar).Value = txtDeptflr.Text;
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

                    room_index = lvRooms.SelectedIndices[0];
                    lblRmName.Text = lvRooms.Items[room_index].SubItems[3].Text + " " + lvRooms.Items[room_index].Text;

                    txtIP.Text = lvRooms.Items[room_index].SubItems[4].Text;
                    txtMAC.Text = lvRooms.Items[room_index].SubItems[5].Text;
                    txtPort.Text = lvRooms.Items[room_index].SubItems[6].Text;

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
                    cmd.CommandText = pVariables.qRoomSel;
                    cmd.Parameters.AddWithValue("@p1", lvRooms.Items[room_index].SubItems[1].Text);
                    conn.Open();
                    reader = cmd.ExecuteReader();
                    if (reader.Read()) {
                        txtSubjDesc.Text = reader["subject"].ToString();
                        txtFaculty.Text = reader["faculty"].ToString();
                        txtCourse.Text = reader["name"].ToString();
                        txtDay.Text = Program.FirstCharToUpper(reader["day"].ToString());
                        txtTStart.Text = reader["start"].ToString();
                        txtTEnd.Text = reader["end"].ToString();

                        // Calculate time left
                        DateTime end_time = DateTime.Parse(String.Format("{0} {1}", DateTime.Now.ToString("yyyy-MM-dd"), reader["o_end"].ToString()));
                        TimeSpan tdiff = end_time - DateTime.Now;
                        lblUpTime.Text = tdiff.Hours.ToString() + "H : " + tdiff.Minutes.ToString() + "M : " + tdiff.Seconds.ToString() + "S";
                    }
                    conn.Close();

                    cmd = conn.CreateCommand();
                    cmd.CommandText = @"SELECT * FROM pzem_v WHERE dev_id = @p1;";
                    cmd.Parameters.AddWithValue("@p1", lvRooms.Items[room_index].SubItems[2].Text);
                    conn.Open();
                    reader = cmd.ExecuteReader();
                    if (reader.Read()) {
                        //value
                        Volt.Text = reader["volt"].ToString();
                        Curr.Text = reader["current"].ToString();
                        Power.Text = reader["power"].ToString();
                        Energy.Text = reader["energy"].ToString();
                        Freq.Text = reader["frequency"].ToString();
                        PF.Text = reader["pf"].ToString();

                        //min
                        mVolt.Text = reader["volt_min"].ToString();
                        mCurr.Text = reader["current_min"].ToString();
                        mPower.Text = reader["power_min"].ToString();
                        mEnergy.Text = reader["energy_min"].ToString();
                        mFreq.Text = reader["frequency_min"].ToString();
                        mPF.Text = reader["pf_min"].ToString();

                        //max
                        xVolt.Text = reader["volt_max"].ToString();
                        xCurr.Text = reader["current_max"].ToString();
                        xPower.Text = reader["power_max"].ToString();
                        xEnergy.Text = reader["energy_max"].ToString();
                        xFreq.Text = reader["frequency_max"].ToString();
                        xPF.Text = reader["pf_max"].ToString();

                        // avarage
                        aVolt.Text = reader["volt_avg"].ToString();
                        aCurr.Text = reader["current_avg"].ToString();
                        aPower.Text = reader["power_avg"].ToString();
                        aEnergy.Text = reader["energy_avg"].ToString();
                        aFreq.Text = reader["frequency_avg"].ToString();
                        aPF.Text = reader["pf_avg"].ToString();
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

        //private void calcEnergy() {
        //    if (!txtFT.Text.Equals("") && !txtMS.Text.Equals("")) {
        //        float ft = 0, ms = 0, energy = 0;

        //        if (float.TryParse(txtFT.Text, out ft) && float.TryParse(txtMS.Text, out ms) && float.TryParse(Energy.Text, out energy)) {
        //            float fVat = (energy * ft + ms) * 12 / 100;
        //            txtVat.Text = fVat.ToString("#0.##");
        //            txtExpense.Text = ((energy * ft + ms) + fVat).ToString("#,##0.00");
        //        }
        //    }
        //}

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
                open.Filter = "Image Files(*.jpg; *.jpeg; *.gif; *.bmp; *.png)|*.jpg; *.jpeg; *.gif; *.bmp; *.png";
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
                else if (tabReport.SelectedTab.Text == "Device") oda.SelectCommand.Parameters.AddWithValue("@p1", "dev");
                else if (tabReport.SelectedTab.Text == "Information") oda.SelectCommand.Parameters.AddWithValue("@p1", "data");
                else if (tabReport.SelectedTab.Text == "System") oda.SelectCommand.Parameters.AddWithValue("@p1", "sys");

                oda.Fill(dt);
                dgSysLogs.DataSource = dt;
                conn.Close();

                lblLogCount.Text = string.Format("Last: {0}, {1} Log Entries. (Maximum 100)", dt.Rows.Count, tabReport.SelectedTab.Text);

                dgSysLogs.Columns[2].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
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

        private void cboPort_SelectedIndexChanged(object sender, EventArgs e) {
            InitRFID();
        }
    }
}

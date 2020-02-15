using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;

using static SMFGC.Program;

namespace SMFGC {
    class handleClient {
        TcpClient clientSocket;
        readonly MySqlConnection conn = new MySqlConnection(pVariables.sConn);
        MySqlCommand cmd;
        MySqlDataReader reader;

        Thread t;

        public void startClient(TcpClient inClient) {
            this.clientSocket = inClient;
            t = new Thread(doTasks);
            t.Start();
        }

        public void exitThread() {
            t.Abort();
        }

        private void doTasks() {
            Byte[] buffer = new Byte[1024];
            byte[] msg;

            bool dev_verified = false, session_resume = false, relay1 = false, relay2 = false, sfv_enable = false;
            int dev_id = 0, room_id = 0, faculty_id = 0, faculty_level = 0, sched_id = 0;
            int dev_status = 0, dev_check_delay = 10, tleft_led_delay = 2, alarm_led_delay = 3; // <-- TL_LED Delay before send another command
            int pzem_err_rpt = 60; // In seconds

            String data, room_name = "", faculty = "", last_uid = "", log_msg = "";
            String dev_ip = ((IPEndPoint)clientSocket.Client.RemoteEndPoint).Address.ToString();

            DateTime session_start = DateTime.Now;
            DateTime end_time = DateTime.Parse(String.Format("{0} {1}", DateTime.Now.ToString("yyyy-MM-dd"), "23:59:59"));
            TimeSpan sfv_time = TimeSpan.Parse("00:30:00");

            try {
                NetworkStream stream = clientSocket.GetStream();

                int i;
                while ((i = stream.Read(buffer, 0, buffer.Length)) != 0) {

                    // Translate data bytes to a ASCII string.
                    data = Encoding.ASCII.GetString(buffer, 0, i);
                    //Console.WriteLine("Data Recieved: {0}", data);

                    // Checking data headers
                    if (!data.Contains("DEV:")) {

                        Console.WriteLine("Unknown Message: \"{0}\"; (Connection Closed).", data);
                        sysLog("dev", String.Format("IP Address: {0}; The device is unknown.", dev_ip), 16);
                        break;
                    }

                    // Verify if the device is known to server.
                    if (!dev_verified && data.Contains("DEV:")) {
                        dev_id = Convert.ToInt32(data.Split(',')[0].Replace("DEV:", ""));

                        cmd = conn.CreateCommand();
                        cmd.CommandText = pVariables.qDeviceCheck;
                        cmd.Parameters.Add("@p1", MySqlDbType.Int32).Value = dev_id;
                        conn.Open();
                        reader = cmd.ExecuteReader();

                        if (reader.Read()) {
                            dev_verified = true;

                            last_uid = reader["last_uidtag"].ToString();
                            room_id = Convert.ToInt32(reader["id"]);
                            room_name = reader["name"].ToString();
                            relay1 = Convert.ToBoolean((int)reader["relay_1"]);
                            relay2 = Convert.ToBoolean((int)reader["relay_2"]);

                            sysLog("dev", String.Format("Device ID/IP: {0}:{1} on Room ID/Name: {2}:{3}; Accepted.", dev_id, dev_ip, room_id, room_name), 64);
                            Console.WriteLine("Device Accepted: {0}:{1}, on Room: ID/Name: {2}:{3}; Accepted.", dev_id, dev_ip, room_id, room_name);
                        }
                        conn.Close();

                        if (dev_verified) {
                            // Device accepted update the db status and uptime                 
                            UpdateDevStatus(dev_id, dev_ip, 2, last_uid);

                            if (!last_uid.Equals("")) session_resume = true;
                        }
                        else {

                            sysLog("dev", String.Format("Device ID/IP: {0}:{1}; Not registered.", dev_id, dev_ip), 16);
                            Console.WriteLine("Device ID/IP: {0}:{1}; Not registered. (Server Connection Closed)", dev_id, dev_ip);
                            break;
                        }
                    }

                    // Process the data sent by the client.
                    if (dev_verified && (data.Contains("UID:") || session_resume)) {

                        string uidtag = (session_resume) ? last_uid : data.Split(',')[1].Split(' ')[1];

                        cmd = conn.CreateCommand();
                        cmd.CommandText = pVariables.qUidTagCheck;
                        cmd.Parameters.Add("@p1", MySqlDbType.VarChar).Value = uidtag;

                        conn.Open();
                        reader = cmd.ExecuteReader();
                        if (reader.Read()) {
                            // Backup old ID's
                            if (Convert.ToInt32(reader["level"]) > 0 && !last_uid.Equals("") && last_uid != uidtag) {
                                msg = Encoding.ASCII.GetBytes("g");
                                stream.Write(msg, 0, msg.Length);
                                if (conn != null && conn.State == ConnectionState.Open) conn.Close();
                                Console.WriteLine("Login tag is not same to logout tag.");
                                continue;
                            }
                            faculty_id = Convert.ToInt32(reader["id"]);
                            faculty = reader["faculty"].ToString();
                            faculty_level = Convert.ToInt32(reader["level"]);

                            switch (faculty_level) {
                                case 0: // for guard/visor.
                                    last_uid = "";
                                    dev_status = 2;
                                    UpdateDevStatus(dev_id, dev_ip, dev_status, last_uid);
                                    msg = Encoding.ASCII.GetBytes("d");
                                    stream.Write(msg, 0, msg.Length);
                                    log_msg = "Classroom closed.";
                                    break;

                                case 1:  // for prof schedules

                                    // Professor Monitoring/Verification
                                    if (Convert.ToInt32(reader["sfv_count"]) >= Convert.ToInt32(reader["sfv_limit"])) {
                                        sfv_time = TimeSpan.Parse(reader["sfv_time"].ToString());
                                        sfv_enable = true;
                                    }

                                    TimeSpan talarm = DateTime.Now - session_start;
                                    if (last_uid == uidtag && sfv_enable && sfv_time.TotalMinutes < talarm.TotalMinutes) {
                                        session_start = DateTime.Now;
                                        log_msg = "Presence Verified.";
                                        break;
                                    }

                                    //Logout
                                    if (!last_uid.Equals("") && data.Contains("UID:")) {
                                        if (last_uid == uidtag) {
                                            last_uid = "";
                                            dev_status = 2;
                                            UpdateDevStatus(dev_id, dev_ip, dev_status, last_uid);
                                            msg = Encoding.ASCII.GetBytes("d");
                                            stream.Write(msg, 0, msg.Length);
                                            log_msg = "Logged-Out.";

                                            // remove sfv points
                                            TimeSpan tleft = end_time - DateTime.Now;
                                            if (tleft.TotalMinutes < 10) {
                                                FacultySFVPoints(faculty_id, -1);
                                            }
                                        }
                                    }
                                    else {
                                        if (conn != null && conn.State == ConnectionState.Open) conn.Close();
                                        // Session Resume & Login
                                        
                                        // check schedule
                                        cmd = conn.CreateCommand();
                                        cmd.CommandText = pVariables.qCheckSched;
                                        cmd.Parameters.Add("@p1", MySqlDbType.Int32).Value = room_id;
                                        cmd.Parameters.Add("@p2", MySqlDbType.Int32).Value = faculty_id;
                                        conn.Open();
                                        reader = cmd.ExecuteReader();

                                        if (reader.Read()) {
                                            
                                            sched_id = Convert.ToInt32(reader["id"]);
                                            end_time = DateTime.Parse(String.Format("{0} {1}", DateTime.Now.ToString("yyyy-MM-dd"), reader["end_time"].ToString()));
                                            session_start = DateTime.Now;

                                            if (relay1 && relay2) {
                                                msg = Encoding.ASCII.GetBytes("c");
                                            }
                                            else if (relay1 && !relay2) {
                                                msg = Encoding.ASCII.GetBytes("a");
                                            }
                                            else if (!relay1 && relay2) {
                                                msg = Encoding.ASCII.GetBytes("b");
                                            }
                                            else {
                                                msg = Encoding.ASCII.GetBytes("e");
                                            }
                                            stream.Write(msg, 0, msg.Length);

                                            if (last_uid.Equals("")) {
                                                log_msg = "Logged-In.";
                                            }
                                            else {
                                                log_msg = "Session Resumed.";
                                                session_resume = false;
                                            }

                                            last_uid = uidtag;
                                            dev_status = 3;
                                            UpdateDevStatus(dev_id, dev_ip, dev_status, last_uid);
                                        }
                                        else {
                                            msg = (session_resume) ? Encoding.ASCII.GetBytes("d")  : Encoding.ASCII.GetBytes("f");
                                            stream.Write(msg, 0, msg.Length);
                                            log_msg = "No schedule available.";
                                            session_resume = false;
                                            last_uid = "";
                                            dev_status = 2;
                                            UpdateDevStatus(dev_id, dev_ip, dev_status, last_uid);
                                        }
                                    }
                                    break;

                                case 2: // master

                                    //Logout
                                    if (!last_uid.Equals("") && data.Contains("UID:")) {
                                        if (last_uid == uidtag) {
                                            last_uid = "";
                                            dev_status = 2;
                                            UpdateDevStatus(dev_id, dev_ip, dev_status, last_uid);
                                            msg = Encoding.ASCII.GetBytes("d");
                                            stream.Write(msg, 0, msg.Length);
                                            log_msg = "Logged-Out.";
                                        }
                                    }
                                    else {
                                        // Turn on all relay
                                        msg = Encoding.ASCII.GetBytes("c");
                                        stream.Write(msg, 0, msg.Length);
                                        end_time = DateTime.Parse(String.Format("{0} {1}", DateTime.Now.ToString("yyyy-MM-dd"), "23:59:59"));

                                        last_uid = uidtag;
                                        dev_status = 3;
                                        UpdateDevStatus(dev_id, dev_ip, dev_status, last_uid);
                                        log_msg = "Logged-In.";
                                    }
                                    break;
                            }
                            sysLog("userauth", String.Format("Faculty ID/Name: {0}:{1}, with Level: {2} tag; Used on Room ID/Name: {3}:{4}; {5}", faculty_id, faculty, faculty_level, room_id, room_name, log_msg), 64);
                            Console.WriteLine("Faculty ID/Name: {0}:{1}, with Level: {2} tag; Used on Room ID/Name: {3}:{4}; {5}", faculty_id, faculty, faculty_level, room_id, room_name, log_msg);
                        }
                        else {
                            // Send command to blink LEDs
                            msg = Encoding.ASCII.GetBytes("x");
                            stream.Write(msg, 0, msg.Length);

                            sysLog("userauth", String.Format("Faculty UIDTag: {0}; not found.", uidtag), 48);
                            Console.WriteLine("Faculty UIDTag: {0}; not found.", uidtag);
                        }
                        if (conn != null && conn.State == ConnectionState.Open) conn.Close();
                    }
                    else if (dev_verified && data.Contains("PZM:")) {

                        if (data.Contains("NaN") && pzem_err_rpt <= 0 ) {
                            pzem_err_rpt = 60; //reset
                            sysLog("dev", String.Format("Device ID/IP: {0}:{1}; Part Zone Expansion Module (PZEM) error on reading data.", dev_id, dev_ip), 16);
                            Console.WriteLine("Error Reading PZEM Data.");
                        }
                        if (!data.Contains("NaN") && pzem_err_rpt <= 0) {
                            pzem_err_rpt = 60; //reset
                            data = data.Split(',')[1].Replace("PZM:", "").Trim();

                            String[] pzem = data.Split('-');
                            cmd = conn.CreateCommand();
                            cmd.CommandText = pVariables.qPZEMLog;
                            cmd.Parameters.Add("@p1", MySqlDbType.Int32).Value = dev_id;
                            cmd.Parameters.Add("@p2", MySqlDbType.Double).Value = Convert.ToDouble(pzem[0]);
                            cmd.Parameters.Add("@p3", MySqlDbType.Double).Value = Convert.ToDouble(pzem[1]);
                            cmd.Parameters.Add("@p4", MySqlDbType.Double).Value = Convert.ToDouble(pzem[2]);
                            cmd.Parameters.Add("@p5", MySqlDbType.Double).Value = Convert.ToDouble(pzem[3]);
                            cmd.Parameters.Add("@p6", MySqlDbType.Double).Value = Convert.ToDouble(pzem[4]);
                            cmd.Parameters.Add("@p7", MySqlDbType.Double).Value = Convert.ToDouble(pzem[5]);
                            conn.Open();
                            cmd.ExecuteNonQuery();
                            conn.Close();

                            Console.WriteLine("PZEM Data recorded to database.");
                        }
                        pzem_err_rpt -= 1;
                    }
                    else if (dev_verified && data.Contains("RLY:")) {
                        Console.WriteLine("Device acknowledge the command.");
                    }

                    // CONTINUES BLINKING RED 	- 10 MINUTES LEFT WARNING
                    if (dev_verified && dev_status == 3 && !data.Contains("UID:")) {
                        TimeSpan tleft = end_time - DateTime.Now;

                        if (tleft.TotalMinutes < 0) {
                            last_uid = "";
                            dev_status = 2;

                            UpdateDevStatus(dev_id, dev_ip, dev_status, last_uid);

                            msg = Encoding.ASCII.GetBytes("d");
                            stream.Write(msg, 0, msg.Length);

                            log_msg = "Schedule Ended.";
                            sysLog("userauth", String.Format("Faculty ID/Name: {0}:{1}, on Room ID/Name: {3}:{4}; {5}", faculty_id, faculty, faculty_level, room_id, room_name, log_msg), 64);
                            tleft_led_delay = 3; //reset

                            // add sfv points
                            FacultySFVPoints(faculty_id, 1);
                        }
                        else if (tleft.TotalMinutes < 10) {
                            if (tleft_led_delay <= 0) {
                                msg = Encoding.ASCII.GetBytes("f");
                                stream.Write(msg, 0, msg.Length);

                                tleft_led_delay = 2; //reset
                                log_msg = String.Format("Schedule is ending in -> {0} Minutes and {1} Seconds Left.", tleft.Minutes, tleft.Seconds);
                            }
                            else {
                                tleft_led_delay -= 1;
                            }
                        }
                    }

                    // Professor Verification
                    if (dev_verified && sfv_enable && dev_status == 3 && !data.Contains("UID:")) {
                        TimeSpan talarm = DateTime.Now - session_start;
                        if (sfv_time.TotalMinutes < talarm.TotalMinutes) {

                            if (alarm_led_delay <= 0) {
                                msg = Encoding.ASCII.GetBytes("x");
                                stream.Write(msg, 0, msg.Length);

                                alarm_led_delay = 3; //reset
                            }
                            else {
                                alarm_led_delay -= 1;
                            }
                            Console.WriteLine("Verification Alarm: {0} Mins", talarm.TotalMinutes);

                            if (talarm.TotalMinutes > (sfv_time.TotalMinutes + 5)) {
                                last_uid = "";
                                dev_status = 2;
                                UpdateDevStatus(dev_id, dev_ip, dev_status, last_uid);
                                msg = Encoding.ASCII.GetBytes("d");
                                stream.Write(msg, 0, msg.Length);
                                log_msg = "Classroom closed by the system.";
                                sysLog("userauth", String.Format("Faculty ID/Name: {0}:{1}, on Room ID/Name: {3}:{4}; {5}", faculty_id, faculty, faculty_level, room_id, room_name, log_msg), 64);
                                // add sfv points
                                FacultySFVPoints(faculty_id, 1);
                            }
                        }
                    }

                    // get ping status checked by pingClient class
                    if (dev_verified && dev_check_delay <= 0) {

                        cmd = conn.CreateCommand();
                        cmd.CommandText = pVariables.qDevPingCheck;
                        cmd.Parameters.Add("@p1", MySqlDbType.Int32).Value = dev_id;
                        conn.Open();
                        reader = cmd.ExecuteReader();

                        if (reader.Read()) {
                            Console.WriteLine("TCP Link check on Device: {0}, IP: {1} - OK.", dev_id, dev_ip);
                            dev_check_delay = 10;
                        }
                        else {
                            sysLog("dev", String.Format("Device ID/IP: {0}:{1}; Connection to the client has been lost.", dev_id, dev_ip), 16);
                            Console.WriteLine("System detected Broken TCP Link on Device ID: {0}, IP: {1} - Connection Lost.", dev_id, dev_ip);
                            break;
                        }
                        conn.Close();
                    }
                    else if (dev_verified && dev_check_delay > 0) {
                        dev_check_delay -= 1;
                    }

                    Thread.Sleep(1000);
                }
            }
            catch (Exception ex) {
                Console.WriteLine("Error: " + ex.Message);
                sysLog("sys", ex.Message, 16);
            }
            finally {
                // If we're in error (timeout, thread stopped...) close socket and return
                if (conn != null && conn.State == ConnectionState.Open) conn.Close();

                if (dev_verified) UpdateDevStatus(dev_id, dev_ip, 1, "-");

                if (clientSocket != null) {
                    clientSocket.Close();

                    Console.WriteLine("Client Connection Closed.");
                    sysLog("sys", "Client connection closed by the server.", 16);
                }
            }
        }

        private void UpdateDevStatus(int dev_id, string ip, int status, string lastuid) {
            if (conn.State == ConnectionState.Open) conn.Close();

            // Update the status and uptime
            cmd = conn.CreateCommand();
            cmd.CommandText = pVariables.qUpdateDevPing;
            cmd.Parameters.Add("@p1", MySqlDbType.Int32).Value = status;
            cmd.Parameters.Add("@p2", MySqlDbType.VarChar).Value = lastuid;
            cmd.Parameters.Add("@p3", MySqlDbType.VarChar).Value = ip;
            cmd.Parameters.Add("@p4", MySqlDbType.Int32).Value = dev_id;
            conn.Open();
            cmd.ExecuteNonQuery();
            conn.Close();
        }

        private void FacultySFVPoints(int fid, int val) {
            if (conn.State == ConnectionState.Open) conn.Close();

            cmd = conn.CreateCommand();
            cmd.CommandText = pVariables.qFacultSFV;
            cmd.Parameters.Add("@p1", MySqlDbType.Int32).Value = val;
            cmd.Parameters.Add("@p2", MySqlDbType.Int32).Value = fid;
            conn.Open();
            cmd.ExecuteNonQuery();
            conn.Close();
        }

    }
}

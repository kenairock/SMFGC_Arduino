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

namespace SMFGC
{
    class handleClient
    {
        TcpClient clientSocket;
        readonly MySqlConnection conn = new MySqlConnection(pVariables.sConn);
        MySqlCommand cmd;
        MySqlDataReader reader;

        Thread t;

        public void startClient(TcpClient inClientSocket)
        {
            this.clientSocket = inClientSocket;
            t = new Thread(doTasks);
            t.Start();
        }

        public void exitThread()
        {
            t.Abort();
        }

        private void doTasks()
        {
            // Buffer for reading data
            Byte[] buffer = new Byte[1024];
            String dev_ip = "", room_name = "", data = null, faculty = "";
            int dev_id = 0, room_id = 0, sched_id = 0, room_status = 0, next_tcp_check = 10, timeleft_led = 3; // <-- TL_LED Delay before send another command
            bool dev_verified = false, relay1 = false, relay2 = false;
            byte[] msg;

            DateTime end_time = DateTime.Parse("00:00:00");

            try
            {
                NetworkStream stream = clientSocket.GetStream();

                int i;
                while ((i = stream.Read(buffer, 0, buffer.Length)) != 0)
                {

                    // Translate data bytes to a ASCII string.
                    data = System.Text.Encoding.ASCII.GetString(buffer, 0, i);

                    if (!data.Contains("DEV:"))
                    {
                        Console.WriteLine("Unknown Device: \"{0}\" (Server Connection Closed)", data);
                        sysLog(0, "", "devinfo", "Unknown: (Sending data that doesn't known to server!) Device Denied.", 16);
                        break;
                    }

                    // Verify if the device is known to server.
                    if (!dev_verified)
                    {
                        dev_id = Convert.ToInt32(data.Split(',')[0].Replace("DEV:", ""));
                        dev_ip = ((IPEndPoint)clientSocket.Client.RemoteEndPoint).Address.ToString();

                        cmd = conn.CreateCommand();
                        cmd.CommandText = pVariables.qDeviceCheck;
                        cmd.Parameters.Add("@p1", MySqlDbType.Int32).Value = dev_id;
                        cmd.Parameters.Add("@p2", MySqlDbType.VarChar).Value = dev_ip;
                        conn.Open();
                        reader = cmd.ExecuteReader();

                        if (reader.Read())
                        {
                            room_id = Convert.ToInt32(reader["room_id"]);
                            room_name = reader["classroom"].ToString();
                            relay1 = Convert.ToBoolean((reader["relay1"]));
                            relay2 = Convert.ToBoolean((reader["relay2"]));
                            room_status = Convert.ToInt32((reader["status"]));
                            dev_verified = true;
                            Console.WriteLine("Device Accepted: {0}, {1}, Room: {2}:{3}", dev_id, dev_ip, room_id, room_name);
                            sysLog(dev_id, "", "devinfo", "Device Accepted.", 64);
                        }
                        conn.Close();

                        if (dev_verified)
                        {
                            // Update the status and uptime
                            RoomUpdateSingle(room_id, 2);
                        }
                        else
                        {
                            Console.WriteLine("Unknown Device: {0}, {1} (Server Connection Closed)", dev_id, dev_ip);
                            sysLog(dev_id, "", "devinfo", "Unknown: (Device is known but not registered to database!) Device Denied.", 16);
                            break;
                        }
                    }

                    // CONTINUES BLINKING RED 	- 10 MINUTES LEFT WARNING
                    if (dev_verified && room_status == 3 && !data.Contains("UID:"))
                    {
                        TimeSpan tdiff = end_time - DateTime.UtcNow;

                        if (tdiff.Minutes < 10)
                        {
                            // Send command to blink LEDs
                            if (tdiff.Minutes <= 0)
                            {
                                // Turn off all relays
                                msg = System.Text.Encoding.ASCII.GetBytes("d");
                                stream.Write(msg, 0, msg.Length);

                                room_status = 2;
                                RoomUpdateSingle(room_id, room_status);

                                sysLog(dev_id, "", "userauth", "Room: " + room_name + ", Faulty: " + faculty + ", Shedule Ended.", 64);
                                Console.WriteLine("Schedule ended on Room: {0} automatically.", room_name);
                            }
                            else
                            {
                                if (timeleft_led <= 0)
                                {
                                    msg = System.Text.Encoding.ASCII.GetBytes("f");
                                    stream.Write(msg, 0, msg.Length);

                                    timeleft_led = 3; //reset
                                    Console.WriteLine("Schedule on Room: {0} is ending in -> {1} Minutes and {2} Seconds Left.", room_name, tdiff.Minutes, tdiff.Seconds);
                                }
                                else
                                {
                                    timeleft_led -= 1;
                                }
                            }
                        }
                    }

                    // Process the data sent by the client.
                    if (dev_verified && data.Contains("UID:"))
                    {

                        string uidtag = data.Split(',')[1].Split(' ')[1];
                        string tag_type = data.Split(',')[2];

                        bool valid_uidtag = checkUidTagToDB(uidtag);

                        if (tag_type == "IN" && valid_uidtag)
                        {

                            // Tag is ok but check schedule
                            cmd = conn.CreateCommand();
                            cmd.CommandText = pVariables.qCheckSched;
                            cmd.Parameters.Add("@p1", MySqlDbType.Int32).Value = room_id;
                            conn.Open();
                            reader = cmd.ExecuteReader();

                            if (reader.Read())
                            {

                                sched_id = Convert.ToInt32(reader["sched_id"].ToString());
                                faculty = reader["faculty"].ToString();
                                end_time = DateTime.Parse(reader["end_time"].ToString());

                                if (relay1 && relay2)
                                {
                                    msg = System.Text.Encoding.ASCII.GetBytes("c");
                                }
                                else if (relay1 && !relay2)
                                {
                                    msg = System.Text.Encoding.ASCII.GetBytes("a");
                                }
                                else if (!relay1 && relay2)
                                {
                                    msg = System.Text.Encoding.ASCII.GetBytes("b");
                                }
                                else
                                {
                                    msg = System.Text.Encoding.ASCII.GetBytes("e");
                                }
                                // Send back a response.
                                stream.Write(msg, 0, msg.Length);
                                Console.WriteLine("Schedule started on Room: {0}:{1} with Schedule ID: {2}", room_id, room_name, sched_id);
                                sysLog(dev_id, uidtag, "userauth", "Room: " + room_name + ", Faulty: " + faculty + ", Logged in.", 64);
                                conn.Close();

                                room_status = 3;
                                RoomUpdateLastUID(room_id, uidtag, room_status);
                            }
                            else
                            {
                                // Send command to blink LEDs
                                msg = System.Text.Encoding.ASCII.GetBytes("f");
                                stream.Write(msg, 0, msg.Length);

                                Console.WriteLine("No schedule available on Room: {0}:{1}", room_id, room_name);
                                sysLog(dev_id, uidtag, "userauth", "Room: " + room_name + ", Faulty: " + faculty + ", No Schedule available.", 48);
                            }

                            conn.Close();
                        }
                        else if (tag_type == "OUT" && valid_uidtag)
                        {

                            // Check if LOGIN UID != LOGOUT UID
                            // getlast UID from Database
                            cmd = conn.CreateCommand();
                            cmd.CommandText = pVariables.qGetRoomUID;
                            cmd.Parameters.Add("@p1", MySqlDbType.VarChar).Value = dev_id;
                            cmd.Parameters.Add("@p2", MySqlDbType.VarChar).Value = uidtag;
                            conn.Open();
                            reader = cmd.ExecuteReader();

                            if (reader.Read())
                            {
                                // Turn off all relays
                                msg = System.Text.Encoding.ASCII.GetBytes("d");
                                stream.Write(msg, 0, msg.Length);

                                room_status = 2;
                                RoomUpdateSingle(room_id, room_status);
                                Console.WriteLine("Schedule ended on Room: {0}:{1} with Schedule ID: {2}", room_id, room_name, sched_id);
                                sysLog(dev_id, uidtag, "userauth", "Room: " + room_name + ", Faulty: " + faculty + ", Logged out.", 64);
                            }
                            else
                            {
                                // Send command to blink LEDs
                                msg = System.Text.Encoding.ASCII.GetBytes("g");
                                stream.Write(msg, 0, msg.Length);

                                sysLog(dev_id, uidtag, "userauth", "Room: " + room_name + ", Faulty: " + faculty + ", Login and Logout Tag are not same.", 48);
                            }
                            conn.Close();
                        }
                        else
                        {
                            // Send command to blink LEDs
                            msg = System.Text.Encoding.ASCII.GetBytes("x");
                            stream.Write(msg, 0, msg.Length);

                            Console.WriteLine("Unknown uidTag: {0}", uidtag);
                            sysLog(dev_id, uidtag, "userauth", "Unknown User [RFID Tag]: Denied by server.", 48);
                        }
                    }
                    else if (dev_verified && data.Contains("PZM:"))
                    {

                        if (data.Contains("NaN"))
                        {
                            sysLog(dev_id, "", "system", "Part Zone Expansion Module (PZEM) is not reading data.", 16);
                            Console.WriteLine("Error Reading PZEM Data.");
                        }
                        else
                        {
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
                    }
                    else if (dev_verified && data.Contains("RLY:"))
                    {
                        Console.WriteLine("Device acknowledge the command.");
                        sysLog(dev_id, "", "devinfo", "Client aknowledge the command given by the server.", 64);
                    }

                    // get ping status checked by pingClient class
                    if (dev_verified && next_tcp_check <= 0)
                    {

                        cmd = conn.CreateCommand();
                        cmd.CommandText = pVariables.qTCPBrokenCheck;
                        cmd.Parameters.Add("@p1", MySqlDbType.Int32).Value = dev_id;
                        conn.Open();
                        reader = cmd.ExecuteReader();

                        if (reader.Read())
                        {
                            Console.WriteLine("TCP Link check on Device: {0}, IP: {1} - OK.", dev_id, dev_ip);
                        }
                        else
                        {
                            RoomUpdateSingle(room_id, 1);
                            sysLog(dev_id, "", "devinfo", "Client connection lost.", 16);
                            Console.WriteLine("System detected Broken TCP Link on Device: {0}, IP: {1} - Connection Lost.", dev_id, dev_ip);
                            break;
                        }
                        conn.Close();
                        next_tcp_check = 10;
                    }
                    else if (dev_verified && next_tcp_check > 0)
                    {
                        next_tcp_check -= 1;
                    }

                    Thread.Sleep(1000);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.ToString());
            }
            finally
            {
                // If we're in error (timeout, thread stopped...) close socket and return

                if (conn.State == ConnectionState.Open) conn.Close();

                if (dev_verified) RoomUpdateSingle(room_id, 1);

                if (clientSocket != null)
                {
                    if (clientSocket.Connected) clientSocket.Close();
                    Console.WriteLine("Client Connection Closed.");
                    sysLog(0, "", "system", "Client connection closed by the server.", 16);
                }
            }
        }

        private bool checkUidTagToDB(string tag)
        {

            if (conn.State == ConnectionState.Open) conn.Close();

            bool ret = false;

            cmd = conn.CreateCommand();
            cmd.CommandText = pVariables.qUidTagCheck;
            cmd.Parameters.Add("@p1", MySqlDbType.VarChar).Value = tag;

            conn.Open();
            reader = cmd.ExecuteReader();
            if (reader.Read()) ret = true;
            conn.Close();

            return ret;
        }

        private void RoomUpdateSingle(int room_id, int status)
        {
            if (conn.State == ConnectionState.Open) conn.Close();

            // Update the status and uptime
            cmd = conn.CreateCommand();
            cmd.CommandText = pVariables.qRoomUpdateSingle;
            cmd.Parameters.Add("@p1", MySqlDbType.Int32).Value = status;
            cmd.Parameters.Add("@p2", MySqlDbType.Int32).Value = room_id;
            conn.Open();
            cmd.ExecuteNonQuery();
            conn.Close();
        }

        private void RoomUpdateLastUID(int room_id, string uidtag, int status)
        {
            if (conn.State == ConnectionState.Open) conn.Close();

            // Update room last_uidtag
            cmd = conn.CreateCommand();
            cmd.CommandText = pVariables.qRoomUpdateUID;
            cmd.Parameters.Add("@p1", MySqlDbType.VarChar).Value = uidtag;
            cmd.Parameters.Add("@p2", MySqlDbType.Int32).Value = status;
            cmd.Parameters.Add("@p3", MySqlDbType.Int32).Value = room_id;
            conn.Open();
            cmd.ExecuteNonQuery();
            conn.Close();
        }
    }
}

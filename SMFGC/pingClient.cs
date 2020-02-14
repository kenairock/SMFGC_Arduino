using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SMFGC {
    class pingClient {
        readonly MySqlConnection conn = new MySqlConnection(pVariables.sConn);
        MySqlCommand cmd;
        MySqlDataReader reader;

        Thread t;

        Ping pingSender = new Ping();
        PingOptions options = new PingOptions();

        List<string> ips = new List<string>();

        public void startPing() {
            this.options.DontFragment = true;

            try {
                cmd = conn.CreateCommand();
                cmd.CommandText = pVariables.qDevices;
                conn.Open();
                reader = cmd.ExecuteReader();

                while (reader.Read()) ips.Add(reader["ip_addr"].ToString());

                t = new Thread(doTask);
                t.Priority = ThreadPriority.BelowNormal;
                t.Start();
            }
            catch (Exception ex) {
                throw new ArgumentException(ex.Message);
            }
            finally {
                if (conn.State == ConnectionState.Open) conn.Close();
            }

        }

        public void exitThread() {
            t.Abort();
        }

        private void doTask() {
            try {
                PingReply reply;

                while (true) {

                    // Clear all previous ipaddresses
                    ips.Clear();

                    // Query List of ipaddress
                    cmd = conn.CreateCommand();
                    cmd.CommandText = pVariables.qDevices;
                    conn.Open();
                    reader = cmd.ExecuteReader();
                    while (reader.Read()) ips.Add(reader["ip_addr"].ToString());
                    conn.Close();

                    foreach (string ip_addr in ips) {
                        Console.Write("Pinging Client: {0} -> ", ip_addr);

                        reply = pingSender.Send(ip_addr, 30, Encoding.ASCII.GetBytes("1"), options);
                        Console.WriteLine(reply.Status);

                        cmd = conn.CreateCommand();
                        cmd.CommandText = pVariables.qUpdateDevPing_IP;

                        if (reply.Status == IPStatus.Success) {
                            cmd.Parameters.Add("@p1", MySqlDbType.Int32).Value = 1;
                        }
                        else {
                            cmd.Parameters.Add("@p1", MySqlDbType.Int32).Value = 0;
                        }

                        cmd.Parameters.Add("@p2", MySqlDbType.VarChar).Value = ip_addr;
                        conn.Open();
                        cmd.ExecuteNonQuery();
                        conn.Close();

                        Thread.Sleep(3000); // Delay between client pings
                    }
                    Thread.Sleep(15000); // Task Delay
                }
            }
            catch (Exception ex) {
                throw new ArgumentException(ex.Message);
            }
        }
    }
}

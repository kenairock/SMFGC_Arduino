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
        PingOptions options = new PingOptions(64, true);
        PingReply reply;

        int timeout = 500;
        byte[] buffer = new byte[32];

        List<string> ips = new List<string>();

        public void startPing() {
            try {
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
            if (conn.State == ConnectionState.Open) conn.Close();
            t.Abort();
        }

        private void doTask() {
            while (true) {

                // Clear all previous ipaddress
                ips.Clear();

                // Query new list of ipaddress
                cmd = conn.CreateCommand();
                cmd.CommandText = pVariables.qDeviceIPs;
                conn.Open();
                reader = cmd.ExecuteReader();
                while (reader.Read()) ips.Add(reader["ip_addr"].ToString());
                conn.Close();

                foreach (string ip_addr in ips) {
                   
                    reply = pingSender.Send(ip_addr, timeout, buffer, options);

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

                    Console.WriteLine("Pinging Client: {0} -> {1}", ip_addr, reply.Status);

                    Thread.Sleep(1000); // Delay between client pings
                }

                Thread.Sleep(15000); // Task Delay
            }

        }
    }
}

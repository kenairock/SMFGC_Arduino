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
            cmd = conn.CreateCommand();
            cmd.CommandText = pVariables.qRoomPing;

            try {
                conn.Open();
                reader = cmd.ExecuteReader();

                while (reader.Read()) ips.Add(reader["ip_add"].ToString());

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
                while (true) {

                    foreach (string ipadd in ips) {
                        Console.Write("Ping Client: {0} - ", ipadd);

                        PingReply reply = pingSender.Send(ipadd, 15, Encoding.ASCII.GetBytes("."), options);
                        Console.WriteLine(reply.Status);

                        cmd = conn.CreateCommand();
                        cmd.CommandText = pVariables.qRoomUpdateStatus;

                        if (reply.Status == IPStatus.Success) {
                            cmd.Parameters.Add("@p1", MySqlDbType.Int32).Value = 1;
                        }
                        else {
                            cmd.Parameters.Add("@p1", MySqlDbType.Int32).Value = 0;
                        }

                        cmd.Parameters.Add("@p2", MySqlDbType.VarChar).Value = ipadd;
                        conn.Open();
                        cmd.ExecuteNonQuery();
                        conn.Close();

                        Thread.Sleep(3000);
                    }

                    Thread.Sleep(10000);
                }

            }
            catch (Exception ex) {
                throw new ArgumentException(ex.Message);
            }
        }
    }
}

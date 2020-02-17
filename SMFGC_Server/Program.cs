using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SMFGC_Server {
    class Program {
        static int port = 2316;
        static bool exitApp = false, ping_output = false;
        static string tformat = "HH:mm:ss";

        static DateTime time_start = DateTime.Now;

        static TcpListener serverSocket;
        static Thread th_server;
        static Thread th_pinger;
        static int client_count = 0;

        private const int MF_BYCOMMAND = 0x00000000;
        public const int SC_CLOSE = 0xF060;

        [DllImport("user32.dll")]
        public static extern int DeleteMenu(IntPtr hMenu, int nPosition, int wFlags);

        [DllImport("user32.dll")]
        private static extern IntPtr GetSystemMenu(IntPtr hWnd, bool bRevert);

        [DllImport("kernel32.dll", ExactSpelling = true)]
        private static extern IntPtr GetConsoleWindow();

        static void Main(string[] args) {
            var attributes = typeof(Program).GetTypeInfo().Assembly.GetCustomAttributes(typeof(AssemblyTitleAttribute));
            var assemblyTitleAttribute = attributes.SingleOrDefault() as AssemblyTitleAttribute;

            Console.Title = assemblyTitleAttribute?.Title + " - " + Environment.Version;

            string lfile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs") + @"\latest.log";
            if (File.Exists(lfile)) {
                string fdate = File.GetLastWriteTime(lfile).ToString("yyyy-MM-dd-HH-mm-ss");
                string newname = lfile.Replace("latest", fdate);
                File.Move(lfile, newname);
                string zip = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs") + @"\"+ fdate + ".zip";
                using (ZipArchive zipArchive = ZipFile.Open(zip, ZipArchiveMode.Create)) {
                    zipArchive.CreateEntryFromFile(newname, fdate + ".log");
                }
                File.Delete(newname);
            }

            Output.WriteLine(string.Format("[{0}] [main/{1}]: Running on OS Ver.: {2} {3}", DateTime.Now.ToString(tformat), "INFO", Environment.OSVersion, (Environment.Is64BitOperatingSystem ? "64bit" : "32bit")));
            Output.WriteLine(string.Format("[{0}] [main/{1}]: User/Machine Name: {2}/{3}", DateTime.Now.ToString(tformat), "INFO", Environment.UserName, Environment.MachineName));
            Output.WriteLine(string.Format("[{0}] [main/{1}]: Starting server", DateTime.Now.ToString(tformat), "INFO"));
            Thread.Sleep(1000);

            DeleteMenu(GetSystemMenu(GetConsoleWindow(), false), SC_CLOSE, MF_BYCOMMAND);

            Output.WriteLine(string.Format("[{0}] [main/{1}]: Loading settings", DateTime.Now.ToString(tformat), "INFO"));
            Output.WriteLine(string.Format("[{0}] [main/{1}]: Client ping result info: {2}", DateTime.Now.ToString(tformat), "INFO", ((ping_output) ? "ENABLED" : "DISABLED")));


            Output.WriteLine(string.Format("[{0}] [main/{1}]: Starting server on ip/port [*:{2}]", DateTime.Now.ToString(tformat), "INFO", port));
            // Evaluate current system tcp connections. This is the same information provided
            // by the netstat command line application, just in .Net strongly-typed object
            // form.  We will look through the list, and if our port we would like to use
            // in our TcpClient is occupied, we will set exitApp to true.
            IPGlobalProperties ipGlobalProperties = IPGlobalProperties.GetIPGlobalProperties();
            IPEndPoint[] tcpConnInfoArray = ipGlobalProperties.GetActiveTcpListeners();

            foreach (IPEndPoint endpoint in tcpConnInfoArray) {
                if (endpoint.Port == port) {
                    //not available
                    exitApp = true;

                    Output.WriteLine(string.Format("[{0}] [main/{1}]: **** FAILED TO BIND TO PORT! : {2}", DateTime.Now.ToString(tformat), "ERROR", port));
                    Output.WriteLine(string.Format("[{0}] [main/{1}]: Perhaps a server is already running on that port?", DateTime.Now.ToString(tformat), "ERROR"));
                    Thread.Sleep(1000);
                    break;
                }
            }

            if (!exitApp) {
                // port is available! start listening to the clients
                try {
                    serverSocket = new TcpListener(IPAddress.Any, port);
                    serverSocket.Start();

                    // start accepting clients
                    th_server = new Thread(clientListener);
                    th_server.Priority = ThreadPriority.Normal;
                    th_server.Start();
                }
                catch (Exception ex) {
                    Output.WriteLine(string.Format("[{0}] [Listener Thread/{1}]: {2}", DateTime.Now.ToString(tformat), "ERROR", ex.Message));
                    exitApp = true;
                }
            }

            if (!exitApp) {
                // check database connection
                MySqlConnection conn = new MySqlConnection(SMFGC.pVariables.sConn);
                try {
                    Output.WriteLine(string.Format("[{0}] [Database/{1}]: Checking...", DateTime.Now.ToString(tformat), "INFO"));
                    conn.Open();
                    if (conn.State == ConnectionState.Open) Output.WriteLine(string.Format("[{0}] [Database/{1}]: Connection sucessfull.", DateTime.Now.ToString(tformat), "INFO"));
                    conn.Close();
                    sysLog(conn, "sys", "Server started.", 64);
                }
                catch (Exception ex) {
                    Output.WriteLine(string.Format("[{0}] [Database/{1}]: {2}", DateTime.Now.ToString(tformat), "ERROR", ex.Message));
                }
                finally {
                    if (conn.State == ConnectionState.Open) conn.Close();
                }
            }

            if (!exitApp) {
                // database is available! start pinging the clients
                try {
                    th_pinger = new Thread(clientPinger);
                    th_pinger.Priority = ThreadPriority.BelowNormal;
                    th_pinger.Start();
                }
                catch (Exception ex) {
                    Output.WriteLine(string.Format("[{0}] [Pinger Thread/{1}]: {2}", DateTime.Now.ToString(tformat), "ERROR", ex.Message));
                    exitApp = true;
                }
            }

            if (!exitApp) {
                TimeSpan tspan = DateTime.Now - time_start;
                Output.WriteLine(string.Format("[{0}] [main/{1}]: Done({2}s)! For help, type \"/help\"", DateTime.Now.ToString(tformat), "INFO", tspan.TotalSeconds.ToString("n")));
                Output.WriteLine(string.Format("[{0}] [main/{1}]: Timings Reset", DateTime.Now.ToString(tformat), "INFO"));
                time_start = DateTime.Now;
            }

            while (!exitApp) {
                switch (Console.ReadLine()) {
                    case "stop":
                        exitApp = true;

                        Output.WriteLine(string.Format("[{0}] [main/{1}]: Stopping the server...", DateTime.Now.ToString(tformat), "INFO"));
                        Output.WriteLine(string.Format("[{0}] [Shutdown Thread/{1}]: Closing listener on [0:0:0:0:0:0:0:0:{2}]", DateTime.Now.ToString(tformat), "INFO", port));
                        serverSocket.Stop();

                        Output.WriteLine(string.Format("[{0}] [Shutdown Thread/{1}]: Closing pending connections", DateTime.Now.ToString(tformat), "INFO"));
                        Output.WriteLine(string.Format("[{0}] [Shutdown Thread/{1}]: Disconnecting {2} connections", DateTime.Now.ToString(tformat), "INFO", client_count));
                        th_server.Abort();
                        th_pinger.Abort();

                        MySqlConnection conn = new MySqlConnection(SMFGC.pVariables.sConn);
                        sysLog(conn, "sys", "Server stopped.", 64);

                        break;

                    case "help":
                        Output.WriteLine(string.Format("[{0}] [main/{1}]: ================[Commands Help]================", DateTime.Now.ToString(tformat), "INFO"));
                        Output.WriteLine(string.Format("[{0}] [main/{1}]:  ", DateTime.Now.ToString(tformat), "INFO"));

                        Output.WriteLine(string.Format("[{0}] [main/{1}]:  /clear (clean output window)", DateTime.Now.ToString(tformat), "INFO"));
                        Output.WriteLine(string.Format("[{0}] [main/{1}]:  /ping (enable and disable ping result output)", DateTime.Now.ToString(tformat), "INFO"));
                        Output.WriteLine(string.Format("[{0}] [main/{1}]:  /stop (stop and exit the server)", DateTime.Now.ToString(tformat), "INFO"));

                        Output.WriteLine(string.Format("[{0}] [main/{1}]:  ", DateTime.Now.ToString(tformat), "INFO"));
                        Output.WriteLine(string.Format("[{0}] [main/{1}]: ===============================================", DateTime.Now.ToString(tformat), "INFO"));
                        break;

                    case "ping":
                        ping_output = (ping_output) ? false : true;
                        Output.WriteLine(string.Format("[{0}] [main/{1}]: Client ping result info: {2}", DateTime.Now.ToString(tformat), "INFO", (ping_output ? "ENABLED" : "DISABLED")));
                        break;

                    case "clear":
                        Console.Clear();
                        Output.WriteLine(string.Format("[{0}] [main/{1}]: Cleared.", DateTime.Now.ToString(tformat), "INFO"));
                        break;

                    default:
                        Output.WriteLine(string.Format("[{0}] [main/{1}]: Unknown command: Use \"help\" to show list of commands.", DateTime.Now.ToString(tformat), "WARN"));
                        break;
                }
            }

            TimeSpan uptime = DateTime.Now - time_start;
            Output.WriteLine(string.Format("[{0}] [main/{1}]: Uptime: {2} Days, {3} Hours, {4} Minutes and {5} Seconds",
                DateTime.Now.ToString(tformat), "INFO", (int)uptime.TotalDays, (int)uptime.Hours, (int)uptime.Minutes, (int)uptime.Seconds));


            Output.WriteLine(string.Format("[{0}] [Shutdown Thread/{1}]: Closing IO threads...", DateTime.Now.ToString(tformat), "INFO"));
            Thread.Sleep(1000);
            Output.WriteLine(string.Format("[{0}] [Shutdown Thread/{1}]: Thank you and goodbye", DateTime.Now.ToString(tformat), "INFO"));
            Thread.Sleep(5000);
        }

        private static void clientListener() {
            TcpClient cl = default(TcpClient);
            string ip, port;

            Output.WriteLine(string.Format("[{0}] [Listener Thread/{1}]: Started.", DateTime.Now.ToString(tformat), "INFO"));

            while (true) {
                cl = serverSocket.AcceptTcpClient();

                client_count += 1;
                handleClient hc = new handleClient();
                hc.startClient(cl, client_count);

                ip = ((IPEndPoint)cl.Client.RemoteEndPoint).Address.ToString();
                port = ((IPEndPoint)cl.Client.RemoteEndPoint).Port.ToString();

                Output.WriteLine(string.Format("[{0}] [Handle Client #{1}/{2}]: IP Address of client is {3} connecting...", DateTime.Now.ToString(tformat), client_count, "INFO", ip));
                Thread.Sleep(100);

                Output.WriteLine(string.Format("[{0}] [Listener Thread/{1}]: Client #{2}[{3}:{4}] connected.", DateTime.Now.ToString(tformat), "INFO", client_count, ip, port));
                Thread.Sleep(1000);
            }
        }

        private static void clientPinger() {
            MySqlConnection conn = new MySqlConnection(SMFGC.pVariables.sConn);
            MySqlCommand cmd;
            MySqlDataReader reader;

            Ping pingSender = new Ping();
            PingOptions options = new PingOptions(64, true);
            PingReply reply;

            int timeout = 500;
            byte[] buffer = new byte[32];

            List<string> ips = new List<string>();
            Output.WriteLine(string.Format("[{0}] [Pinger Thread/{1}]: Started.", DateTime.Now.ToString(tformat), "INFO"));

            while (true) {
                // Clear all previous ipaddress
                ips.Clear();

                // Query new list of ipaddress
                cmd = conn.CreateCommand();
                cmd.CommandText = SMFGC.pVariables.qDeviceIPs;
                conn.Open();
                reader = cmd.ExecuteReader();
                while (reader.Read()) ips.Add(reader["ip_addr"].ToString());
                conn.Close();

                foreach (string ip_addr in ips) {

                    reply = pingSender.Send(ip_addr, timeout, buffer, options);

                    cmd = conn.CreateCommand();
                    cmd.CommandText = SMFGC.pVariables.qUpdateDevPing_IP;

                    if (reply.Status == IPStatus.Success) {
                        cmd.Parameters.Add("@p1", MySqlDbType.Int32).Value = 1;
                        if (ping_output) Output.WriteLine(string.Format("[{0}] [Pinger Thread/{1}]: Client: {2} -> {3}", DateTime.Now.ToString(tformat), "INFO", ip_addr, reply.Status));
                    }
                    else {
                        cmd.Parameters.Add("@p1", MySqlDbType.Int32).Value = 0;
                        if (ping_output) Output.WriteLine(string.Format("[{0}] [Pinger Thread/{1}]: Client: {2} -> {3}", DateTime.Now.ToString(tformat), "WARN", ip_addr, reply.Status));
                    }

                    cmd.Parameters.Add("@p2", MySqlDbType.VarChar).Value = ip_addr;
                    conn.Open();
                    cmd.ExecuteNonQuery();
                    conn.Close();

                    Thread.Sleep(1000); // Delay between client pings
                }

                Thread.Sleep(15000); // Task Delay                
            }

        }

        public static void sysLog(MySqlConnection conn, string process, string message, int alert) {
            //Error   16
            //The message box contains a symbol consisting of white X in a circle with a red background.

            //Information     64
            //The message box contains a symbol consisting of a lowercase letter i in a circle.

            //None    0
            //The message box contains no symbols.

            //Question    32
            //The message box contains a symbol consisting of a question mark in a circle. The question mark message icon is no longer recommended because it does not clearly represent a specific type of message and because the phrasing of a message as a question could apply to any message type.In addition, users can confuse the question mark symbol with a help information symbol.Therefore, do not use this question mark symbol in your message boxes.The system continues to support its inclusion only for backward compatibility.

            //Warning     48
            //The message box contains a symbol consisting of an exclamation point in a triangle with a yellow background.

            try {
                if (conn != null && conn.State == ConnectionState.Open) conn.Close();

                MySqlCommand cmd = conn.CreateCommand();
                cmd.CommandText = SMFGC.pVariables.qLogger;
                cmd.Parameters.Add("@p1", MySqlDbType.VarChar).Value = process;
                cmd.Parameters.Add("@p2", MySqlDbType.Int32).Value = alert;
                cmd.Parameters.Add("@p3", MySqlDbType.VarChar).Value = message;
                conn.Open();
                cmd.ExecuteNonQuery();
                conn.Close();
            }
            catch (Exception ex) {
                Output.WriteLine(string.Format("[{0}] [Logger Thread/{1}]: ", DateTime.Now.ToString(tformat), "ERROR", ex.Message));
            }
        }
    }
}

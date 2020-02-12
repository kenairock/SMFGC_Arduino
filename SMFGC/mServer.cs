using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SMFGC {
    class mServer {
        TcpListener serverSocket = new TcpListener(IPAddress.Any, pVariables.server_port);
        TcpClient cl = default(TcpClient);

        Thread t;
        public void startServer() {
            try {
                t = new Thread(doTask);
                t.Priority = ThreadPriority.Normal;
                t.Start();
            }
            catch (Exception ex) {
                throw new ArgumentException(ex.Message);
            }
            finally {
                // Stop listening for new clients.
                serverSocket.Stop();
            }
        }

        public void exitThread() {
            serverSocket.Stop();
            t.Abort();
        }

        private void doTask() {
            try {
                serverSocket.Start();

                while (true) {
                    this.cl = serverSocket.AcceptTcpClient();

                    handleClient hc = new handleClient();
                    hc.startClient(this.cl);

                    Console.WriteLine("Client IP: {0} - Connected!", ((IPEndPoint)this.cl.Client.RemoteEndPoint).Address.ToString());

                    Thread.Sleep(1000);
                }
            }
            catch (Exception ex) {
                throw new ArgumentException(ex.Message);
            }
        }
    }
}

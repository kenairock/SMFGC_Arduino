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
        TcpClient client = default(TcpClient);

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
                // Stop listening 
                serverSocket.Stop();
            }
        }

        public void exitThread() {
            serverSocket.Stop();
            t.Abort();
        }

        private void doTask() {

            serverSocket.Start();

            while (true) {
                client = serverSocket.AcceptTcpClient();

                handleClient hc = new handleClient();
                hc.startClient(client);

                //Debug.WriteLine("Client IP: {0} - Listened by the server!", ((IPEndPoint)this.cl.Client.RemoteEndPoint).Address.ToString());

                Thread.Sleep(1000);
            }
        }
    }
}

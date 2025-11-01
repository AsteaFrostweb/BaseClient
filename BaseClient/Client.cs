using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace BaseClient
{
    internal class Client
    {
        TcpClient tcpClient;

        public Client() 
        {
            Main();
        }

        public void Main()
        {
            if (Connect("127.0.0.1", 58585))
            {
                bool isRunning = true;
                while (isRunning) 
                {
                    string message = Console.ReadLine();
                    if (message == "quit")
                        isRunning = false;
                    else
                        SendMessage(message);
                }
            }
            else 
            {
                Console.WriteLine("Unable to connect to server applicaiton");
            }


        }

        public bool Connect(string ip, int port) 
        {
            Console.WriteLine("Connecting to server");
            tcpClient = new TcpClient();
                      

            try
            {
                tcpClient.Connect(ip, port);
                return true;
            }
            catch 
            {
                return false;                
            }
        }

        public void Disconnect() 
        {

            if (tcpClient != null && tcpClient.Connected) 
            {
                Console.WriteLine("Disconnecting from server");
                tcpClient.Close();
            }
        }

        private void SendMessage(string msg) 
        {
            if (tcpClient != null && tcpClient.Connected) 
            {
                NetworkStream ns = tcpClient.GetStream();
                byte[] packet = new byte[1024];
                byte[] msgBytes = Encoding.UTF8.GetBytes(msg);

                msgBytes.CopyTo(packet, 0);
                ns.Write(packet);
            }
        }
    }
}

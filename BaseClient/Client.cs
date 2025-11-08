using BSCShared.Packets;
using System;
using System.Net.Security;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using BSCShared;

namespace BaseClient
{
    internal class Client
    {
        public TcpClient tcpClient { get; private set; }
        public SslStream sslStream { get; private set; }
        public CancellationTokenSource Cts { get; private set; }

        public bool isConnected => tcpClient.Connected;

        public Client() 
        {
            Debugging.whitelist.Add("Client");
        }

        public async Task RunAsync()
        {
            Cts = new CancellationTokenSource();
            while (!Cts.IsCancellationRequested)
            {
                if (await ConnectAsync("127.0.0.1", 58585))
                {
                    _ = Task.Run(() => PingPongPacket.Pulse(sslStream, 5, Cts));
                    _ = Task.Run(() => ClientPacketHandler.HandleIncomingPackets(this));
                    Console.WriteLine("Connected! Type 'quit' to exit.");

                    while (!Cts.IsCancellationRequested)
                    {
                        string message = Console.ReadLine();
                        if (string.IsNullOrEmpty(message)) continue;
                        string[] args = message.Split(' ');
                        switch (args[0]) 
                        {
                            case "msg":
                                await SendMessageAsync(args[1]);
                                break;
                            case "notitest":
                                await TestNotify();
                                break;
                                
                            case "quit":
                                Cts.Cancel();
                                continue;
                            
                        }                        
                    }

                    await Disconnect();
                }
                else
                {
                    Console.WriteLine("Unable to connect to server application. Press enter to retry.");
                }
            }
        }

        private async Task<bool> ConnectAsync(string ip, int port)
        {
            try
            {
                tcpClient = new TcpClient();
                await tcpClient.ConnectAsync(ip, port);

                sslStream = new SslStream(tcpClient.GetStream(), false,
                    (sender, certificate, chain, errors) => true); // trust cert for dev

                await sslStream.AuthenticateAsClientAsync("BaseServer"); // Must match CN
                Console.WriteLine("TLS handshake complete!");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Connection error: {ex.Message}");
                return false;
            }
        }

        private async Task Disconnect()
        {
            try
            {
                await sslStream.DisposeAsync();
                tcpClient.Dispose();          
                Console.WriteLine("Disconnected from server.");
            }
            catch { }
        }

        private async Task TestNotify() 
        {
            Debugging.Log("Client", "Testing notification functionality:");

            Debugging.Log("Client", "Creating entity...");
            Entity e = new Entity();
            if (e != null)
                Debugging.Log("Client", "Entity created.");
            else
                return;

            //Debugging.Log("Client", "Starting response awaiter before sending request...");
            //_ = Task.Run(() => TestAwaitNotification(e));

            Debugging.Log("Client", "Attempting to send NotitestPacket to sever...");
            await new NotitestPacket(e.UID).WriteToStreamAsync(sslStream);
            Debugging.Log("Client", "NotitestPacket send.");

            await e.AwaitNotification("notitest");
            Debugging.Log("Client", "Entity notified with tag \"notitest\" succesfully");

        }
        private async Task TestAwaitNotification(Entity e)
        {
            
        }

        private async Task SendMessageAsync(string message)
        {
            if (tcpClient?.Connected ?? false)
            {
                // Create and send packet
                MessagePacket msgPacket = new MessagePacket(message);
                Console.WriteLine("Message packet created: " + message);

                await msgPacket.WriteToStreamAsync(sslStream);
                Console.WriteLine("Message packet written to stream");
            }
        }
    }
}

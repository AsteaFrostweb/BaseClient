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
        private TcpClient tcpClient;
        private SslStream sslStream;
        private CancellationTokenSource Cts;

        public Client() 
        {
            Debugging.whitelist.Add("Client");
        }

        public async Task RunAsync()
        {
            Cts = new CancellationTokenSource();    

            if (await ConnectAsync("127.0.0.1", 58585))
            {
                _ = Task.Run(() => PingPongPacket.Pulse(sslStream, 1, Cts));
                Console.WriteLine("Connected! Type 'quit' to exit.");

                while (!Cts.IsCancellationRequested)
                {
                    string message = Console.ReadLine();
                    if (string.IsNullOrEmpty(message)) continue;

                    if (message.Equals("quit", StringComparison.OrdinalIgnoreCase))
                        Cts.Cancel();
                    else
                        await SendMessageAsync(message);
                }

                await Disconnect();
            }
            else
            {
                Console.WriteLine("Unable to connect to server application.");
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

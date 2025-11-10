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
        public CancellationTokenSource clientCTS { get; private set; }
        public CancellationTokenSource connectionCTS { get; private set; }
        public bool isConnected => tcpClient != null && tcpClient.Connected;

        public string IP { get; private set; }
        public int Port { get; private set; }

        public Client(string ip, int port) 
        {
            Debugging.whitelist.Add("Client");
            IP = ip;
            Port = port;
        }

        public async Task RunAsync()
        {
            clientCTS = new CancellationTokenSource();

            await InitializeConnection(IP, Port);

            while (!clientCTS.IsCancellationRequested)
            {
                string message = Console.ReadLine();
                if (string.IsNullOrEmpty(message)) continue;
                string[] args = message.Split(' ');
                switch (args[0]) 
                {
                    case "msg":
                        await Msg(args);
                        break;
                    case "notitest":
                        await TestNotify();
                        break;
                    case "connect":
                        await InitializeConnection(IP, Port);
                        break;
                    case "disconnect":
                        await Disconnect();
                        break;                   
                    case "quit":
                        clientCTS.Cancel();
                        continue;
                            
                }                        
            }
            await Disconnect();           
               
            
        }

        private async Task MonitorConnection() 
        {
            while (isConnected && !connectionCTS.IsCancellationRequested && !clientCTS.IsCancellationRequested) 
            {
                await Task.Delay(1000);
            }
            if (connectionCTS != null && !connectionCTS.IsCancellationRequested)
            {
                connectionCTS.Cancel();
                Debugging.Log("Client", "ConnectionMonitor: closing collection.");
            }


        }
        private async Task InitializeConnection(string ip, int port) 
        {
            if (isConnected)
            {
                Debugging.Log("Client", "Already connected to the server!");
            }


            connectionCTS = new CancellationTokenSource();
            Debugging.Log("Client", "Attempting initital connection to server");
            await ConnectAsync(ip, port);
            if (isConnected)
            {
                _ = Task.Run(() => PingPongPacket.Pulse(sslStream, 5, connectionCTS));
                _ = Task.Run(() => ClientPacketHandler.HandleIncomingPackets(this));
                _ = Task.Run(() => MonitorConnection());
                Console.WriteLine("Connected! Type 'quit' to exit.");
            }
            else 
            {
                Debugging.Log("Client", "Unable to connect to server. Try reconnect with command: connect");
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
            if (!isConnected)
            {
                Debugging.Log("Client", "Cannot disconnect. Client is not connected to server.");
                return;
            }
            try
            {                
                await connectionCTS.CancelAsync();
                await sslStream.DisposeAsync();
                tcpClient.Dispose();
                Debugging.Log("Client", "Disconnected from server.");
            }
            catch 
            {
                Debugging.Log("Client", "Error disconnecting from server.");
            }
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

        private async Task Msg(string[] args)
        {
            if (args.Length < 2)
            {
                Debugging.Log("Client", "Parameter error. Syntax: msg [message]");
            }
            await SendMessageAsync(args[1]);
        }
        private async Task SendMessageAsync(string message)
        {
            if (isConnected)
            {
                // Create and send packet
                MessagePacket msgPacket = new MessagePacket(message);
               

                await msgPacket.WriteToStreamAsync(sslStream);
                Debugging.Log("Client", $"Message: '{message}' send to server.");
            }
            else
                Debugging.Log("Client", "Unable to send message as client is disconnected from server");
        }



    }
}

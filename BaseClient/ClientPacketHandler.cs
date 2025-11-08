using BaseClient;
using BSCShared;
using BSCShared.Packets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Security;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace BaseClient
{
    public static class ClientPacketHandler
    {       

        internal static async Task HandleIncomingPackets(Client client) 
        {
            while (!client.Cts.IsCancellationRequested)
            {

                if (!(client.tcpClient != null && client.tcpClient.Connected))
                    break;
                Packet p = await Packet.ReadFromStreamAsync(client.sslStream);
                if (p != null)
                    await HandlePacket(p);
            }
        }

        public static async Task HandlePacket(Packet packet)
        {
            switch (packet)
            {
                case MessagePacket msgPacket:
                    Debugging.Log("Client", $"Message from server: {msgPacket.message}");
                    break;
               
                case NotitestPacket notitestPacket:
                    HandleNotitestPacket(notitestPacket);
                    break;
            }
        }


        private static void HandleNotitestPacket(NotitestPacket packet) 
        {
            Debugging.Log("Client", $"Notitest packet received.");
            Entity.Notify(packet.entityUID, "notitest", Array.Empty<object>());
            Debugging.Log("Client", $"Notified entity:{packet.entityUID} of notitest.");
        }
     
    }
}

using KMP;
using KMP.Networking;
using KMP.Networking.Packets;
using KMP.Networking.Transport;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace NetworkTesting
{
    class Program
    {
        static void Main(string[] args)
        {
            Log.MinLogLevel = Log.LogLevels.Error;

            if (args.Length > 0 && args[0] == "-server")
            {
                Console.WriteLine("[Server] Starting...");
                KMPCommon.Side = PacketSide.Server;
                NetworkConnectionListener listener = new NetworkConnectionListener(
                NetworkServerPreferences.CreateTCP("0.0.0.0", 1000), false);
                // Kill server to quit thing
                Console.WriteLine("[Server] Listening on {0}", listener.Preferences.BindingAddress);
                new Thread(ServerHandler) { IsBackground = false }.Start(listener);
            }else{
                // Start server in background
                Console.WriteLine("[NetworkTest] Creating new instance of server in seperate AppDomain");
                // Server has to be started in another app domain because of KSPCommon.Side
                // The seperate app domain makes the programs run completely independent, but in the same CLR instance.
                var domain = AppDomain.CreateDomain("ServerDomain");
                domain.ExecuteAssembly("NetworkTesting.exe", new String[]{ "-server"} );
                // Create client
                Console.WriteLine("[Client] Connecting to server...");
                NetworkConnection connection = new NetworkConnection(
                    new TCPTransport(
                        new System.Net.Sockets.TcpClient("localhost", 1000)));
                connection.PacketArrived += connection_client_PacketArrived;
                connection.SendPacket(NetworkHelper.ClientHandshake("TestAccount", Guid.Empty, "TestClient"));
            }            
        }

        static void connection_client_PacketArrived(NetworkConnection connection, KMP.Networking.Packets.AbstractPacket packet)
        {
            if (packet is HandshakePacket)
            {
                var handshake = packet as HandshakePacket;
                if (handshake.Accepted)
                {
                    Console.WriteLine("[Client] Connection accepted!");
                }
                else
                {
                    Console.WriteLine("[Client] Connection rejected:\r\n\tReason: {0}", handshake.RejectReason);
                }
            }
        }

        static void ServerHandler(object srvObj)
        {
            NetworkConnectionListener listener = srvObj as NetworkConnectionListener;
            while (true)
            {
                if (listener.Pending)
                {
                    var connection = listener.Accept();
                    new Thread(ClientHandler) { IsBackground = true }.Start(connection);
                }
                else
                {
                    Thread.Sleep(100);
                }
            }
        }

        /* Server per client handler */
        static void ClientHandler(object conn)
        {
            var connection = conn as NetworkConnection;
            connection.PacketArrived += connection_PacketArrived;
        }

        static void connection_PacketArrived(NetworkConnection connection, KMP.Networking.Packets.AbstractPacket packet)
        {
            Console.WriteLine("[Server] Packet arrived '{0}'", packet.PacketType);
            if (packet is HandshakePacket)
            {
                var handshake = packet as HandshakePacket;
                Console.WriteLine("[Server] Handshake: {0} as {1} running version {2}", handshake.Username, handshake.ClientToken, handshake.Version);
                Console.WriteLine("[Server] Kicking client!");
                connection.SendPacket(NetworkHelper.RejectClient("I don't like your face! :O (also: Banned for 365 days)"));
            }
        }
    }
}

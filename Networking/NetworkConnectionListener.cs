//#define UDP_SUPPORT

using KMP.Networking.Transport;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace KMP.Networking
{
    public class NetworkConnectionListener
    {
        public NetworkServerPreferences Preferences { get; private set; }
        public bool IsUsingIPv6 { get; private set; }
        private TcpListener tcpListener;
#if UDP_SUPPORT
        private UdpClient udpClient;
#endif
        public NetworkConnectionListener(NetworkServerPreferences preferences, bool enableIPv6)
        {
            this.IsUsingIPv6 = enableIPv6;
            this.Preferences = preferences;
            if (Preferences.UseTcp)
            {
                tcpListener = new TcpListener(GetTCPEP());
                tcpListener.Start();
            }
#if UDP_SUPPORT
            if (Preferences.UseUdp)
            {
                udpClient = new UdpClient(preferences.UdpPort);
            }
#endif
        }

        public NetworkConnection Accept()
        {
            var tcpClient = tcpListener.AcceptTcpClient();
            var transport = new TCPTransport(tcpClient);
            return new NetworkConnection(transport);
        }

        private IPEndPoint GetTCPEP()
        {
            IPAddress addr;
            var address = Preferences.BindingAddress;
            address = "0.0.0.0".Equals(address) && IsUsingIPv6 ? "::" : address;
            if (!IPAddress.TryParse(address, out addr))
            {
                Log.Warning("Configured ip binding is invalid {0}, defaulting to {1}", address,
                    address = IsUsingIPv6 ? "::" : "0.0.0.0");
                addr = IPAddress.Parse(address);
            }
            return new IPEndPoint(addr, Preferences.TcpPort);
        }

        public bool Pending
        {
#if !UDP_SUPPORT
            get{
                return tcpListener.Pending();
            }
#endif
        }
    }

    public class NetworkServerPreferences
    {
        public int TcpPort { get; private set; }
        public bool UseTcp { get; private set; }
#if UDP_SUPPORT
        public int UdpPort { get; private set; }
        public bool UseUdp { get; private set; }
#endif
        public String BindingAddress { get; private set; }
        private NetworkServerPreferences() { }
        public static NetworkServerPreferences CreateTCP(String bindingAddress, int port)
        {
            return new NetworkServerPreferences() { BindingAddress = bindingAddress, TcpPort = port, UseTcp = true };
        }

#if UDP_SUPPORT
        public static NetworkServerPreferences CreateUDP(String bindingAddress, int port)
        {
            return new NetworkServerPreferences() { BindingAddress = bindingAddress, UdpPort = port, UseUdp = true };
        }

        public static NetworkServerPreferences CreateTCPAndUDP(String bindingAddress, int tcpPort, int udpPort)
        {
            return new NetworkServerPreferences() { BindingAddress = bindingAddress, UseUdp = true, UseTcp = true, TcpPort = tcpPort, UdpPort = udpPort };
        }
#endif
    }
}

using KMP.Networking.Packets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace KMP.Networking
{
    /// <summary>
    /// Higher level helper to convert byte arrays to and from Abstract Packets
    /// </summary>
    public class NetworkHelper
    {
        #region Packet Mapping
        private static Dictionary<PacketType, Type> PacketTypeMap = new Dictionary<PacketType,Type>();
        static NetworkHelper()
        {
            PacketTypeMap.Add(PacketType.Handshake, typeof(HandshakePacket));
        }
        #endregion

        #region Static Methods
        public static AbstractPacket CreatePacket(byte[] data)
        {
            var typeId = AbstractPacket.Identify(data);
            var type = PacketTypeMap[typeId];
            var instance = type.GetConstructor(Type.EmptyTypes).Invoke(null) as AbstractPacket;
            if (instance != null)
            {
                instance.DerivePacket(data, KMPCommon.Side);
                return instance;
            }
            else
            {
                Log.Debug("Cannot convert type {0} packet in to {1}. Instance returned null", typeId, type);
            }
            return null;
        }

        public static byte[] SerializePacket(AbstractPacket packet)
        {
            return packet.BuildPacket(KMPCommon.Side);
        }
        #endregion

        #region Packet Creation Helpers

        public static HandshakePacket ClientHandshake(String username, Guid token, String clientVersion)
        {
            return new HandshakePacket()
            {
                Username = username,
                ClientToken = token,
                Version = clientVersion
            };
        }

        public static HandshakePacket AcceptClient(String serverVersion, int clientId, byte gamemode, int totalVessels, String modControlInfo)
        {
            return new HandshakePacket()
            {
                Accepted = true,
                Version = serverVersion,
                ClientID = clientId,
                GameMode = gamemode,
                VesselCount = totalVessels,
                ModControlSettings = modControlInfo
            };
        }

        public static HandshakePacket RejectClient(String rejectReason)
        {
            return new HandshakePacket()
            {
                Accepted = false,
                RejectReason = rejectReason
            };
        }
        #endregion
    }
    
}

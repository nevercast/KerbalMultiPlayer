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
            PacketTypeMap.Add(PacketType.TimeSync, typeof(SubspaceLockPacket));
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

        /// <summary>
        /// Create a client handshake to connect to a server with
        /// </summary>
        /// <param name="username">Client username</param>
        /// <param name="token">Client token</param>
        /// <param name="clientVersion">Client version</param>
        /// <returns>Handshake packet</returns>
        public static HandshakePacket ClientHandshake(String username, Guid token, String clientVersion)
        {
            return new HandshakePacket()
            {
                Username = username,
                ClientToken = token,
                Version = clientVersion
            };
        }

        /// <summary>
        /// Create a server handshake to send to the client
        /// </summary>
        /// <param name="serverVersion">Server version</param>
        /// <param name="clientId">Client ID</param>
        /// <param name="gamemode">Server gamemode</param>
        /// <param name="totalVessels">Total vessels in database</param>
        /// <param name="modControlInfo">Mod control data</param>
        /// <returns>Handshake packet</returns>
        public static HandshakePacket ServerHandshake(String serverVersion, int protocolVersion, int clientId, int gamemode, int totalVessels, byte[] modControlInfo)
        {
            return new HandshakePacket()
            {
                IsRejection = false,
                Version = serverVersion,
                ProtocolVersion = protocolVersion,
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
                IsRejection = true,
                RejectReason = rejectReason
            };
        }

        public static SubspaceLockPacket SubspaceLock(double SubspaceTick, long ServerTime, float SubspaceSpeed)
        {
            return new SubspaceLockPacket()
            {
                SubspaceTick = SubspaceTick,
                ServerTime = ServerTime,
                SubspaceSpeed = SubspaceSpeed
            };
        }
        #endregion
    }
    
}

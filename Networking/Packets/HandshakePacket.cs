using KMP.Networking.Conversion;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KMP.Networking.Packets
{
    public class HandshakePacket : AbstractPacket
    {
        /// <summary>
        /// Create new Handshake packet
        /// </summary>
        public HandshakePacket() : base(PacketType.Handshake) { }

        // Propeties sent by the client
        #region Client Properties
        /// <summary>
        /// Client Username
        /// </summary>
        public String Username { get; set; }
        /// <summary>
        /// Client login token
        /// Guid.Empty if no token available
        /// </summary>
        public Guid ClientToken { get; set; }
        #endregion

        #region Shared Properties
        /// <summary>
        /// KMP Version
        /// </summary>
        public String Version { get; set; }
        #endregion

        // Properties sent by the server
        #region Server Properties
        /// <summary>
        /// False if the Server has accepted a client handshake
        /// </summary>
        public bool IsRejection { get; set; }
        /// <summary>
        /// If the client has been denied connection, this will hold the reason
        /// </summary>
        public String RejectReason { get; set; }
        /// <summary>
        /// Protocol version in use on the server
        /// </summary>
        public int ProtocolVersion { get; set; }
        /// <summary>
        /// The client id of the connecting client
        /// </summary>
        public int ClientID { get; set; }
        /// <summary>
        /// Server gamemode
        /// </summary>
        public int GameMode { get; set; }
        /// <summary>
        /// Amount of ships in initial sync
        /// </summary>
        public int VesselCount { get; set; }
        /// <summary>
        /// Server mod control
        /// TODO: The client should send it's settings and hashes and the 
        /// server should kick the client if it's bad
        /// </summary>
        public byte[] ModControlSettings { get; set; }

        #endregion

        protected override void DecodePacket(NetworkMessage message, PacketSide side)
        {
            switch (side)
            {
                    // Server has received from client
                case PacketSide.Server:
                    Username = message.ReadString();
                    ClientToken = message.ReadGuid();
                    Version = message.ReadString();
                    break;
                    // Client has received from server
                case PacketSide.Client:
                    this.ProtocolVersion = message.ReadInt();
                    this.IsRejection = message.ReadBoolean();
                    if (this.IsRejection)
                    {
                        this.RejectReason = message.ReadString();
                    }
                    else
                    {
                        this.Version = message.ReadString();
                        this.ClientID = message.ReadInt();
                        this.GameMode = message.ReadByte();
                        this.VesselCount = message.ReadInt();
                        this.ModControlSettings = message.ReadByteArray();
                    }
                    break;
            }
        }

        protected override void PreparePacket(NetworkMessage message, PacketSide side)
        {
            switch (side)
            {
                    // Server is sending to client
                case PacketSide.Server:
                    // Network protocol version
                    message.WriteInt(ProtocolVersion);
                    // Is the client allowed on this server
                    message.WriteBoolean(IsRejection);
                    if (IsRejection)
                    {
                        // Get off my lawn
                        message.WriteString(RejectReason);
                    }
                    else
                    {
                        // Server Software Version
                        message.WriteString(Version);
                        // Client ID
                        message.WriteInt(ClientID);
                        // Server Gamemode
                        message.WriteByte((byte)GameMode);
                        // Amount of Vessels in initial sync
                        message.WriteInt(VesselCount);
                        // The ModControl data
                        message.WriteByteArray(ModControlSettings);
                    }
                    break;
                    // Client is sending to server
                case PacketSide.Client:
                    message.WriteString(Username);
                    message.WriteGuid(ClientToken);
                    message.WriteString(Version);
                    break;
            }
        }
    }
}

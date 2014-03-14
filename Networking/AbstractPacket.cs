using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace KMP.Networking
{
    public class AbstractPacket
    {
        /// <summary>
        /// The type of packet
        /// </summary>
        public PacketType PacketType { get; private set; }

        /// <summary>
        /// The Priority of the Packet
        /// </summary>
        public PacketPriority PacketPriority { get; private set; }

        protected AbstractPacket(PacketType packetType)
        {
            this.PacketType = packetType;
        }

        /// <summary>
        /// Used to write information to the message
        /// </summary>
        /// <param name="message">Empty message to fill with packet info</param>
        /// <param name="side">The side that is sending the packet</param>
        protected abstract void PreparePacket(NetworkMessage message, PacketSide side);

        /// <summary>
        /// Used to read information from message
        /// </summary>
        /// <param name="message">Message containing packet info</param>
        /// <param name="side">The side that is decoding the packet (receiver)</param>
        protected abstract void DecodePacket(NetworkMessage message, PacketSide side);

        /// <summary>
        /// Converts this packet in to a blob ready for transport
        /// </summary>
        /// <returns>Serialized packet</returns>
        /// <param name="side">The side that is going to send the packet</param>
        public byte[] BuildPacket(PacketSide side)
        {
            using (NetworkMessage message = new NetworkMessage(PacketType))
            {
                PreparePacket(message, side);
                return message.GetPacket();
            }
        }

        /// <summary>
        /// Creates a Packet from the data
        /// </summary>
        /// <param name="data">data to create packet from</param>
        /// <param name="side">The side that has received the packet</param>
        public void DerivePacket(byte[] data, PacketSide side)
        {
            using (NetworkMessage message = NetworkMessage.BuildFromData(data))
            {
                if (message.Type != PacketType)
                {
                    throw new IOException(String.Format("Packet type {0} cannot be derived to {1}", message.Type, PacketType);
                }
                DecodePacket(message, side);
            }
        }
    }
    
    /// <summary>
    /// Specifies the queue that the packet will be placed in
    /// </summary>
    public enum PacketPriority
    {
        High,
        Normal,
        Low
    }

    /// <summary>
    /// The side the packet is being processed on
    /// Side is used to change the contents of the packet to allow 
    /// different data outgoing and incoming for the same packet id
    /// </summary>
    public enum PacketSide
    {
        Client,
        Server
    }
}

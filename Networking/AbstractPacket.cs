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
        protected abstract void PreparePacket(NetworkMessage message);
        /// <summary>
        /// Used to read information from message
        /// </summary>
        /// <param name="message">Message containing packet info</param>
        protected abstract void DecodePacket(NetworkMessage message);

        /// <summary>
        /// Converts this packet in to a blob ready for transport
        /// </summary>
        /// <returns>Serialized packet</returns>
        public byte[] BuildPacket()
        {
            using (NetworkMessage message = new NetworkMessage(PacketType))
            {
                PreparePacket(message);
                return message.GetPacket();
            }
        }

        /// <summary>
        /// Creates a Packet from the data
        /// </summary>
        /// <param name="data">data to create packet from</param>
        public void DerivePacket(byte[] data)
        {
            using (NetworkMessage message = NetworkMessage.BuildFromData(data))
            {
                if (message.Type != PacketType)
                {
                    throw new IOException(String.Format("Packet type {0} cannot be derived to {1}", message.Type, PacketType);
                }
                DecodePacket(message);
            }
        }
    }
    
    public enum PacketPriority
    {
        High,
        Normal,
        Low
    }
}

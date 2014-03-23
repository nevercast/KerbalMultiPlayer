using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KMP.Networking.Packets
{
    public class TimeSyncPacket : AbstractPacket
    {
        public TimeSyncPacket() : base(PacketType.TimeSync) { }

        public double SubspaceTick { get; set; }
        public long ServerTime { get; set; }
        public float SubspaceSpeed { get; set; }

        protected override void PreparePacket(Conversion.NetworkMessage message, PacketSide side)
        {
            PacketPriority = Util.Priority.Realtime;
            switch (side)
            {
                // Server sending to client
                case PacketSide.Server:
                    message.WriteDouble(SubspaceTick);
                    message.WriteLong(ServerTime);
                    message.WriteFloat(SubspaceSpeed);
                    break;
            }
        }

        protected override void DecodePacket(Conversion.NetworkMessage message, PacketSide side)
        {
            switch (side)
            {
                case PacketSide.Client:
                    SubspaceTick = message.ReadDouble();
                    ServerTime = message.ReadLong();
                    SubspaceSpeed = message.ReadFloat();
                    break;
            }
        }
    }
}

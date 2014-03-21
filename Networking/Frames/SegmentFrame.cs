using KMP.Networking.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KMP.Networking.Frames
{
    /// <summary>
    /// Top Frame that encapsulates a packet and supports segmenting
    /// </summary>
    public class SegmentFrame
    {
        private static short NextSegmentNumber = 0;
        private static short GenSegmentNumber()
        {
            if (NextSegmentNumber == short.MaxValue)
            {
                NextSegmentNumber = 0;
                return short.MaxValue;
            }
            return NextSegmentNumber++;
        }

        public short SegmentNumber { get; private set; }
        public SegmentPacketType Type { get; private set; }
        public long UniqueNumber { get; private set; }

        private SegmentFrame(short segNo, SegmentPacketType type, long unum)
        {
            this.SegmentNumber = segNo;
            this.Type = type;
            this.UniqueNumber = unum;
        }

        public static SegmentFrame CreateInit(long dataLength)
        {
            return new SegmentFrame(GenSegmentNumber(), SegmentPacketType.Init, dataLength);
        }

        public static SegmentFrame CreateSegment(SegmentFrame init, byte[] segmentData, long offset)
        {
            if (init.Type != SegmentPacketType.Init) throw new ArgumentException("Supplied frame is not an Init frame");
            if (offset + segmentData.Length > init.UniqueNumber) throw new ArgumentException("Segment exceeds length of Init frame");
            return new SegmentFrame(init.SegmentNumber, SegmentPacketType.Segment, offset);
        }

        public static SegmentFrame CreateChecksum(SegmentFrame init, byte[] data)
        {
            if (init.Type != SegmentPacketType.Init) throw new ArgumentException("Supplied frame is not an Init frame");
            return new SegmentFrame(init.SegmentNumber, SegmentPacketType.Checksum, Hash.MD5(data));
        }

        public Boolean IsIntegrityOkay(byte[] receivedData)
        {
            if (Type == SegmentPacketType.Checksum)
            {
                return Hash.MD5(receivedData) == UniqueNumber;
            }
            return true;
        }

        public static SegmentFrame[] GetFrames(AbstractPacket packet)
        {

        }
    }

    public enum SegmentPacketType {
        Init,
        Segment,
        Checksum
    }
}

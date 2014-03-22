using KMP.Networking.Packets;
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
        private const int SEGMENT_SIZE = 1024;

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
        public Priority Priority { get; private set; }

        public SegmentFrame(short segNo, SegmentPacketType type, long unum)
        {
            this.SegmentNumber = segNo;
            this.Type = type;
            this.UniqueNumber = unum;
        }

        private static SegmentFrame CreateInit(long dataLength)
        {
            return new SegmentFrame(GenSegmentNumber(), SegmentPacketType.Init, dataLength);
        }

        private static SegmentFrame CreateSegment(SegmentFrame init, byte[] segmentData, long offset)
        {
            if (init.Type != SegmentPacketType.Init) throw new ArgumentException("Supplied frame is not an Init frame");
            if (offset + segmentData.Length > init.UniqueNumber) throw new ArgumentException("Segment exceeds length of Init frame");
            return new SegmentFrame(init.SegmentNumber, SegmentPacketType.Segment, offset);
        }

        private static SegmentFrame CreateChecksum(SegmentFrame init, byte[] data)
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
            var priority = packet.PacketPriority;
            var completeData = packet.BuildPacket(KMPCommon.Side);
            var init = CreateInit(completeData.LongLength);
            var end = CreateChecksum(init, completeData);
            init.Priority = priority;
            end.Priority = priority;
            List<SegmentFrame> segments = new List<SegmentFrame>();
            for (long i = 0; i < completeData.LongLength; i += SEGMENT_SIZE)
            {
                byte[] data = new byte[i + SEGMENT_SIZE >= completeData.LongLength ? completeData.LongLength - 1 : SEGMENT_SIZE];
                Array.Copy(completeData, i, data, 0, data.Length);
                var segment = CreateSegment(init, data, i);
                segment.Priority = priority;
                segments.Add(segment);
            }
            SegmentFrame[] allFrames = new SegmentFrame[segments.Count + 2];
            allFrames[0] = init;
            Array.Copy(segments.ToArray(), 0, allFrames, 1, segments.Count);
            allFrames[segments.Count + 1] = end;
            return allFrames;
        }

        public byte[] Divide(byte[] data)
        {
            if (Type != SegmentPacketType.Segment) return null;
            var segmentSize = Math.Min(SEGMENT_SIZE, data.Length - UniqueNumber);
            byte[] part = new byte[segmentSize];
            Array.Copy(data, UniqueNumber, part, 0, segmentSize);
            return part;
        }
    }

    public enum SegmentPacketType : byte {
        Init,
        Segment,
        Checksum
    }
}

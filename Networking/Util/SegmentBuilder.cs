using KMP.Networking.Conversion;
using KMP.Networking.Frames;
using KMP.Networking.Packets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KMP.Networking.Util
{
    public class SegmentBuilder
    {
        static Dictionary<short, byte[]> segmentCache = new Dictionary<short, byte[]>();
        /// <summary>
        /// Issues a frame to the cache
        /// </summary>
        /// <param name="frame">Frame to send to cache</param>
        /// <param name="segment">Optional segment data</param>
        /// <returns>AbstractPacket on completion of data, else null</returns>
        public static AbstractPacket Issue(SegmentFrame frame, byte[] segment = null)
        {
            switch (frame.Type)
            {
                case SegmentPacketType.Init:
                    {
                        /* Creating new frame */
                        var id = frame.SegmentNumber;
                        if (segmentCache.ContainsKey(id) && segmentCache[id].Length == frame.UniqueNumber)
                            return null;
                        segmentCache[id] = new byte[frame.UniqueNumber];
                        break;
                    }
                case SegmentPacketType.Segment:
                    {
                        /* Data */
                        if (segment == null || segment.Length == 0) return null;
                        var id = frame.SegmentNumber;
                        if (!segmentCache.ContainsKey(id)) return null;
                        var destdata = segmentCache[id];
                        if (destdata.Length < frame.UniqueNumber + segment.Length)
                        {
                            Log.Debug("Segment cache {0} not initilized to accomodate segment {1}@{2}", id, segment.Length, frame.UniqueNumber);
                            return null;
                        }
                        Array.Copy(segment, 0, destdata, frame.UniqueNumber, segment.Length);
                        break;
                    }
                case SegmentPacketType.Checksum:
                    {
                        if (!segmentCache.ContainsKey(frame.SegmentNumber))
                        {
                            /* Missing frame */
                            Log.Debug("Segment frame {0} missing for checksum {1}", frame.SegmentNumber, frame.UniqueNumber);
                            return null;
                        }
                        var data = segmentCache[frame.SegmentNumber];
                        if (frame.IsIntegrityOkay(data))
                        {
                            /* Publish frame as complete */
                            segmentCache.Remove(frame.SegmentNumber);
                            return NetworkHelper.CreatePacket(data);
                        }
                        else
                        {
                            Log.Debug("Segment frame {0} failed integrity check", frame.SegmentNumber);
                        }
                        break;
                    }
                case SegmentPacketType.QuickFrame:
                    Log.Debug("Unpacking QuickFrame");
                    // Create the frames from the segment
                    using (NetworkMessage msg = NetworkMessage.BuildFromData(segment))
                    {
                        var length = msg.ReadShort();
                        var segmentData = msg.ReadBytes(length);
                        var checksum = msg.ReadLong();
                        /* Issue the init */
                        Issue(new SegmentFrame(frame.SegmentNumber, SegmentPacketType.Init, length));
                        /* Issue the segment */
                        Issue(new SegmentFrame(frame.SegmentNumber, SegmentPacketType.Segment, 0), segmentData);
                        /* Issue the checksum and return the final packet */
                        return Issue(new SegmentFrame(frame.SegmentNumber, SegmentPacketType.Checksum, checksum));
                    }
            }
            return null;
        }

        /// <summary>
        /// Issue segment from raw network data
        /// </summary>
        /// <param name="message">Data from network</param>
        /// <returns>AbstractPacket on completion of packet, else null</returns>
        internal static AbstractPacket Issue(byte[] message)
        {
            using (NetworkMessage msg = NetworkMessage.BuildFromData(message))
            {
                return Issue(new SegmentFrame(
                        msg.ReadShort(),
                        msg.ReadEnum<SegmentPacketType>(),
                        msg.ReadLong()),
                    msg.ReadRemaining());
            }
        }
    }
}

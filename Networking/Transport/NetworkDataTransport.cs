using KMP.Networking.Conversion;
using KMP.Networking.Frames;
using KMP.Networking.Packets;
using KMP.Networking.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace KMP.Networking.Transport
{
    /// <summary>
    /// Higher level abstract class to transport bytes from one end to another
    /// </summary>
    public abstract class NetworkDataTransport
    {
        protected NetworkDataTransport()
        {
            (new Thread(OutboundPump) { IsBackground = true, Name = "Outbound Segment pump" }).Start();
        }

        protected abstract void Transmit(byte[] packet);
        public event AsyncMessageCallback MessageArrived;
        public delegate void AsyncMessageCallback(byte[] packet);

        /// <summary>
        /// Transmitting
        /// </summary>
        private PriorityQueue<byte[]> OutputOrderedData = new PriorityQueue<byte[]>();

        // Buffers data until we have a handler
        private Queue<byte[]> DataQueue = new Queue<byte[]>();
        protected void OnDataArrived(byte[] message)
        {
            DataQueue.Enqueue(message);
            if (MessageArrived != null)
            {
                while (DataQueue.Count > 0)
                {
                    try
                    {
                        MessageArrived(DataQueue.Dequeue());
                    }
                    catch (Exception ex)
                    {
                        Log.Warning("Error occured in OnDataArrived({1}) while publishing data: {0}", ex, this);
                    }
                }
            }
            if (DataQueue.Count > 50)
            {
                Log.Warning("NetworkDataTransport({1}) receive queue is overloaded: {0} pending packets", DataQueue.Count, this);
            }
        }

        private void OutboundPump()
        {
            while (!Connected) Thread.Sleep(100); /* Wait for subclass to connect */
            while (Connected)
            {
                if (OutputOrderedData.Count > 0)
                {
                    Transmit(OutputOrderedData.Dequeue());
                }
                else
                {
                    Thread.Sleep(50);
                }
            }
        }

        /// <summary>
        /// Send data over network
        /// </summary>
        /// <remarks>This method splits the data in to fragments and sends them via NetworkDataTransport.Transmit</remarks>
        /// <param name="data">Data to send</param>
        public void Send(AbstractPacket packet)
        {
            var segments = SegmentFrame.GetFrames(packet);
            var packedData = packet.BuildPacket(KMPCommon.Side);
            foreach (var segment in segments)
            {
                using (var msg = new NetworkMessage())
                {
                    msg.WriteShort(segment.SegmentNumber);
                    msg.WriteEnum<SegmentPacketType>(segment.Type);
                    msg.WriteLong(segment.UniqueNumber);
                    var extraData = segment.Divide(packedData);
                    if (extraData != null)
                    {
                        msg.WriteBytes(extraData);
                    }
                    OutputOrderedData.Enqueue(msg.GetPacket(), segment.Priority);
                }
            }
        }

        public abstract bool Connected { get; }
    }
}

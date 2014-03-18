using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace KMP.Networking.Transport
{
    /// <summary>
    /// Higher level abstract class to transport bytes from one end to another
    /// </summary>
    public abstract class NetworkDataTransport
    {
        protected abstract void Transmit(byte[] packet);
        public event AsyncMessageCallback MessageArrived;
        public delegate void AsyncMessageCallback(byte[] packet);

        /// <summary>
        /// Receiving
        /// </summary>
        private Dictionary<int, byte[]> PacketFragments = new Dictionary<int, byte[]>();

        /// <summary>
        /// Transmitting
        /// </summary>
        private PriorityQueue<byte[]> OutputOrderedData = new PriorityQueue<byte[]>();

        // Buffers data until we have a handler
        private Queue<byte[]> DataQueue = new Queue<byte[]>();
        protected void OnDataArrived(byte[] message)
        {
            int uid = BitConverter.ToInt32(message, 0);
            int fragmentId = BitConverter.ToInt32(message, 4);
            int offset = BitConverter.ToInt32(message, 8);
            if (offset == 0)
            {
                // New inital packet
                int totalLength = BitConverter.ToInt32(message, 12);
                if (PacketFragments.ContainsKey(uid))
                {
                    Log.Debug("Duplicate packet fragment definition");
                    if (PacketFragments[uid].Length != totalLength)
                    {
                        Log.Warning("Duplicate packet fragment size mismatch. Discarding old packet");
                        PacketFragments.Remove(uid);
                        PacketFragments.Add(uid, new byte[totalLength]);
                    }
                }
                else
                {
                    PacketFragments.Add(uid, new byte[totalLength]);
                }
                
            }
            Array.Copy(message, 16, PacketFragments[uid], offset, message.Length - 16);
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

        /// <summary>
        /// Send data over network
        /// </summary>
        /// <remarks>This method splits the data in to fragments and sends them via NetworkDataTransport.Transmit</remarks>
        /// <param name="data">Data to send</param>
        public void Send(AbstractPacket packet)
        {

        }

        public abstract bool Connected { get; }
    }
}

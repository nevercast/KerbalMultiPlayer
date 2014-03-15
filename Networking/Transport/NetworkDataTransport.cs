using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KMP.Networking.Transport
{
    /// <summary>
    /// Higher level abstract class to transport bytes from one end to another
    /// </summary>
    public abstract class NetworkDataTransport
    {
        public abstract void Transmit(byte[] packet);
        public event AsyncMessageCallback MessageArrived;
        public delegate void AsyncMessageCallback(byte[] packet);

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
    }
}

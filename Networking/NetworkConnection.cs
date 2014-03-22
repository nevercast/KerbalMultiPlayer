using KMP.Networking.Packets;
using KMP.Networking.Transport;
using KMP.Networking.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace KMP.Networking
{
    /// <summary>
    /// Highest level part of End Point-End Point connections
    /// </summary>
    public class NetworkConnection
    {
        private const int THREAD_SLEEP_MS = 20;
        private PriorityQueue<AbstractPacket> PacketQueue = new PriorityQueue<AbstractPacket>();
        private PriorityQueue<AbstractPacket> OutboundQueue = new PriorityQueue<AbstractPacket>();
        public NetworkDataTransport Transport { get; private set; }
        /// <summary>
        /// Create a new Network Connection
        /// </summary>
        /// <param name="transport">The transport to use for sending and receiving data</param>
        public NetworkConnection(NetworkDataTransport transport)
        {
            Transport = transport;
            Transport.MessageArrived += transport_MessageArrived;
            new Thread(QueuePumpLoop).Start();
        }

        // When a packet arrives over the transport, send it to the segment builder to build the segments in to a packet
        private void transport_MessageArrived(byte[] data)
        {
            var packet = SegmentBuilder.Issue(data);
            if (packet != null) /* Null if the packet is incomplete */
            {
                // Now that we have a packet, place it in the prioritized queue
                PacketQueue.Enqueue(packet, packet.PacketPriority);
                // That's all the time we spend in this thread, lowering network latency
            }
        }

        /// <summary>
        /// Send packet to the other end of this connection
        /// </summary>
        /// <remarks>Packet is only queued for send, it will be sent from another thread</remarks>
        /// <param name="packet">Packet to send</param>
        public void SendPacket(AbstractPacket packet)
        {
            OutboundQueue.Enqueue(packet, packet.PacketPriority);
        }

        private void QueuePumpLoop()
        {
            Thread.CurrentThread.Name = "NetworkConnection Queue Pump";
            Thread.CurrentThread.IsBackground = true;
            try
            {
                while (true)
                {
                    // Push all our packets out because that is quick
                    if (OutboundQueue.Count > 0)
                    {
                        var packet = OutboundQueue.Dequeue();
                        Transport.Send(packet);
                        continue;
                    }
                    // And then handle the processing of all the received ones, which is slower
                    if (PacketQueue.Count > 0)
                    {
                        var packet = PacketQueue.Dequeue();
                        OnPacketArrived(packet);
                        continue;
                    }
                    Thread.Sleep(THREAD_SLEEP_MS);
                }
            }
            finally
            {
                Log.Debug("NetworkConnection Queue Pump is aborted.");
            }
        }

        public delegate void PacketArrivedHandler(NetworkConnection connection, AbstractPacket packet);
        public event PacketArrivedHandler PacketArrived;

        private void OnPacketArrived(AbstractPacket packet)
        {
            if (PacketArrived != null)
            {
                try
                {
                    PacketArrived(this, packet);
                }
                catch (Exception ex)
                {
                    Log.Warning("Error occured announcing packet arrival: {0}\r\n\tIn: {1}", ex, this);
                }
            }
        }

        public bool Connected
        {
            get
            {
                return Transport.Connected;
            }
        }

    }
}

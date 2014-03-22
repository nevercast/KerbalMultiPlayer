using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace KMP.Networking.Transport
{
    /// <summary>
    /// Network data transport implementation that transports payloads from one
    /// end and back again over TCP.
    /// </summary>
    public class TCPTransport : NetworkDataTransport
    {
        public TcpClient Client { get; private set; }
        public TCPTransport(TcpClient client)
        {
            this.Client = client;
            this.Client.NoDelay = true;
            new Thread(Run).Start();
        }

        protected override void Transmit(byte[] data)
        {
            var header = BitConverter.GetBytes(data.Length);
            Client.GetStream().Write(header, 0, 4);
            Client.GetStream().Write(data, 0, data.Length);
            Client.GetStream().Flush();
        }

        public override bool Connected
        {
            get { return Client == null ? false : Client.Connected; }
        }

        private void Run()
        {
            try
            {
                Thread.CurrentThread.Name = "TCPTransport Receiver Thread";
                Thread.CurrentThread.IsBackground = true;
                byte[] lengthBuffer = new byte[4];
                // Pre-created buffer used for all packets < 10k
                byte[] packetBuffer = new byte[10480];
                int c = 0;
                while (Client.Connected)
                {
                    c += Client.GetStream().Read(lengthBuffer, c, 4 - c);
                    if (c >= 4) c = 0;
                    else continue; /* Keep receiving data until we have our int */
                    var length = BitConverter.ToInt32(lengthBuffer, 0);
                    try
                    {
                        var tmpBuffer = length > packetBuffer.Length ? new byte[length] : packetBuffer;
                        while (c < length)
                        {
                            c += Client.GetStream().Read(tmpBuffer, c, length - c);
                        }
                        if (tmpBuffer == packetBuffer && length < packetBuffer.Length)
                        {
                            var finalBuffer = new byte[length];
                            Array.Copy(tmpBuffer, finalBuffer, length);
                            tmpBuffer = finalBuffer;
                        }
                        OnDataArrived(tmpBuffer);

                    }
                    catch (Exception ex)
                    {
                        Log.Warning("Exception in TCPTransport.Run {0}", ex);
                    }
                    finally
                    {
                        c = 0;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Debug("Thread TCPTransport quit with {0}", ex);
            }
            finally
            {
                Log.Debug("TCPTransport Thread has quit");
            }
        }

        public override void Close()
        {
            if (Client != null)
            {
                try
                {
                    Client.Client.Disconnect(false);
                }
                catch { }
                try
                {
                    Client.Close();
                }
                catch { }
                Client = null;
            }
        }
    }
}

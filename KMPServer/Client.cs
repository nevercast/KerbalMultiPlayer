using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Net.Sockets;
using System.Threading;
using System.Net;
using System.Collections;
using System.Collections.Concurrent;
using KMP.Networking;
using KMP.Networking.Packets;
namespace KMPServer
{
	class Client
	{
		public enum ActivityLevel
		{
			INACTIVE,
			IN_GAME,
			IN_FLIGHT
		}

		//Repurpose SEND_BUFFER_SIZE to split large messages.
        public const int POLL_INTERVAL = 60000;

		//Properties

		public Server parent
		{
			private set;
			get;
		}

        #region Fields
        public int clientIndex;
		public String username;
		public Guid guid;
		public int playerID;
		public Guid currentVessel = Guid.Empty;
		public int currentSubspaceID = -1;
		public double enteredSubspaceAt = 0d;
		public double lastTick = 0d;
		public double syncOffset = 0.01d;
		public int lagWarning = 0;
		public bool warping = false;
		public bool hasReceivedScenarioModules = false;
		public float averageWarpRate = 1f;
		
		public bool receivedHandshake;

		public byte[] screenshot;
		public String watchPlayerName;
		public byte[] sharedCraftFile;
		public String sharedCraftName;
		public KMPCommon.CraftType sharedCraftType;

		public long connectionStartTime;
		public long lastReceiveTime;
		public long lastSyncTime;
		public long lastUDPACKTime;
        public long lastPollTime = 0;

		public long lastInGameActivityTime;
		public long lastInFlightActivityTime;
		public ActivityLevel activityLevel;

		public NetworkConnection Connection { get; private set ; }

		public object tcpClientLock = new object();
		public object timestampLock = new object();
		public object activityLevelLock = new object();
		public object screenshotLock = new object();
		public object watchPlayerNameLock = new object();
		public object sharedCraftLock = new object();
		
		public string disconnectMessage = "";

        #endregion

        #region Constructor
        public Client(Server parent, NetworkConnection connection)
		{
            this.Connection = connection;
			this.parent = parent;
			resetProperties();
		}
        #endregion

        #region State Properties

        public bool isValid
        {
            get
            {
               // bool isConnected = false;
                if (this.Connection != null && this.Connection.Connected)
                {                   
                    Socket clientSocket = this.Connection.Client;
                    try
                    {
						if (receivedHandshake)
						{
							if ((parent.currentMillisecond - lastPollTime) > POLL_INTERVAL)
							{
							    lastPollTime = parent.currentMillisecond;
								//Removed redundant "Socket.Available" check and increased the Poll "Timeout" from 10ms to 500ms - Dani
								//Change SocketRead to SocketWrite. Also, no need for such high timeout.
								return clientSocket.Poll(200000, SelectMode.SelectWrite);
							}
							else
							{
							    // They have max 10 seconds to get their shit together. 
							    return true;
							}
						} else return true;
                    }
                    catch (SocketException)
                    {
                        // Unknown error
                        return false;
                    } catch (ObjectDisposedException)
                    {
                        // Socket closed
                        return false;
                    }
                    catch (Exception ex)
                    {
                        // Shouldn't happen, pass up.
                        parent.passExceptionToMain(ex);
                    }
                    
                    return true;
                }
                return false;
            }
        }

        public bool isReady
        {
            get
            {
                return (isValid && this.receivedHandshake);
            }
        }

        public IPAddress IPAddress
        {
            get
            {
                if (Connection == null) { return null; }
                return (Connection.Client.RemoteEndPoint as IPEndPoint).Address;
            }
        }

        #endregion


        #region Methods

        public void resetProperties()
		{
			username = "";
			screenshot = null;
			watchPlayerName = String.Empty;
			receivedHandshake = false;

			sharedCraftFile = null;
			sharedCraftName = String.Empty;
			sharedCraftType = KMPCommon.CraftType.VAB;

			lastUDPACKTime = 0;

			lock (activityLevelLock)
			{
				activityLevel = Client.ActivityLevel.INACTIVE;
				lastInGameActivityTime = parent.currentMillisecond;
				lastInFlightActivityTime = parent.currentMillisecond;
			}

			lock (timestampLock)
			{
				lastReceiveTime = parent.currentMillisecond;
				connectionStartTime = parent.currentMillisecond;
				lastSyncTime = parent.currentMillisecond;
			}
		}

		public void updateReceiveTimestamp()
		{
			lock (timestampLock)
			{
				lastReceiveTime = parent.currentMillisecond;
			}
		}

		public void disconnected()
		{
			screenshot = null;
			watchPlayerName = String.Empty;

			sharedCraftFile = null;
			sharedCraftName = String.Empty;
		}

        #endregion

        #region Messages
        
        private void MessageReceived(NetworkConnection connection, AbstractPacket packet)
        {

        }

        private void messageReceived (KMPCommon.ClientMessageID id, byte[] data)
		{
			parent.queueClientMessage (this, id, data);
		}

        /*
		private void syncTimeRewrite(ref byte[] next_message) {
			//SYNC_TIME Rewriting
			int next_message_id = BitConverter.ToInt32(next_message, 0);
			if (next_message_id == (int)KMPCommon.ServerMessageID.SYNC_TIME) {
				byte[] next_message_stripped = new byte[next_message.Length - 8];
				Array.Copy (next_message, 8, next_message_stripped, 0, next_message.Length - 8);
				byte[] next_message_decompressed = KMPCommon.Decompress(next_message_stripped);
				byte[] time_sync_rewrite = new byte[24];
				next_message_decompressed.CopyTo(time_sync_rewrite, 0);
				BitConverter.GetBytes(DateTime.UtcNow.Ticks).CopyTo(time_sync_rewrite, 16);
				next_message = Server.buildMessageArray(KMPCommon.ServerMessageID.SYNC_TIME, time_sync_rewrite);
			}
		}
        */

        public void SendMessage(AbstractPacket packet)
        {
            Connection.SendPacket(packet);
            // For reference, all these packets are high priority
            /*
                case (int)KMPCommon.ServerMessageID.HANDSHAKE:
                case (int)KMPCommon.ServerMessageID.HANDSHAKE_REFUSAL:
                case (int)KMPCommon.ServerMessageID.SERVER_MESSAGE:
                case (int)KMPCommon.ServerMessageID.TEXT_MESSAGE:
                case (int)KMPCommon.ServerMessageID.MOTD_MESSAGE:
                case (int)KMPCommon.ServerMessageID.SERVER_SETTINGS:
                case (int)KMPCommon.ServerMessageID.KEEPALIVE:
                case (int)KMPCommon.ServerMessageID.CONNECTION_END:
                case (int)KMPCommon.ServerMessageID.UDP_ACKNOWLEDGE:
                case (int)KMPCommon.ServerMessageID.PING_REPLY:
             */
        }
        #endregion

        #region Activity

        public void updateActivityLevel(ActivityLevel level)
		{
			bool changed = false;

			lock (activityLevelLock)
			{
				switch (level)
				{
					case ActivityLevel.IN_GAME:
						lastInGameActivityTime = parent.currentMillisecond;
						currentVessel = Guid.Empty;
						break;

					case ActivityLevel.IN_FLIGHT:
						lastInFlightActivityTime = parent.currentMillisecond;
						lastInGameActivityTime = parent.currentMillisecond;
						break;
				}

				if (level > activityLevel)
				{
					activityLevel = level;
					changed = true;
				}
			}

			if (changed)
				parent.clientActivityLevelChanged(this);
        }

        #endregion

    }

	public class StateObject
	{
		// Client socket.
		public TcpClient workClient = null;
	}
}

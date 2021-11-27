﻿using System;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
#pragma warning disable 1591
namespace Quick.Communication
{
    /// <summary>
    /// Represents a base class of pipe communication
    /// </summary>
    public abstract class PipeBase
    {
        #region Constructor
        protected PipeBase()
        {
            _packetSpliter = new SimplePacketSpliter();
            _recvQueue = new RingQueue();
            _sendBuffer = new DynamicBufferStream();
            if (SynchronizationContext.Current != null)
            {
                _syncContext = SynchronizationContext.Current;
            }
            else
            {
                _syncContext = new SynchronizationContext();
                SynchronizationContext.SetSynchronizationContext(_syncContext);
            }
        }

        #endregion

        #region Protected Members
        private DynamicBufferStream _sendBuffer;
        protected PipeStream _pipeStream;
        protected Thread _workThread;
        protected string _pipeName;
        protected SynchronizationContext _syncContext;
        protected RingQueue _recvQueue;
        protected IPacketSpliter _packetSpliter;
        protected SingleThreadTaskScheduler _taskScheduler;
        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the encoding when transmitting text in pipe communication.
        /// </summary>
        /// <returns>The encoding when transmitting text in pipe communication. The default is UTF8.</returns>
        public Encoding TextEncoding { get; set; } = Encoding.UTF8;

        protected bool _isRunning = false;
        /// <summary>
        /// Gets a value indicating the running status of the pipe.
        /// </summary>
        /// <returns>true if the pipe is running; otherwise, false. The default is false.</returns>
        public bool IsRunning
        {
            get { return _isRunning; }
        }

        protected ClientStatus _status = ClientStatus.Closed;
        /// <summary>
        /// Gets the client status of the pipe
        /// </summary>
        /// <returns>The client status of the pipe. The default is ClientStatus.Closed.</returns>
        public ClientStatus Status
        {
            get { return _status; }
        }

        #endregion

        #region Events
        /// <summary>
        /// Represents the method that will handle the message received event of a Quick.Communication.PipeBase object.
        /// </summary>
        public event EventHandler<DataMessageReceivedEventArgs> MessageReceived;

        /// <summary>
        /// Represents the method that will handle the client status changed event of a Quick.Communication.PipeBase object.
        /// </summary>
        public event EventHandler<PipeClientStatusChangedEventArgs> ClientStatusChanged;
        #endregion

        #region Protected Functions

        protected void NotifyMessageReceived(DataPacket dataPacket)
        {
            byte[] data = dataPacket.Data.ToArray();
            Task.Factory.StartNew((tempData) =>
            {
                MessageReceived?.Invoke(this, new DataMessageReceivedEventArgs((byte[])tempData));
            }, data, CancellationToken.None, TaskCreationOptions.None, _taskScheduler);
        }

        protected void NotifyStatusChanged(ClientStatus newStatus)
        {
            if (_status != newStatus)
            {
                _status = newStatus;
                ClientStatusChanged?.Invoke(this, new PipeClientStatusChangedEventArgs(_status));
            }
        }

        #endregion

        #region Public Functions
        /// <summary>
        /// Sends the specified message data to a connected pipe client.
        /// </summary>
        /// <param name="messageData">the message to be sent.</param>
        public void SendMessage(byte[] messageData)
        {
            SendMessage(messageData, 0, messageData.Length);
        }

        public void SendMessage(byte[] messageData, int offset, int count)
        {
            if (!_isRunning)
            {
                throw new NotRunningException("the pipe is not running");
            }
            if (_pipeStream.IsConnected)
            {
                lock (_sendBuffer)
                {
                    ArraySegment<byte> dataPacket = _packetSpliter.MakePacket(messageData, offset, count, _sendBuffer);
                    _pipeStream.Write(dataPacket.Array, dataPacket.Offset, dataPacket.Count);
                }
            }
        }
        /// <summary>
        /// Sends the specified message text to a connected pipe client.
        /// </summary>
        /// <param name="text">The text to be sent.</param>
        /// <param name="textEncoding">The text encoding in pipe communication.</param>
        public void SendText(string text, Encoding textEncoding)
        {
            byte[] messageData = textEncoding.GetBytes(text);
            SendMessage(messageData);
        }

        /// <summary>
        /// Sends the specified message text to a connected pipe client using default text encoding, see TextEncoding property.
        /// </summary>
        /// <param name="text">The text to be sent.</param>
        public void SendText(string text)
        {
            SendText(text, TextEncoding);
        }

        #endregion
    }
    /// <summary>
    /// Provides data for the Quick.Communication.PipeBase.ClientStatusChanged event.
    /// </summary>
    public class PipeClientStatusChangedEventArgs : EventArgs
    {
        public PipeClientStatusChangedEventArgs(ClientStatus status)
        {
            Status = status;
        }
        /// <summary>
        /// Gets or sets the client status.
        /// </summary>
        /// <returns>The client status. The default is ClientStatus.Closed.</returns>
        public ClientStatus Status { get; }
    }

}

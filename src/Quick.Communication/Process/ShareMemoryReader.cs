using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;

namespace Quick.Communication
{
    /// <summary>
    /// Represents a ShareMemoryReader object that derived from Quick.Communication.ShareMemoryBase
    /// </summary>
    public class ShareMemoryReader : ShareMemoryBase
    {
        #region Constructor
        /// <summary>
        /// Initializes a new instance of the Quick.Communication.ShareMemoryReader
        /// </summary>
        /// <param name="uniqueName">An unique name for share memory communication</param>
        /// <param name="bufferSize">The buffer size of the share memory</param>
        public ShareMemoryReader(string uniqueName, int bufferSize) : base(uniqueName, bufferSize)
        {
            _recvBuffer = new RingQueue();
        }
        #endregion

        #region Private Members
        private Thread _recvThread;
        private RingQueue _recvBuffer;
        SingleThreadTaskScheduler _taskScheduler;
        #endregion

        #region Events
        /// <summary>
        /// Represents the method that will handle the message received event of a Quick.Communication.ShareMemoryReader object.
        /// </summary>
        public event EventHandler<DataMessageReceivedEventArgs> MessageReceived;

        #endregion

        #region Private Functions

        private void RecvProc()
        {
            _isOpen = true;
            while (_isOpen)
            {
                //wait data
                _notifyEvent.WaitOne();
                if (!_isOpen)
                {
                    return;
                }
                _mutex.WaitOne();
                byte[] readData = ReadData();
                _notifyEvent.Reset();
                _mutex.ReleaseMutex();
                if (readData != null)
                {
                    _recvBuffer.Write(readData);
                    int endPos = 0;
                    List<DataPacket> packets = _packetSpliter.GetPackets(_recvBuffer.Buffer, 0, _recvBuffer.DataLength, 0, out endPos);
                    if (packets != null && packets.Count > 0)
                    {
                        _recvBuffer.Remove(endPos);
                        foreach (DataPacket message in packets)
                        {
                            byte[] newData = message.Data.ToArray();
                            Task.Factory.StartNew((obj) =>
                            {
                                MessageReceived?.Invoke(this, new DataMessageReceivedEventArgs((byte[])obj));
                            }, newData, CancellationToken.None, TaskCreationOptions.None, _taskScheduler);
                        }
                    }
                }

            }
        }

        #endregion

        #region Public Members

        /// <summary>
        /// Open the share memory
        /// </summary>
        public new void Open()
        {
            base.Open();
            _taskScheduler = new SingleThreadTaskScheduler();
            _recvThread = ThreadEx.Start(RecvProc);
        }

        /// <summary>
        /// Close the share memory
        /// </summary>
        public new void Close()
        {
            if (!_isOpen)
            {
                return;
            }
            _isOpen = false;
            _notifyEvent.Set();
            _recvThread.Join(2000);
            base.Close();
            _taskScheduler.Stop();
        }

        #endregion
    }
}

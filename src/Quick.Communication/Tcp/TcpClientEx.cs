using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
#pragma warning disable 1591
namespace Quick.Communication
{
    /// <summary>
    /// Represents a tcp client object that derived from Quick.Communication.TcpBase
    /// </summary>
    public class TcpClientEx : TcpBase
    {
        #region  Contructor

        /// <summary>
        /// Initializes a new instance of the Quick.Communication.TcpClientEx using default packet spliter. 
        /// The default packet spliter is SimplePacketSpliter().
        /// </summary>
        /// <param name="autoReconnect">true if use auto reconnect feature; otherwise, false.</param>
        public TcpClientEx(bool autoReconnect)
            : this(autoReconnect, new SimplePacketSpliter())
        {

        }

        /// <summary>
        /// Initializes a new instance of the Quick.Communication.TcpClientEx using specific packet spliter without auto reconnect feature.
        /// </summary>
        /// <param name="packetSpliter">The spliter which is used to split stream data into packets.</param>
        public TcpClientEx(IPacketSpliter packetSpliter)
            : this(false, packetSpliter)
        {

        }

        /// <summary>
        /// Initializes a new instance of the Quick.Communication.TcpClientEx.
        /// </summary>
        /// <param name="autoReconnect">true if use auto reconnect feature; otherwise, false.</param>
        /// <param name="packetSpliter">The spliter which is used to split stream data into packets.</param>
        public TcpClientEx(bool autoReconnect, IPacketSpliter packetSpliter)
        {
            byte[] array = Guid.NewGuid().ToByteArray();
            _clientID = BitConverter.ToInt64(array, 0);
            _packetSpliter = packetSpliter;
            _autoReconnect = autoReconnect;
            _tcpConfig = new TcpClientConfig();
            _reconnectWaitEvent = new ManualResetEvent(false);
        }

        #endregion

        #region Private Members
        private Socket _clientSocket;
        private ManualResetEvent _reconnectWaitEvent;
        private string[] _groupArray;
        #endregion

        #region Properties

        /// <summary>
        /// Gets the configuration of tcp client.
        /// </summary>
        public TcpClientConfig ClientConfig
        {
            get { return (TcpClientConfig)_tcpConfig; }
        }

        /// <summary>
        /// Gets or sets an user defined object.
        /// </summary>
        public object Tag { get; set; }

        private long _clientID;
        /// <summary>
        /// Gets or sets client id in long type.
        /// </summary>
        public long ClientID
        {
            get { return _clientID; }
        }

        private ClientStatus _clientStatus = ClientStatus.Closed;
        /// <summary>
        /// Gets or sets client status.
        /// </summary>
        /// <returns>The client status. The default is ClientStatus.Closed.</returns>
        public ClientStatus Status
        {
            get { return _clientStatus; }
        }


        private IPEndPoint _serverEndPoint;
        /// <summary>
        /// Gets or sets server ip end point.
        /// </summary>
        public IPEndPoint ServerIPEndPoint
        {
            get { return _serverEndPoint; }
        }

        private bool _autoReconnect;

        /// <summary>
        /// Gets or sets a value which can enable or disable auto reconnect feature.
        /// Setting the property during running time will cause an exception.
        /// </summary>
        /// <returns>true if enable auto reconnect feature; otherwise, false. The default is false.</returns>
        public bool AutoReconnect
        {
            get { return _autoReconnect; }
            set
            {
                if (_autoReconnect == value)
                {
                    return;
                }
                else
                {
                    if (_isRunning)
                    {
                        throw new Exception("can not change auto reconnect value during running time");
                    }
                    else
                    {
                        _autoReconnect = value;
                    }
                }
            }
        }

        /// <summary>
        /// Gets the group array that the client has joined. The default is null.
        /// </summary>
        public string[] GroupArray
        {
            get { return _groupArray; }
        }
        #endregion

        #region Events
        /// <summary>
        /// Represents the method that will handle the client status changed event of a Quick.Communication.TcpClientEx object.
        /// </summary>
        public event EventHandler<ClientStatusChangedEventArgs> ClientStatusChanged;
        /// <summary>
        /// Represents the method that will handle the message received event of a Quick.Communication.TcpClientEx object.
        /// </summary>
        public event EventHandler<MessageReceivedEventArgs> MessageReceived;
        #endregion

        #region Private Functions

        protected override void OnRawMessageReceived(MessageReceivedEventArgs args)
        {
            if (ReceivedMessageFilter(args))
            {
                return;
            }
            MessageReceived?.Invoke(this, args);
        }

        private void BeforeConnect(string strIp, int nPort, int timeOut)
        {
            if (nPort <= 0 || nPort >= 65535)
            {
                throw new Exception("Invalid port");
            }
            if (timeOut < 2000)
            {
                throw new Exception("time out must bigger than 2000");
            }
            IPAddress ipAddress;
            if (!IPAddress.TryParse(strIp, out ipAddress))
            {
                throw new Exception("Invalid ip address");
            }

            _reconnectWaitEvent.Reset();
            _serverEndPoint = new IPEndPoint(ipAddress, nPort);
            InitClientSocket();
            _isRunning = true;
        }

        private void InitClientSocket()
        {
            _clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            if (ClientConfig.EnableKeepAlive)
            {
                TcpUtility.SetKeepAlive(_clientSocket, ClientConfig.KeepAliveTime, ClientConfig.KeepAliveInterval);
            }
        }

        private void SetStatusAndNotify(ClientStatus status)
        {
            if (status == _clientStatus)
            {
                return;
            }
            _clientStatus = status;
            var args = new ClientStatusChangedEventArgs(_clientID, (IPEndPoint)_serverEndPoint, _clientStatus);

            Task.Factory.StartNew((obj) =>
            {
                ClientStatusChanged?.Invoke(this, (ClientStatusChangedEventArgs)obj);
            }, args);
        }

        private void AfterConnect(bool isConnected)
        {
            if (!_isRunning)
            {
                return;
            }
            if (isConnected)
            {
                AddNewClient(_clientSocket);
            }
            else
            {
                _clientSocket.Close();
                _clientSocket.Dispose();
                if (!_autoReconnect)
                {
                    _isRunning = false;
                    SetStatusAndNotify(ClientStatus.Closed);
                }
                else
                {
                    //if auto reconnect is used, we should do reconnect
                    ThreadEx.Start(() =>
                    {
                        //reconnect to server 2 seconds later
                        _reconnectWaitEvent.WaitOne(2000);
                        if (_isRunning)
                        {
                            InitClientSocket();
                            ConnectAsyncInner(null);
                        }
                    });
                }
            }
        }

        protected override void OnClientStatusChanged(bool isInThread, ClientStatusChangedEventArgs args, HashSet<string> groups)
        {
            ClientStatus status = args.Status;
            if (status == ClientStatus.Connected)
            {
                SetStatusAndNotify(status);
            }
            else if (status == ClientStatus.Closed)
            {
                if (!_autoReconnect)
                {
                    _isRunning = false;
                    //if (isInThread)
                    //{
                    //    _syncContext.Post((state) =>
                    //    {
                    //        SetStatusAndNotify(ClientStatus.Closed);
                    //    }, null);
                    //}
                    //else
                    //{
                    //    SetStatusAndNotify(ClientStatus.Closed);
                    //}
                    SetStatusAndNotify(ClientStatus.Closed);
                }
                else
                {
                    if (!_isRunning)
                    {
                        SetStatusAndNotify(ClientStatus.Closed);
                        //_syncContext.Post((state) =>
                        //{
                        //    SetStatusAndNotify(ClientStatus.Closed);
                        //}, null);
                        return;
                    }

                    SetStatusAndNotify(ClientStatus.Connecting);
                    //_syncContext.Post((state) =>
                    //{
                    // AfterConnect(false);
                    //SetStatusAndNotify(ClientStatus.Connecting);
                    ThreadEx.Start(() =>
                    {
                        //reconnect to server 2 seconds later
                        _reconnectWaitEvent.WaitOne(2000);

                        if (_isRunning)
                        {
                            InitClientSocket();
                            ConnectAsyncInner(null);
                        }
                    });
                    //}, null);
                }
            }
        }

        private void ConnectAsyncInner(Action<bool> callback)
        {
            SetStatusAndNotify(ClientStatus.Connecting);
            _clientSocket.BeginConnect(_serverEndPoint, (asyncResult) =>
            {
                if (_isRunning)
                {
                    bool isConnected = _clientSocket != null && _clientSocket.Connected;
                    //_syncContext.Post((state) =>
                    //{
                    AfterConnect(isConnected);
                    callback?.Invoke(isConnected);
                    // }, null);
                }
            }, null);
        }

        #endregion

        #region Public Functions

        /// <summary>
        /// Set tcp client parameter.
        /// </summary>
        /// <param name="clientConfig">The client parameter, see Quick.Communication.TcpClientParam.</param>
        public void SetTcpClientParam(TcpClientConfig clientConfig)
        {
            if (_clientStatus != ClientStatus.Closed)
            {
                throw new Exception("can not set tcp client parameter during running");
            }
            _tcpConfig = clientConfig;
        }

        /// <summary>
        /// Connect to tcp server in asynchronous mode.The default timeout is 5 seconds.
        /// </summary>
        /// <param name="ipAddress">The ip address of server in string type.</param>
        /// <param name="port">The server listening port.</param>
        /// <param name="callback">The callback after the connection completed. 
        /// true if connected successfully; otherwise, false.</param>
        public void ConnectAsync(string ipAddress, int port, Action<bool> callback)
        {
            ConnectAsync(ipAddress, port, 5000, callback);
        }

        /// <summary>
        /// Connect to tcp server in asynchronous mode.
        /// </summary>
        /// <param name="ipAddress">The ip address of server in string type.</param>
        /// <param name="port">The server listening port.</param>
        /// <param name="timeout">The connection timeout in Millseconds.</param>
        /// <param name="callback">The callback after the connection completed. 
        /// true if connected successfully; otherwise, false.</param>
        public void ConnectAsync(string ipAddress, int port, int timeout, Action<bool> callback)
        {
            if (_isRunning)
            {
                throw new AlreadyRunningException("The client is running");
            }
            BeforeConnect(ipAddress, port, timeout);
            ConnectAsyncInner(callback);
        }

        /// <summary>
        /// Connect to tcp server in synchronous mode.
        /// </summary>
        /// <param name="ipAddress">The ip address of server in string type.</param>
        /// <param name="port">The server listening port.</param>
        /// <param name="timeOut">The timeout of the connection in Millseconds.The default is int.MaxValue.</param>
        /// <returns>true if connected successfully; otherwise, false. </returns>
        public bool Connect(string ipAddress, int port, int timeOut = int.MaxValue)
        {
            if (_isRunning)
            {
                throw new AlreadyRunningException("The client is running");
            }
            BeforeConnect(ipAddress, port, timeOut);
            SetStatusAndNotify(ClientStatus.Connecting);
            IAsyncResult asyncRes = _clientSocket.BeginConnect(_serverEndPoint, null, null);
            asyncRes.AsyncWaitHandle.WaitOne(timeOut);

            bool isConnected = _clientSocket != null && _clientSocket.Connected;

            AfterConnect(isConnected);
            return isConnected;
        }

        /// <summary>
        /// Close tcp client to release all the resources.
        /// </summary>
        public virtual void Close()
        {
            if (!_isRunning)
            {
                return;
            }
            _isRunning = false;
            _reconnectWaitEvent.Set();
            _clientSocket.Close();
            _clientSocket.Dispose();
            long clientID = -1;
            try
            {
                clientID = _clients.Keys.First();
            }
            catch { }
            if (clientID >= 0)
                CloseClient(false, clientID);
            else
                SetStatusAndNotify(ClientStatus.Closed);
        }

        #region Send Message

        /// <summary>
        /// Send message data to server in synchronous mode.
        /// </summary>
        /// <param name="messageData">The message data to be sent.</param>
        /// <returns>The number of bytes sent to the server.</returns>
        public int SendMessage(byte[] messageData)
        {
            return SendMessage(messageData, 0, messageData.Length);
        }

        /// <summary>
        /// Send message data to server in synchronous mode.
        /// </summary>
        /// <param name="messageData">The message data to be sent.</param>
        /// <param name="offset">The offset of the message data.</param>
        /// <param name="count">The count of bytes to be sent.</param>
        /// <returns>The number of bytes sent to the server.</returns>
        public int SendMessage(byte[] messageData, int offset, int count)
        {
            if (_clientStatus != ClientStatus.Connected || _clients.Count == 0)
            {
                throw new Exception(Constants.ExMessageClientNotConnected);
            }
            if (count <= 0)
            {
                throw new ArgumentException("The count must be greater than zero.");
            }
            Contract.Assert(messageData != null, "The messageData can not be null");
            Contract.Assert(offset >= 0, "The offset can not be less than zero");
            DynamicBufferStream sendBuffer = _clients.First().Value.SendBuffer;
            lock (sendBuffer)
            {
                ArraySegment<byte> packetForSend = _packetSpliter.MakePacket(messageData, offset, count, sendBuffer);
                int sendTotal = 0;
                int sendIndex = packetForSend.Offset;
                while (sendTotal < packetForSend.Count)
                {
                    int single = _clientSocket.Send(packetForSend.Array, sendIndex, packetForSend.Count - sendTotal, SocketFlags.None);
                    sendTotal += single;
                    sendIndex += single;
                }
            }
            return messageData.Length;
        }

        /// <summary>
        /// Send message text to server in synchronous mode with specific encoding.
        /// </summary>
        /// <param name="text">The message text to be sent.</param>
        /// <param name="encoding">The text encoding in tcp communication.</param>
        public void SendText(string text, Encoding encoding)
        {
            byte[] messageData = encoding.GetBytes(text);
            SendMessage(messageData);
        }

        /// <summary>
        /// Send message text to server in synchronous mode with default encoding, see TextEncoding property.
        /// </summary>
        /// <param name="text">The message text to be sent.</param>
        public void SendText(string text)
        {
            SendText(text, TextEncoding);
        }

        #endregion

        #region Send Group Message

        /// <summary>
        /// Join group after connected to the server. Make sure the server enable group feature before call this method.
        /// </summary>
        /// <param name="groupArray">The group array that the client to join.</param>
        public void JoinGroup(params string[] groupArray)
        {
            if (_clientStatus != ClientStatus.Connected)
            {
                throw new Exception(Constants.ExMessageClientNotConnected);
            }
            byte[] joinGroupMessage = JoinGroupMessage.MakeMessage(groupArray);
            SendMessage(joinGroupMessage);
            _groupArray = groupArray;
        }

        /// <summary>
        /// Send message data to specific group collection in synchronous mode.
        /// </summary>
        /// <param name="groupNameCollection">The group collection to receive the message.</param>
        /// <param name="messageData">The message data to be sent to group collection.</param>
        /// <param name="loopBack">True if the message returns to the sender; otherwise false. The default is false.</param>
        /// <returns>The number of bytes sent to the server.</returns>
        public int SendGroupMessage(IEnumerable<string> groupNameCollection, byte[] messageData, bool loopBack = false)
        {
            return SendGroupMessage(groupNameCollection, messageData, 0, messageData.Length, loopBack);
        }

        /// <summary>
        /// Send message data to specific group collection in synchronous mode.
        /// </summary>
        /// <param name="groupNameCollection">The group collection to receive the message.</param>
        /// <param name="messageData">The message data to be sent to group collection.</param>
        /// <param name="offset">The offset of the message data.</param>
        /// <param name="count">The count of bytes to be sent.</param>
        /// <param name="loopBack">True if the message returns to the sender; otherwise false. The default is false.</param>
        /// <returns>The number of bytes sent to the server.</returns>
        public int SendGroupMessage(IEnumerable<string> groupNameCollection, byte[] messageData, int offset, int count, bool loopBack = false)
        {
            if (_clientStatus != ClientStatus.Connected)
            {
                throw new Exception(Constants.ExMessageClientNotConnected);
            }
            if (groupNameCollection == null)
            {
                throw new Exception("the group name collection can not be null");
            }
            byte[] groupMessageData = GroupTransmitMessage.MakeGroupMessage(groupNameCollection, new ArraySegment<byte>(messageData, offset, count), loopBack);
            return SendMessage(groupMessageData);
        }

        /// <summary>
        /// Send message text to specific group collection in synchronous mode with specific encoding.
        /// </summary>
        /// <param name="groupNameCollection">The group collection to receive the message.</param>
        /// <param name="text">The message text to be sent to group collection.</param>
        /// <param name="encoding">The text encoding in tcp communication.</param>
        /// <param name="loopBack">True if the message returns to the sender; otherwise false. The default is false.</param>
        public void SendGroupText(IEnumerable<string> groupNameCollection, string text, Encoding encoding, bool loopBack = false)
        {
            byte[] messageData = encoding.GetBytes(text);
            SendGroupMessage(groupNameCollection, messageData, loopBack);
        }

        /// <summary>
        /// Send message text to specific group collection in synchronous mode with default encoding, see TextEncoding property.
        /// </summary>
        /// <param name="groupNameCollection">The group collection to receive the message.</param>
        /// <param name="text">The message text to be sent to group collection.</param>
        /// <param name="loopBack">True if the message returns to the sender; otherwise false. The default is false.</param>
        public void SendGroupText(IEnumerable<string> groupNameCollection, string text, bool loopBack = false)
        {
            SendGroupText(groupNameCollection, text, TextEncoding, loopBack);
        }

        #endregion

        #endregion
    }
}

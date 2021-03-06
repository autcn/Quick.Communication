using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
#pragma warning disable 1591
namespace Quick.Communication
{
    /// <summary>
    /// Represents TCP server based on SocketAsyncEvent(IOCP)
    /// </summary>
    public class TcpServer : TcpBase
    {
        #region Constructor

        /// <summary>
        /// Initializes a new instance of the Quick.Communication.TcpServer.The default spliter is SimplePacketSpliter().
        /// </summary>
        public TcpServer() : this(new SimplePacketSpliter())
        {

        }

        /// <summary>
        /// Initializes a new instance of the Quick.Communication.TcpServer using specific packet spliter.
        /// </summary>
        /// <param name="packetSpliter">The spliter used to split stream data into complete packets.</param>
        public TcpServer(IPacketSpliter packetSpliter)
        {
            _packetSpliter = packetSpliter;
        }
        #endregion

        #region Private members

        private int _listenPort; //listen port
        private Socket _serverSocket; //server listen socket
        private ConcurrentDictionary<string, HashSet<long>> _groups;

        #endregion

        #region Properties

        private TcpServerConfig ServerConfig
        {
            get { return (TcpServerConfig)_tcpConfig; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether to enable group feature.
        /// </summary>
        /// <returns>true if enable group feature; otherwise, false. The default is false.</returns>
        public bool EnableGroup { get; set; } = false;

        /// <summary>
        /// Get all clients.
        /// </summary>
        public ConcurrentDictionary<long, ClientContext> Clients { get { return _clients; } }

        #endregion

        #region Events

        /// <summary>
        /// Represents the method that will handle the client status changed event of a Quick.Communication.TcpServer object.
        /// </summary>
        public event EventHandler<ClientStatusChangedEventArgs> ClientStatusChanged;

        /// <summary>
        /// Represents the method that will handle the message received event of a Quick.Communication.TcpServer object.
        /// </summary>
        public event EventHandler<MessageReceivedEventArgs> MessageReceived;

        #endregion

        #region Private functions

        protected override void OnClientStatusChanged(bool isInThread, ClientStatusChangedEventArgs args, HashSet<string> clientGroups)
        {
            if (args.Status == ClientStatus.Closed)
            {
                RemoveClientFromGroup(args.ClientID, clientGroups);
            }
            ClientStatusChanged?.Invoke(this, args);
        }

        protected override void OnRawMessageReceived(MessageReceivedEventArgs args)
        {
            if (args.Error == null)
            {
                ArraySegment<byte> rawSegment = args.MessageRawData;
                if (rawSegment.Count >= 4)
                {
                    UInt32 specialMark = BitConverter.ToUInt32(rawSegment.Array, rawSegment.Offset);
                    if (specialMark == TcpUtility.JOIN_GROUP_MARK)
                    {
                        if (!EnableGroup)
                        {
                            return;
                        }

                        JoinGroupMessage joinGroupMsg = null;
                        if (!JoinGroupMessage.TryParse(rawSegment, out joinGroupMsg))
                        {
                            return;
                        }

                        if (_clients.TryGetValue(args.ClientID, out ClientContext clientContext))
                        {
                            clientContext.Groups = joinGroupMsg.GroupSet;
                            AddClientToGroup(clientContext);
                        }
                        return;
                    }
                    else if (specialMark == TcpUtility.GROUP_TRANSMIT_MSG_MARK
                        || specialMark == TcpUtility.GROUP_TRANSMIT_MSG_LOOP_BACK_MARK)
                    {
                        bool loopBack = specialMark == TcpUtility.GROUP_TRANSMIT_MSG_LOOP_BACK_MARK;
                        if (!EnableGroup)
                        {
                            return;
                        }
                        GroupTransmitMessage transPacket = null;
                        if (!GroupTransmitMessage.TryParse(rawSegment, out transPacket))
                        {
                            return;
                        }
                        try
                        {
                            if (!ServerConfig.AllowCrossGroupMessage)
                            {
                                ClientContext sourceClient = GetClient(args.ClientID);
                                if (sourceClient == null || sourceClient.Groups == null || sourceClient.Groups.Count == 0)
                                {
                                    return;
                                }
                                foreach (string groupName in transPacket.GroupNameCollection)
                                {
                                    if (!sourceClient.Groups.Contains(groupName))
                                    {
                                        return;
                                    }
                                }
                            }
                            SendGroupMessage(transPacket.GroupNameCollection, transPacket.TransMessageData.Array, transPacket.TransMessageData.Offset, transPacket.TransMessageData.Count
                                , loopBack ? -1 : args.ClientID);
                        }
                        catch
                        {
                            
                        }
                        return;
                    }
                }
                if (ReceivedMessageFilter(args))
                {
                    return;
                }
            }

            MessageReceived?.Invoke(this, args);
            return;
        }

        private void RemoveClientFromGroup(long clientID, HashSet<string> groups)
        {
            if (groups == null)
            {
                return;
            }
            foreach (string groupName in groups)
            {
                if (_groups.TryGetValue(groupName, out HashSet<long> clients))
                {
                    if (clients.Contains(clientID))
                    {
                        clients.Remove(clientID);
                    }
                }
            }
        }

        private void AddClientToGroup(ClientContext clientContext)
        {
            foreach (string groupName in clientContext.Groups)
            {
                if (!_groups.TryGetValue(groupName, out HashSet<long> clients))
                {
                    clients = new HashSet<long>();
                    _groups.TryAdd(groupName, clients);
                }
                clients.Add(clientContext.ClientID);
            }
        }

        private void ProcessAccept(SocketAsyncEventArgs sockAsyncEventArgs)
        {
            if (!_isRunning)
            {
                return;
            }
            if (sockAsyncEventArgs.SocketError == SocketError.Success)
            {
                Socket clientSocket = sockAsyncEventArgs.AcceptSocket;
                if (clientSocket.Connected)
                {
                    //ClientsHandler handler = GetFreeClientsHandler();
                    //ClientContext newClientContext = handler.AddNewClient(clientSocket);
                    AddNewClient(clientSocket);
                }
            }
            if (sockAsyncEventArgs.SocketError != SocketError.OperationAborted)
            {
                StartAccept(sockAsyncEventArgs);
            }
        }

        private void StartAccept(SocketAsyncEventArgs sockAsyncEventArgs)
        {
            if (!_isRunning)
            {
                return;
            }
            if (sockAsyncEventArgs == null)
            {
                sockAsyncEventArgs = new SocketAsyncEventArgs();
                sockAsyncEventArgs.Completed += (sender, sockAsyncArgs) =>
                {
                    ProcessAccept(sockAsyncArgs);
                };
            }
            else
            {
                //socket must be cleared since the context object is being reused
                sockAsyncEventArgs.AcceptSocket = null;
            }
            try
            {
                if (!_serverSocket.AcceptAsync(sockAsyncEventArgs))
                {
                    ProcessAccept(sockAsyncEventArgs);
                }
            }
            catch
            {
                return;
            }
        }

        private void SendGroupMessage(IEnumerable<string> groupNameCollection, byte[] messageData, int offset, int count, long exceptClientID)
        {
            if (!EnableGroup)
            {
                throw new Exception("can not send group message when EnableGroup property is false");
            }
            ArraySegment<byte> packetForSend = _packetSpliter.MakePacket(messageData, offset, count, new DynamicBufferStream());
            List<Task> tasks = new List<Task>();
            foreach (string groupName in groupNameCollection)
            {
                if (_groups.TryGetValue(groupName, out HashSet<long> clients))
                {
                    foreach (long clientID in clients)
                    {
                        if (clientID != exceptClientID && _clients.TryGetValue(clientID, out ClientContext clientContext))
                        {
                            Task task = Task.Run(() => SendMessage(clientID, packetForSend.Array, packetForSend.Offset, packetForSend.Count));
                            tasks.Add(task);
                        }
                    }
                }
            }
            Task.WaitAll(tasks.ToArray());
        }

        #endregion

        #region Public functions

        /// <summary>
        /// Add a client to specific group.
        /// </summary>
        /// <param name="clientID">The client id.</param>
        /// <param name="groupName">The group that the client to join.</param>
        public void AddClientToGroup(long clientID, string groupName)
        {
            ClientContext clientCtx = GetClient(clientID);
            if (clientCtx != null)
            {
                if (clientCtx.Groups == null)
                {
                    clientCtx.Groups = new HashSet<string>();
                }
                if (!clientCtx.Groups.Contains(groupName))
                {
                    clientCtx.Groups.Add(groupName);
                    AddClientToGroup(clientCtx);
                }
            }
        }

        /// <summary>
        /// Get if the client is in specific group.
        /// </summary>
        /// <param name="clientID">The client id.</param>
        /// <param name="groupName">The name of the group.</param>
        /// <returns></returns>
        public bool IsClientInGroup(long clientID, string groupName)
        {
            ClientContext clientCtx = GetClient(clientID);
            if (clientCtx == null || clientCtx.Groups == null)
            {
                return false;
            }
            return clientCtx.Groups.Contains(groupName);
        }

        /// <summary>
        /// Start tcp server listening on a specific port.
        /// </summary>
        /// <param name="listenPort">The listening port of the server.</param>
        public void Start(int listenPort)
        {
            Start(listenPort, TcpServerConfig.Default);
        }

        /// <summary>
        /// Start udp server listening on a specific port using TcpServerParam parameter.
        /// </summary>
        /// <param name="listenPort">The listening port of the server.</param>
        /// <param name="serverConfig">The server's parameter, see TcpServerParam.</param>
        public void Start(int listenPort, TcpServerConfig serverConfig)
        {
            if (_isRunning)
            {
                throw new AlreadyRunningException(Constants.ExMessageServerAlreadyRunning);
            }
            // _clientContextPool = new ClientContextPool(serverConfig.MaxClientCount, serverConfig.SocketAsyncBufferSize);
            Contract.Requires(listenPort > 0 && listenPort < 65535);
            Contract.Requires(serverConfig != null);
            _tcpConfig = serverConfig;
            _listenPort = listenPort;
            try
            {
                //create listen socket
                _serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                if (ServerConfig.EnableKeepAlive)
                {
                    TcpUtility.SetKeepAlive(_serverSocket, ServerConfig.KeepAliveTime, ServerConfig.KeepAliveInterval);
                }
                _serverSocket.Bind(new IPEndPoint(IPAddress.Any, listenPort));
                _serverSocket.Listen(ServerConfig.MaxPendingCount);
                _groups = new ConcurrentDictionary<string, HashSet<long>>();
            }
            catch (Exception ex)
            {
                throw new Exception(Constants.ExMessageStartServerFailed, ex);
            }

            _isRunning = true;
            StartAccept(null);
        }

        /// <summary>
        /// Stop the server and release all the resources.
        /// </summary>
        public void Stop()
        {
            if (!_isRunning)
            {
                return;
            }
            _isRunning = false;
            _serverSocket.Close();
            _serverSocket.Dispose();
            _serverSocket = null;
            foreach (ClientContext clientContext in _clients.Values)
            {
                IPEndPoint ipEndPt = clientContext.IPEndPoint;
                clientContext.Status = ClientStatus.Closed;
                clientContext.ClientSocket.Close();
                clientContext.ClientSocket.Dispose();
            }
            _clients.Clear();
            _groups.Clear();
        }

        /// <summary>
        /// Get one client context by client id.
        /// </summary>
        /// <param name="clientID">The client id in long type.</param>
        /// <returns>The client context if the client exist; otherwise, null.</returns>
        public ClientContext GetClient(long clientID)
        {
            if (!_isRunning)
            {
                throw new Exception(Constants.ExMessageServerNotRunning);
            }
            ClientContext clientContext = null;
            _clients.TryGetValue(clientID, out clientContext);
            return clientContext;
        }

        /// <summary>
        /// Gets clients from specific group.
        /// </summary>
        /// <param name="groupName">The group of the clients.</param>
        /// <returns>The list of the client context.</returns>
        public List<ClientContext> GetGroupClients(string groupName)
        {
            List<ClientContext> results = new List<ClientContext>();
            if (_groups.TryGetValue(groupName, out HashSet<long> clients))
            {
                foreach (long clientID in clients)
                {
                    if (_clients.TryGetValue(clientID, out ClientContext clientContext))
                    {
                        results.Add(clientContext);
                    }
                }
            }
            return results;
        }

        /// <summary>
        /// Close client connection by client id.
        /// </summary>
        /// <param name="clientID">The client id to disconnect from the server.</param>
        public void CloseClient(long clientID)
        {
            base.CloseClient(false, clientID);
        }

        /// <summary>
        /// Send message data to specific client in synchronous mode.
        /// </summary>
        /// <param name="clientID">The client id to receive message.</param>
        /// <param name="messageData">The message data to be sent.</param>
        /// <returns>The number of bytes sent to the server.</returns>
        public int SendMessage(long clientID, byte[] messageData)
        {
            return SendMessage(clientID, messageData, 0, messageData.Length);
        }

        /// <summary>
        /// Send message data to specific client in synchronous mode.
        /// </summary>
        /// <param name="clientID">The client id to receive message.</param>
        /// <param name="offset">The offset of the message data.</param>
        /// <param name="count">The count of bytes to be sent.</param>
        /// <param name="messageData">The message data to be sent.</param>
        /// <returns>The number of bytes sent to the server.</returns>
        public int SendMessage(long clientID, byte[] messageData, int offset, int count)
        {
            if (!_isRunning)
            {
                throw new Exception(Constants.ExMessageServerNotRunning);
            }
            if (count <= 0)
            {
                throw new ArgumentException(Constants.ExMessageCountInvalid);
            }
            Contract.Assert(messageData != null, Constants.ExMessageMsgDataInvalid);
            Contract.Assert(offset >= 0, Constants.ExMessageOffsetInvalid);
            ClientContext clientContext = null;
            _clients.TryGetValue(clientID, out clientContext);
            if (clientContext == null)
            {
                throw new Exception(Constants.ExMessageClientNotExist);
            }

            if (clientContext.Status != ClientStatus.Connected)
            {
                throw new Exception(Constants.ExMessageClientNotConnected);
            }

            lock (clientContext.SendBuffer)
            {
                ArraySegment<byte> packetForSend = _packetSpliter.MakePacket(messageData, offset, count, clientContext.SendBuffer);
                int sendTotal = 0;
                int sendIndex = packetForSend.Offset;
                while (sendTotal < packetForSend.Count)
                {
                    int single = clientContext.ClientSocket.Send(packetForSend.Array, sendIndex, packetForSend.Count - sendTotal, SocketFlags.None);
                    sendTotal += single;
                    sendIndex += single;
                }
                return sendTotal;
            }
        }

        /// <summary>
        /// Send message text to specific client in synchronous mode with default encoding, see TextEncoding property.
        /// </summary>
        /// <param name="clientID">The client id to send messsage.</param>
        /// <param name="text">The message text to be sent.</param>
        /// <returns>The number of bytes sent to the server.</returns>
        public void SendText(long clientID, string text)
        {
            SendMessage(clientID, TextEncoding.GetBytes(text));
        }

        /// <summary>
        /// Send message data to all clients in asynchronous mode.
        /// </summary>
        /// <param name="messageData">The message data to be sent.</param>
        /// <returns>An System.IAsyncResult collection that references the asynchronous send.</returns>
        public void BroadcastMessage(byte[] messageData)
        {
            BroadcastMessage(messageData, 0, messageData.Length);
        }

        /// <summary>
        /// Send message data to all clients in asynchronous mode.
        /// </summary>
        /// <param name="messageData">The message data to be sent.</param>
        /// <param name="offset">The offset of the message data.</param>
        /// <param name="count">The count of bytes to be sent.</param>
        /// <returns>An System.IAsyncResult collection that references the asynchronous send.</returns>
        public void BroadcastMessage(byte[] messageData, int offset, int count)
        {
            if (!_isRunning)
            {
                throw new Exception(Constants.ExMessageServerNotRunning);
            }
            List<Task> tasks = new List<Task>();
            foreach (long clientId in _clients.Keys.ToList())
            {
                Task task = Task.Run(() => SendMessage(clientId, messageData, offset, count));
                tasks.Add(task);
            }
            Task.WaitAll(tasks.ToArray());
        }

        /// <summary>
        /// Send message text to all clients in asynchronous mode.
        /// </summary>
        /// <param name="text">The message text to be sent.</param>
        /// <returns>An System.IAsyncResult collection that references the asynchronous send.</returns>
        public void BroadcastText(string text)
        {
            BroadcastMessage(TextEncoding.GetBytes(text));
        }

        /// <summary>
        /// Send message data to specific clients.
        /// </summary>
        /// <param name="groupNameCollection">The group collection to receive the message.</param>
        /// <param name="messageData">The message data to be sent.</param>
        /// <returns>An System.IAsyncResult collection that references the asynchronous send.</returns>
        public void SendGroupMessage(IEnumerable<string> groupNameCollection, byte[] messageData)
        {
            SendGroupMessage(groupNameCollection, messageData, 0, messageData.Length);
        }

        /// <summary>
        /// Send message data to specific group collection.
        /// </summary>
        /// <param name="groupNameCollection">The group collection to receive the message.</param>
        /// <param name="messageData">The message data to be sent to group collection.</param>
        /// <param name="offset">The offset of the message data.</param>
        /// <param name="count">The count of bytes to be sent.</param>
        public void SendGroupMessage(IEnumerable<string> groupNameCollection, byte[] messageData, int offset, int count)
        {
            if (!_isRunning)
            {
                throw new Exception(Constants.ExMessageServerNotRunning);
            }
            SendGroupMessage(groupNameCollection, messageData, offset, count, -1);
        }

        /// <summary>
        /// Send message text to specific group collection with default encoding, see TextEncoding property.
        /// </summary>
        /// <param name="groupNameCollection">The group collection to receive the message.</param>
        /// <param name="text">The message text to be sent.</param>
        public void SendGroupText(IEnumerable<string> groupNameCollection, string text)
        {
            SendGroupMessage(groupNameCollection, TextEncoding.GetBytes(text));
        }

        #endregion
    }
}


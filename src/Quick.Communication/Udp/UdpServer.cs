using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Quick.Communication
{
    /// <summary>
    /// Represents an easy object for udp communication.
    /// </summary>
    public class UdpServer
    {
        #region Private Members
        private bool _isRunning = false;
        private ushort _serverPort;
        private Thread _recvThread;
        private UdpClient _udpServer;
        private UdpClient _udpSender;
        private IPEndPoint _listenEndPoint;
        private string _multicastAddress;
        private SingleThreadTaskScheduler _taskScheduler;
        #endregion

        #region Properties

        /// <summary>
        /// Gets a value indicating the running status of the udp server.
        /// </summary>
        /// <returns>true if the server is running; otherwise, false. The default is false.</returns>
        public bool IsRunning { get { return _isRunning; } }

        /// <summary>
        /// Gets or sets the encoding when transmitting text in udp communicaiton.
        /// </summary>
        /// <returns>The encoding when transmitting text in udp communication. The default is UTF8.</returns>
        public Encoding TextEncoding { get; set; } = Encoding.UTF8;

        /// <summary>
        /// Gets the listening port.
        /// </summary>
        public int ServerPort => _serverPort;

        #endregion

        #region Events
        /// <summary>
        /// Represents the method that will handle the message received event of a Quick.Communication.Udp.UdpServer object.
        /// </summary>
        public event EventHandler<UdpMessageReceivedEventArgs> MessageReceived; //收到数据
        #endregion

        #region Private Functions
        private void RecvProc()
        {
            _isRunning = true;
            while (_isRunning)
            {
                try
                {
                    IPEndPoint remoteEndPoint = null;
                    byte[] receivedData = _udpServer.Receive(ref remoteEndPoint);
                    Task.Factory.StartNew(() =>
                    {
                        MessageReceived?.Invoke(this, new UdpMessageReceivedEventArgs(remoteEndPoint, receivedData));
                    }, CancellationToken.None, TaskCreationOptions.None, _taskScheduler);
                }
                catch
                {
                    break;
                }
            }
        }

        #endregion

        #region Public Functions


        /// <summary>
        /// Start udp server listening on a specific port.
        /// </summary>
        /// <param name="port">The listening port of the server.</param>
        public void Start(ushort port)
        {
            Start(null, port);
        }

        /// <summary>
        /// Start udp server listening on a specific port and join a mulitcast group.
        /// </summary>
        /// <param name="multicastAddress">The multicast address to join.</param>
        /// <param name="nPort">The listening port of the server.</param>
        public void Start(string multicastAddress, ushort nPort)
        {
            if (_isRunning)
            {
                throw new AlreadyRunningException(Constants.ExMessageServerAlreadyRunning);
            }
            if (nPort <= 0 || nPort >= 65535)
            {
                throw new ArgumentException("invalid port number");
            }
            _serverPort = nPort;
            try
            {
                _listenEndPoint = new IPEndPoint(IPAddress.Any, _serverPort);
                _udpServer = new UdpClient(_listenEndPoint);
                _udpSender = new UdpClient();
                if (!string.IsNullOrEmpty(multicastAddress))
                {
                    _multicastAddress = multicastAddress;
                    _udpServer.JoinMulticastGroup(IPAddress.Parse(multicastAddress));
                }

                _taskScheduler = new SingleThreadTaskScheduler();
                _recvThread = ThreadEx.Start(RecvProc);
            }
            catch (System.Exception ex)
            {
                if (_udpServer != null)
                {
                    _udpServer.Close();
                }
                throw new Exception("udp server start failed." + ex.Message);
            }
        }

        /// <summary>
        /// Stop the udp server
        /// </summary>
        public void Stop()
        {
            if (!_isRunning)
            {
                return;
            }
            _isRunning = false;
            _udpServer.Close();
            _taskScheduler.Stop();
            _recvThread.Join(500);
            _udpSender.Close();
        }

        /// <summary>
        /// Send message data to specific IPEndPoint.
        /// </summary>
        /// <param name="ipEndPoint">The destination IPEndPoint where the message will be sent to.</param>
        /// <param name="messageData">The message data for sending.</param>
        /// <param name="length">The length of the message data for sending.</param>
        public void SendMessage(IPEndPoint ipEndPoint, byte[] messageData, int length)
        {
            if (!_isRunning)
            {
                throw new NotRunningException(Constants.ExMessageServerNotRunning);
            }
            _udpSender.BeginSend(messageData, length, ipEndPoint, null, null);
        }

        /// <summary>
        /// Send message text to specific IPEndPoint.
        /// </summary>
        /// <param name="ipEndPoint">The destination IPEndPoint where the message will be sent to.</param>
        /// <param name="text">The message text for sending.</param>
        /// <param name="encoding">The encoding of the message text, see TextEncoding property.</param>
        public void SendText(IPEndPoint ipEndPoint, string text, Encoding encoding)
        {
            byte[] data = encoding.GetBytes(text);
            SendMessage(ipEndPoint, data, data.Length);
        }

        /// <summary>
        /// Send message text to specific IPEndPoint using default encoding.
        /// </summary>
        /// <param name="ipEndPoint">The destination IPEndPoint where the message will be sent to.</param>
        /// <param name="text">The message text for sending.</param>
        public void SendText(IPEndPoint ipEndPoint, string text)
        {
            SendText(ipEndPoint, text, TextEncoding);
        }

        /// <summary>
        /// Send message data to multicast group listening on specific port.
        /// </summary>
        /// <param name="messageData">The message data to be sent.</param>
        /// <param name="length">The length of the sending data.</param>
        /// <param name="nPort">The multicast group port to receive the message.</param>
        public void SendGroupMessage(byte[] messageData, int length, ushort nPort)
        {
            if (!_isRunning)
            {
                throw new NotRunningException(Constants.ExMessageServerNotRunning);
            }
            if (string.IsNullOrEmpty(_multicastAddress))
            {
                throw new NotRunningException("the udp group is not supported by start parameter");
            }
            IPEndPoint newEndPoint = new IPEndPoint(IPAddress.Parse(_multicastAddress), nPort);
            _udpSender.BeginSend(messageData, length, newEndPoint, null, null);
        }

        /// <summary>
        /// Send message data to multicast group listening on specific port.
        /// </summary>
        /// <param name="messageData">The message data to be sent.</param>
        /// <param name="nPort">The multicast group port to receive the message.</param>
        public void SendGroupMessage(byte[] messageData, ushort nPort)
        {
            SendGroupMessage(messageData, messageData.Length, nPort);
        }

        /// <summary>
        /// Send message text to multicast group listening on specific port.
        /// </summary>
        /// <param name="text">The message text to be sent.</param>
        /// <param name="encoding">The encoding of the message text.</param>
        /// <param name="nPort">The multicast group port to receive the message.</param>
        public void SendGroupText(string text, Encoding encoding, ushort nPort)
        {
            byte[] data = encoding.GetBytes(text);
            SendGroupMessage(data, data.Length, nPort);
        }

        /// <summary>
        /// Send message text to multicast group listening port using default text encoding.
        /// </summary>
        /// <param name="text">The message text to be sent.</param>
        /// <param name="nPort">The multicast group port to receive the message.</param>
        public void SendGroupText(string text, ushort nPort)
        {
            SendGroupText(text, TextEncoding, nPort);
        }

        #endregion
    }

    /// <summary>
    /// Provides data for udp MessageReceived event.
    /// </summary>
    public class UdpMessageReceivedEventArgs : EventArgs
    {
        /// <summary>
        /// Create an instance of UdpMessageReceivedEventArgs class.
        /// </summary>
        /// <param name="iPEndPoint">The ip end point where the message is from.</param>
        /// <param name="messageData">The received message.</param>
        public UdpMessageReceivedEventArgs(IPEndPoint iPEndPoint, byte[] messageData)
        {
            IPEndPoint = iPEndPoint;
            MessageData = messageData;

        }
        /// <summary>
        /// Gets or sets the source IPEndPoint of the message.
        /// </summary>
        /// <returns>The source IPEndPoint of the message.</returns>
        public IPEndPoint IPEndPoint { get; }

        /// <summary>
        /// Gets or sets the message data that received from the network.
        /// </summary>
        /// <returns>The message data received from the network.</returns>
        public byte[] MessageData { get; }
    }

}

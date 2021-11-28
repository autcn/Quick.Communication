using Quick.Rpc;
using System;
using System.Net;

namespace Quick.Communication
{
    /// <summary>
    /// Represents the client in udp RPC communication.
    /// </summary>
    public class UdpRpcClient : UdpServer, IRpcClient
    {
        #region Constructor

        /// <summary>
        /// Create an instance of UdpRpcClient class.
        /// </summary>
        public UdpRpcClient()
        {
            _proxyGenerator = new ServiceProxyGenerator(this);
            MessageReceived += UdpRpcClient_MessageReceived;
        }

        #endregion

        #region Private members

        private ServiceProxyGenerator _proxyGenerator; //The service proxy generator for client.

        #endregion

        #region Events

        /// <summary>
        /// The event will be triggered when RPC return data received.
        /// </summary>
        public event EventHandler<RpcReturnDataEventArgs> RpcReturnDataReceived;

        #endregion

        #region Private methods

        private void UdpRpcClient_MessageReceived(object sender, UdpMessageReceivedEventArgs e)
        {
            RpcReturnDataReceived?.Invoke(this, new RpcReturnDataEventArgs(e.MessageData));
        }

        private byte[] MakePacketWithData(byte[] body)
        {
            byte[] packet = new byte[body.Length + 4];
            Buffer.BlockCopy(BitConverter.GetBytes(ServerPort), 0, packet, 0, 4);
            Buffer.BlockCopy(body, 0, packet, 4, body.Length);
            return packet;
        }

        #endregion

        #region Public methods

        /// <summary>
        /// Register the service proxy type to the channel.
        /// </summary>
        /// <param name="ipAddress">The ip address of the RPC server.</param>
        /// <param name="port">The port of the RPC server.</param>
        public void RegisterClientServiceProxy<TService>(string ipAddress, int port)
        {
            _proxyGenerator.RegisterServiceProxy<TService>(new IPEndPoint(IPAddress.Parse(ipAddress), port));
        }

        /// <summary>
        /// UnRegister the service proxy type in the channel.
        /// </summary>
        /// <typeparam name="TService">The service proxy type that will be called by user.</typeparam>
        public void UnRegisterClientServiceProxy<TService>()
        {
            _proxyGenerator.UnRegisterServiceProxy<TService>();
        }

        /// <summary>
        /// Get the service proxy from the channel.The user can use the service proxy to call RPC service.
        /// </summary>
        /// <typeparam name="TService">The service proxy type that will be called by user.</typeparam>
        /// <returns>The instance of the service proxy.</returns>
        public TService GetClientServiceProxy<TService>()
        {
            return _proxyGenerator.GetServiceProxy<TService>();
        }

        void IRpcClient.SendInvocation(SendInvocationContext context)
        {
            if (!IsRunning)
            {
                throw new Exception("The client is not running.");
            }
            IPEndPoint serverIpEndPoint = (IPEndPoint)context.ServiceToken;
            byte[] packet = MakePacketWithData(context.InvocationBytes);
            SendMessage(serverIpEndPoint, packet, packet.Length);
        }

        #endregion

    }
}

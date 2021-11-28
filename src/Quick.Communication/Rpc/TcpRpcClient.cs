using Quick.Rpc;
using System;
using System.Linq;

namespace Quick.Communication
{
    /// <summary>
    /// Represents the client in tcp RPC communication.
    /// </summary>
    public class TcpRpcClient : TcpClientEx, IRpcClient
    {
        #region Constructor

        /// <summary>
        /// Initializes a new instance of the Quick.Communication.TcpClientEx using default packet spliter. 
        /// The default packet spliter is SimplePacketSpliter().
        /// </summary>
        /// <param name="autoReconnect">true if use auto reconnect feature; otherwise, false.</param>
        public TcpRpcClient(bool autoReconnect) : base(autoReconnect)
        {
            _proxyGenerator = new ServiceProxyGenerator(this);
        }

        /// <summary>
        /// Initializes a new instance of the Quick.Communication.TcpClientEx using specific packet spliter without auto reconnect feature.
        /// </summary>
        /// <param name="packetSpliter">The spliter which is used to split stream data into packets.</param>
        public TcpRpcClient(IPacketSpliter packetSpliter) : base(packetSpliter)
        {
            _proxyGenerator = new ServiceProxyGenerator(this);
        }

        /// <summary>
        /// Initializes a new instance of the Quick.Communication.TcpClientEx.
        /// </summary>
        /// <param name="autoReconnect">true if use auto reconnect feature; otherwise, false.</param>
        /// <param name="packetSpliter">The spliter which is used to split stream data into packets.</param>
        public TcpRpcClient(bool autoReconnect, IPacketSpliter packetSpliter) : base(autoReconnect, packetSpliter)
        {
            _proxyGenerator = new ServiceProxyGenerator(this);
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

        /// <summary>
        /// Override the function to pass the received data to rpc event handler.
        /// </summary>
        /// <param name="tcpRawMessageArgs">A message received from RPC server.</param>
        /// <returns></returns>
        protected override bool ReceivedMessageFilter(MessageReceivedEventArgs tcpRawMessageArgs)
        {
            RpcReturnDataReceived?.Invoke(this, new RpcReturnDataEventArgs(tcpRawMessageArgs.MessageRawData.ToArray()));
            return false;
        }

        #endregion

        #region Public methods

        /// <summary>
        /// Register the service proxy type to the channel.
        /// </summary>
        /// <typeparam name="TService">The service proxy type that will be called by user.</typeparam>
        public void RegisterClientServiceProxy<TService>()
        {
            _proxyGenerator.RegisterServiceProxy<TService>(null);
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
            SendMessage(context.InvocationBytes);
        }

        #endregion

    }
}

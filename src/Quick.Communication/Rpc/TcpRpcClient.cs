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
            _rpcServerExecutor = new RpcServerExecutor();
        }

        /// <summary>
        /// Initializes a new instance of the Quick.Communication.TcpClientEx using specific packet spliter without auto reconnect feature.
        /// </summary>
        /// <param name="packetSpliter">The spliter which is used to split stream data into packets.</param>
        public TcpRpcClient(IPacketSpliter packetSpliter) : base(packetSpliter)
        {
            _proxyGenerator = new ServiceProxyGenerator(this);
            _rpcServerExecutor = new RpcServerExecutor();
        }

        /// <summary>
        /// Initializes a new instance of the Quick.Communication.TcpClientEx.
        /// </summary>
        /// <param name="autoReconnect">true if use auto reconnect feature; otherwise, false.</param>
        /// <param name="packetSpliter">The spliter which is used to split stream data into packets.</param>
        public TcpRpcClient(bool autoReconnect, IPacketSpliter packetSpliter) : base(autoReconnect, packetSpliter)
        {
            _proxyGenerator = new ServiceProxyGenerator(this);
            _rpcServerExecutor = new RpcServerExecutor();
        }

        #endregion

        #region Private members

        private RpcServerExecutor _rpcServerExecutor; //The service executor for RPC server.
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
            byte flag = tcpRawMessageArgs.MessageRawData.ElementAt(0);
            byte[] content = TcpRpcCommon.GetRpcContent(tcpRawMessageArgs.MessageRawData);
            if (flag == TcpRpcCommon.RpcResponse)
            {
                RpcReturnDataReceived?.Invoke(this, new RpcReturnDataEventArgs(content));
            }
            else
            {
                try
                {
                    _rpcServerExecutor.ExecuteAsync(content).ContinueWith(task =>
                    {
                        SendMessage(TcpRpcCommon.MakeRpcPacket(task.Result, TcpRpcCommon.RpcResponse));
                    });
                }
                catch
                {

                }
            }

            return false;
        }

        #endregion

        #region Public methods

        /// <summary>
        /// Add service to RPC server container.
        /// </summary>
        /// <typeparam name="TInterface">The interface of service.</typeparam>
        /// <param name="instance">The instance of the service that implement the TInterface.</param>
        public void AddLocalService<TInterface>(TInterface instance)
        {
            _rpcServerExecutor.AddService<TInterface>(instance);
        }

        /// <summary>
        /// Add service to RPC server container.
        /// </summary>
        /// <typeparam name="TInterface">The interface of service.</typeparam>
        /// <param name="constructor">The func delegate used to create service instance.</param>
        public void AddLocalService<TInterface>(Func<TInterface> constructor)
        {
            _rpcServerExecutor.AddService<TInterface>(constructor);
        }

        /// <summary>
        /// Register the service proxy type to the channel.
        /// </summary>'
        /// <param name="serviceType">The service proxy type that will be called by user.</param>
        public void RegisterRemoteServiceProxy(Type serviceType)
        {
            _proxyGenerator.RegisterServiceProxy(serviceType, null);
        }

        /// <summary>
        /// Register the service proxy type of remote side.
        /// </summary>
        /// <typeparam name="TService">The service proxy type that will be called by user.</typeparam>
        public void RegisterRemoteServiceProxy<TService>()
        {
            _proxyGenerator.RegisterServiceProxy<TService>(null);
        }

        /// <summary>
        /// UnRegister the service proxy type of remote side.
        /// </summary>
        /// <param name="serviceType">The service proxy type that will be called by user.</param>
        public void UnRegisterRemoteServiceProxy(Type serviceType)
        {
            _proxyGenerator.UnRegisterServiceProxy(serviceType);
        }

        /// <summary>
        /// UnRegister the service proxy type in the channel.
        /// </summary>
        /// <typeparam name="TService">The service proxy type that will be called by user.</typeparam>
        public void UnRegisterRemoteServiceProxy<TService>()
        {
            _proxyGenerator.UnRegisterServiceProxy<TService>();
        }

        /// <summary>
        /// Get the service proxy of remote side.The user can use the service proxy to call RPC service.
        /// </summary>
        /// <typeparam name="TService">The service proxy type that will be called by user.</typeparam>
        /// <returns>The instance of the service proxy.</returns>
        public TService GetRemoteServiceProxy<TService>()
        {
            return _proxyGenerator.GetServiceProxy<TService>();
        }

        /// <summary>
        /// Get the service proxy of remote side.The user can use the service proxy to call RPC service.
        /// </summary>
        /// <param name="serviceType">The service proxy type that will be called by user.</param>
        /// <returns>The instance of the service proxy.</returns>
        public object GetRemoteServiceProxy(Type serviceType)
        {
            return _proxyGenerator.GetServiceProxy(serviceType);
        }

        void IRpcClient.SendInvocation(SendInvocationContext context)
        {
            SendMessage(TcpRpcCommon.MakeRpcPacket(context.InvocationBytes, TcpRpcCommon.RpcRequest));
        }

        #endregion

    }
}

using Quick.Rpc;
using System;
using System.Linq;

namespace Quick.Communication
{
    /// <summary>
    /// Represents the server in tcp RPC communication.
    /// </summary>
    public class TcpRpcServer : TcpServer
    {
        #region Constructor

        /// <summary>
        /// Initializes a new instance of the Quick.Communication.RpcServer using specific packet spliter.
        /// </summary>
        /// <param name="packetSpliter">The spliter used to split stream data into complete packets.</param>
        public TcpRpcServer(IPacketSpliter packetSpliter) : base(packetSpliter)
        {
            _rpcServerExecutor = new RpcServerExecutor();
        }

        /// <summary>
        /// Create an instance of TcpRpcServer() class.
        /// </summary>
        public TcpRpcServer()
        {
            _rpcServerExecutor = new RpcServerExecutor();
        }

        #endregion

        #region Private members

        private RpcServerExecutor _rpcServerExecutor; //The service executor for RPC server.

        #endregion

        #region Private methods

        /// <summary>
        /// Override the function to pass the request data to rpc call handler.
        /// </summary>
        /// <param name="tcpRawMessageArgs">A request message from RPC client.</param>
        /// <returns>If true, message delivery to continue. Otherwise false.</returns>
        protected override bool ReceivedMessageFilter(MessageReceivedEventArgs tcpRawMessageArgs)
        {
            byte[] data = tcpRawMessageArgs.MessageRawData.ToArray();
            long clientId = tcpRawMessageArgs.ClientID;

            try
            {
                _rpcServerExecutor.ExecuteAsync(data).ContinueWith(task =>
                {
                    SendMessage(clientId, task.Result);
                });
            }
            catch
            {

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
        public void AddServerService<TInterface>(TInterface instance)
        {
            _rpcServerExecutor.AddService<TInterface>(instance);
        }

        /// <summary>
        /// Add service to RPC server container.
        /// </summary>
        /// <typeparam name="TInterface">The interface of service.</typeparam>
        /// <param name="constructor">The func delegate used to create service instance.</param>
        public void AddServerService<TInterface>(Func<TInterface> constructor)
        {
            _rpcServerExecutor.AddService<TInterface>(constructor);
        }

        #endregion

    }
}

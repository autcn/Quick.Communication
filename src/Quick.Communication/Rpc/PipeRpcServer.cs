using Quick.Rpc;
using System;
using System.Linq;

namespace Quick.Communication
{
    /// <summary>
    /// Represents the server in pipeline PRC communication.
    /// </summary>
    public class PipeRpcServer : PipeServer
    {
        #region Constructor

        /// <summary>
        /// Create an instance of PipeRpcServer class.
        /// </summary>
        public PipeRpcServer()
        {
            _rpcServerExecutor = new RpcServerExecutor();
            base.MessageReceived += PipeRpcServer_MessageReceived;
        }

        #endregion

        #region Private members

        private RpcServerExecutor _rpcServerExecutor; //The service executor for RPC server.

        #endregion

        #region Private methods

        private void PipeRpcServer_MessageReceived(object sender, DataMessageReceivedEventArgs e)
        {
            try
            {
                _rpcServerExecutor.ExecuteAsync(e.Data).ContinueWith(task =>
                {
                    SendMessage(task.Result);
                });
            }
            catch
            {

            }
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

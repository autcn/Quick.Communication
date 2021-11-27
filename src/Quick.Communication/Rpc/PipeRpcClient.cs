using Quick.Rpc;
using System;

namespace Quick.Communication
{
    /// <summary>
    /// Represents the client in pipeline RPC communication.
    /// </summary>
    public class PipeRpcClient : PipeClient, IRpcClient
    {
        #region Constructor

        /// <summary>
        /// Create an instance of PipeRpcClient class.
        /// </summary>
        public PipeRpcClient()
        {
            _proxyGenerator = new ServiceProxyGenerator(this);
            MessageReceived += PipeRpcClient_MessageReceived;
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

        private void PipeRpcClient_MessageReceived(object sender, DataMessageReceivedEventArgs e)
        {
            RpcReturnDataReceived?.Invoke(this, new RpcReturnDataEventArgs(e.Data));
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

        void IRpcClient.SendInvocation(byte[] invocationBytes, object userToken)
        {
            SendMessage(invocationBytes);
        }

        #endregion

    }
}

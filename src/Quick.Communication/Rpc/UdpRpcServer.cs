using Quick.Rpc;
using System;
using System.Net;

namespace Quick.Communication
{
    /// <summary>
    /// Represents the server in udp RPC communication.
    /// </summary>
    public class UdpRpcServer : UdpServer
    {
        #region Constructor

        /// <summary>
        /// Create an instance of UdpRpcServer class.
        /// </summary>
        public UdpRpcServer()
        {
            _rpcServerExecutor = new RpcServerExecutor();
            base.MessageReceived += UdpRpcServer_MessageReceived;
        }

        #endregion

        #region Private members

        private RpcServerExecutor _rpcServerExecutor; //The service executor for RPC server.

        #endregion

        #region Private methods

        private byte[] GetBodyData(byte[] data)
        {
            byte[] body = new byte[data.Length - 4];
            Buffer.BlockCopy(data, 4, body, 0, body.Length);
            return body;
        }

        private void UdpRpcServer_MessageReceived(object sender, UdpMessageReceivedEventArgs e)
        {
            try
            {
                int returnPort = BitConverter.ToInt32(e.MessageData, 0);
                byte[] body = GetBodyData(e.MessageData);
                IPEndPoint returnAddress = new IPEndPoint(e.IPEndPoint.Address, returnPort);
                _rpcServerExecutor.ExecuteAsync(body).ContinueWith(task =>
                {
                    SendMessage(returnAddress, task.Result, task.Result.Length);
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

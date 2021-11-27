using System;
using System.Net;
using System.Net.Sockets;

namespace Quick.Communication
{
    /// <summary>
    /// Provides common definitions and methods for tcp communication
    /// </summary>
    public static class TcpUtility
    {
        /// <summary>
        /// A 32-bit unsigned integer that represents join group mark.
        /// </summary>
        public const UInt32 JOIN_GROUP_MARK = 0xFA9FCB89;

        /// <summary>
        /// A 32-bit unsigned integer that represents transmiting message to specific group except the sender.
        /// </summary>
        public const UInt32 GROUP_TRANSMIT_MSG_MARK = 0xBCA2BAD4;

        /// <summary>
        /// A 32-bit unsigned integer that represents transmiting message to specific group.
        /// </summary>
        public const UInt32 GROUP_TRANSMIT_MSG_LOOP_BACK_MARK = 0xECA2BAD3;

        /// <summary>
        /// Max group description length in join group messge.
        /// </summary>
        public const int MaxGroupDesLength = 1024;

        /// <summary>
        /// Set socket parameters for keep alive.
        /// </summary>
        /// <param name="socket">The socket to set keep alive parameter.</param>
        /// <param name="keepAliveTime">The keep alive time.</param>
        /// <param name="keepAliveInterval">The keep alive interval.</param>
        public static void SetKeepAlive(Socket socket, uint keepAliveTime, uint keepAliveInterval)
        {
            //the following code is not supported in linux ,so try catch is used here.
            try
            {
                byte[] inValue = new byte[12];
                Buffer.BlockCopy(BitConverter.GetBytes((int)1), 0, inValue, 0, 4);
                Buffer.BlockCopy(BitConverter.GetBytes(keepAliveTime), 0, inValue, 4, 4);
                Buffer.BlockCopy(BitConverter.GetBytes(keepAliveInterval), 0, inValue, 8, 4);
                socket.IOControl(IOControlCode.KeepAliveValues, inValue, null);
            }
            catch
            {
                socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
            }
        }
    }

    /// <summary>
    /// Provides data for tcp ClientStatusChanged event.
    /// </summary>
    public class ClientStatusChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Create an instance of ClientStatusChangedEventArgs class.
        /// </summary>
        /// <param name="clientId">The client's id.</param>
        /// <param name="ipEndPoint">The end point where the message is from.</param>
        /// <param name="status">The client status.</param>
        public ClientStatusChangedEventArgs(long clientId, IPEndPoint ipEndPoint, ClientStatus status)
        {
            ClientID = clientId;
            IPEndPoint = ipEndPoint;
            Status = status;
        }

        /// <summary>
        /// Gets or sets the client id in long type.
        /// </summary>
        public long ClientID { get; }

        /// <summary>
        /// Gets or sets a value indicating the source IPEndPoint of the message.
        /// </summary>
        public IPEndPoint IPEndPoint { get; }

        /// <summary>
        /// Gets or sets the client status.
        /// </summary>
        /// <returns>The client status. The default is ClientStatus.Closed.</returns>
        public ClientStatus Status { get; } = ClientStatus.Closed;
    }


    /// <summary>
    /// Provides data for tcp MessageReceived event.
    /// </summary>
    public class MessageReceivedEventArgs : EventArgs
    {
        /// <summary>
        /// Create an instance of MessageReceivedEventArgs class.
        /// </summary>
        /// <param name="clientId">The client's id</param>
        /// <param name="error">The error occurred during the transfer.</param>
        public MessageReceivedEventArgs(long clientId, Exception error)
        {
            ClientID = clientId;
            Error = error;
        }

        /// <summary>
        /// Create an instance of MessageReceivedEventArgs class.
        /// </summary>
        /// <param name="clientId">The client's id</param>
        /// <param name="messageRawData">The received message in raw data</param>
        public MessageReceivedEventArgs(long clientId, ArraySegment<byte> messageRawData)
        {
            ClientID = clientId;
            MessageRawData = messageRawData;
        }
        /// <summary>
        /// Gets or sets the id of the client where the message from.
        /// </summary>
        public long ClientID { get; }

        /// <summary>
        /// Gets or sets the message raw data that received from the network.The raw data storage may use outside buffer.
        /// </summary>
        /// <returns>The message data received from the network.</returns>
        public ArraySegment<byte> MessageRawData { get; }

        /// <summary>
        /// Gets or sets the error of the message.
        /// </summary>
        public Exception Error { get; }
    }

}

using System;

namespace Quick.Communication
{
    /// <summary>
    /// Represents an raw tcp message object that received from the network.The data may be stored outside.
    /// </summary>
    public class TcpRawMessage
    {
        /// <summary>
        /// The id of the client where the message from.
        /// </summary>
        public long ClientID { get; set; }

        /// <summary>
        /// Gets or sets the message raw data that received from the network.The raw data storage may use outside buffer.
        /// </summary>
        /// <returns>The message data received from the network.</returns>
        public ArraySegment<byte> MessageRawData { get; set; }
    }

}

using System;

namespace Quick.Communication
{
    /// <summary>
    /// Represents a complete data packet
    /// </summary>
    public class DataPacket
    {
        /// <summary>
        /// Gets or sets the data of the packet.
        /// </summary>
        public ArraySegment<byte> Data { get; set; }

        /// <summary>
        /// Gets or sets the client id of the packet.
        /// </summary>
        public long ClientID { get; set; }
    }
}

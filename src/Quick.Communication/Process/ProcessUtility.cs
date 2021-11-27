using System;

namespace Quick.Communication
{
    /// <summary>
    /// Provides data for the DataMessageReceived event.
    /// </summary>
    public class DataMessageReceivedEventArgs : EventArgs
    {
        /// <summary>
        /// Create an instance of DataMessageReceivedEventArgs class
        /// </summary>
        /// <param name="data"></param>
        public DataMessageReceivedEventArgs(byte[] data)
        {
            Data = data;
        }
        /// <summary>
        /// Gets or sets the data of the event args.
        /// </summary>
        /// <returns>The data of the event args.</returns>
        public byte[] Data { get;  }
    }

}

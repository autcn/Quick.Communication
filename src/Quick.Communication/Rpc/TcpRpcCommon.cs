using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Quick.Communication
{
    internal static class TcpRpcCommon
    {
        public const byte RpcRequest = 1;
        public const byte RpcResponse = 2;
        public static byte[] MakeRpcPacket(byte[] payload, byte flag)
        {
            byte[] result = new byte[payload.Length + 1];
            result[0] = flag;
            Buffer.BlockCopy(payload, 0, result, 1, payload.Length);
            return result;
        }

        public static byte[] GetRpcContent(ArraySegment<byte> rawData)
        {
            return (new ArraySegment<byte>(rawData.Array, rawData.Offset + 1, rawData.Count - 1)).ToArray();
        }
    }
}

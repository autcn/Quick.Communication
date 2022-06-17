using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Quick.Communication
{
    internal static class TcpRpcCommon
    {
        public const int RpcRequest = 83983712;
        public const int RpcResponse = 90193741;
        public static byte[] MakeRpcPacket(byte[] payload, int flag)
        {
            byte[] flagBytes = BitConverter.GetBytes(flag);
            byte[] result = new byte[payload.Length + 4];
            Buffer.BlockCopy(flagBytes, 0, result, 0, 4);
            Buffer.BlockCopy(payload, 0, result, 4, payload.Length);
            return result;
        }

        public static byte[] GetRpcContent(ArraySegment<byte> rawData)
        {
            return (new ArraySegment<byte>(rawData.Array, rawData.Offset + 4, rawData.Count - 4)).ToArray();
        }

        public static int GetFlag(ArraySegment<byte> rawData)
        {
            if(rawData.Count < 4)
            {
                return -1;
            }
            return BitConverter.ToInt32(rawData.Array, rawData.Offset);
        }
    }
}

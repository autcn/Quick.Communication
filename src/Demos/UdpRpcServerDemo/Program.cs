using Quick.Communication;
using System;
using RpcProtocolDemo;
using ServerDemo.BLL;

namespace UdpRpcServerDemo
{
    class Program
    {
        static void Main(string[] args)
        {
            UdpRpcServer udpRpcServer = new UdpRpcServer();
            udpRpcServer.AddServerService<IOrderService>(new OrderService());
            udpRpcServer.Start(5001);
            Console.WriteLine("The server is running.\r\nPress any key to exit!");
            Console.ReadLine();
            udpRpcServer.Stop();
        }
    }
}

using Quick.Communication;
using System;
using RpcProtocolDemo;
using ServerDemo.BLL;

namespace TcpRpcServerDemo
{
    class Program
    {
        static void Main(string[] args)
        {
            TcpRpcServer tcpRpcServer = new TcpRpcServer();
            tcpRpcServer.AddServerService<IOrderService>(new OrderService());
            tcpRpcServer.Start(5000);
            Console.WriteLine("The server is running.\r\nPress any key to exit!");
            Console.ReadLine();
            tcpRpcServer.Stop();
        }
    }
}

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
            tcpRpcServer.AddLocalService<IOrderService>(new OrderService());
            tcpRpcServer.RegisterRemoteServiceProxy<IClientService>();
            tcpRpcServer.Start(5000);

            Console.WriteLine("The server is running.\r\nPress any key to send hello to client.");
            Console.ReadLine();
            IClientService clientService = tcpRpcServer.GetFirstClientServiceProxy<IClientService>();
            clientService.Notify("Hello, this is server");

            Console.WriteLine("Press any key to exit!");
            Console.ReadLine();
            tcpRpcServer.Stop();
        }
    }
}

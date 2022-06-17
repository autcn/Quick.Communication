using Quick.Communication;
using System;
using RpcProtocolDemo;
using ServerDemo.BLL;
using System.Text;

namespace TcpRpcServerDemo
{
    class Program
    {
        static void Main(string[] args)
        {
            TcpRpcServer tcpRpcServer = new TcpRpcServer();
            tcpRpcServer.AddLocalService<IOrderService>(new OrderService());
            tcpRpcServer.RegisterRemoteServiceProxy<IClientService>();
            tcpRpcServer.MessageReceived += TcpRpcServer_MessageReceived;
            tcpRpcServer.Start(5000);

            Console.WriteLine("The server is running.\r\nPress any key to send hello to client.");
            Console.ReadLine();
            IClientService clientService = tcpRpcServer.GetFirstClientServiceProxy<IClientService>();
            clientService.Notify("Hello, this is server");
            tcpRpcServer.BroadcastText("This is broadcast text");

            Console.WriteLine("Press any key to exit!");
            Console.ReadLine();
            tcpRpcServer.Stop();
        }

        private static void TcpRpcServer_MessageReceived(object sender, MessageReceivedEventArgs e)
        {
            byte[] data = e.MessageRawData.ToArray();
            Console.WriteLine(Encoding.UTF8.GetString(data));
        }
    }
}

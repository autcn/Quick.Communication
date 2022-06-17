using ClientDemo.BLL;
using Quick.Communication;
using RpcProtocolDemo;
using System;
using System.Linq;
using System.Text;

namespace TcpRpcClientDemo
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                TcpRpcClient tcpRpcClient = new TcpRpcClient(true);
                tcpRpcClient.RegisterRemoteServiceProxy<IOrderService>();
                tcpRpcClient.AddLocalService<IClientService>(new ClientService());
                tcpRpcClient.MessageReceived += TcpRpcClient_MessageReceived;
                tcpRpcClient.Connect("127.0.0.1", 5000);

                IOrderService orderService = tcpRpcClient.GetRemoteServiceProxy<IOrderService>();

                DateTime serverTime = orderService.GetServerTime();
                Console.WriteLine($"The server time is {serverTime}");

                orderService.RunOtherTest();
                orderService.RunMultiThreadTest();

                string sendText = Console.ReadLine();
                tcpRpcClient.SendText(sendText);

                Console.WriteLine("Press any key to exit!");
                Console.ReadLine();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.ReadLine();
            }
        }

        private static void TcpRpcClient_MessageReceived(object sender, MessageReceivedEventArgs e)
        {
            byte[] data = e.MessageRawData.ToArray();
            Console.WriteLine(Encoding.UTF8.GetString(data));
        }
    }
}

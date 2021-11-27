using ClientDemo.BLL;
using Quick.Communication;
using RpcProtocolDemo;
using System;
using System.Net;

namespace UdpRpcClientDemo
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                UdpRpcClient udpRpcClient = new UdpRpcClient();
                udpRpcClient.RegisterClientServiceProxy<IOrderService>("127.0.0.1", 5001);
                udpRpcClient.Start(5002);

                IOrderService orderService = udpRpcClient.GetClientServiceProxy<IOrderService>();

                DateTime serverTime = orderService.GetServerTime();
                Console.WriteLine($"The server time is {serverTime}");

                orderService.RunOtherTest();

                Console.WriteLine("Press any key to exit!");
                Console.ReadLine();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.ReadLine();
            }
        }
    }
}

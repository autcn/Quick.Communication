using Newtonsoft.Json;
using Quick.Communication;
using RpcProtocolDemo;
using System;
using System.Collections.Generic;
using ClientDemo.BLL;

namespace TcpRpcClientDemo
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                TcpRpcClient tcpRpcClient = new TcpRpcClient(true);
                tcpRpcClient.RegisterClientServiceProxy<IOrderService>();
                tcpRpcClient.Connect("127.0.0.1", 5000);

                IOrderService orderService = tcpRpcClient.GetClientServiceProxy<IOrderService>();

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

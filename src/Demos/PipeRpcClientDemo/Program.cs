using ClientDemo.BLL;
using Quick.Communication;
using RpcProtocolDemo;
using System;

namespace PipeRpcClientDemo
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                PipeRpcClient pipeRpcClient = new PipeRpcClient();
                pipeRpcClient.RegisterClientServiceProxy<IOrderService>();
                pipeRpcClient.Connect("PIPE_NAME");

                IOrderService orderService = pipeRpcClient.GetClientServiceProxy<IOrderService>();

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

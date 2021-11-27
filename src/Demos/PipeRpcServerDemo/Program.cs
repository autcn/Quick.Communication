using Quick.Communication;
using System;
using RpcProtocolDemo;
using ServerDemo.BLL;

namespace PipeRpcServerDemo
{
    class Program
    {
        static void Main(string[] args)
        {
            PipeRpcServer pipeRpcServer = new PipeRpcServer();
            pipeRpcServer.AddServerService<IOrderService>(new OrderService());
            pipeRpcServer.Start("PIPE_NAME");
            Console.WriteLine("The server is running.\r\nPress any key to exit!");
            Console.ReadLine();
            pipeRpcServer.Stop();
        }
    }
}

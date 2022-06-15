using RpcProtocolDemo;
using System;
using System.Collections.Generic;
using System.Text;

namespace ClientDemo.BLL
{
    public class ClientService : IClientService
    {
        public void Notify(string message)
        {
            Console.Write("Recv message from server: " + message);
        }
    }
}

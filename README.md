# Quick.Communication

The library provides a variety of RPC communication methods, include tcp, udp and pipe. It is very easy using them  to write RPC programs.

The nuget url is:  https://www.nuget.org/packages/Quick.Communication.Rpc

## Protocol 

First of all, create a dll project to define the RPC protocol. 

``` c#
public interface IOrderService
{
    LoginResult Login(LoginRequest request);
}
public class LoginRequest
{
    public string User { get; set; }
    public string Password { get; set; }
}

public class LoginResult
{
    public int UserId { get; set; }
    public bool IsSuccess { get; set; }
    public string Remark { get; set; }
    public string Token { get; set; }
}
```

## TCP

#### (1) Server

Start a server to privde RPC services.

``` c#
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
```

#### (2) Client

The followling codes illustrate how to create client to make RPC call.

``` c#
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
```

## Pipe

#### (1) Server

``` c#
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
```



#### (2) Client

``` c#
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
```



## UDP

#### (1) Server

``` c#
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
```

#### (2) Client

``` c#
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
```


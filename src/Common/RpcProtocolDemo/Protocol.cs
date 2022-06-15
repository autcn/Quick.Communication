using System;
using System.Collections.Generic;

namespace RpcProtocolDemo
{
    //Warning: Do not use object member in protocol. It can cause unpredictable problems.

    public interface IOrderService
    {
        LoginResult Login(LoginRequest request);
        bool IsUserNameExist(string userName);
        List<Order> GetUserOrderList(int userId, string token);
        double SubmitOrder(int orderId);
        void LockUser(int userId, int? days);
        int? GetUserLockDays(int userId);
        void Ping();
        DateTime GetServerTime();
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

    public class Order
    {
        public int Id { get; set; }
        public string OrderNumber { get; set; }
        public string Name { get; set; }
        public string Status { get; set; }
    }

    //The client functions
    public interface IClientService
    {
        void Notify(string message);
    }
}

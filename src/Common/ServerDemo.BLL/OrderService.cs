using RpcProtocolDemo;
using System;
using System.Collections.Generic;

namespace ServerDemo.BLL
{
    public class OrderService : IOrderService
    {
        public bool IsUserNameExist(string userName)
        {
            if (string.IsNullOrEmpty(userName))
            {
                throw new ArgumentException("The user name is required.");
            }
            userName = userName.ToLower();
            return userName == "admin" || userName == "guest";
        }

        public LoginResult Login(LoginRequest request)
        {
            if (request == null)
            {
                throw new Exception("The request parameter is required.");
            }
            LoginResult result = new LoginResult();
            if (request.User == "admin" && request.Password == "password")
            {
                result.UserId = 2334143;
                result.IsSuccess = true;
                result.Token = "2938d828s8a8823";
            }
            else
            {
                result.Remark = "The user name or password is invalid.";
            }
            return result;
        }

        public List<Order> GetUserOrderList(int userId, string token)
        {
            if (token != "2938d828s8a8823")
            {
                throw new InvalidOperationException("The token is invalid.");
            }
            List<Order> list = new List<Order>();
            for (int i = 1; i <= 3; i++)
            {
                Order order = new Order();
                order.Id = i;
                order.Name = "Name" + i.ToString();
                order.OrderNumber = "2349380185" + i.ToString();
                order.Status = "WaitForPay" + i.ToString();
                list.Add(order);
            }
            return list;
        }

        public double SubmitOrder(int orderId)
        {
            return orderId == 1 ? 0.0 : 3.0 / 7.0;
        }

        public void LockUser(int userId, int? days)
        {
            if (days == null)
            {
                Console.WriteLine($"The user {userId} is locked permanently");
            }
            else
            {
                Console.WriteLine($"The user {userId} is locked for {days.Value} days");
            }
        }

        public int? GetUserLockDays(int userId)
        {
            if (userId == 1)
            {
                return null;
            }
            return 4;
        }

        public void Ping()
        {
        }

        public DateTime GetServerTime()
        {
            return DateTime.Now;
        }
    }
}

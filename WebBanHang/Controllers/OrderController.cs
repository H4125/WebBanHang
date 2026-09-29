using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using WebBanHang.Models; // Sửa lại đúng tên thư mục Models của dự án

namespace WebBanHang.Controllers
{
    public class OrderController : Controller
    {
        private readonly PCStoreContext _context;

        // Bơm PCStoreContext thông qua Dependency Injection
        public OrderController(PCStoreContext context)
        {
            _context = context;
        }

        // Hàm hỗ trợ kiểm tra quyền Admin
        private bool IsAdmin()
        {
            int? role = HttpContext.Session.GetInt32("Role");
            return role == 1 || role == 2;
        }

        // ==========================================
        // 1. DÀNH CHO KHÁCH HÀNG (USER)
        // ==========================================

        // Hiển thị lịch sử mua hàng của user đang đăng nhập
        public IActionResult MyOrders()
        {
            string username = HttpContext.Session.GetString("Username");
            if (string.IsNullOrEmpty(username))
            {
                return RedirectToAction("Login", "Account");
            }

            var account = _context.Accounts.FirstOrDefault(a => a.Username == username);
            if (account == null) return RedirectToAction("Login", "Account");

            var customer = _context.Customers.FirstOrDefault(c => c.AccountId == account.Id);
            if (customer == null) return View(new List<Order>()); // Trả về danh sách rỗng nếu chưa có thông tin

            // Lấy danh sách đơn hàng của khách này, sắp xếp mới nhất lên đầu
            var orders = _context.Orders
                        .Where(o => o.CustomerId == customer.Id)
                        .OrderByDescending(o => o.OrderDate)
                        .ToList();

            return View(orders);
        }

        // Xem chi tiết một đơn hàng cụ thể
        public IActionResult Details(int id)
        {
            var orderDetails = _context.OrderDetails
                                       .Where(od => od.OrderId == id)
                                       .ToList();
            ViewBag.OrderId = id;
            return View(orderDetails);
        }

        // ==========================================
        // 2. DÀNH CHO QUẢN TRỊ VIÊN (ADMIN)
        // ==========================================

        // Admin xem toàn bộ đơn hàng trong hệ thống
        public IActionResult AdminManage()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

            var allOrders = _context.Orders.OrderByDescending(o => o.OrderDate).ToList();
            return View(allOrders);
        }

        // Admin cập nhật trạng thái đơn
        [HttpPost]
        public IActionResult UpdateStatus(int orderId, int orderStatus, int shippingStatus)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

            var order = _context.Orders.FirstOrDefault(o => o.Id == orderId);
            if (order != null)
            {
                order.OrderStatus = orderStatus;
                order.ShippingStatus = shippingStatus;

                // ASP.NET Core dùng SaveChanges thay cho SubmitChanges
                _context.SaveChanges();
            }
            return RedirectToAction("AdminManage");
        }
    }
}
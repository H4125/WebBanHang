using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using WebBanHang.Models;
using System.Linq;

namespace WebBanHang.Controllers
{
    public class AccountController : Controller
    {
        private readonly PCStoreContext _context;

        public AccountController(PCStoreContext context)
        {
            _context = context;
        }

        // --- 1. CHỨC NĂNG ĐĂNG KÝ ---
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Register(string username, string password, string fullname, string email, string phone, string address)
        {
            // Kiểm tra xem tên đăng nhập đã tồn tại chưa
            if (_context.Accounts.Any(a => a.Username == username))
            {
                ViewBag.Error = "Tên đăng nhập đã tồn tại!";
                return View();
            }

            // Lưu tài khoản vào bảng Account với Role = 3 (Khách hàng)
            var account = new Account
            {
                Username = username,
                Password = password,
                Role = 3
            };
            _context.Accounts.Add(account);
            _context.SaveChanges(); // Lưu để lấy được Id của Account

            // Lưu thông tin khách hàng vào bảng Customer
            var customer = new Customer
            {
                FullName = fullname,
                Email = email,
                Phone = phone,
                Address = address,
                AccountId = account.Id
            };
            _context.Customers.Add(customer);
            _context.SaveChanges();

            // Đăng ký xong chuyển hướng về trang Đăng nhập
            return RedirectToAction("Login");
        }

        // --- 2. CHỨC NĂNG ĐĂNG NHẬP ---
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Login(string username, string password)
        {
            // Tìm tài khoản khớp username và password
            var acc = _context.Accounts.FirstOrDefault(a => a.Username == username && a.Password == password);
            if (acc != null)
            {
                // Lưu thông tin vào Session
                HttpContext.Session.SetString("Username", acc.Username);
                HttpContext.Session.SetInt32("Role", acc.Role);
                HttpContext.Session.SetInt32("AccountId", acc.Id);

                return RedirectToAction("Index", "Home");
            }

            ViewBag.Error = "Sai tên đăng nhập hoặc mật khẩu!";
            return View();
        }

        // --- 3. CHỨC NĂNG ĐĂNG XUẤT ---
        public IActionResult Logout()
        {
            HttpContext.Session.Clear(); // Xóa sạch Session
            return RedirectToAction("Index", "Home");
        }
    }
}
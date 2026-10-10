using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using WebBanHang.Models;
using System.Linq;
using WebBanHang.Helpers; // KHÔNG THỂ THIẾU: Khai báo để dùng [CustomAuthorize]

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
            if (_context.Accounts.Any(a => a.Username == username))
            {
                ViewBag.Error = "Tên đăng nhập đã tồn tại!";
                return View();
            }

            var account = new Account
            {
                Username = username,
                Password = password,
                Role = 3
            };
            _context.Accounts.Add(account);
            _context.SaveChanges();

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
            var acc = _context.Accounts.FirstOrDefault(a => a.Username == username && a.Password == password);
            if (acc != null)
            {
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
            HttpContext.Session.Clear();
            return RedirectToAction("Index", "Home");
        }

        // ====================================================
        // --- 4. CHỨC NĂNG QUẢN LÝ PHÂN QUYỀN (CHỈ ROLE 1) ---
        // ====================================================

        [HttpGet]
        [CustomAuthorize(1)] // Ổ khóa: Chỉ Role 1 mới được vào
        public IActionResult ManageRoles()
        {
            // Lấy ID người đang đăng nhập để không tự hiển thị chính mình (tránh việc tự hạ quyền bản thân)
            int currentUserId = HttpContext.Session.GetInt32("AccountId") ?? 0;

            // Lấy danh sách tất cả tài khoản trừ tài khoản đang đăng nhập
            var accounts = _context.Accounts.Where(a => a.Id != currentUserId).ToList();

            return View(accounts);
        }

        [HttpPost]
        [CustomAuthorize(1)] // Ổ khóa: Chỉ Role 1 mới được thực hiện đổi quyền
        public IActionResult UpdateRole(int accountId, int newRole)
        {
            var acc = _context.Accounts.Find(accountId);
            if (acc != null)
            {
                // Chỉ cho phép gán Role 1, 2 hoặc 3
                if (newRole >= 1 && newRole <= 3)
                {
                    acc.Role = newRole;
                    _context.Accounts.Update(acc);
                    _context.SaveChanges();
                }
            }
            // Cập nhật xong load lại trang
            return RedirectToAction("ManageRoles");
        }
    }
}
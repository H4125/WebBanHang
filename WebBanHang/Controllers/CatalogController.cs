using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using WebBanHang.Models;
using System.Linq;

namespace WebBanHang.Controllers
{
    public class CatalogController : Controller
    {
        private readonly PCStoreContext _context;

        public CatalogController(PCStoreContext context)
        {
            _context = context;
        }

        // Hàm hỗ trợ kiểm tra quyền (Chỉ Role 1 và 2 được vào)
        private bool IsAdmin()
        {
            int? role = HttpContext.Session.GetInt32("Role");
            return role == 1 || role == 2;
        }

        // 1. HIỂN THỊ DANH SÁCH DANH MỤC
        public IActionResult Index()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

            var catalogs = _context.Catalogs.ToList();
            return View(catalogs);
        }

        // 2. HIỂN THỊ SẢN PHẨM THEO DANH MỤC (Đã được chuyển đổi từ file của bạn)
        public IActionResult SanPhams(int catalogId)
        {
            // Không cần tạo DataContext mới, sử dụng _context có sẵn
            var dsProduct = _context.Products
                .Where(x => x.CatalogId == catalogId)
                .ToList();

            return View(dsProduct);
        }

        // 3. THÊM DANH MỤC MỚI
        [HttpGet]
        public IActionResult Create()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");
            return View();
        }

        [HttpPost]
        public IActionResult Create(Catalog catalog)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

            if (ModelState.IsValid)
            {
                _context.Catalogs.Add(catalog);
                _context.SaveChanges();
                return RedirectToAction(nameof(Index));
            }
            return View(catalog);
        }

        // 4. SỬA DANH MỤC (Đã được chuyển đổi để dùng Model Binding thay cho Request.Form)
        [HttpGet]
        public IActionResult Edit(int id)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

            var catalog = _context.Catalogs.FirstOrDefault(x => x.Id == id);
            if (catalog == null)
            {
                return NotFound();
            }
            return View(catalog);
        }

        [HttpPost]
        public IActionResult Edit(Catalog catalog)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

            if (ModelState.IsValid)
            {
                _context.Catalogs.Update(catalog);
                _context.SaveChanges();
                return RedirectToAction(nameof(Index));
            }
            return View(catalog);
        }

        // 5. XÓA DANH MỤC (Đã được chuyển đổi lệnh DeleteOnSubmit thành Remove)
        public IActionResult Delete(int id)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

            var catalog = _context.Catalogs.FirstOrDefault(x => x.Id == id);
            if (catalog != null)
            {
                _context.Catalogs.Remove(catalog);
                _context.SaveChanges();
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
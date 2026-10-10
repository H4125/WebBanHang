using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using WebBanHang.Models;
using System.Linq;

namespace WebBanHang.Controllers
{
    public class BrandController : Controller
    {
        private readonly PCStoreContext _context;

        public BrandController(PCStoreContext context)
        {
            _context = context;
        }

        private bool IsAdmin()
        {
            int? role = HttpContext.Session.GetInt32("Role");
            return role == 1 || role == 2;
        }

        // 1. DANH SÁCH HÃNG
        public IActionResult Index()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");
            var brands = _context.Brands.ToList();
            return View(brands);
        }

        // 2. THÊM HÃNG MỚI
        [HttpGet]
        public IActionResult Create()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");
            return View();
        }

        [HttpPost]
        public IActionResult Create(Brand brand)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");
            ModelState.Remove("Products");
            if (ModelState.IsValid)
            {
                _context.Brands.Add(brand);
                _context.SaveChanges();
                return RedirectToAction(nameof(Index));
            }
            return View(brand);
        }

        // 3. SỬA HÃNG
        [HttpGet]
        public IActionResult Edit(int id)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");
            var brand = _context.Brands.Find(id);
            if (brand == null) return NotFound();
            return View(brand);
        }

        [HttpPost]
        public IActionResult Edit(Brand brand)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");
            ModelState.Remove("Products");
            if (ModelState.IsValid)
            {
                _context.Brands.Update(brand);
                _context.SaveChanges();
                return RedirectToAction(nameof(Index));
            }
            return View(brand);
        }

        // 4. XÓA HÃNG
        public IActionResult Delete(int id)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");
            var brand = _context.Brands.Find(id);
            if (brand != null)
            {
                _context.Brands.Remove(brand);
                _context.SaveChanges();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
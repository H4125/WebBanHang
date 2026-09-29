using Microsoft.AspNetCore.Mvc;
using WebBanHang.Models;
using System.Linq;

namespace WebBanHang.Controllers
{
    public class HomeController : Controller
    {
        private readonly PCStoreContext _context;

        
        public HomeController(PCStoreContext context)
        {
            _context = context;
        }

        public IActionResult Index(int page = 1)
        {
            int pageSize = 12; // 12 sản phẩm 1 trang (4 sản phẩm/hàng x 3 hàng)

            // Tính tổng số lượng sản phẩm trong database
            var totalProducts = _context.Products.Count();

            // Tính tổng số trang (Làm tròn lên: ví dụ 13 SP / 12 = 1.08 -> 2 trang)
            var totalPages = (int)Math.Ceiling((double)totalProducts / pageSize);

            // Dùng Skip và Take để cắt đúng số lượng sản phẩm của trang hiện tại
            var products = _context.Products
                .OrderByDescending(p => p.Id) // Ưu tiên hiển thị sản phẩm mới nhất lên đầu
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            // Gửi thông tin phân trang sang View để vẽ nút bấm
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;

            return View(products);
        }
        public IActionResult Contact()
        {
            return View();
        }
    }
}
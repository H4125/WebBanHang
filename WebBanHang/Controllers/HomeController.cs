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

        public IActionResult Index()
        {
            // Lấy toàn bộ danh sách sản phẩm từ SQL Server
            var products = _context.Products.ToList();
            return View(products);
        }
        public IActionResult Contact()
        {
            return View();
        }
    }
}
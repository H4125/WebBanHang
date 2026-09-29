using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using WebBanHang.Models;

namespace WebBanHang.Controllers
{
    public class HomeController : Controller
    {
        private readonly PCStoreContext _context;

        
        public HomeController(PCStoreContext context)
        {
            _context = context;
        }

        public IActionResult Index(string searchString, int? catalogId, decimal? minPrice, decimal? maxPrice)
        {
            // Đổ dữ liệu Danh mục ra Dropdown list
            ViewBag.CatalogList = new SelectList(_context.Catalogs, "Id", "CatalogName", catalogId);

            // Khởi tạo truy vấn lấy tất cả sản phẩm, kèm theo thông tin Danh mục
            var products = _context.Products.Include(p => p.Catalog).AsQueryable();

            // 1. Lọc theo tên sản phẩm (Khách hàng thường chỉ tìm theo tên, không cần tìm theo Mã SP)
            if (!string.IsNullOrEmpty(searchString))
            {
                products = products.Where(p => p.ProductName.Contains(searchString));
            }

            // 2. Lọc theo Danh mục
            if (catalogId.HasValue)
            {
                products = products.Where(p => p.CatalogId == catalogId.Value);
            }

            // 3. Lọc theo khoảng giá
            if (minPrice.HasValue)
            {
                products = products.Where(p => p.UnitPrice >= minPrice.Value);
            }
            if (maxPrice.HasValue)
            {
                products = products.Where(p => p.UnitPrice <= maxPrice.Value);
            }

            // Lưu lại giá trị tìm kiếm để hiển thị trên View
            ViewBag.SearchString = searchString;
            ViewBag.MinPrice = minPrice;
            ViewBag.MaxPrice = maxPrice;

            return View(products.ToList());
        }
        public IActionResult Contact()
        {
            return View();
        }
    }
}
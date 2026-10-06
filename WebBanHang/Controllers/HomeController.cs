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
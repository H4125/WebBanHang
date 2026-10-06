using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System;
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

        // ĐÃ GỘP CHUNG TẤT CẢ THAM SỐ TÌM KIẾM VÀ PHÂN TRANG VÀO 1 HÀM DUY NHẤT
        public IActionResult Index(string searchString, int? catalogId, decimal? minPrice, decimal? maxPrice, int page = 1)
        {
            int pageSize = 12; // 12 sản phẩm 1 trang

            // Đổ dữ liệu Danh mục ra Dropdown list
            ViewBag.CatalogList = new SelectList(_context.Catalogs, "Id", "CatalogName", catalogId);

            // 1. Khởi tạo truy vấn
            var products = _context.Products.Include(p => p.Catalog).AsQueryable();

            // 2. Xử lý các điều kiện lọc
            if (!string.IsNullOrEmpty(searchString))
            {
                products = products.Where(p => p.ProductName.Contains(searchString));
            }

            if (catalogId.HasValue)
            {
                products = products.Where(p => p.CatalogId == catalogId.Value);
            }

            if (minPrice.HasValue)
            {
                products = products.Where(p => p.UnitPrice >= minPrice.Value);
            }
            if (maxPrice.HasValue)
            {
                products = products.Where(p => p.UnitPrice <= maxPrice.Value);
            }

            // 3. Tính toán tổng số trang SAU KHI đã lọc dữ liệu
            var totalProducts = products.Count();
            var totalPages = (int)Math.Ceiling((double)totalProducts / pageSize);

            // 4. Lấy dữ liệu của trang hiện tại (Skip và Take)
            var pagedProducts = products
                .OrderByDescending(p => p.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            // 5. Lưu lại các giá trị để hiển thị trên View
            ViewBag.SearchString = searchString;
            ViewBag.MinPrice = minPrice;
            ViewBag.MaxPrice = maxPrice;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;

            return View(pagedProducts);
        }

        public IActionResult Contact()
        {
            return View();
        }
    }
}
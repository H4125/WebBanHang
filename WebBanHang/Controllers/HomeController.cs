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

        // BỔ SUNG THÊM THAM SỐ brandId VÀO HÀM
        public IActionResult Index(string searchString, int? catalogId, int? brandId, decimal? minPrice, decimal? maxPrice, int page = 1)
        {
            int pageSize = 12; // 12 sản phẩm 1 trang

            // Đổ dữ liệu Danh mục và Hãng ra Dropdown list
            ViewBag.CatalogList = new SelectList(_context.Catalogs, "Id", "CatalogName", catalogId);
            ViewBag.BrandList = new SelectList(_context.Brands, "Id", "BrandName", brandId);

            // 1. Khởi tạo truy vấn (Bổ sung Include(p => p.Brand))
            var products = _context.Products
                                   .Include(p => p.Catalog)
                                   .Include(p => p.Brand)
                                   .AsQueryable();

            // 2. Xử lý các điều kiện lọc
            if (!string.IsNullOrEmpty(searchString))
            {
                products = products.Where(p => p.ProductName.Contains(searchString));
            }

            if (catalogId.HasValue)
            {
                products = products.Where(p => p.CatalogId == catalogId.Value);
            }

            // BỘ LỌC THEO HÃNG SẢN XUẤT
            if (brandId.HasValue)
            {
                products = products.Where(p => p.BrandId == brandId.Value);
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
            ViewBag.BrandId = brandId; // Lưu trạng thái Hãng đang chọn
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
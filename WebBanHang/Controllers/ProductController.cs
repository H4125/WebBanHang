using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using WebBanHang.Models;
using System.Linq;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Hosting;
using System.IO;
using System;
using Microsoft.EntityFrameworkCore;

namespace WebBanHang.Controllers
{
    public class ProductController : Controller
    {
        private readonly PCStoreContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public ProductController(PCStoreContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        // Hàm hỗ trợ kiểm tra quyền
        private bool IsAdmin()
        {
            int? role = HttpContext.Session.GetInt32("Role");
            return role == 1 || role == 2;
        }

        // 1. DANH SÁCH & TÌM KIẾM (Đã cập nhật thêm tham số brandId)
        // 1. DANH SÁCH & TÌM KIẾM (Đã cập nhật dùng List để lọc Checkbox)
        public IActionResult Index(string searchString, List<int> catalogIds, List<int> brandIds, decimal? minPrice, decimal? maxPrice)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

            // Nạp toàn bộ danh sách Danh mục và Hãng lên View để vẽ Checkbox
            ViewBag.Catalogs = _context.Catalogs.ToList();
            ViewBag.Brands = _context.Brands.ToList();

            var products = _context.Products
                                   .Include(p => p.Catalog)
                                   .Include(p => p.Brand)
                                   .AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                products = products.Where(p => p.ProductName.Contains(searchString) || p.ProductCode.Contains(searchString));
            }

            // Lọc theo nhiều Danh mục (dùng Contains)
            if (catalogIds != null && catalogIds.Count > 0)
            {
                products = products.Where(p => p.CatalogId.HasValue && catalogIds.Contains(p.CatalogId.Value));
            }

            // Lọc theo nhiều Hãng (dùng Contains)
            if (brandIds != null && brandIds.Count > 0)
            {
                products = products.Where(p => p.BrandId.HasValue && brandIds.Contains(p.BrandId.Value));
            }

            if (minPrice.HasValue)
            {
                products = products.Where(p => p.UnitPrice >= minPrice.Value);
            }
            if (maxPrice.HasValue)
            {
                products = products.Where(p => p.UnitPrice <= maxPrice.Value);
            }

            // Giữ lại trạng thái trên giao diện
            ViewBag.SearchString = searchString;
            ViewBag.MinPrice = minPrice;
            ViewBag.MaxPrice = maxPrice;

            // Lưu lại danh sách các ID đã tick
            ViewBag.SelectedCatalogs = catalogIds ?? new List<int>();
            ViewBag.SelectedBrands = brandIds ?? new List<int>();

            return View(products.ToList());
        }

        public IActionResult Details(int id)
        {
            var product = _context.Products
                                  .Include(p => p.Catalog)
                                  .Include(p => p.Brand)
                                  .FirstOrDefault(x => x.Id == id);
            if (product == null) return NotFound();
            return View(product);
        }

        // 2. THÊM SẢN PHẨM
        [HttpGet]
        public IActionResult Create()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

            ViewBag.CatalogId = new SelectList(_context.Catalogs, "Id", "CatalogName");
            // Thêm danh sách hãng vào giao diện thêm mới
            ViewBag.BrandId = new SelectList(_context.Brands, "Id", "BrandName");

            return View();
        }

        [HttpPost]
        public IActionResult Create(Product product, IFormFile imageFile)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

            if (ModelState.IsValid)
            {
                if (imageFile != null && imageFile.Length > 0)
                {
                    string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images");
                    if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                    string uniqueFileName = Guid.NewGuid().ToString() + "_" + imageFile.FileName;
                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        imageFile.CopyTo(fileStream);
                    }
                    product.Picture = "/images/" + uniqueFileName;
                }
                else
                {
                    product.Picture = "https://via.placeholder.com/200";
                }

                _context.Products.Add(product);
                _context.SaveChanges();
                return RedirectToAction(nameof(Index));
            }

            ViewBag.CatalogId = new SelectList(_context.Catalogs, "Id", "CatalogName", product.CatalogId);
            ViewBag.BrandId = new SelectList(_context.Brands, "Id", "BrandName", product.BrandId);
            return View(product);
        }

        // 3. SỬA SẢN PHẨM
        [HttpGet]
        public IActionResult Edit(int id)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

            var product = _context.Products.Find(id);
            if (product == null) return NotFound();

            ViewBag.CatalogId = new SelectList(_context.Catalogs, "Id", "CatalogName", product.CatalogId);
            // Thêm danh sách hãng vào giao diện sửa
            ViewBag.BrandId = new SelectList(_context.Brands, "Id", "BrandName", product.BrandId);
            return View(product);
        }

        [HttpPost]
        public IActionResult Edit(int id, Product product, IFormFile imageFile)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

            var existingProduct = _context.Products.Find(id);
            if (existingProduct == null) return NotFound();

            _context.Entry(existingProduct).CurrentValues.SetValues(product);

            if (imageFile != null && imageFile.Length > 0)
            {
                string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images");
                if (!System.IO.Directory.Exists(uploadsFolder))
                {
                    System.IO.Directory.CreateDirectory(uploadsFolder);
                }

                string uniqueFileName = Guid.NewGuid().ToString() + "_" + imageFile.FileName;
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new System.IO.FileStream(filePath, System.IO.FileMode.Create))
                {
                    imageFile.CopyTo(fileStream);
                }

                existingProduct.Picture = "/images/" + uniqueFileName;
            }

            _context.Products.Update(existingProduct);
            _context.SaveChanges();

            return RedirectToAction(nameof(Index));
        }

        // 4. XÓA SẢN PHẨM
        public IActionResult Delete(int id)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

            var product = _context.Products.Find(id);
            if (product != null)
            {
                _context.Products.Remove(product);
                _context.SaveChanges();
            }
            return RedirectToAction(nameof(Index));
        }

        // ==========================================
        // CÁC CHỨC NĂNG SẮP XẾP & LỌC 
        // ==========================================
        public IActionResult SapXepSanPhamTheoDonGia()
        {
            var dsProduct = _context.Products.OrderBy(x => x.UnitPrice).ToList();
            return View("Index", dsProduct);
        }

        public IActionResult SapXepSanPhamTheoDonGiaVaTen()
        {
            var dsProduct = _context.Products
                .OrderBy(x => x.UnitPrice)
                .ThenByDescending(x => x.ProductName)
                .ToList();
            return View("Index", dsProduct);
        }

        public IActionResult DanhSachSanPhamChonLoc()
        {
            var dsProduct = _context.Products
                .Select(x => new ProductForDisplay()
                {
                    ProductCode = x.ProductCode,
                    ProductName = x.ProductName
                })
                .ToList();

            return View(dsProduct);
        }

        // ==========================================
        // XỬ LÝ STORED PROCEDURE TRONG ASP.NET CORE
        // ==========================================
        public IActionResult LayToanBo_Sp()
        {
            var dsSP = _context.Products.FromSqlRaw("EXEC ToanBoSanPham").ToList();
            return View("Index", dsSP);
        }

        public IActionResult ChiTietSanPham_Sp(int id)
        {
            var sp = _context.Products.FromSqlRaw("EXEC ChiTietSanPham {0}", id).AsEnumerable().FirstOrDefault();
            return View("Details", sp);
        }

        public IActionResult CapNhatSanPham_Sp(int id, int unitPrice)
        {
            _context.Database.ExecuteSqlRaw("EXEC CapNhatGia {0}, {1}", id, unitPrice);
            return RedirectToAction(nameof(Index));
        }

        public IActionResult XoaSanPham_Sp(int id)
        {
            _context.Database.ExecuteSqlRaw("EXEC XoaSanPham {0}", id);
            return RedirectToAction(nameof(Index));
        }
    }

    public class ProductForDisplay
    {
        public string ProductCode { get; set; }
        public string ProductName { get; set; }
    }
}
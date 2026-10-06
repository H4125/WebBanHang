using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using WebBanHang.Models;
using System.Linq;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Hosting;
using System.IO;
using System;
using Microsoft.EntityFrameworkCore; // Bổ sung thư viện này để dùng Include và thực thi lệnh SQL raw

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

        //CRUD & TÌM KIẾM
        public IActionResult Index(string searchString, int? catalogId, decimal? minPrice, decimal? maxPrice)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

            // Lấy dữ liệu Danh mục đưa ra Dropdown list trên giao diện
            ViewBag.CatalogList = new SelectList(_context.Catalogs, "Id", "CatalogName", catalogId);

            // Bắt đầu khởi tạo câu truy vấn (chưa gọi ToList() để chưa chạy query xuống SQL)
            var products = _context.Products.Include(p => p.Catalog).AsQueryable();

            // Lọc theo từ khóa (Tìm trong Tên SP hoặc Mã SP)
            if (!string.IsNullOrEmpty(searchString))
            {
                products = products.Where(p => p.ProductName.Contains(searchString) || p.ProductCode.Contains(searchString));
            }

            // Lọc theo Danh mục
            if (catalogId.HasValue)
            {
                products = products.Where(p => p.CatalogId == catalogId.Value);
            }

            // Lọc theo khoảng giá
            if (minPrice.HasValue)
            {
                products = products.Where(p => p.UnitPrice >= minPrice.Value);
            }
            if (maxPrice.HasValue)
            {
                products = products.Where(p => p.UnitPrice <= maxPrice.Value);
            }

            // Lưu lại các giá trị tìm kiếm vào ViewBag để giữ nguyên chữ trên ô nhập sau khi nhấn Tìm
            ViewBag.SearchString = searchString;
            ViewBag.MinPrice = minPrice;
            ViewBag.MaxPrice = maxPrice;

            // Chốt câu truy vấn và lấy danh sách kết quả
            return View(products.ToList());
        }

        public IActionResult Details(int id)
        {
            var product = _context.Products.Include(p => p.Catalog).FirstOrDefault(x => x.Id == id);
            if (product == null) return NotFound();
            return View(product);
        }

        [HttpGet]
        public IActionResult Create()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");
            ViewBag.CatalogId = new SelectList(_context.Catalogs, "Id", "CatalogName");
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
            return View(product);
        }

        [HttpGet]
        public IActionResult Edit(int id)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

            var product = _context.Products.Find(id);
            if (product == null) return NotFound();

            ViewBag.CatalogId = new SelectList(_context.Catalogs, "Id", "CatalogName", product.CatalogId);
            return View(product);
        }

        [HttpPost]
        public IActionResult Edit(Product product, IFormFile imageFile)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

            // 1. Lấy đúng sản phẩm gốc từ CSDL lên để đối chiếu
            var existingProduct = _context.Products.Find(product.Id);
            if (existingProduct == null) return NotFound();

            // 2. Chép toàn bộ giá trị mới từ form (bao gồm Số lượng, Tên, Giá...) đè lên dữ liệu cũ
            // Lệnh này cực kỳ thông minh: nó tự bỏ qua các trường liên kết gây lỗi, và giữ nguyên các cột không có trong form
            _context.Entry(existingProduct).CurrentValues.SetValues(product);

            // 3. Xử lý ảnh (Chỉ khi nào Admin chọn ảnh mới thì mới lưu đè)
            if (imageFile != null && imageFile.Length > 0)
            {
                string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }
            if (ModelState.IsValid)
            {
                if (imageFile != null && imageFile.Length > 0)
                {
                    string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images");
                    if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                    string uniqueFileName = Guid.NewGuid().ToString() + "_" + imageFile.FileName;
                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);
                string uniqueFileName = Guid.NewGuid().ToString() + "_" + imageFile.FileName;
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        imageFile.CopyTo(fileStream);
                    }
                    product.Picture = "/images/" + uniqueFileName;
                }
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    imageFile.CopyTo(fileStream);
                }

                existingProduct.Picture = "/images/" + uniqueFileName;
            }

            // 4. Cập nhật và lưu thay đổi xuống Database
            _context.Products.Update(existingProduct);
            _context.SaveChanges();

            return RedirectToAction(nameof(Index));
        }

        // 4. XÓA SẢN PHẨM
            ViewBag.CatalogId = new SelectList(_context.Catalogs, "Id", "CatalogName", product.CatalogId);
            return View(product);
        }

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
        // 2. CÁC CHỨC NĂNG SẮP XẾP & LỌC (Ghép từ file cũ)
        // ==========================================
        public IActionResult SapXepSanPhamTheoDonGia()
        {
            var dsProduct = _context.Products.OrderBy(x => x.UnitPrice).ToList();
            return View("Index", dsProduct); // Tái sử dụng View Index để hiển thị
        }

        public IActionResult SapXepSanPhamTheoDonGiaVaTen()
        {
            // Trong C#, kết hợp điều kiện sắp xếp dùng ThenBy/ThenByDescending
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
        // 3. XỬ LÝ STORED PROCEDURE TRONG ASP.NET CORE
        // ==========================================
        public IActionResult LayToanBo_Sp()
        {
            // Thay vì gọi context.ToanBoSanPham(), ASP.NET Core dùng FromSqlRaw
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
            // Với thao tác Update/Delete/Insert, ASP.NET Core dùng ExecuteSqlRaw
            _context.Database.ExecuteSqlRaw("EXEC CapNhatGia {0}, {1}", id, unitPrice);
            return RedirectToAction(nameof(Index));
        }

        public IActionResult XoaSanPham_Sp(int id)
        {
            _context.Database.ExecuteSqlRaw("EXEC XoaSanPham {0}", id);
            return RedirectToAction(nameof(Index));
        }
    }

    // Class phụ trợ dùng cho hàm DanhSachSanPhamChonLoc
    // Nếu bạn đã khai báo trong Models thì có thể xóa đoạn này
    public class ProductForDisplay
    {
        public string ProductCode { get; set; }
        public string ProductName { get; set; }
    }
}
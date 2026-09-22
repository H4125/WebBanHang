using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using WebBanHang.Models;
using System.Linq;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Hosting;
using System.IO;

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

        // Hàm hỗ trợ kiểm tra quyền (Chỉ Role 1 và 2 được vào)
        private bool IsAdmin()
        {
            int? role = HttpContext.Session.GetInt32("Role");
            return role == 1 || role == 2;
        }

        // 1. DANH SÁCH SẢN PHẨM
        public IActionResult Index()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

            var products = _context.Products.ToList();
            return View(products);
        }

        // 2. THÊM SẢN PHẨM
        [HttpGet]
        public IActionResult Create()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

            // Đổ danh sách Catalog ra Dropdown list
            ViewBag.CatalogId = new SelectList(_context.Catalogs, "Id", "CatalogName");
            return View();
        }

        [HttpPost]
        public IActionResult Create(Product product, IFormFile imageFile) 
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

            if (ModelState.IsValid)
            {
                // Xử lý lưu ảnh nếu người dùng có upload
                if (imageFile != null && imageFile.Length > 0)
                {
                    string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images");
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }

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
                    // Nếu không tải ảnh, gán ảnh mặc định
                    product.Picture = "https://via.placeholder.com/200";
                }

                _context.Products.Add(product);
                _context.SaveChanges();
                return RedirectToAction(nameof(Index));
            }

            ViewBag.CatalogId = new SelectList(_context.Catalogs, "Id", "CatalogName", product.CatalogId);
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
            return View(product);
        }

        [HttpPost]
        public IActionResult Edit(Product product, IFormFile imageFile) // Bổ sung tham số IFormFile
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

            if (ModelState.IsValid)
            {
                // Nếu người dùng có bấm chọn một file ảnh mới từ máy tính
                if (imageFile != null && imageFile.Length > 0)
                {
                    // Xác định thư mục lưu: wwwroot/images
                    string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images");
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder); // Tự động tạo thư mục nếu chưa có
                    }

                    // Đổi tên file để tránh trùng lặp (dùng Guid)
                    string uniqueFileName = Guid.NewGuid().ToString() + "_" + imageFile.FileName;
                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    // Copy file từ form upload vào thư mục của project
                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        imageFile.CopyTo(fileStream);
                    }

                    // Gán lại đường dẫn mới cho sản phẩm
                    product.Picture = "/images/" + uniqueFileName;
                }
                // Lưu ý: Nếu người dùng không chọn ảnh mới, giá trị product.Picture vẫn được giữ nguyên nhờ thẻ hidden ở View.

                _context.Products.Update(product);
                _context.SaveChanges();
                return RedirectToAction(nameof(Index));
            }

            ViewBag.CatalogId = new SelectList(_context.Catalogs, "Id", "CatalogName", product.CatalogId);
            return View(product);
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
    }
}
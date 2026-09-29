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

                string uniqueFileName = Guid.NewGuid().ToString() + "_" + imageFile.FileName;
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);

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
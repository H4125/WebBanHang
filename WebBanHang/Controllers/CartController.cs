using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration; // Dùng để đọc appsettings.json thay cho ConfigurationManager
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using WebBanHang.Helpers;
using WebBanHang.Models; 

namespace WebBanHang.Controllers
{
    public class CartController : Controller
    {
        // 1. Dùng Dependency Injection thay vì new DataContext
        private readonly PCStoreContext _context;
        private readonly IConfiguration _configuration;
        const string CART_KEY = "Cart";

        public CartController(PCStoreContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        // Hàm hỗ trợ lấy giỏ hàng từ Session

        public List<CartItem> GetCartItems()
        {
            var cart = HttpContext.Session.GetObjectFromJson<List<CartItem>>(CART_KEY);
            if (cart == null)
            {
                cart = new List<CartItem> ();
            }
            return cart;
        }

        public IActionResult Index()
        {
            var cart = GetCartItems();
            if (cart.Count == 0)
            {
                ViewBag.Message = "Giỏ hàng của bạn đang trống!";
            }
            else
            {
                ViewBag.TotalQuantity = cart.Sum(x => x.Quantity);
                ViewBag.TotalAmount = cart.Sum(x => x.TotalPrice);
            }
            return View(cart);
        }

        public IActionResult AddToCart(int productId)
        {
            var cart = GetCartItems();
            var item = cart.FirstOrDefault(p => p.ProductId == productId);
            var product = _context.Products.Find(productId);

            if (product == null) return RedirectToAction(nameof(Index));

            int currentQty = item != null ? item.Quantity : 0;

            // Kiểm tra tồn kho (Logic của chúng ta)
            if (currentQty + 1 > product.Quantity)
            {
                TempData["Error"] = $"Sản phẩm '{product.ProductName}' chỉ còn {product.Quantity} chiếc!";
                return RedirectToAction(nameof(Index));
            }

            if (item != null)
            {
                item.Quantity++;
            }
            else
            {
                cart.Add(new CartItem
                {
                    ProductId = product.Id,
                    ProductName = product.ProductName,
                    Picture = product.Picture,
                    UnitPrice = product.UnitPrice,
                    Quantity = 1
                });
            }

            HttpContext.Session.SetObjectAsJson(CART_KEY, cart);
            return RedirectToAction(nameof(Index));
        }

        // --- TÍNH NĂNG MỚI: Cập nhật AJAX (Đã sửa cú pháp Core) ---
        [HttpPost]
        public JsonResult UpdateQuantityAjax(int id, int quantity)
        {
            var cart = GetCartItems();
            var item = cart.FirstOrDefault(x => x.ProductId == id);
            var product = _context.Products.Find(id);

            if (item != null && product != null)
            {
                // Kiểm tra tồn kho trước khi cập nhật bằng AJAX
                if (quantity > product.Quantity)
                {
                    return Json(new { Success = false, Message = $"Chỉ còn {product.Quantity} sản phẩm trong kho." });
                }
                item.Quantity = quantity > 0 ? quantity : 1;
                HttpContext.Session.SetObjectAsJson(CART_KEY, cart);
            }

            var totalAmount = cart.Sum(x => x.TotalPrice);
            var totalQuantity = cart.Sum(x => x.Quantity);

            return Json(new
            {
                Success = true,
                ItemTotalStr = string.Format("{0:0,0} VNĐ", item?.TotalPrice ?? 0),
                TotalAmountStr = string.Format("{0:0,0} VNĐ", totalAmount),
                TotalQuantity = totalQuantity
            });
        }


        [HttpPost]
        public IActionResult UpdateCart(int productId, int quantity)
        {
            // 1. Lấy giỏ hàng hiện tại từ Session
            var cart = GetCartItems();

            // 2. Tìm sản phẩm khách hàng muốn cập nhật
            var item = cart.FirstOrDefault(x => x.ProductId == productId);

            if (item != null)
            {
                // 3. Cập nhật số lượng mới (đảm bảo số lượng luôn >= 1)
                item.Quantity = quantity > 0 ? quantity : 1;

                // 4. Lưu giỏ hàng mới đè lên giỏ hàng cũ trong Session
                HttpContext.Session.SetObjectAsJson(CART_KEY, cart);
            }

            // 5. Trả khách hàng về lại trang Giỏ hàng để xem kết quả
            return RedirectToAction("Index");
        }

        // --- TÍNH NĂNG MỚI: Xóa AJAX ---
        [HttpPost]
        public JsonResult RemoveItemAjax(int id)
        {
            var cart = GetCartItems();
            var item = cart.FirstOrDefault(x => x.ProductId == id);

            if (item != null)
            {
                cart.Remove(item);
                HttpContext.Session.SetObjectAsJson(CART_KEY, cart);
            }

            var totalAmount = cart.Sum(x => x.TotalPrice);
            var totalQuantity = cart.Sum(x => x.Quantity);

            return Json(new
            {
                Success = true,
                TotalAmountStr = string.Format("{0:0,0} VNĐ", totalAmount),
                TotalQuantity = totalQuantity,
                CartEmpty = cart.Count == 0
            });
        }

        public IActionResult Checkout()
        {
            var cart = GetCartItems();
            if (cart.Count == 0) return RedirectToAction(nameof(Index));
            return View(cart);
        }

        [HttpPost]
        public IActionResult Checkout(IFormCollection form) // ASP.NET Core dùng IFormCollection
        {
            var cart = GetCartItems();
            if (cart == null || !cart.Any()) return RedirectToAction(nameof(Index));

            string paymentMethod = form["paymentMethod"];
            int currentCustomerId = 1; // Mặc định hoặc lấy từ Session đăng nhập

            // 1. Tạo đơn hàng với các cột trạng thái mới
            Order newOrder = new Order
            {
                OrderDate = DateTime.Now,
                CustomerId = currentCustomerId,
                CustomerPhone = form["customerPhone"],
                ShippingAddress = form["shippingAddress"],
                Status = false,
                OrderStatus = 0,    // Chờ duyệt
                ShippingStatus = 0  // Chờ lấy hàng
            };

            _context.Orders.Add(newOrder); // Dùng .Add() thay cho InsertOnSubmit
            _context.SaveChanges();

            // 2. Thêm chi tiết và TRỪ TỒN KHO
            foreach (var item in cart)
            {
                OrderDetail detail = new OrderDetail
                {
                    OrderId = newOrder.Id,
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice
                };
                _context.OrderDetails.Add(detail);

                // Trừ tồn kho trong DB
                var productInDb = _context.Products.Find(item.ProductId);
                if (productInDb != null)
                {
                    productInDb.Quantity -= item.Quantity;
                    _context.Products.Update(productInDb);
                }
            }
            _context.SaveChanges(); // Dùng .SaveChanges() thay cho SubmitChanges

            // 3. Xử lý VNPAY (Đã sửa cách đọc file cấu hình Core)
            if (paymentMethod == "VNPAY")
            {
                string vnp_Url = _configuration["VNPay:BaseUrl"];
                string vnp_TmnCode = _configuration["VNPay:TmnCode"];
                string vnp_HashSecret = _configuration["VNPay:HashSecret"];
                string vnp_Returnurl = _configuration["VNPay:ReturnUrl"];

                long amount = (long)(cart.Sum(x => x.TotalPrice) * 100);

                // Khởi tạo thư viện VNPay (Giả định bạn đã có class VnPayLibrary trong Models)
                VnPayLibrary vnpay = new VnPayLibrary();
                vnpay.AddRequestData("vnp_Version", "2.1.0");
                vnpay.AddRequestData("vnp_Command", "pay");
                vnpay.AddRequestData("vnp_TmnCode", vnp_TmnCode);
                vnpay.AddRequestData("vnp_Amount", amount.ToString());
                vnpay.AddRequestData("vnp_CreateDate", DateTime.Now.ToString("yyyyMMddHHmmss"));
                vnpay.AddRequestData("vnp_CurrCode", "VND");
                vnpay.AddRequestData("vnp_IpAddr", HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1");
                vnpay.AddRequestData("vnp_Locale", "vn");
                vnpay.AddRequestData("vnp_OrderInfo", "Thanh toan don hang: " + newOrder.Id);
                vnpay.AddRequestData("vnp_OrderType", "other");
                vnpay.AddRequestData("vnp_ReturnUrl", vnp_Returnurl);
                vnpay.AddRequestData("vnp_TxnRef", newOrder.Id.ToString());

                string paymentUrl = vnpay.CreateRequestUrl(vnp_Url, vnp_HashSecret);
                return Redirect(paymentUrl);
            }

            // Thanh toán COD
            HttpContext.Session.Remove(CART_KEY);
            return RedirectToAction("OrderSuccess");
        }

        public IActionResult OrderSuccess()
        {
            return View();
        }

        public IActionResult PaymentCallback()
        {
            // Logic callback VNPay giữ nguyên luồng, chuyển cú pháp đọc QueryString sang ASP.NET Core
            var queryDictionary = HttpContext.Request.Query;
            if (queryDictionary.Count > 0)
            {
                string vnp_HashSecret = _configuration["VNPay:HashSecret"];
                VnPayLibrary vnpay = new VnPayLibrary();

                foreach (var kvp in queryDictionary)
                {
                    if (!string.IsNullOrEmpty(kvp.Key) && kvp.Key.StartsWith("vnp_"))
                    {
                        vnpay.AddResponseData(kvp.Key, kvp.Value.ToString());
                    }
                }

                long orderId = Convert.ToInt64(vnpay.GetResponseData("vnp_TxnRef"));
                long vnp_ResponseCode = Convert.ToInt64(vnpay.GetResponseData("vnp_ResponseCode"));
                bool checkSignature = vnpay.ValidateSignature(vnpay.GetResponseData("vnp_SecureHash"), vnp_HashSecret);

                if (checkSignature)
                {
                    if (vnp_ResponseCode == 0)
                    {
                        Order order = _context.Orders.FirstOrDefault(x => x.Id == orderId);
                        if (order != null)
                        {
                            order.Status = true; // Đã thanh toán
                            _context.SaveChanges();
                        }
                        HttpContext.Session.Remove(CART_KEY);
                        return RedirectToAction("OrderSuccess");
                    }
                }
            }
            return RedirectToAction("Index", "Home");
        }
    }
}
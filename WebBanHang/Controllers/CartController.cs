using Microsoft.AspNetCore.Mvc;
using WebBanHang.Models;
using WebBanHang.Helpers;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Configuration;
using System;

namespace WebBanHang.Controllers
{
    public class CartController : Controller
    {
        private readonly PCStoreContext _context;
        private readonly IConfiguration _configuration;
        const string CART_KEY = "Cart";

        public CartController(PCStoreContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        // Hàm hỗ trợ: Lấy giỏ hàng từ Session
        private List<CartItem> GetCartItems()
        {
            var cart = HttpContext.Session.GetObjectFromJson<List<CartItem>>(CART_KEY);
            return cart ?? new List<CartItem>();
        }

        // Xem giỏ hàng
        public IActionResult Index()
        {
            var cart = GetCartItems();
            ViewBag.Total = cart.Sum(item => item.TotalPrice);
            return View(cart);
        }

        // Thêm sản phẩm vào giỏ
        public IActionResult AddToCart(int productId)
        {
            var cart = GetCartItems();
            var item = cart.FirstOrDefault(p => p.ProductId == productId);

            var product = _context.Products.Find(productId);
            if (product == null)
            {
                TempData["Error"] = "Sản phẩm không tồn tại!";
                return RedirectToAction(nameof(Index));
            }

            int currentQty = item != null ? item.Quantity : 0;

            if (currentQty + 1 > product.Quantity)
            {
                TempData["Error"] = $"Rất tiếc, '{product.ProductName}' chỉ còn {product.Quantity} chiếc trong kho!";
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

        // Cập nhật số lượng
        [HttpPost]
        public IActionResult UpdateCart(int productId, int quantity)
        {
            var cart = GetCartItems();
            var item = cart.FirstOrDefault(p => p.ProductId == productId);
            if (item != null)
            {
                var product = _context.Products.Find(productId);
                if (product != null)
                {
                    int desiredQuantity = quantity > 0 ? quantity : 1;

                    // Kiểm tra tồn kho trước khi cập nhật
                    if (desiredQuantity > product.Quantity)
                    {
                        TempData["Error"] = $"Rất tiếc, '{product.ProductName}' chỉ còn {product.Quantity} chiếc trong kho!";
                    }
                    else
                    {
                        item.Quantity = desiredQuantity;
                        HttpContext.Session.SetObjectAsJson(CART_KEY, cart);
                    }
                }
            }
            return RedirectToAction(nameof(Index));
        }

        // Xóa sản phẩm khỏi giỏ
        public IActionResult RemoveCart(int productId)
        {
            var cart = GetCartItems();
            var item = cart.FirstOrDefault(p => p.ProductId == productId);
            if (item != null)
            {
                cart.Remove(item);
                HttpContext.Session.SetObjectAsJson(CART_KEY, cart);
            }
            return RedirectToAction(nameof(Index));
        }

        // Thanh toán COD
        [HttpGet]
        public IActionResult Checkout()
        {
            var username = HttpContext.Session.GetString("Username");
            if (string.IsNullOrEmpty(username))
            {
                return RedirectToAction("Login", "Account");
            }

            var cart = GetCartItems();
            if (!cart.Any()) return RedirectToAction("Index");

            var order = new Order
            {
                CustomerId = HttpContext.Session.GetInt32("AccountId"),
                OrderDate = System.DateTime.Now,
                Status = false
            };
            _context.Orders.Add(order);
            _context.SaveChanges();

            foreach (var item in cart)
            {
                var detail = new OrderDetail
                {
                    OrderId = order.Id,
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice
                };
                _context.OrderDetails.Add(detail);

                // TRỪ SỐ LƯỢNG TRONG KHO (COD)
                var productInDb = _context.Products.Find(item.ProductId);
                if (productInDb != null)
                {
                    productInDb.Quantity -= item.Quantity;
                    _context.Products.Update(productInDb);
                }
            }
            _context.SaveChanges();

            HttpContext.Session.Remove(CART_KEY);

            return RedirectToAction("CheckoutSuccess");
        }

        public IActionResult CheckoutSuccess()
        {
            return View();
        }

        // Chuyển hướng thanh toán VNPay
        public IActionResult PaymentWithVNPay()
        {
            var username = HttpContext.Session.GetString("Username");
            if (string.IsNullOrEmpty(username)) return RedirectToAction("Login", "Account");

            var cart = GetCartItems();
            if (!cart.Any()) return RedirectToAction("Index");

            var order = new Order
            {
                CustomerId = HttpContext.Session.GetInt32("AccountId"),
                OrderDate = DateTime.Now,
                Status = false
            };
            _context.Orders.Add(order);
            _context.SaveChanges();

            foreach (var item in cart)
            {
                _context.OrderDetails.Add(new OrderDetail
                {
                    OrderId = order.Id,
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice
                });
            }
            _context.SaveChanges();
            // Lưu ý: Chưa trừ số lượng kho ở đây vì khách chưa thanh toán xong

            string vnp_Returnurl = _configuration["VnPay:ReturnUrl"];
            string vnp_Url = _configuration["VnPay:BaseUrl"];
            string vnp_TmnCode = _configuration["VnPay:TmnCode"];
            string vnp_HashSecret = _configuration["VnPay:HashSecret"];

            VnPayLibrary vnpay = new VnPayLibrary();
            vnpay.AddRequestData("vnp_Version", "2.1.0");
            vnpay.AddRequestData("vnp_Command", "pay");
            vnpay.AddRequestData("vnp_TmnCode", vnp_TmnCode);
            vnpay.AddRequestData("vnp_Amount", (cart.Sum(c => c.TotalPrice) * 100).ToString("0"));
            vnpay.AddRequestData("vnp_CreateDate", DateTime.Now.ToString("yyyyMMddHHmmss"));
            vnpay.AddRequestData("vnp_CurrCode", "VND");
            vnpay.AddRequestData("vnp_IpAddr", VnPayLibrary.GetIpAddress(HttpContext));
            vnpay.AddRequestData("vnp_Locale", "vn");
            vnpay.AddRequestData("vnp_OrderInfo", "Thanh toan don hang " + order.Id);
            vnpay.AddRequestData("vnp_OrderType", "other");
            vnpay.AddRequestData("vnp_ReturnUrl", vnp_Returnurl);
            vnpay.AddRequestData("vnp_TxnRef", order.Id.ToString());

            string paymentUrl = vnpay.CreateRequestUrl(vnp_Url, vnp_HashSecret);
            return Redirect(paymentUrl);
        }

        // Nhận kết quả VNPay
        public IActionResult PaymentCallback()
        {
            var vnpay = new VnPayLibrary();

            foreach (var (key, value) in Request.Query)
            {
                if (!string.IsNullOrEmpty(key) && key.StartsWith("vnp_"))
                {
                    vnpay.AddResponseData(key, value.ToString());
                }
            }

            string vnp_HashSecret = _configuration["VnPay:HashSecret"];
            string vnp_SecureHash = Request.Query["vnp_SecureHash"];
            long orderId = Convert.ToInt64(vnpay.GetResponseData("vnp_TxnRef"));
            string vnp_ResponseCode = vnpay.GetResponseData("vnp_ResponseCode");

            bool checkSignature = vnpay.ValidateSignature(vnp_SecureHash, vnp_HashSecret);
            if (checkSignature)
            {
                if (vnp_ResponseCode == "00")
                {
                    var order = _context.Orders.Find((int)orderId);
                    if (order != null)
                    {
                        order.Status = true;

                        // TRỪ SỐ LƯỢNG TRONG KHO (VNPAY)
                        // Lấy danh sách sản phẩm của đơn hàng này để trừ
                        var orderDetails = _context.OrderDetails.Where(od => od.OrderId == orderId).ToList();
                        foreach (var detail in orderDetails)
                        {
                            var productInDb = _context.Products.Find(detail.ProductId);
                            if (productInDb != null)
                            {
                                productInDb.Quantity -= detail.Quantity;
                                _context.Products.Update(productInDb);
                            }
                        }

                        _context.SaveChanges();
                    }
                    HttpContext.Session.Remove(CART_KEY);
                    ViewBag.Message = "Thanh toán thành công! Đơn hàng của bạn đã được ghi nhận.";
                }
                else
                {
                    ViewBag.Message = "Thanh toán thất bại (Mã lỗi: " + vnp_ResponseCode + ").";
                }
            }
            else
            {
                ViewBag.Message = "Lỗi bảo mật: Chữ ký số không hợp lệ!";
            }

            return View();
        }
    }
}
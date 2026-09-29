using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using WebBanHang.Models; // Đảm bảo namespace này khớp với thư mục Models của bạn

namespace WebBanHang.Controllers
{
    public class ReportController : Controller
    {
        // Sử dụng PCStoreContext thông qua Dependency Injection giống hệt các Controller khác
        private readonly PCStoreContext _context;

        public ReportController(PCStoreContext context)
        {
            _context = context;
        }

        // 1. TRANG BÁO CÁO TỔNG QUAN (HIỂN THỊ BIỂU ĐỒ 30 NGÀY GẦN NHẤT & TOP 5)
        public IActionResult Index()
        {
            var model = new ComputerReportViewModel();

            // Lấy dữ liệu trực tiếp bằng EF Core thay vì dùng ExecuteQuery SQL thuần
            var orders = _context.Orders.ToList();
            var orderDetails = _context.OrderDetails.ToList();
            var customers = _context.Customers.ToList();
            var products = _context.Products.ToList();

            // Thống kê tổng quan
            model.TotalRevenue = orderDetails.Sum(d => d.Quantity * d.UnitPrice);
            model.TotalOrders = orders.Count;
            model.TotalComputersSold = orderDetails.Sum(d => d.Quantity);
            model.TotalCustomers = customers.Count;

            // BIỂU ĐỒ DOANH THU 30 NGÀY GẦN NHẤT (Tự động lặp qua từng ngày)
            var endDate = DateTime.Now.Date;
            var startDate = endDate.AddDays(-29); // Lấy tròn 30 ngày gần đây

            var chartLabelsList = new List<string>();
            var chartDataList = new List<decimal>();

            for (DateTime day = startDate; day <= endDate; day = day.AddDays(1))
            {
                chartLabelsList.Add(day.ToString("dd/MM"));

                var revenueInDay = (from o in orders
                                    where o.OrderDate.HasValue && o.OrderDate.Value.Date == day
                                    join d in orderDetails on o.Id equals d.OrderId
                                    select d.Quantity * d.UnitPrice).Sum();

                chartDataList.Add(revenueInDay);
            }

            model.ChartLabels = chartLabelsList.ToArray();
            model.ChartData = chartDataList.ToArray();

            // Top 5 sản phẩm bán chạy
            var topData = orderDetails
                .GroupBy(d => d.ProductId)
                .Select(g => new { ProductId = g.Key, Quantity = g.Sum(x => x.Quantity), TotalAmount = g.Sum(x => x.Quantity * x.UnitPrice) })
                .OrderByDescending(x => x.Quantity).Take(5).ToList();

            model.TopProducts = (from tp in topData
                                 join p in products on tp.ProductId equals p.Id
                                 select new TopComputerViewModel
                                 {
                                     ProductCode = p.ProductCode ?? p.Id.ToString(), // Tránh lỗi nếu chưa có mã SP
                                     ProductName = p.ProductName,
                                     Picture = p.Picture,
                                     QuantitySold = tp.Quantity,
                                     TotalRevenue = tp.TotalAmount
                                 }).ToList();

            ViewBag.ProductList = new SelectList(products, "Id", "ProductName");

            return View(model);
        }

        // 2. TRANG THỐNG KÊ CHI TIẾT SẢN PHẨM THEO TỪNG NGÀY
        public IActionResult ProductReport(int? productId, DateTime? fromDate, DateTime? toDate)
        {
            var products = _context.Products.ToList();
            var orders = _context.Orders.ToList();
            var orderDetails = _context.OrderDetails.ToList();
            var customers = _context.Customers.ToList();

            ViewBag.ProductList = new SelectList(products, "Id", "ProductName", productId);

            var filterModel = new ProductReportFilterViewModel
            {
                SelectedProductId = productId,
                FromDate = fromDate ?? DateTime.Now.AddDays(-30),
                ToDate = toDate ?? DateTime.Now
            };

            // Dữ liệu 4 thẻ thống kê tổng quan
            ViewBag.TotalRevenue = orderDetails.Sum(d => d.Quantity * d.UnitPrice);
            ViewBag.TotalOrders = orders.Count;
            ViewBag.TotalComputersSold = orderDetails.Sum(d => d.Quantity);
            ViewBag.TotalCustomers = customers.Count;

            if (productId.HasValue)
            {
                var selectedProd = products.FirstOrDefault(p => p.Id == productId.Value);
                filterModel.SelectedProductName = selectedProd != null ? selectedProd.ProductName : "";

                // Lọc đơn hàng nằm trong khoảng ngày được chọn
                var filteredOrders = orders.Where(o => o.OrderDate.HasValue
                                                    && o.OrderDate.Value.Date >= filterModel.FromDate.Value.Date
                                                    && o.OrderDate.Value.Date <= filterModel.ToDate.Value.Date).ToList();

                var queryDetails = (from d in orderDetails
                                    join o in filteredOrders on d.OrderId equals o.Id
                                    join c in customers on o.CustomerId equals c.Id into custGroup
                                    from cust in custGroup.DefaultIfEmpty()
                                    where d.ProductId == productId.Value
                                    select new ProductSalesDetailViewModel
                                    {
                                        OrderId = o.Id,
                                        OrderDate = o.OrderDate ?? DateTime.Now,
                                        CustomerName = cust != null ? cust.FullName : "Khách lẻ",
                                        Quantity = d.Quantity,
                                        UnitPrice = d.UnitPrice
                                    }).ToList();

                filterModel.SalesDetails = queryDetails;
                filterModel.FilteredTotalQuantity = queryDetails.Sum(x => x.Quantity);

                // Tính trực tiếp Doanh thu để tránh lỗi thiếu hàm TotalPrice ở model con
                filterModel.FilteredTotalRevenue = queryDetails.Sum(x => x.Quantity * x.UnitPrice);
            }

            return View(filterModel);
        }



        // 3. TRANG XUẤT HÓA ĐƠN CHO TỪNG ĐƠN HÀNG
        [HttpGet]
        public IActionResult PrintInvoice(int orderId)
        {
            // 1. Tìm đơn hàng
            var order = _context.Orders.FirstOrDefault(o => o.Id == orderId);
            if (order == null) return NotFound();

            // 2. Lấy thông tin khách hàng (nếu có)
            var customer = _context.Customers.FirstOrDefault(c => c.Id == order.CustomerId);

            // 3. Gom chi tiết đơn hàng và thông tin sản phẩm
            var orderDetails = (from d in _context.OrderDetails
                                join p in _context.Products on d.ProductId equals p.Id
                                where d.OrderId == orderId
                                select new ComputerInvoiceDetailViewModel
                                {
                                    ProductCode = p.ProductCode ?? "SP_" + p.Id,
                                    ProductName = p.ProductName,
                                    WarrantyMonths = 12, // Giả định bảo hành mặc định 12 tháng, nếu bảng Product có cột bảo hành bạn thay bằng p.Warranty nhé
                                    Quantity = d.Quantity,
                                    UnitPrice = d.UnitPrice,
                                    TotalPrice = d.Quantity * d.UnitPrice
                                }).ToList();

            // 4. Đóng gói vào ViewModel
            var model = new ComputerInvoiceViewModel
            {
                OrderId = order.Id,
                OrderDate = order.OrderDate ?? DateTime.Now,
                CustomerName = customer != null ? customer.FullName : "Khách lẻ",

                // Ưu tiên lấy số điện thoại và địa chỉ giao hàng trực tiếp trên đơn hàng (vì khách có thể nhập địa chỉ nhận khác)
                CustomerPhone = order.CustomerPhone ?? (customer != null ? customer.Phone : "N/A"),
                CustomerAddress = order.ShippingAddress ?? (customer != null ? customer.Address : "N/A"),

                TotalAmount = orderDetails.Sum(x => x.TotalPrice),
                Details = orderDetails
            };

            return View(model);
        }
    }
}
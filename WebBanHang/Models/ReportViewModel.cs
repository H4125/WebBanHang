using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace WebBanHang.Models
{
    // Model dùng cho bộ lọc thống kê theo sản phẩm và khoảng thời gian
    public class ProductReportFilterViewModel
    {
        public int? SelectedProductId { get; set; }

        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
        public DateTime? FromDate { get; set; }

        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
        public DateTime? ToDate { get; set; }

        // Kết quả thống kê lọc được
        public string SelectedProductName { get; set; }
        public int FilteredTotalQuantity { get; set; }
        public decimal FilteredTotalRevenue { get; set; }

        // Danh sách chi tiết các đơn hàng bán ra trong khoảng thời gian đã chọn
        public List<ProductSalesDetailViewModel> SalesDetails { get; set; } = new List<ProductSalesDetailViewModel> ();
    }

    public class ProductSalesDetailViewModel
    {
        public int OrderId { get; set; }
        public DateTime OrderDate { get; set; }
        public string CustomerName { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice => Quantity * UnitPrice;
    }

    // Class tổng hợp báo cáo chính
    public class ComputerReportViewModel
    {
        public decimal TotalRevenue { get; set; }
        public int TotalOrders { get; set; }
        public int TotalComputersSold { get; set; }
        public int TotalCustomers { get; set; }

        public string[] ChartLabels { get; set; }
        public decimal[] ChartData { get; set; }

        public List<TopComputerViewModel> TopProducts { get; set; }

        // Tích hợp bộ lọc
        public ProductReportFilterViewModel Filter { get; set; } = new ProductReportFilterViewModel();
    }

    public class TopComputerViewModel
    {
        public string ProductCode { get; set; }
        public string ProductName { get; set; }
        public string Picture { get; set; }
        public int QuantitySold { get; set; }
        public decimal TotalRevenue { get; set; }
    }

    // Class tổng hợp thông tin cho toàn bộ trang In Hóa Đơn
    public class ComputerInvoiceViewModel
    {
        public int OrderId { get; set; }
        public DateTime OrderDate { get; set; }
        public string CustomerName { get; set; }
        public string CustomerPhone { get; set; }
        public string CustomerAddress { get; set; }
        public decimal TotalAmount { get; set; }

        // Danh sách các sản phẩm khách mua trong hóa đơn này
        public List<ComputerInvoiceDetailViewModel> Details { get; set; } = new List<ComputerInvoiceDetailViewModel> ();
    }

    // Class chứa thông tin chi tiết của từng dòng sản phẩm
    public class ComputerInvoiceDetailViewModel
    {
        public string ProductCode { get; set; }
        public string ProductName { get; set; }
        public int? WarrantyMonths { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice { get; set; }
    }
}
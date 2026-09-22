namespace WebBanHang.Models
{
    public class CartItem
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public string Picture { get; set; }
        public decimal UnitPrice { get; set; }
        public int Quantity { get; set; }

        // Tổng tiền = Đơn giá * Số lượng
        public decimal TotalPrice => UnitPrice * Quantity;
    }
}
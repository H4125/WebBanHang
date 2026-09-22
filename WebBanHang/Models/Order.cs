using System;
using System.Collections.Generic;

namespace WebBanHang.Models;

public partial class Order
{
    public int Id { get; set; }

    public int? CustomerId { get; set; }

    public DateTime? OrderDate { get; set; }

    public bool Status { get; set; }

    public string? ShippingAddress { get; set; }

    public string? CustomerPhone { get; set; }

    public virtual Customer? Customer { get; set; }

    public virtual ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
}

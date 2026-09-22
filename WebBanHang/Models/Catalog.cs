using System;
using System.Collections.Generic;

namespace WebBanHang.Models;

public partial class Catalog
{
    public int Id { get; set; }

    public string CatalogCode { get; set; } = null!;

    public string CatalogName { get; set; } = null!;

    public virtual ICollection<Product> Products { get; set; } = new List<Product>();
}

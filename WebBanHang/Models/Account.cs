using System;
using System.Collections.Generic;

namespace WebBanHang.Models;

public partial class Account
{
    public int Id { get; set; }

    public string Username { get; set; } = null!;

    public string Password { get; set; } = null!;

    public int Role { get; set; }

    public virtual ICollection<Customer> Customers { get; set; } = new List<Customer>();
}

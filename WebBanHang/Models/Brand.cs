using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebBanHang.Models
{
    [Table("Brand")]
    public class Brand
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string BrandName { get; set; }

        // Mối quan hệ 1-Nhiều: 1 Hãng có nhiều Sản phẩm
        public virtual ICollection<Product> Products { get; set; }
    }
}

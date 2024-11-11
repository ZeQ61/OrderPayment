using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OrderPayment.Models
{
    public enum Category
    {
        süt_ürünleri,
        et_ürünleri,
        meyveler,
        sebzeler,
    }

    public class Product
    {
        [Key]
        public int Id { get; set; }
        public string Name { get; set; }
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public Category Status { get; set; }

        public string Image { get; set; } // Resim Base64 olarak kaydedilecek
    }
}

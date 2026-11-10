using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProductCategory.Models
{
    public class Product
    {
        [Key]
        public int ProductId { get; set; }
        [Required(ErrorMessage ="Phai nhap ten san pham")]
        [StringLength(100)]// Gioi han do dai cua ten san pham la 100
        public string? ProductName { get; set; }
        [Range(0.01, 100000, ErrorMessage ="Gia san pham phai lon hon 0 va nho hon 100000")]
        public decimal ProductPrice { get; set; }
        // File metadata for the optional uploaded product document.
        public string? DataFile { get; set; }
        public string? DataFileName { get; set; }
        // Foregin key to Category - khoa ngoai
        [ForeignKey("Category")]
        public int CategoryId { get; set; }
        // Navigation property - thuoc tinh dieu huong
        public Category? Category { get; set; }
    }
}

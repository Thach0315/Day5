using System.ComponentModel.DataAnnotations;

namespace ProductCategory.Models
{
    public class Category
    {
        [Key]
        public int CategoryId { get; set; }
        [Required(ErrorMessage = "Category name is required.")]
        [StringLength(100)]
        public string? CategoryName { get; set; }
        // Navigation property - thuoc tinh dieu huong
        public ICollection<Product>? Products { get; set; } // Hoac co the su dung List<Product>
    }
}

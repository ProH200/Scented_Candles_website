using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ScentedCandleWebsite.Models
{
    public class Product
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100, ErrorMessage = "Product name cannot exceed 100 characters")]
        [Display(Name = "Product Name")]
        public string Name { get; set; } = string.Empty;

        [Required]
        [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters")]
        [Display(Name = "Description")]
        [DataType(DataType.MultilineText)]
        public string Description { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Currency)]
        [Column(TypeName = "decimal(18,2)")]
        [Range(0.01, 999999.99, ErrorMessage = "Price must be between 0.01 and 999,999.99")]
        [Display(Name = "Price")]
        public decimal Price { get; set; }

        [Required]
        [Display(Name = "Stock Quantity")]
        [Range(0, int.MaxValue, ErrorMessage = "Stock quantity cannot be negative")]
        public int StockQuantity { get; set; }

        [Display(Name = "Product Image")]
        [Url(ErrorMessage = "Please enter a valid image URL")]
        public string? ImageUrl { get; set; }

        // Additional image URLs for gallery
        [Display(Name = "Additional Image 1")]
        [Url]
        public string? ImageUrl2 { get; set; }

        [Display(Name = "Additional Image 2")]
        [Url]
        public string? ImageUrl3 { get; set; }

        [Display(Name = "Additional Image 3")]
        [Url]
        public string? ImageUrl4 { get; set; }

        [Required]
        [StringLength(50, ErrorMessage = "Scent cannot exceed 50 characters")]
        [Display(Name = "Scent")]
        public string Scent { get; set; } = string.Empty;

        [Required]
        [StringLength(30, ErrorMessage = "Color cannot exceed 30 characters")]
        [Display(Name = "Color")]
        public string Color { get; set; } = string.Empty;

        [Required]
        [StringLength(50, ErrorMessage = "Burn time cannot exceed 50 characters")]
        [Display(Name = "Burn Time")]
        public string BurnTime { get; set; } = string.Empty;

        [Required]
        [StringLength(50, ErrorMessage = "Wax type cannot exceed 50 characters")]
        [Display(Name = "Wax Type")]
        public string WaxType { get; set; } = string.Empty;

        [Display(Name = "Size/Weight")]
        [StringLength(50)]
        public string? Size { get; set; }

        [Display(Name = "Featured Product")]
        public bool IsFeatured { get; set; } = false;

        [Display(Name = "On Sale")]
        public bool IsOnSale { get; set; } = false;

        [Display(Name = "Sale Price")]
        [DataType(DataType.Currency)]
        [Column(TypeName = "decimal(18,2)")]
        public decimal? SalePrice { get; set; }

        [Display(Name = "Discount Percentage")]
        [Range(0, 100)]
        public int? DiscountPercentage { get; set; }

        public int? CategoryId { get; set; }

        [ForeignKey("CategoryId")]
        [Display(Name = "Category")]
        public virtual Category? Category { get; set; }

        [Display(Name = "Active")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Created Date")]
        [DataType(DataType.DateTime)]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Display(Name = "Last Updated")]
        [DataType(DataType.DateTime)]
        public DateTime? UpdatedAt { get; set; }

        [Display(Name = "Required Role")]
        public string RequiredRole { get; set; } = "Customer";

        
       // [NotMapped]Telling the database not to add the columnin the database...
        [Display(Name = "Current Price")]
        public decimal CurrentPrice => IsOnSale && SalePrice.HasValue ? SalePrice.Value : Price;

        //[NotMapped] Telling the database not to add the column in the database...
        [Display(Name = "Savings")]
        public decimal Savings => IsOnSale && SalePrice.HasValue ? Price - SalePrice.Value : 0;

        //[NotMapped]
        [Display(Name = "In Stock")]
        public bool InStock => IsActive && StockQuantity > 0;

        //[NotMapped]
        [Display(Name = "Low Stock")]
        public bool LowStock => InStock && StockQuantity < 10;
    }
}
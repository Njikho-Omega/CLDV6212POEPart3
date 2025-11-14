using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ABCRetailersPOE.Models;

namespace ABCRetailersPOE.Models
{
    [Table("Cart")]
    public class Cart
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required(ErrorMessage = "Customer username is required")]
        [MaxLength(100, ErrorMessage = "Customer username cannot exceed 100 characters")]
        [Display(Name = "Customer Username")]
        public string CustomerUsername { get; set; } = string.Empty;

        [Required(ErrorMessage = "Product ID is required")]
        [MaxLength(100, ErrorMessage = "Product ID cannot exceed 100 characters")]
        [Display(Name = "Product ID")]
        public string ProductId { get; set; } = string.Empty;

        [Required(ErrorMessage = "Quantity is required")]
        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1")]
        [Display(Name = "Quantity")]
        public int Quantity { get; set; }

       // [Display(Name = "Added Date")]
       // public DateTime AddedDate { get; set; } = DateTime.Now;

        // Navigation property (if you have a User model)
       // [ForeignKey("CustomerUsername")]
        //public virtual User User { get; set; }
    }
}
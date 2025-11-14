using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ABCRetailersPOE.Models
{
    [Table("Users")]
    public class User
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Username is required")]
        [MaxLength(100, ErrorMessage = "Username cannot exceed 100 characters")]
        [Display(Name = "Username")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required")]
        [MaxLength(255, ErrorMessage = "Password hash cannot exceed 255 characters")]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string PasswordHash { get; set; } = string.Empty;

        [Required(ErrorMessage = "Role is required")]
        [MaxLength(20, ErrorMessage = "Role cannot exceed 20 characters")]
        [Display(Name = "User Role")]
        public string Role { get; set; } = "Customer";
    }
}

using System.ComponentModel.DataAnnotations;

namespace SmartLibrary.Models
{
    public class User
    {
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public required string Username { get; set; }

        [Required]
        public required string PasswordHash { get; set; }

        [Required]
        [StringLength(20)]
        public required string Role { get; set; } // "Admin" ou "Manager"
    }
}

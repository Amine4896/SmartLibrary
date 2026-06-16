using System;
using System.ComponentModel.DataAnnotations;

namespace SmartLibrary.Models
{
    public class ActivityLog
    {
        public int Id { get; set; }

        public int? UserId { get; set; }
        public User? User { get; set; }

        [Required]
        [StringLength(50)]
        public required string Username { get; set; }

        [Required]
        [StringLength(20)]
        public required string UserRole { get; set; }

        [Required]
        public required string Action { get; set; }

        public DateTime Timestamp { get; set; } = DateTime.Now;
    }
}

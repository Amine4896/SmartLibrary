using System.ComponentModel.DataAnnotations;

namespace SmartLibrary.Models
{
    public class Borrow
    {
        public int Id { get; set; }

        [Required]
        public required string BorrowerName { get; set; }

        public DateTime BorrowDate { get; set; } = DateTime.Now;

        public DateTime? ReturnDate { get; set; }

        public DateTime DueDate { get; set; } = DateTime.Now.AddDays(14);

        [Required]
        public string Status { get; set; } = "Approved"; // "Pending", "Approved", "Rejected", "Returned"

        public int? UserId { get; set; }
        public User? User { get; set; }

        public List<BorrowItem> BorrowItems { get; set; } = new();
    }
}
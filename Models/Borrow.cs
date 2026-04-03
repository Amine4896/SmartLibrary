using System.ComponentModel.DataAnnotations;

namespace SmartLibrary.Models
{
    public class Borrow
    {
        public int Id { get; set; }

        [Required]
        public string BorrowerName { get; set; }

        public DateTime BorrowDate { get; set; } = DateTime.Now;

        public DateTime? ReturnDate { get; set; }

        public List<BorrowItem> BorrowItems { get; set; } = new();
    }
}
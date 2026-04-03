using System.ComponentModel.DataAnnotations;

namespace SmartLibrary.Models
{
    public class Book
    {
        public int Id { get; set; }

        [Required]
        public required string Title { get; set; }

        public required string Author { get; set; }

        public decimal Price { get; set; }

        public int StockQuantity { get; set; }

        public int CategoryId { get; set; }
        public  Category? Category { get; set; }
    }
}
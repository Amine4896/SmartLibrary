using System.ComponentModel.DataAnnotations;

namespace SmartLibrary.Models
{
    public class Category
    {
        public int Id { get; set; }

        [Required]
        public required string Name { get; set; }
    }
}
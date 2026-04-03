namespace SmartLibrary.Models
{
    public class BorrowItem
    {
        public int Id { get; set; }

        public int BorrowId { get; set; }
        public Borrow? Borrow { get; set; }

        public int BookId { get; set; }
        public Book? Book { get; set; }
    }
}
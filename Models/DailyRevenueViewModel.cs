using System;

namespace SmartLibrary.Models
{
    public class DailyRevenueViewModel
    {
        public DateTime Date { get; set; }
        public int BorrowedBooksCount { get; set; }
        public decimal Revenue { get; set; }
    }
}

using Microsoft.EntityFrameworkCore;
using SmartLibrary.Data;
using SmartLibrary.Models;

namespace SmartLibrary.Services;

public class BookService
{
    private readonly ApplicationDbContext _context;

    public BookService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<Book>> GetAvailableBooks()
    {
        return await _context.Books
            .Where(b => b.StockQuantity > 0)
            .ToListAsync();
    }

    public async Task<List<Book>> GetAllBooks()
    {
        return await _context.Books.ToListAsync();
    }

    public async Task<List<Book>> SearchBooks(string keyword)
    {
        return await _context.Books
            .Where(b => b.Title.Contains(keyword))
            .ToListAsync();
    }

    public async Task<int> GetTotalBooks()
    {
        return await _context.Books.CountAsync();
    }
}
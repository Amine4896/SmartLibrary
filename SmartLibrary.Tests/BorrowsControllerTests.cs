using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using SmartLibrary.Controllers;
using SmartLibrary.Data;
using SmartLibrary.Models;
using Xunit;

namespace SmartLibrary.Tests
{
    public class BorrowsControllerTests
    {
        private ApplicationDbContext GetDatabaseContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;

            var databaseContext = new ApplicationDbContext(options);
            databaseContext.Database.EnsureCreated();
            return databaseContext;
        }

        private BorrowsController GetController(ApplicationDbContext context, string username, string role)
        {
            var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
                new Claim(ClaimTypes.Name, username),
                new Claim(ClaimTypes.Role, role)
            }, "mock"));

            var controller = new BorrowsController(context);
            controller.ControllerContext = new ControllerContext()
            {
                HttpContext = new DefaultHttpContext() { User = user }
            };

            // Set TempData
            var tempData = new TempDataDictionary(new DefaultHttpContext(), new DummyTempDataProvider());
            controller.TempData = tempData;

            return controller;
        }

        public class DummyTempDataProvider : ITempDataProvider
        {
            public IDictionary<string, object> LoadTempData(HttpContext context)
            {
                return new Dictionary<string, object>();
            }

            public void SaveTempData(HttpContext context, IDictionary<string, object> values)
            {
            }
        }

        [Fact]
        public async Task Create_WithAvailableStock_ShouldSucceedAndNotDecrementStockImmediatelyForClient()
        {
            // Arrange
            using var context = GetDatabaseContext();
            
            // Seed a user
            var user = new User { Username = "amine", Role = "Client", PasswordHash = "hash" };
            context.Users.Add(user);

            // Seed a category and book
            var category = new Category { Name = "Mathématiques" };
            context.Categories.Add(category);
            
            var book = new Book { Title = "Algèbre Linéaire", Author = "Jean Dupont", Price = 150, StockQuantity = 5, Category = category };
            context.Books.Add(book);
            await context.SaveChangesAsync();

            var controller = GetController(context, "amine", "Client");

            // Act
            var result = await controller.Create("amine", new List<int> { book.Id });

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);

            // Client demands are "Pending", so stock is NOT decremented yet (until approved)
            var updatedBook = await context.Books.FindAsync(book.Id);
            Assert.Equal(5, updatedBook.StockQuantity);

            var borrow = await context.Borrows.FirstOrDefaultAsync();
            Assert.NotNull(borrow);
            Assert.Equal("Pending", borrow.Status);
            Assert.Equal("amine", borrow.BorrowerName);
        }

        [Fact]
        public async Task Create_WithNoStock_ShouldFailWithModelError()
        {
            // Arrange
            using var context = GetDatabaseContext();
            
            var user = new User { Username = "amine", Role = "Client", PasswordHash = "hash" };
            context.Users.Add(user);

            var category = new Category { Name = "Mathématiques" };
            context.Categories.Add(category);
            
            // Stock is 0
            var book = new Book { Title = "Algèbre Linéaire", Author = "Jean Dupont", Price = 150, StockQuantity = 0, Category = category };
            context.Books.Add(book);
            await context.SaveChangesAsync();

            var controller = GetController(context, "amine", "Client");

            // Act
            var result = await controller.Create("amine", new List<int> { book.Id });

            // Assert
            // Should return the View (since validation failed)
            Assert.Null(controller.TempData["SuccessMessage"]);
            Assert.False(controller.ModelState.IsValid);
            Assert.True(controller.ModelState.ErrorCount > 0);
            
            var borrow = await context.Borrows.FirstOrDefaultAsync();
            Assert.Null(borrow);
        }

        [Fact]
        public async Task ReturnBook_ShouldSetReturnedStatusAndIncrementStock()
        {
            // Arrange
            using var context = GetDatabaseContext();
            
            var user = new User { Username = "amine", Role = "Client", PasswordHash = "hash" };
            context.Users.Add(user);

            var category = new Category { Name = "Mathématiques" };
            context.Categories.Add(category);
            
            var book = new Book { Title = "Algèbre Linéaire", Author = "Jean Dupont", Price = 150, StockQuantity = 4, Category = category };
            context.Books.Add(book);
            
            // Create an approved borrow
            var borrow = new Borrow
            {
                BorrowerName = "amine",
                UserId = user.Id,
                Status = "Approved",
                BorrowDate = DateTime.Now.AddDays(-5),
                DueDate = DateTime.Now.AddDays(9),
                BorrowItems = new List<BorrowItem>()
            };
            borrow.BorrowItems.Add(new BorrowItem { Book = book });
            context.Borrows.Add(borrow);
            await context.SaveChangesAsync();

            var controller = GetController(context, "amine", "Client");

            // Act
            var result = await controller.ReturnBook(borrow.Id);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);

            var updatedBorrow = await context.Borrows.FindAsync(borrow.Id);
            Assert.Equal("Returned", updatedBorrow.Status);
            Assert.NotNull(updatedBorrow.ReturnDate);

            // Stock should be incremented (4 -> 5)
            var updatedBook = await context.Books.FindAsync(book.Id);
            Assert.Equal(5, updatedBook.StockQuantity);
        }
    }
}

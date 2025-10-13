using LibraryManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LibraryManagement.Infrastructure.Data
{
    public class DataSeeder
    {
        private readonly LibraryDbContext _context;
        private readonly ILogger<DataSeeder> _logger;

        public DataSeeder(LibraryDbContext context, ILogger<DataSeeder> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task SeedAsync()
        {
            try
            {
                await SeedLibrariesAsync();
                await SeedBooksAsync();
                await SeedBorrowRecordsAsync();
                _logger.LogInformation("Database seeded with initial data successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while seeding the database.");
            }
        }

        private async Task SeedLibrariesAsync()
        {
            if (!await _context.Libraries.AnyAsync())
            {
                var libraries = new List<Library>
                {
                    new Library
                    {
                        Name = "Central Library",
                        Location = "Main Street",
                        Description = "A hub for book lovers",
                        CreatedAt = DateTime.UtcNow
                    },
                    new Library
                    {
                        Name = "Downtown Branch",
                        Location = "Downtown",
                        Description = "A small branch offering study space",
                        CreatedAt = DateTime.UtcNow
                    }
                };

                await _context.Libraries.AddRangeAsync(libraries);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Libraries seeded successfully.");
            }
        }

        private async Task SeedBooksAsync()
        {
            if (!await _context.Books.AnyAsync())
            {
                var library = await _context.Libraries.FirstOrDefaultAsync();
                if (library != null)
                {
                    var books = new List<Book>
                    {
                        new Book
                        {
                            Title = "To Kill a Mockingbird",
                            Author = "Harper Lee",
                            ISBN = "9780061120084",
                            PublishedYear = 1960,
                            TotalCopies = 10,
                            CopiesAvailable = 10,
                            LibraryId = library.Id,
                            CreatedAt = DateTime.UtcNow
                        },
                        new Book
                        {
                            Title = "1984",
                            Author = "George Orwell",
                            ISBN = "9780451524935",
                            PublishedYear = 1949,
                            TotalCopies = 5,
                            CopiesAvailable = 5,
                            LibraryId = library.Id,
                            CreatedAt = DateTime.UtcNow
                        }
                    };

                    await _context.Books.AddRangeAsync(books);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Books seeded successfully.");
                }
            }
        }

        private async Task SeedBorrowRecordsAsync()
        {
            if (!await _context.BorrowRecords.AnyAsync())
            {
                var user = await _context.Users.FirstOrDefaultAsync();
                var book = await _context.Books.FirstOrDefaultAsync();

                if (user != null && book != null)
                {
                    var borrowRecord = new BorrowRecord
                    {
                        BookId = book.Id,
                        UserId = user.Id,
                        BorrowDate = DateTime.UtcNow,
                        DueDate = DateTime.UtcNow.AddDays(14),
                        ReturnDate = null,
                        FineAmount = 0,
                        Notes = "First Borrow",
                        CreatedAt = DateTime.UtcNow
                    };

                    await _context.BorrowRecords.AddAsync(borrowRecord);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Borrow records seeded successfully.");
                }
            }
        }
    }
}
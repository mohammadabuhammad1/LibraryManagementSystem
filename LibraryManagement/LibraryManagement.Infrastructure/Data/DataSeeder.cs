using LibraryManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LibraryManagement.Infrastructure.Data;

public partial class DataSeeder(LibraryDbContext context, ILogger<DataSeeder> logger)
{
    // LoggerMessage delegates for high-performance logging
    private static readonly Action<ILogger, Exception?> _databaseSeededSuccessfully =
        LoggerMessage.Define(LogLevel.Information, new EventId(1, "DatabaseSeeded"), "Database seeded with initial data successfully.");

    private static readonly Action<ILogger, string, Exception?> _seedingError =
        LoggerMessage.Define<string>(LogLevel.Error, new EventId(2, "SeedingError"), "An error occurred while seeding the database: {ErrorMessage}");

    private static readonly Action<ILogger, Exception?> _librariesSeeded =
        LoggerMessage.Define(LogLevel.Information, new EventId(3, "LibrariesSeeded"), "Libraries seeded successfully.");

    private static readonly Action<ILogger, Exception?> _booksSeeded =
        LoggerMessage.Define(LogLevel.Information, new EventId(4, "BooksSeeded"), "Books seeded successfully.");

    private static readonly Action<ILogger, Exception?> _borrowRecordsSeeded =
        LoggerMessage.Define(LogLevel.Information, new EventId(5, "BorrowRecordsSeeded"), "Borrow records seeded successfully.");

    public async Task SeedAsync()
    {
        try
        {
            await SeedLibrariesAsync().ConfigureAwait(false);
            await SeedBooksAsync().ConfigureAwait(false);
            await SeedBorrowRecordsAsync().ConfigureAwait(false);
            _databaseSeededSuccessfully(logger, null);
        }
        catch (DbUpdateException dbEx)
        {
            // ✓ Specific exception for database update errors
            _seedingError(logger, dbEx.Message, dbEx);
        }
        catch (InvalidOperationException invalidOpEx)
        {
            // ✓ Specific exception for invalid operations (e.g., missing dependencies)
            _seedingError(logger, invalidOpEx.Message, invalidOpEx);
        }
        catch (Exception ex)
        {
            // ✓ Generic exception as fallback
            _seedingError(logger, ex.Message, ex);
            throw; // ✓ Re-throw to indicate seeding failure
        }
    }

    private async Task SeedLibrariesAsync()
    {
        if (!await context.Libraries.AnyAsync().ConfigureAwait(false))
        {
            List<Library> libraries = new() 
            {
                new()
                {
                    Name = "Central Library",
                    Location = "Main Street",
                    Description = "A hub for book lovers",
                    CreatedAt = DateTime.UtcNow
                },
                new()
                {
                    Name = "Downtown Branch",
                    Location = "Downtown",
                    Description = "A small branch offering study space",
                    CreatedAt = DateTime.UtcNow
                }
            };

            await context.Libraries.AddRangeAsync(libraries).ConfigureAwait(false);
            await context.SaveChangesAsync().ConfigureAwait(false);
            _librariesSeeded(logger, null);
        }
    }

    private async Task SeedBooksAsync()
    {
        if (!await context.Books.AnyAsync().ConfigureAwait(false))
        {
            Library? library = await context.Libraries.FirstOrDefaultAsync().ConfigureAwait(false);  
            if (library != null)
            {
                List<Book> books = new()  
                {
                    new()
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
                    new()
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

                await context.Books.AddRangeAsync(books).ConfigureAwait(false);
                await context.SaveChangesAsync().ConfigureAwait(false);
                _booksSeeded(logger, null);
            }
        }
    }

    private async Task SeedBorrowRecordsAsync()
    {
        if (!await context.BorrowRecords.AnyAsync().ConfigureAwait(false))
        {
            ApplicationUser? user = await context.Users.FirstOrDefaultAsync().ConfigureAwait(false);  
            Book? book = await context.Books.FirstOrDefaultAsync().ConfigureAwait(false);  

            if (user != null && book != null)
            {
                BorrowRecord borrowRecord = new()  
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

                await context.BorrowRecords.AddAsync(borrowRecord).ConfigureAwait(false);
                await context.SaveChangesAsync().ConfigureAwait(false);
                _borrowRecordsSeeded(logger, null);
            }
        }
    }
}
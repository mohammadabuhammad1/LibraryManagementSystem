using LibraryManagement.Domain.Entities;
using LibraryManagement.Infrastructure.Data;
using LibraryManagement.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagement.Infrastructure.Repositories;

public class BookRepository : GenericRepository<Book>, IBookRepository
{
    public BookRepository(LibraryDbContext context) : base(context) { }

    public async Task<Book?> GetByIsbnAsync(string isbn)
    {
        return await Context.Set<Book>().FirstOrDefaultAsync(b => b.ISBN == isbn).ConfigureAwait(false);
    }

    public async Task<IEnumerable<Book>> GetAvailableBooksAsync()
    {
        return await Context.Set<Book>()
            .Where(b => b.CopiesAvailable > 0)
            .ToListAsync().ConfigureAwait(false);
    }

    public async Task<IEnumerable<Book>> GetBooksByLibraryAsync(int libraryId)
    {
        return await Context.Set<Book>()
            .Where(b => b.LibraryId == libraryId)
            .ToListAsync().ConfigureAwait(false);
    }

    public async Task<IEnumerable<Book>> GetBorrowedBooksByUserAsync(string userId)
    {
        return await Context.Set<Book>()
            .Where(b => b.BorrowRecords.Any(br =>
                br.UserId == userId &&
                br.ReturnDate == null)) // Only active borrows
            .ToListAsync().ConfigureAwait(false);
    }
}
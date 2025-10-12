using LibraryManagement.Domain.Entities;
using LibraryManagement.Infrastructure.Data;
using LibraryManagement.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagement.Infrastructure.Repositories
{
    public class BookRepository : GenericRepository<Book>, IBookRepository
    {
        public BookRepository(LibraryDbContext context) : base(context) { }

        public async Task<Book?> GetByIsbnAsync(string isbn)
        {
            return await _dbSet.FirstOrDefaultAsync(b => b.ISBN == isbn);
        }

        public async Task<IEnumerable<Book>> GetAvailableBooksAsync()
        {
            return await _dbSet
                .Where(b => b.CopiesAvailable > 0)
                .ToListAsync();
        }

        public async Task<IEnumerable<Book>> GetBooksByLibraryAsync(int libraryId)
        {
            return await _dbSet
                .Where(b => b.LibraryId == libraryId)
                .ToListAsync();
        }
    }
}
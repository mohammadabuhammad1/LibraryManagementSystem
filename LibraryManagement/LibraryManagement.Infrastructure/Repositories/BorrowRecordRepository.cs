using Microsoft.EntityFrameworkCore;
using LibraryManagement.Domain.Entities;
using LibraryManagement.Domain.Interfaces;
using LibraryManagement.Infrastructure.Data;

namespace LibraryManagement.Infrastructure.Repositories
{
    public class BorrowRecordRepository : GenericRepository<BorrowRecord>, IBorrowRecordRepository
    {
        public BorrowRecordRepository(LibraryDbContext context) : base(context) { }

        public async Task<IEnumerable<BorrowRecord>> GetActiveBorrowsByUserAsync(string userId)
        {
            return await _dbSet
                .Include(br => br.Book)
                .Include(br => br.User)
                .Where(br => br.UserId == userId && !br.IsReturned)
                .ToListAsync();
        }

        public async Task<IEnumerable<BorrowRecord>> GetOverdueBorrowsAsync()
        {
            return await _dbSet
                .Include(br => br.Book)
                .Include(br => br.User)
                .Where(br => !br.IsReturned && br.DueDate < DateTime.UtcNow)
                .ToListAsync();
        }

        public async Task<BorrowRecord?> GetActiveBorrowByBookAndUserAsync(int bookId, string userId)
        {
            return await _dbSet
                .Include(br => br.Book)
                .Include(br => br.User)
                .FirstOrDefaultAsync(br => br.BookId == bookId && br.UserId == userId && !br.IsReturned);
        }

        public async Task<IEnumerable<BorrowRecord>> GetBorrowHistoryByUserAsync(string userId)
        {
            return await _dbSet
                .Include(br => br.Book)
                .Include(br => br.User)
                .Where(br => br.UserId == userId)
                .OrderByDescending(br => br.BorrowDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<BorrowRecord>> GetBorrowHistoryByBookAsync(int bookId)
        {
            return await _dbSet
                .Include(br => br.Book)
                .Include(br => br.User)
                .Where(br => br.BookId == bookId)
                .OrderByDescending(br => br.BorrowDate)
                .ToListAsync();
        }
    }
}
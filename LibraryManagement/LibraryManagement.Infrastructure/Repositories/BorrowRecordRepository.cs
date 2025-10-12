using Microsoft.EntityFrameworkCore;
using LibraryManagement.Domain.Entities;
using LibraryManagement.Domain.Interfaces;
using LibraryManagement.Infrastructure.Data;

namespace LibraryManagement.Infrastructure.Repositories
{
    public class BorrowRecordRepository : GenericRepository<BorrowRecord>, IBorrowRecordRepository
    {
        public BorrowRecordRepository(LibraryDbContext context) : base(context) { }

        public async Task<IEnumerable<BorrowRecord>> GetActiveBorrowsByMemberAsync(int memberId)
        {
            return await _dbSet
                .Include(br => br.Book)
                .Include(br => br.Member)
                .Where(br => br.MemberId == memberId && !br.IsReturned)
                .ToListAsync();
        }

        public async Task<IEnumerable<BorrowRecord>> GetOverdueBorrowsAsync()
        {
            return await _dbSet
                .Include(br => br.Book)
                .Include(br => br.Member)
                .Where(br => !br.IsReturned && br.DueDate < DateTime.UtcNow)
                .ToListAsync();
        }

        public async Task<BorrowRecord?> GetActiveBorrowByBookAndMemberAsync(int bookId, int memberId)
        {
            return await _dbSet
                .Include(br => br.Book)
                .Include(br => br.Member)
                .FirstOrDefaultAsync(br => br.BookId == bookId && br.MemberId == memberId && !br.IsReturned);
        }

        public async Task<IEnumerable<BorrowRecord>> GetBorrowHistoryByMemberAsync(int memberId)
        {
            return await _dbSet
                .Include(br => br.Book)
                .Include(br => br.Member)
                .Where(br => br.MemberId == memberId)
                .OrderByDescending(br => br.BorrowDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<BorrowRecord>> GetBorrowHistoryByBookAsync(int bookId)
        {
            return await _dbSet
                .Include(br => br.Book)
                .Include(br => br.Member)
                .Where(br => br.BookId == bookId)
                .OrderByDescending(br => br.BorrowDate)
                .ToListAsync();
        }
    }
}
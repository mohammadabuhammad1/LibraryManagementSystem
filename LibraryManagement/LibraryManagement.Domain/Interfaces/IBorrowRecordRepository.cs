using LibraryManagement.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibraryManagement.Domain.Interfaces
{
    public interface IBorrowRecordRepository : IGenericRepository<BorrowRecord>
    {
        Task<IEnumerable<BorrowRecord>> GetActiveBorrowsByMemberAsync(int memberId);
        Task<IEnumerable<BorrowRecord>> GetOverdueBorrowsAsync();
        Task<BorrowRecord?> GetActiveBorrowByBookAndMemberAsync(int bookId, int memberId);
        Task<IEnumerable<BorrowRecord>> GetBorrowHistoryByMemberAsync(int memberId);
        Task<IEnumerable<BorrowRecord>> GetBorrowHistoryByBookAsync(int bookId);
    }
}

using LibraryManagement.Application.Dtos;

namespace LibraryManagement.Application.Interfaces
{
    public interface IBorrowRecordService
    {
        Task<BorrowRecordDto> BorrowBookAsync(CreateBorrowRecordDto borrowDto);
        Task<BorrowRecordDto> ReturnBookAsync(ReturnBookDto returnDto);
        Task<IEnumerable<BorrowRecordDto>> GetUserBorrowHistoryAsync(string userId);
        Task<IEnumerable<BorrowRecordDto>> GetOverdueBooksAsync();
        Task<IEnumerable<BorrowRecordDto>> GetActiveBorrowsByUserAsync(string userId); 
        Task<decimal> CalculateFineAsync(int borrowRecordId);
        Task<bool> CanUserViewFineAsync(int borrowRecordId, string userId);

    }
}
using LibraryManagement.Application.Dtos;

namespace LibraryManagement.Application.Interfaces
{
    public interface IBorrowRecordService
    {
        Task<BorrowRecordDto> BorrowBookAsync(CreateBorrowRecordDto borrowDto);
        Task<BorrowRecordDto> ReturnBookAsync(ReturnBookDto returnDto);
        Task<IEnumerable<BorrowRecordDto>> GetMemberBorrowHistoryAsync(int memberId);
        Task<IEnumerable<BorrowRecordDto>> GetOverdueBooksAsync();
        Task<IEnumerable<BorrowRecordDto>> GetActiveBorrowsByMemberAsync(int memberId);
        Task<decimal> CalculateFineAsync(int borrowRecordId);
    }
}
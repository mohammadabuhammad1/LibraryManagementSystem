

using LibraryManagement.Application.Dtos;

public class UserBorrowHistoryDto
{
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public List<string> Roles { get; set; } = new List<string>();
    public List<BorrowRecordDto> ActiveBorrows { get; set; } = new List<BorrowRecordDto>();
    public List<BorrowRecordDto> BorrowHistory { get; set; } = new List<BorrowRecordDto>();
    public int OverdueBooksCount { get; set; }
    public decimal TotalFines { get; set; }
}

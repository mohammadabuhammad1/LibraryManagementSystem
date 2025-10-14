using LibraryManagement.Application.Dtos.Books;
using System.Collections.ObjectModel;

namespace LibraryManagement.Application.Dtos.Books;
public class UserBorrowHistoryDto
{
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public Collection<string> Roles { get;  } = [];
    public Collection<BorrowRecordDto> ActiveBorrows { get; } = [];
    public Collection<BorrowRecordDto> BorrowHistory { get; } = [];
    public int OverdueBooksCount { get; set; }
    public decimal TotalFines { get; set; }
}

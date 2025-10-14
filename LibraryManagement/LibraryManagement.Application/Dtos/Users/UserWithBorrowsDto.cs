using System.Collections.ObjectModel;

namespace LibraryManagement.Application.Dtos.Users;

public class UserWithBorrowsDto
{
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public Collection<string> Roles { get; } = [];
    public int ActiveBorrowsCount { get; set; }
    public int TotalBorrowsCount { get; set; }
    public int OverdueBooksCount { get; set; }
    public bool HasOverdueBooks { get; set; }
    public DateTime MembershipDate { get; set; }
}

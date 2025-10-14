using System.Collections.ObjectModel;

namespace LibraryManagement.Application.Dtos.Users;
public class UserDto
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public DateTime MembershipDate { get; set; }
    public bool IsActive { get; set; }
    public int TotalBooksBorrowed { get; set; }
    public int ActiveBorrows { get; set; }
    public int OverdueBooks { get; set; }
    public decimal TotalFines { get; set; }

    public Collection<string> Roles { get;  } = [];
}

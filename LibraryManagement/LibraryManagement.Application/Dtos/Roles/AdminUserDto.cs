using System.Collections.ObjectModel;

namespace LibraryManagement.Application.Dtos.Roles;
public class AdminUserDto
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public DateTime MembershipDate { get; set; }
    public bool IsActive { get; set; }
    public Collection<string> Roles { get;} = [];
    public int TotalBorrows { get; set; }
    public int ActiveBorrows { get; set; }
    public DateTime? LastLogin { get; set; }
}
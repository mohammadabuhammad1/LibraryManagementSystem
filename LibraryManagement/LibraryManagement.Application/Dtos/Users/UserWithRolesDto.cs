using System.Collections.ObjectModel;

namespace LibraryManagement.Application.Dtos.Users;
public class UserWithRolesDto
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public Collection<string> Roles { get; } = [];
    public DateTime MembershipDate { get; set; }
    public bool IsActive { get; set; }
}

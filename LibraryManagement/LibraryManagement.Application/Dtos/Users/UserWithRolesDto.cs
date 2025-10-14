public class UserWithRolesDto
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public List<string> Roles { get; set; } = new List<string>();
    public DateTime MembershipDate { get; set; }
    public bool IsActive { get; set; }
}

using Microsoft.AspNetCore.Identity;

namespace LibraryManagement.Domain.Entities
{
    public class ApplicationUser : IdentityUser
    {
        public string Name { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public DateTime MembershipDate { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = true;

        // Navigation property
        public ICollection<BorrowRecord> BorrowRecords { get; set; } = new List<BorrowRecord>();
        public  ICollection<IdentityUserRole<string>> UserRoles { get; set; } = new List<IdentityUserRole<string>>();
    }
}
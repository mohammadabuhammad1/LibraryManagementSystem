using LibraryManagement.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace LibraryManagement.API.Extensions
{
    public static class UserManagerExtensions
    {
        public static async Task<ApplicationUser> FindByEmailFromClaimsPrincipal(
            this UserManager<ApplicationUser> userManager, ClaimsPrincipal user)
        {
            var email = user?.FindFirstValue(ClaimTypes.Email);

            if (string.IsNullOrEmpty(email))
                return null;

            return await userManager.Users
                .SingleOrDefaultAsync(x => x.Email == email);
        }

        public static async Task<ApplicationUser> FindUserByClaimsPrincipleWithBorrowRecords(
            this UserManager<ApplicationUser> userManager, ClaimsPrincipal user)
        {
            var email = user?.FindFirstValue(ClaimTypes.Email);

            if (string.IsNullOrEmpty(email))
                return null;

            return await userManager.Users
                .Include(u => u.BorrowRecords)
                .ThenInclude(br => br.Book)
                .SingleOrDefaultAsync(x => x.Email == email);
        }
    }
}
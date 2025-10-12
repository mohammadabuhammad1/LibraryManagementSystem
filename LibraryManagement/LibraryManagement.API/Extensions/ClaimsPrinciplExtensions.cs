using System.Security.Claims;

namespace LibraryManagement.API.Extensions
{
    public static class ClaimsPrincipalExtensions
    {
        public static string GetUserEmail(this ClaimsPrincipal user)
        {
            return user?.FindFirstValue(ClaimTypes.Email);
        }

        public static string GetUserId(this ClaimsPrincipal user)
        {
            return user?.FindFirstValue(ClaimTypes.NameIdentifier);
        }

        public static string GetUserName(this ClaimsPrincipal user)
        {
            return user?.FindFirstValue(ClaimTypes.Name);
        }
    }
}
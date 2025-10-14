using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using LibraryManagement.Domain.Entities;
using System.Security.Claims;

namespace LibraryManagement.API.Controllers;

[ApiController]
[Route("api/[controller]")]
internal class BaseApiController(UserManager<ApplicationUser> userManager) : ControllerBase
{

    // Get current user from JWT token
    protected async Task<ApplicationUser?> GetCurrentUserAsync()
    {
        return await userManager.GetUserAsync(User).ConfigureAwait(false);
    }

    // Get current user ID from JWT token
    protected string? GetCurrentUserId()
    {
        return User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    }

    // Get current user email from JWT token
    protected string? GetCurrentUserEmail()
    {
        return User.FindFirst(ClaimTypes.Email)?.Value;
    }

    // Check if current user has specific role
    protected bool CurrentUserHasRole(string role)
    {
        return User.IsInRole(role);
    }

    // Get all roles of current user
    protected IEnumerable<string> GetCurrentUserRoles()
    {
        return User.FindAll(ClaimTypes.Role).Select(c => c.Value);
    }
}
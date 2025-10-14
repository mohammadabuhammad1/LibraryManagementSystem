using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using LibraryManagement.Domain.Entities;
using System.Security.Claims;

namespace LibraryManagement.API.Controllers;

[ApiController]
[Route("api/[controller]")]
internal class BaseApiController(UserManager<ApplicationUser> userManager) : ControllerBase
{

    protected async Task<ApplicationUser?> GetCurrentUserAsync()
    {
        return await userManager.GetUserAsync(User).ConfigureAwait(false);
    }

    protected string? GetCurrentUserId()
    {
        return User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    }

    protected string? GetCurrentUserEmail()
    {
        return User.FindFirst(ClaimTypes.Email)?.Value;
    }

    protected bool CurrentUserHasRole(string role)
    {
        return User.IsInRole(role);
    }

    protected IEnumerable<string> GetCurrentUserRoles()
    {
        return User.FindAll(ClaimTypes.Role).Select(c => c.Value);
    }
}
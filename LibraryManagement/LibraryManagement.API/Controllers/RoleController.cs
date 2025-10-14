using LibraryManagement.Infrastructure.Constants;
using LibraryManagement.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using LibraryManagement.Domain.Entities;
using LibraryManagement.Application.Dtos.Roles;

namespace LibraryManagement.API.Controllers;

[Authorize(Roles = $"{UserRoles.Admin},{UserRoles.SuperAdmin}")]
[ApiController]
[Route("api/[controller]")]
internal class RolesController(
    IRoleService roleService,
    UserManager<ApplicationUser> userManager) : BaseApiController(userManager)
{

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<List<RoleDto>>> GetRoles()
    {
        List<RoleDto> roles = await roleService.GetAllRolesAsync().ConfigureAwait(false);
        return Ok(roles);
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult> CreateRole([FromBody] string roleName)
    {
        if (string.IsNullOrWhiteSpace(roleName))
            return BadRequest("Role name is required");

        if (await roleService.RoleExistsAsync(roleName).ConfigureAwait(false))
            return BadRequest("Role already exists");

        IdentityResult result = await roleService.CreateRoleAsync(roleName).ConfigureAwait(false);

        if (result.Succeeded)
            return Ok(new { message = $"Role '{roleName}' created successfully" });

        return BadRequest(result.Errors);
    }

    [HttpPost("assign")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult> AssignRoleToUser([FromBody] AssignRoleDto assignRoleDto)
    {
        try
        {
            if (!await roleService.RoleExistsAsync(assignRoleDto.RoleName).ConfigureAwait(false))
                return BadRequest($"Role '{assignRoleDto.RoleName}' does not exist");

            var result = await roleService.AssignRoleToUserAsync(assignRoleDto.UserId, assignRoleDto.RoleName).ConfigureAwait(false);

            if (result.Succeeded)
                return Ok(new { message = $"Role '{assignRoleDto.RoleName}' assigned to user successfully" });

            return BadRequest(result.Errors);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("remove")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult> RemoveRoleFromUser([FromBody] AssignRoleDto removeRoleDto)
    {
        try
        {
            if (!await roleService.RoleExistsAsync(removeRoleDto.RoleName).ConfigureAwait(false))
                return BadRequest($"Role '{removeRoleDto.RoleName}' does not exist");

            var result = await roleService.RemoveRoleFromUserAsync(removeRoleDto.UserId, removeRoleDto.RoleName).ConfigureAwait(false);

            if (result.Succeeded)
                return Ok(new { message = $"Role '{removeRoleDto.RoleName}' removed from user successfully" });

            return BadRequest(result.Errors);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet("user/{userId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<List<string>>> GetUserRoles(string userId)
    {
        try
        {
            IList<string> roles = await roleService.GetUserRolesAsync(userId).ConfigureAwait(false);
            return Ok(roles);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet("{roleName}/users")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<List<UserRoleDto>>> GetUsersInRole(string roleName)
    {
        if (!await roleService.RoleExistsAsync(roleName).ConfigureAwait(false))
            return NotFound($"Role '{roleName}' not found");

        List<UserRoleDto> users = await roleService.GetUsersInRoleAsync(roleName).ConfigureAwait(false);
        return Ok(users);
    }

    [HttpDelete("{roleName}")]
    [Authorize(Roles = UserRoles.SuperAdmin)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult> DeleteRole(string roleName)
    {
        try
        {
            if (!await roleService.RoleExistsAsync(roleName).ConfigureAwait(false))
                return NotFound($"Role '{roleName}' not found");

            if (IsProtectedRole(roleName))
                return BadRequest($"Cannot delete protected role '{roleName}'");

            IdentityResult result = await roleService.DeleteRoleAsync(roleName).ConfigureAwait(false);

            if (result.Succeeded)
                return Ok(new { message = $"Role '{roleName}' deleted successfully" });

            return BadRequest(result.Errors);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    private static bool IsProtectedRole(string roleName)
    {
        var protectedRoles = new[] { UserRoles.SuperAdmin, UserRoles.Admin, UserRoles.Librarian, UserRoles.Member };
        return protectedRoles.Contains(roleName);
    }
}
using LibraryManagement.Infrastructure.Constants;
using LibraryManagement.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using LibraryManagement.Domain.Entities;

namespace LibraryManagement.API.Controllers
{
    [Authorize(Roles = $"{UserRoles.Admin},{UserRoles.SuperAdmin}")]
    [ApiController]
    [Route("api/[controller]")]
    public class RolesController : BaseApiController
    {
        private readonly IRoleService _roleService;

        public RolesController(
            IRoleService roleService,
            UserManager<ApplicationUser> userManager) : base(userManager) 
        {
            _roleService = roleService ?? throw new ArgumentNullException(nameof(roleService));
        }

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<List<RoleDto>>> GetRoles()
        {
            var roles = await _roleService.GetAllRolesAsync();
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

            if (await _roleService.RoleExistsAsync(roleName))
                return BadRequest("Role already exists");

            var result = await _roleService.CreateRoleAsync(roleName);

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
                if (!await _roleService.RoleExistsAsync(assignRoleDto.RoleName))
                    return BadRequest($"Role '{assignRoleDto.RoleName}' does not exist");

                var result = await _roleService.AssignRoleToUserAsync(assignRoleDto.UserId, assignRoleDto.RoleName);

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
                if (!await _roleService.RoleExistsAsync(removeRoleDto.RoleName))
                    return BadRequest($"Role '{removeRoleDto.RoleName}' does not exist");

                var result = await _roleService.RemoveRoleFromUserAsync(removeRoleDto.UserId, removeRoleDto.RoleName);

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
                var roles = await _roleService.GetUserRolesAsync(userId);
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
            if (!await _roleService.RoleExistsAsync(roleName))
                return NotFound($"Role '{roleName}' not found");

            var users = await _roleService.GetUsersInRoleAsync(roleName);
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
                if (!await _roleService.RoleExistsAsync(roleName))
                    return NotFound($"Role '{roleName}' not found");

                if (IsProtectedRole(roleName))
                    return BadRequest($"Cannot delete protected role '{roleName}'");

                var result = await _roleService.DeleteRoleAsync(roleName);

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
}
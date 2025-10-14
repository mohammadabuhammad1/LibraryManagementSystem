using LibraryManagement.Application.Interfaces;
using LibraryManagement.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagement.API.Services
{
    public class RoleService : IRoleService
    {
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly UserManager<ApplicationUser> _userManager;

        public RoleService(RoleManager<IdentityRole> roleManager, UserManager<ApplicationUser> userManager)
        {
            _roleManager = roleManager;
            _userManager = userManager;
        }

        public async Task<IdentityResult> CreateRoleAsync(string roleName)
        {
            return await _roleManager.CreateAsync(new IdentityRole(roleName));
        }

        public async Task<bool> RoleExistsAsync(string roleName)
        {
            return await _roleManager.RoleExistsAsync(roleName);
        }

        public async Task<IdentityResult> AssignRoleToUserAsync(string userId, string roleName)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                throw new Exception("User not found");

            return await _userManager.AddToRoleAsync(user, roleName);
        }

        public async Task<IdentityResult> RemoveRoleFromUserAsync(string userId, string roleName)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                throw new Exception("User not found");

            return await _userManager.RemoveFromRoleAsync(user, roleName);
        }

        public async Task<IList<string>> GetUserRolesAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                throw new Exception("User not found");

            return await _userManager.GetRolesAsync(user);
        }

        public async Task<List<RoleDto>> GetAllRolesAsync()
        {
            var roles = await _roleManager.Roles.ToListAsync();
            return roles.Select(role => new RoleDto
            {
                Id = role.Id,
                Name = role.Name,
                NormalizedName = role.NormalizedName
            }).ToList();
        }

        public async Task<List<UserRoleDto>> GetUsersInRoleAsync(string roleName)
        {
            var usersInRole = await _userManager.GetUsersInRoleAsync(roleName);
            return usersInRole.Select(user => new UserRoleDto
            {
                UserId = user.Id,
                UserName = user.Name,
                Email = user.Email
            }).ToList();
        }

        public async Task<IdentityResult> DeleteRoleAsync(string roleName)
        {
            var role = await _roleManager.FindByNameAsync(roleName);
            return role == null ? throw new Exception("Role not found") : await _roleManager.DeleteAsync(role);
        }
    }
}
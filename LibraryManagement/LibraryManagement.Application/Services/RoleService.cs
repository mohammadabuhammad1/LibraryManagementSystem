using LibraryManagement.Application.Dtos.Roles;
using LibraryManagement.Application.Interfaces;
using LibraryManagement.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagement.API.Services;

public class RoleService(RoleManager<IdentityRole> roleManager, UserManager<ApplicationUser> userManager) : IRoleService
{
    public async Task<IdentityResult> CreateRoleAsync(string roleName)
    {
        return await roleManager.CreateAsync(new IdentityRole(roleName)).ConfigureAwait(false);
    }

    public async Task<bool> RoleExistsAsync(string roleName)
    {
        return await roleManager.RoleExistsAsync(roleName).ConfigureAwait(false);
    }

    public async Task<IdentityResult> AssignRoleToUserAsync(string userId, string roleName)
    {
        ApplicationUser? user = await userManager.FindByIdAsync(userId).ConfigureAwait(false);

        if (user == null)
            throw new InvalidOperationException("User not found");

        return await userManager.AddToRoleAsync(user, roleName).ConfigureAwait(false);
    }

    public async Task<IdentityResult> RemoveRoleFromUserAsync(string userId, string roleName)
    {
        ApplicationUser? user = await userManager.FindByIdAsync(userId).ConfigureAwait(false);
        if (user == null)
            throw new InvalidOperationException("User not found");

        return await userManager.RemoveFromRoleAsync(user, roleName).ConfigureAwait(false);
    }

    public async Task<IList<string>> GetUserRolesAsync(string userId)
    {
        ApplicationUser? user = await userManager.FindByIdAsync(userId).ConfigureAwait(false);
        if (user == null)
            throw new InvalidOperationException("User not found");

        return await userManager.GetRolesAsync(user).ConfigureAwait(false);
    }

    public async Task<List<RoleDto>> GetAllRolesAsync()
    {
        List<IdentityRole> roles = await roleManager.Roles.ToListAsync().ConfigureAwait(false);

        return roles.Select(role => new RoleDto
        {
            Id = role.Id,
            Name = role.Name ?? string.Empty,
            NormalizedName = role.NormalizedName ?? string.Empty
        }).ToList();
    }

    public async Task<List<UserRoleDto>> GetUsersInRoleAsync(string roleName)
    {
        IList<ApplicationUser> usersInRole = await userManager.GetUsersInRoleAsync(roleName).ConfigureAwait(false);

        return usersInRole.Select(user => new UserRoleDto
        {
            UserId = user.Id,
            UserName = user.Name,
            Email = user.Email ?? string.Empty
        }).ToList();
    }

    public async Task<IdentityResult> DeleteRoleAsync(string roleName)
    {
        IdentityRole? role = await roleManager.FindByNameAsync(roleName).ConfigureAwait(false);

        return role == null ? throw new InvalidOperationException("Role not found") : await roleManager.DeleteAsync(role).ConfigureAwait(false);
    }
}
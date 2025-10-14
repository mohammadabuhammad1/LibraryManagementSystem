using LibraryManagement.Infrastructure.Constants;
using LibraryManagement.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace LibraryManagement.Infrastructure.Data;

public partial class RoleSeeder(
    RoleManager<IdentityRole> roleManager,
    UserManager<ApplicationUser> userManager,
    ILogger<RoleSeeder> logger)
{
    // LoggerMessage delegates for high-performance logging
    private static readonly Action<ILogger, string, Exception?> _roleCreated =
        LoggerMessage.Define<string>(LogLevel.Information, new EventId(1, "RoleCreated"),
            "Created {RoleName} role");

    private static readonly Action<ILogger, Exception?> _superAdminCreated =
        LoggerMessage.Define(LogLevel.Information, new EventId(2, "SuperAdminCreated"),
            "Super Admin user created");

    private static readonly Action<ILogger, string, Exception?> _superAdminCreationFailed =
        LoggerMessage.Define<string>(LogLevel.Error, new EventId(3, "SuperAdminCreationFailed"),
            "Failed to create Super Admin user: {Errors}");

    public async Task SeedRolesAsync()
    {
        string[] roleNames = {
            UserRoles.SuperAdmin,
            UserRoles.Admin,
            UserRoles.Librarian,
            UserRoles.Member
        };

        foreach (string roleName in roleNames)  
        {
            bool roleExist = await roleManager.RoleExistsAsync(roleName).ConfigureAwait(false); 
            if (!roleExist)
            {
                await roleManager.CreateAsync(new IdentityRole(roleName)).ConfigureAwait(false);
                _roleCreated(logger, roleName, null);
            }
        }
    }

    public async Task SeedSuperAdminAsync()
    {
        string superAdminEmail = "superadmin@library.com";
        ApplicationUser? superAdminUser = await userManager.FindByEmailAsync(superAdminEmail).ConfigureAwait(false);  

        if (superAdminUser == null)
        {
            ApplicationUser user = new() 
            {
                Name = "Super Admin",
                Email = superAdminEmail,
                UserName = superAdminEmail,
                Phone = "0000000000",
                MembershipDate = DateTime.UtcNow,
                IsActive = true
            };

            IdentityResult result = await userManager.CreateAsync(user, "SuperAdmin123!").ConfigureAwait(false);  

            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(user, UserRoles.SuperAdmin).ConfigureAwait(false);
                await userManager.AddToRoleAsync(user, UserRoles.Admin).ConfigureAwait(false);
                _superAdminCreated(logger, null);
            }
            else
            {
                string errors = string.Join(", ", result.Errors.Select(e => e.Description)); 
                _superAdminCreationFailed(logger, errors, null);
            }
        }
    }
}
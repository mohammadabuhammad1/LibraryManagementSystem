using LibraryManagement.Infrastructure.Constants;
using LibraryManagement.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace LibraryManagement.Infrastructure.Data
{
    public class RoleSeeder
    {
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<RoleSeeder> _logger;

        public RoleSeeder(
            RoleManager<IdentityRole> roleManager,
            UserManager<ApplicationUser> userManager,
            ILogger<RoleSeeder> logger)
        {
            _roleManager = roleManager;
            _userManager = userManager;
            _logger = logger;
        }

        public async Task SeedRolesAsync()
        {
            string[] roleNames = {
                UserRoles.SuperAdmin,
                UserRoles.Admin,
                UserRoles.Librarian,
                UserRoles.Member
            };

            foreach (var roleName in roleNames)
            {
                var roleExist = await _roleManager.RoleExistsAsync(roleName);
                if (!roleExist)
                {
                    await _roleManager.CreateAsync(new IdentityRole(roleName));
                    _logger.LogInformation("Created {RoleName} role", roleName);
                }
            }
        }

        public async Task SeedSuperAdminAsync()
        {
            var superAdminEmail = "superadmin@library.com";
            var superAdminUser = await _userManager.FindByEmailAsync(superAdminEmail);

            if (superAdminUser == null)
            {
                var user = new ApplicationUser
                {
                    Name = "Super Admin",
                    Email = superAdminEmail,
                    UserName = superAdminEmail,
                    Phone = "0000000000",
                    MembershipDate = DateTime.UtcNow,
                    IsActive = true
                };

                var result = await _userManager.CreateAsync(user, "SuperAdmin123!");

                if (result.Succeeded)
                {
                    await _userManager.AddToRoleAsync(user, UserRoles.SuperAdmin);
                    await _userManager.AddToRoleAsync(user, UserRoles.Admin);
                    _logger.LogInformation("Super Admin user created");
                }
                else
                {
                    _logger.LogError("Failed to create Super Admin user: {Errors}",
                        string.Join(", ", result.Errors.Select(e => e.Description)));
                }
            }
        }
    }
}
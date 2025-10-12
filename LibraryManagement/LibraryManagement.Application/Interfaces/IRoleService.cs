using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibraryManagement.Application.Interfaces
{
    public interface IRoleService
    {
        Task<IdentityResult> CreateRoleAsync(string roleName);
        Task<bool> RoleExistsAsync(string roleName);
        Task<IdentityResult> AssignRoleToUserAsync(string userId, string roleName);
        Task<IdentityResult> RemoveRoleFromUserAsync(string userId, string roleName);
        Task<IList<string>> GetUserRolesAsync(string userId);
        Task<List<RoleDto>> GetAllRolesAsync();
        Task<List<UserRoleDto>> GetUsersInRoleAsync(string roleName);
        Task <IdentityResult> DeleteRoleAsync(string roleName);
    }
}

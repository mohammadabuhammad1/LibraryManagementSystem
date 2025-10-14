using LibraryManagement.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace LibraryManagement.Application.Interfaces;

public interface IUserService
{
    Task<IdentityResult> RegisterAsync(string email, string password, string name, string phone);
    Task<string?> LoginAsync(string email, string password);
    Task<ApplicationUser?> GetUserByIdAsync(string userId);
    Task<ApplicationUser?> GetUserByEmailAsync(string email);
    Task<IdentityResult> UpdateUserAsync(string userId, string name, string phone);
    Task<IdentityResult> DeactivateUserAsync(string userId);
    Task<IdentityResult> ActivateUserAsync(string userId);
}
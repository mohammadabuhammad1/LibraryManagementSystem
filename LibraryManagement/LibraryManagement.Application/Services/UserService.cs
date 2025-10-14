using LibraryManagement.Application.Interfaces;
using LibraryManagement.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace LibraryManagement.Application.Services;

public class UserService(UserManager<ApplicationUser> userManager, IConfiguration configuration) : IUserService
{
    public async Task<IdentityResult> RegisterAsync(string email, string password, string name, string phone)
    {
        ApplicationUser user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            Name = name,
            Phone = phone,
            MembershipDate = DateTime.UtcNow,
            IsActive = true
        };

        return await userManager.CreateAsync(user, password).ConfigureAwait(false);
    }

    public async Task<string?> LoginAsync(string email, string password)
    {
        ApplicationUser? user = await userManager.FindByEmailAsync(email).ConfigureAwait(false);
        if (user == null || !await userManager.CheckPasswordAsync(user, password).ConfigureAwait(false))
        {
            return null;
        }

        if (!user.IsActive)
        {
            throw new InvalidOperationException("User account is deactivated.");
        }

        return GenerateJwtToken(user);
    }

    public async Task<ApplicationUser?> GetUserByIdAsync(string userId)
    {
        return await userManager.FindByIdAsync(userId).ConfigureAwait(false);
    }

    public async Task<ApplicationUser?> GetUserByEmailAsync(string email)
    {
        return await userManager.FindByEmailAsync(email).ConfigureAwait(false);
    }

    public async Task<IdentityResult> UpdateUserAsync(string userId, string name, string phone)
    {
        ApplicationUser? user = await userManager.FindByIdAsync(userId).ConfigureAwait(false);
        if (user == null)
        {
            return IdentityResult.Failed(new IdentityError { Description = "User not found." });
        }

        user.Name = name;
        user.Phone = phone;

        return await userManager.UpdateAsync(user).ConfigureAwait(false);
    }

    public async Task<IdentityResult> DeactivateUserAsync(string userId)
    {
        ApplicationUser? user = await userManager.FindByIdAsync(userId).ConfigureAwait(false);
        if (user == null)
        {
            return IdentityResult.Failed(new IdentityError { Description = "User not found." });
        }

        user.IsActive = false;
        return await userManager.UpdateAsync(user).ConfigureAwait(false);
    }

    public async Task<IdentityResult> ActivateUserAsync(string userId)
    {
        ApplicationUser? user = await userManager.FindByIdAsync(userId).ConfigureAwait(false);
        if (user == null)
        {
            return IdentityResult.Failed(new IdentityError { Description = "User not found." });
        }

        user.IsActive = true;
        return await userManager.UpdateAsync(user).ConfigureAwait(false);
    }

    private string GenerateJwtToken(ApplicationUser user)
    {
        IConfigurationSection jwtSettings = configuration.GetSection("JwtSettings");
        string? secret = jwtSettings["Secret"];
        
        if (string.IsNullOrEmpty(secret))
            throw new InvalidOperationException("JWT Secret is not configured");

        byte[] key = Encoding.UTF8.GetBytes(secret);

        List<Claim> claims = new List<Claim>
        {
            new (ClaimTypes.NameIdentifier, user.Id ?? string.Empty),
            new (ClaimTypes.Email, user.Email ?? string.Empty),
            new (ClaimTypes.Name, user.Name ?? string.Empty),
            new ("membership_date", user.MembershipDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)),
            new ("is_active", user.IsActive.ToString())
        };

        string? expirationHoursString = jwtSettings["ExpirationHours"];
        double expirationHours = 24.0; // Default value
        
        if (!string.IsNullOrEmpty(expirationHoursString))
        {
            expirationHours = Convert.ToDouble(expirationHoursString, CultureInfo.InvariantCulture);
        }

        SecurityTokenDescriptor tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddHours(expirationHours),
            Issuer = jwtSettings["Issuer"] ?? "LibraryManagementAPI",
            Audience = jwtSettings["Audience"] ?? "LibraryManagementUsers",
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        JwtSecurityTokenHandler tokenHandler = new JwtSecurityTokenHandler();
        SecurityToken token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }
}
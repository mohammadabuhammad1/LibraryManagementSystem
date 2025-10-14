using LibraryManagement.Application.Interfaces;
using LibraryManagement.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace LibraryManagement.API.Services;

public class TokenService(IConfiguration config, UserManager<ApplicationUser> userManager) : ITokenService
{
    private readonly SymmetricSecurityKey _key = new(
        Encoding.UTF8.GetBytes(config["JwtSettings:Secret"]
            ?? throw new ArgumentException("JWT Secret key is not configured"))
    );

    public async Task<string> CreateToken(ApplicationUser user)
    {
        ArgumentNullException.ThrowIfNull(user);

        List<Claim> claims = new()
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id ?? string.Empty),
            new Claim(ClaimTypes.Email, user.Email ?? string.Empty),
            new Claim(ClaimTypes.Name, user.Name ?? string.Empty),
            new Claim("membership_date", user.MembershipDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)),
            new Claim("is_active", user.IsActive.ToString())
        };

        IList<string> roles = await userManager.GetRolesAsync(user).ConfigureAwait(false);
        foreach (string role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role ?? string.Empty));
        }

        SigningCredentials creds = new SigningCredentials(_key, SecurityAlgorithms.HmacSha512Signature);

        SecurityTokenDescriptor tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddDays(7),
            SigningCredentials = creds,
            Issuer = config["JwtSettings:Issuer"] ?? "LibraryManagementAPI",
            Audience = config["JwtSettings:Audience"] ?? "LibraryManagementUsers"
        };

        JwtSecurityTokenHandler tokenHandler = new JwtSecurityTokenHandler();
        SecurityToken token = tokenHandler.CreateToken(tokenDescriptor);

        return tokenHandler.WriteToken(token);
    }

    public async Task<string?> GetUserIdFromToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return null;

        try
        {
            JwtSecurityTokenHandler tokenHandler = new JwtSecurityTokenHandler();

            TokenValidationParameters validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = _key,
                ValidateIssuer = true,
                ValidIssuer = config["JwtSettings:Issuer"] ?? "LibraryManagementAPI",
                ValidateAudience = true,
                ValidAudience = config["JwtSettings:Audience"] ?? "LibraryManagementUsers",
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromMinutes(5)
            };

            TokenValidationResult result = await tokenHandler.ValidateTokenAsync(token, validationParameters).ConfigureAwait(false);

            if (result.IsValid)
            {
                return result.ClaimsIdentity.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            }

            return null;
        }
        catch (SecurityTokenException)
        {
            return null;
        }
    }
}
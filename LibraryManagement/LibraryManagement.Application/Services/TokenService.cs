using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using LibraryManagement.Application.Interfaces;
using LibraryManagement.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace LibraryManagement.API.Services
{
    public class TokenService : ITokenService
    {
        private readonly IConfiguration _config;
        private readonly SymmetricSecurityKey _key;
        private readonly UserManager<ApplicationUser> _userManager;

        public TokenService(IConfiguration config, UserManager<ApplicationUser> userManager)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));

            var secretKey = _config["JwtSettings:Secret"] ?? throw new ArgumentException("JWT Secret key is not configured");
            _key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));

            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        }

        public async Task<string> CreateToken(ApplicationUser user)
        {

            ArgumentNullException.ThrowIfNull(user);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id ?? string.Empty),
                new Claim(ClaimTypes.Email, user.Email ?? string.Empty),
                new Claim(ClaimTypes.Name, user.Name ?? string.Empty),
                new Claim("membership_date", user.MembershipDate.ToString("yyyy-MM-dd")),
                new Claim("is_active", user.IsActive.ToString())
            };

            // Add user roles to claims
            var roles = await _userManager.GetRolesAsync(user);
            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role ?? string.Empty));
            }

            var creds = new SigningCredentials(_key, SecurityAlgorithms.HmacSha512Signature);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.Now.AddDays(7),
                SigningCredentials = creds,
                Issuer = _config["JwtSettings:Issuer"] ?? "LibraryManagementAPI",
                Audience = _config["JwtSettings:Audience"] ?? "LibraryManagementUsers"
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);

            return tokenHandler.WriteToken(token);
        }

        public async Task<string?> GetUserIdFromToken(string token)
        {
            if (string.IsNullOrEmpty(token))
                return null;

            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();

                // Validate token 
                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = _key,
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ValidateLifetime = false 
                };

                var principal = tokenHandler.ValidateToken(token, validationParameters, out _);
                var userId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                return userId;
            }
            catch (InvalidOperationException)
            {
                return null;
            }
        }
    }
}
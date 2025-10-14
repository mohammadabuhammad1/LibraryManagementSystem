using LibraryManagement.API.Extensions;
using LibraryManagement.Application.Dtos.Books;
using LibraryManagement.Application.Dtos.Roles;
using LibraryManagement.Application.Dtos.Users;
using LibraryManagement.Application.Interfaces;
using LibraryManagement.Domain.Entities;
using LibraryManagement.Infrastructure.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace LibraryManagement.API.Controllers;

[ApiController]
[Route("api/[controller]")]
internal class AccountController(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    ITokenService tokenService,
    IBorrowRecordService borrowRecordService,
    IRoleService roleService) : BaseApiController(userManager: userManager)
{
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<UserDto>> Login(LoginDto loginDto)
    {
        ArgumentNullException.ThrowIfNull(loginDto);

        if (string.IsNullOrWhiteSpace(loginDto.Email))
            return BadRequest("Email is required");

        ApplicationUser? user = await userManager.FindByEmailAsync(loginDto.Email).ConfigureAwait(false);

        if (user == null)
            return Unauthorized("Invalid email or password");

        if (!user.IsActive)
            return Unauthorized("Account is deactivated. Please contact administrator.");

        Microsoft.AspNetCore.Identity.SignInResult result = await signInManager.CheckPasswordSignInAsync(user, loginDto.Password ?? string.Empty, false).ConfigureAwait(false);

        if (!result.Succeeded)
            return Unauthorized("Invalid email or password");

        IList<string> roles = await userManager.GetRolesAsync(user).ConfigureAwait(false);
        string token = await tokenService.CreateToken(user).ConfigureAwait(false);

        // Set token in cookie for web applications (optional)
        Response.Cookies.Append("access_token", token, new CookieOptions
        {
            HttpOnly = true,
            Secure = true, // Use true in production (HTTPS)
            SameSite = SameSiteMode.Strict,
            Expires = DateTimeOffset.UtcNow.AddDays(7)
        });

        IEnumerable<BorrowRecordDto> activeBorrows = await borrowRecordService.GetActiveBorrowsByUserAsync(user.Id).ConfigureAwait(false);
        IEnumerable<BorrowRecordDto> borrowHistory = await borrowRecordService.GetUserBorrowHistoryAsync(user.Id).ConfigureAwait(false);
        IEnumerable<BorrowRecordDto> overdueBooks = await borrowRecordService.GetOverdueBooksAsync().ConfigureAwait(false);
        int userOverdueBooks = overdueBooks.Count(b => b.UserId == user.Id);

        UserDto userDto = new UserDto
        {
            Id = user.Id,
            Email = user.Email ?? string.Empty,
            Name = user.Name ?? string.Empty,
            Phone = user.Phone ?? string.Empty,
            Token = token,
            MembershipDate = user.MembershipDate,
            IsActive = user.IsActive,
            TotalBooksBorrowed = borrowHistory.Count(),
            ActiveBorrows = activeBorrows.Count(),
            OverdueBooks = userOverdueBooks,
            TotalFines = borrowHistory.Sum(b => b.FineAmount ?? 0)
        };

        // Add roles to the collection
        foreach (string role in roles)
        {
            userDto.Roles.Add(role);
        }

        return userDto;
    }

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ActionResult<UserDto>> Register(RegisterDto registerDto)
    {
        ArgumentNullException.ThrowIfNull(registerDto);

        // Validate input
        if (string.IsNullOrWhiteSpace(registerDto.Email) ||
            string.IsNullOrWhiteSpace(registerDto.Password) ||
            string.IsNullOrWhiteSpace(registerDto.Name))
        {
            return BadRequest("Email, password, and name are required");
        }

        if (await userManager.FindByEmailAsync(registerDto.Email).ConfigureAwait(false) != null)
        {
            return BadRequest("Email address is already in use");
        }

        ApplicationUser user = new ApplicationUser
        {
            Name = registerDto.Name,
            Email = registerDto.Email,
            UserName = registerDto.Email,
            Phone = registerDto.Phone ?? string.Empty,
            MembershipDate = DateTime.UtcNow,
            IsActive = true
        };

        IdentityResult result = await userManager.CreateAsync(user, registerDto.Password).ConfigureAwait(false);

        if (!result.Succeeded)
            return BadRequest(result.Errors.Select(e => e.Description));

        // Assign default role (Member)
        await userManager.AddToRoleAsync(user, UserRoles.Member).ConfigureAwait(false);

        // Get user roles for the response
        IList<string> roles = await userManager.GetRolesAsync(user).ConfigureAwait(false);
        string token = await tokenService.CreateToken(user).ConfigureAwait(false);

        UserDto userDto = new UserDto
        {
            Id = user.Id,
            Email = user.Email ?? string.Empty,
            Name = user.Name ?? string.Empty,
            Phone = user.Phone ?? string.Empty,
            Token = token,
            MembershipDate = user.MembershipDate,
            IsActive = user.IsActive,
            TotalBooksBorrowed = 0,
            ActiveBorrows = 0,
            OverdueBooks = 0,
            TotalFines = 0
        };

        // Add roles to the collection
        foreach (string role in roles)
        {
            userDto.Roles.Add(role);
        }

        return userDto;
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<ActionResult> Logout()
    {
        // Clear the token cookie
        Response.Cookies.Delete("access_token");

        // Sign out if using cookie authentication
        await signInManager.SignOutAsync().ConfigureAwait(false);

        return Ok(new { message = "Logged out successfully" });
    }

    [HttpGet("emailexists")]
    [AllowAnonymous]
    public async Task<ActionResult<bool>> CheckEmailExistsAsync([FromQuery] string email)
    {
        return await userManager.FindByEmailAsync(email).ConfigureAwait(false) != null;
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<UserDto>> GetCurrentUser()
    {
        // Use the base class method to get current user
        ApplicationUser? user = await GetCurrentUserAsync().ConfigureAwait(false);

        if (user == null)
            return Unauthorized("User not found");

        // Get user roles
        IList<string> roles = await userManager.GetRolesAsync(user).ConfigureAwait(false);

        // Get user statistics
        IEnumerable<BorrowRecordDto> activeBorrows = await borrowRecordService.GetActiveBorrowsByUserAsync(user.Id).ConfigureAwait(false);
        IEnumerable<BorrowRecordDto> borrowHistory = await borrowRecordService.GetUserBorrowHistoryAsync(user.Id).ConfigureAwait(false);
        IEnumerable<BorrowRecordDto> overdueBooks = await borrowRecordService.GetOverdueBooksAsync().ConfigureAwait(false);
        int userOverdueBooks = overdueBooks.Count(b => b.UserId == user.Id);

        UserDto userDto = new UserDto
        {
            Id = user.Id,
            Email = user.Email ?? string.Empty,
            Name = user.Name ?? string.Empty,
            Phone = user.Phone ?? string.Empty,
            Token = await tokenService.CreateToken(user).ConfigureAwait(false), // Refresh token
            MembershipDate = user.MembershipDate,
            IsActive = user.IsActive,
            TotalBooksBorrowed = borrowHistory.Count(),
            ActiveBorrows = activeBorrows.Count(),
            OverdueBooks = userOverdueBooks,
            TotalFines = borrowHistory.Sum(b => b.FineAmount ?? 0)
        };

        // Add roles to the collection
        foreach (string role in roles)
        {
            userDto.Roles.Add(role);
        }

        return userDto;
    }

    [Authorize]
    [HttpGet("profile")]
    public async Task<ActionResult<UserDto>> GetUserProfile()
    {
        ApplicationUser? user = await GetCurrentUserAsync().ConfigureAwait(false);

        if (user == null)
            return NotFound("User not found");

        IList<string> roles = await userManager.GetRolesAsync(user).ConfigureAwait(false);

        UserDto userDto = new UserDto
        {
            Id = user.Id,
            Email = user.Email ?? string.Empty,
            Name = user.Name ?? string.Empty,
            Phone = user.Phone ?? string.Empty,
            Token = await tokenService.CreateToken(user).ConfigureAwait(false),
            MembershipDate = user.MembershipDate,
            IsActive = user.IsActive
        };

        // Add roles to the collection
        foreach (string role in roles)
        {
            userDto.Roles.Add(role);
        }

        return userDto;
    }

    [Authorize]
    [HttpPut("profile")]
    public async Task<ActionResult<UserDto>> UpdateUserProfile(UpdateProfileDto updateDto)
    {
        ArgumentNullException.ThrowIfNull(updateDto);

        ApplicationUser? user = await GetCurrentUserAsync().ConfigureAwait(false);

        if (user == null)
            return NotFound("User not found");

        user.Name = updateDto.Name ?? string.Empty;
        user.Phone = updateDto.Phone ?? string.Empty;

        IdentityResult result = await userManager.UpdateAsync(user).ConfigureAwait(false);

        if (!result.Succeeded)
            return BadRequest("Problem updating the user profile");

        // Get updated roles
        IList<string> roles = await userManager.GetRolesAsync(user).ConfigureAwait(false);

        UserDto userDto = new UserDto
        {
            Id = user.Id,
            Email = user.Email ?? string.Empty,
            Name = user.Name ?? string.Empty,
            Phone = user.Phone ?? string.Empty,
            Token = await tokenService.CreateToken(user).ConfigureAwait(false),
            MembershipDate = user.MembershipDate,
            IsActive = user.IsActive
        };

        // Add roles to the collection
        foreach (string role in roles)
        {
            userDto.Roles.Add(role);
        }

        return userDto;
    }

    [Authorize]
    [HttpPost("change-password")]
    public async Task<ActionResult> ChangePassword(ChangePasswordDto changePasswordDto)
    {
        ArgumentNullException.ThrowIfNull(changePasswordDto);

        ApplicationUser? user = await GetCurrentUserAsync().ConfigureAwait(false);

        if (user == null)
            return NotFound("User not found");

        if (changePasswordDto.NewPassword != changePasswordDto.ConfirmNewPassword)
        {
            return BadRequest("New passwords do not match");
        }

        IdentityResult result = await userManager.ChangePasswordAsync(
            user,
            changePasswordDto.CurrentPassword ?? string.Empty,
            changePasswordDto.NewPassword ?? string.Empty
        ).ConfigureAwait(false);

        if (!result.Succeeded)
        {
            IEnumerable<string> errors = result.Errors.Select(e => e.Description);
            return BadRequest(new { errors });
        }

        return Ok(new { message = "Password changed successfully" });
    }

    [Authorize(Roles = $"{UserRoles.Librarian},{UserRoles.Admin},{UserRoles.SuperAdmin}")]
    [HttpGet("users-with-borrows")]
    public async Task<ActionResult<List<UserWithBorrowsDto>>> GetUsersWithBorrows()
    {
        List<ApplicationUser> users = userManager.Users.ToList();
        List<UserWithBorrowsDto> result = new List<UserWithBorrowsDto>();

        foreach (ApplicationUser user in users)
        {
            IEnumerable<BorrowRecordDto> activeBorrows = await borrowRecordService.GetActiveBorrowsByUserAsync(user.Id).ConfigureAwait(false);
            IEnumerable<BorrowRecordDto> borrowHistory = await borrowRecordService.GetUserBorrowHistoryAsync(user.Id).ConfigureAwait(false);
            IList<string> roles = await userManager.GetRolesAsync(user).ConfigureAwait(false);
            IEnumerable<BorrowRecordDto> overdueBooks = await borrowRecordService.GetOverdueBooksAsync().ConfigureAwait(false);
            int userOverdueBooks = overdueBooks.Count(b => b.UserId == user.Id);

            UserWithBorrowsDto userWithBorrows = new UserWithBorrowsDto
            {
                UserId = user.Id,
                UserName = user.Name ?? string.Empty,
                Email = user.Email ?? string.Empty,
                Phone = user.Phone ?? string.Empty,
                IsActive = user.IsActive,
                ActiveBorrowsCount = activeBorrows.Count(),
                TotalBorrowsCount = borrowHistory.Count(),
                OverdueBooksCount = userOverdueBooks,
                HasOverdueBooks = userOverdueBooks > 0,
                MembershipDate = user.MembershipDate
            };

            // Add roles to the collection
            foreach (string role in roles)
            {
                userWithBorrows.Roles.Add(role);
            }

            result.Add(userWithBorrows);
        }

        return result;
    }

    [Authorize(Roles = $"{UserRoles.Librarian},{UserRoles.Admin},{UserRoles.SuperAdmin}")]
    [HttpGet("user/{userId}/borrow-history")]
    public async Task<ActionResult<UserBorrowHistoryDto>> GetUserBorrowHistory(string userId)
    {
        ApplicationUser? user = await userManager.FindByIdAsync(userId).ConfigureAwait(false);
        if (user == null)
            return NotFound("User not found");

        IList<string> roles = await userManager.GetRolesAsync(user).ConfigureAwait(false);
        IEnumerable<BorrowRecordDto> activeBorrows = await borrowRecordService.GetActiveBorrowsByUserAsync(userId).ConfigureAwait(false);
        IEnumerable<BorrowRecordDto> borrowHistory = await borrowRecordService.GetUserBorrowHistoryAsync(userId).ConfigureAwait(false);
        IEnumerable<BorrowRecordDto> overdueBooks = await borrowRecordService.GetOverdueBooksAsync().ConfigureAwait(false);
        int userOverdueBooks = overdueBooks.Count(b => b.UserId == userId);

        UserBorrowHistoryDto userBorrowHistory = new UserBorrowHistoryDto
        {
            UserId = user.Id,
            UserName = user.Name ?? string.Empty,
            Email = user.Email ?? string.Empty,
            IsActive = user.IsActive,
            OverdueBooksCount = userOverdueBooks,
            TotalFines = borrowHistory.Sum(b => b.FineAmount ?? 0)
        };

        // Add roles to the collection
        foreach (string role in roles)
        {
            userBorrowHistory.Roles.Add(role);
        }

        // Add active borrows to the collection
        foreach (BorrowRecordDto borrow in activeBorrows)
        {
            userBorrowHistory.ActiveBorrows.Add(borrow);
        }

        // Add borrow history to the collection
        foreach (BorrowRecordDto borrow in borrowHistory)
        {
            userBorrowHistory.BorrowHistory.Add(borrow);
        }

        return userBorrowHistory;
    }

    [Authorize(Roles = $"{UserRoles.Admin},{UserRoles.SuperAdmin}")]
    [HttpGet("all-users")]
    public async Task<ActionResult<List<AdminUserDto>>> GetAllUsers()
    {
        List<ApplicationUser> users = userManager.Users.ToList();
        List<AdminUserDto> userDtos = new List<AdminUserDto>();

        foreach (ApplicationUser user in users)
        {
            IList<string> roles = await userManager.GetRolesAsync(user).ConfigureAwait(false);
            IEnumerable<BorrowRecordDto> activeBorrows = await borrowRecordService.GetActiveBorrowsByUserAsync(user.Id).ConfigureAwait(false);
            IEnumerable<BorrowRecordDto> borrowHistory = await borrowRecordService.GetUserBorrowHistoryAsync(user.Id).ConfigureAwait(false);

            AdminUserDto adminUser = new AdminUserDto
            {
                Id = user.Id,
                Email = user.Email ?? string.Empty,
                Name = user.Name ?? string.Empty,
                Phone = user.Phone ?? string.Empty,
                MembershipDate = user.MembershipDate,
                IsActive = user.IsActive,
                TotalBorrows = borrowHistory.Count(),
                ActiveBorrows = activeBorrows.Count(),
                LastLogin = DateTime.UtcNow // You might want to store this in ApplicationUser
            };

            // Add roles to the collection
            foreach (string role in roles)
            {
                adminUser.Roles.Add(role);
            }

            userDtos.Add(adminUser);
        }

        return userDtos;
    }

    [Authorize(Roles = $"{UserRoles.Admin},{UserRoles.SuperAdmin}")]
    [HttpGet("user/{userId}")]
    public async Task<ActionResult<AdminUserDto>> GetUserById(string userId)
    {
        ApplicationUser? user = await userManager.FindByIdAsync(userId).ConfigureAwait(false);
        if (user == null)
            return NotFound("User not found");

        IList<string> roles = await userManager.GetRolesAsync(user).ConfigureAwait(false);
        IEnumerable<BorrowRecordDto> activeBorrows = await borrowRecordService.GetActiveBorrowsByUserAsync(userId).ConfigureAwait(false);
        IEnumerable<BorrowRecordDto> borrowHistory = await borrowRecordService.GetUserBorrowHistoryAsync(userId).ConfigureAwait(false);

        AdminUserDto adminUser = new AdminUserDto
        {
            Id = user.Id,
            Email = user.Email ?? string.Empty,
            Name = user.Name ?? string.Empty,
            Phone = user.Phone ?? string.Empty,
            MembershipDate = user.MembershipDate,
            IsActive = user.IsActive,
            TotalBorrows = borrowHistory.Count(),
            ActiveBorrows = activeBorrows.Count(),
            LastLogin = DateTime.UtcNow
        };

        // Add roles to the collection
        foreach (string role in roles)
        {
            adminUser.Roles.Add(role);
        }

        return adminUser;
    }

    [Authorize(Roles = $"{UserRoles.Admin},{UserRoles.SuperAdmin}")]
    [HttpPost("assign-role")]
    public async Task<ActionResult> AssignRoleToUser([FromBody] AssignRoleDto assignRoleDto)
    {
        ArgumentNullException.ThrowIfNull(assignRoleDto);

        try
        {
            IdentityResult result = await roleService.AssignRoleToUserAsync(assignRoleDto.UserId, assignRoleDto.RoleName).ConfigureAwait(false);

            if (result.Succeeded)
                return Ok(new { message = $"Role '{assignRoleDto.RoleName}' assigned successfully" });

            return BadRequest(result.Errors);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [Authorize(Roles = $"{UserRoles.Admin},{UserRoles.SuperAdmin}")]
    [HttpPost("remove-role")]
    public async Task<ActionResult> RemoveRoleFromUser([FromBody] AssignRoleDto removeRoleDto)
    {
        ArgumentNullException.ThrowIfNull(removeRoleDto);

        try
        {
            IdentityResult result = await roleService.RemoveRoleFromUserAsync(removeRoleDto.UserId, removeRoleDto.RoleName).ConfigureAwait(false);

            if (result.Succeeded)
                return Ok(new { message = $"Role '{removeRoleDto.RoleName}' removed successfully" });

            return BadRequest(result.Errors);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [Authorize(Roles = $"{UserRoles.Admin},{UserRoles.SuperAdmin}")]
    [HttpPut("deactivate/{userId}")]
    public async Task<ActionResult> DeactivateUser(string userId)
    {
        ApplicationUser? user = await userManager.FindByIdAsync(userId).ConfigureAwait(false);
        if (user == null)
            return NotFound("User not found");

        user.IsActive = false;
        IdentityResult result = await userManager.UpdateAsync(user).ConfigureAwait(false);

        if (result.Succeeded)
            return Ok(new { message = "User deactivated successfully" });

        return BadRequest("Problem deactivating user");
    }

    [Authorize(Roles = $"{UserRoles.Admin},{UserRoles.SuperAdmin}")]
    [HttpPut("activate/{userId}")]
    public async Task<ActionResult> ActivateUser(string userId)
    {
        ApplicationUser? user = await userManager.FindByIdAsync(userId).ConfigureAwait(false);
        if (user == null)
            return NotFound("User not found");

        user.IsActive = true;
        IdentityResult result = await userManager.UpdateAsync(user).ConfigureAwait(false);

        if (result.Succeeded)
            return Ok(new { message = "User activated successfully" });

        return BadRequest("Problem activating user");
    }

    [Authorize(Roles = UserRoles.SuperAdmin)]
    [HttpDelete("user/{userId}")]
    public async Task<ActionResult> DeleteUser(string userId)
    {
        ApplicationUser? currentUser = await userManager.FindByEmailFromClaimsPrincipal(User).ConfigureAwait(false);
        if (currentUser?.Id == userId)
            return BadRequest("Cannot delete your own account");

        ApplicationUser? user = await userManager.FindByIdAsync(userId).ConfigureAwait(false);
        if (user == null)
            return NotFound("User not found");

        // Check if user has active borrows
        IEnumerable<BorrowRecordDto> activeBorrows = await borrowRecordService.GetActiveBorrowsByUserAsync(userId).ConfigureAwait(false);
        if (activeBorrows.Any())
            return BadRequest("Cannot delete user with active book borrows");

        IdentityResult result = await userManager.DeleteAsync(user).ConfigureAwait(false);

        if (result.Succeeded)
            return Ok(new { message = "User deleted successfully" });

        return BadRequest("Problem deleting user");
    }

    [Authorize(Roles = UserRoles.SuperAdmin)]
    [HttpPost("create-librarian")]
    public async Task<ActionResult<UserDto>> CreateLibrarian(RegisterDto registerDto)
    {
        ArgumentNullException.ThrowIfNull(registerDto);

        if (await userManager.FindByEmailAsync(registerDto.Email).ConfigureAwait(false) != null)
        {
            return BadRequest("Email address is already in use");
        }

        ApplicationUser user = new ApplicationUser
        {
            Name = registerDto.Name,
            Email = registerDto.Email,
            UserName = registerDto.Email,
            Phone = registerDto.Phone ?? string.Empty,
            MembershipDate = DateTime.UtcNow,
            IsActive = true
        };

        IdentityResult result = await userManager.CreateAsync(user, registerDto.Password).ConfigureAwait(false);

        if (!result.Succeeded)
            return BadRequest(result.Errors.Select(e => e.Description));

        // Assign Librarian role instead of Member
        await userManager.AddToRoleAsync(user, UserRoles.Librarian).ConfigureAwait(false);

        IList<string> roles = await userManager.GetRolesAsync(user).ConfigureAwait(false);
        string token = await tokenService.CreateToken(user).ConfigureAwait(false);

        UserDto userDto = new UserDto
        {
            Id = user.Id,
            Email = user.Email ?? string.Empty,
            Name = user.Name ?? string.Empty,
            Phone = user.Phone ?? string.Empty,
            Token = token,
            MembershipDate = user.MembershipDate,
            IsActive = user.IsActive,
            TotalBooksBorrowed = 0,
            ActiveBorrows = 0,
            OverdueBooks = 0,
            TotalFines = 0
        };

        // Add roles to the collection
        foreach (string role in roles)
        {
            userDto.Roles.Add(role);
        }

        return userDto;
    }

    [Authorize(Roles = UserRoles.SuperAdmin)]
    [HttpPost("create-admin")]
    public async Task<ActionResult<UserDto>> CreateAdmin(RegisterDto registerDto)
    {
        ArgumentNullException.ThrowIfNull(registerDto);

        if (await userManager.FindByEmailAsync(registerDto.Email).ConfigureAwait(false) != null)
        {
            return BadRequest("Email address is already in use");
        }

        ApplicationUser user = new ApplicationUser
        {
            Name = registerDto.Name,
            Email = registerDto.Email,
            UserName = registerDto.Email,
            Phone = registerDto.Phone ?? string.Empty,
            MembershipDate = DateTime.UtcNow,
            IsActive = true
        };

        IdentityResult result = await userManager.CreateAsync(user, registerDto.Password).ConfigureAwait(false);

        if (!result.Succeeded)
            return BadRequest(result.Errors.Select(e => e.Description));

        // Assign Admin role
        await userManager.AddToRoleAsync(user, UserRoles.Admin).ConfigureAwait(false);

        IList<string> roles = await userManager.GetRolesAsync(user).ConfigureAwait(false);
        string token = await tokenService.CreateToken(user).ConfigureAwait(false);

        UserDto userDto = new UserDto
        {
            Id = user.Id,
            Email = user.Email ?? string.Empty,
            Name = user.Name ?? string.Empty,
            Phone = user.Phone ?? string.Empty,
            Token = token,
            MembershipDate = user.MembershipDate,
            IsActive = user.IsActive,
            TotalBooksBorrowed = 0,
            ActiveBorrows = 0,
            OverdueBooks = 0,
            TotalFines = 0
        };

        // Add roles to the collection
        foreach (string role in roles)
        {
            userDto.Roles.Add(role);
        }

        return userDto;
    }

    [Authorize(Roles = UserRoles.SuperAdmin)]
    [HttpGet("roles")]
    public async Task<ActionResult<List<RoleDto>>> GetAllRoles()
    {
        List<RoleDto> roles = await roleService.GetAllRolesAsync().ConfigureAwait(false);
        return roles;
    }

    [Authorize(Roles = UserRoles.SuperAdmin)]
    [HttpGet("role/{roleName}/users")]
    public async Task<ActionResult<List<UserRoleDto>>> GetUsersInRole(string roleName)
    {
        if (!await roleService.RoleExistsAsync(roleName).ConfigureAwait(false))
            return NotFound($"Role '{roleName}' not found");

        List<UserRoleDto> users = await roleService.GetUsersInRoleAsync(roleName).ConfigureAwait(false);
        return users;
    }
}
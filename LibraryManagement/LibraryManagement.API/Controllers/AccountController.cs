using API.Dtos;
using LibraryManagement.API.Extensions;
using LibraryManagement.Application.Dtos;
using LibraryManagement.Application.Interfaces;
using LibraryManagement.Domain.Entities;
using LibraryManagement.Infrastructure.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace LibraryManagement.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AccountController : BaseApiController
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ITokenService _tokenService;
        private readonly IBorrowRecordService _borrowRecordService;
        private readonly IRoleService _roleService;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ITokenService tokenService,
            IBorrowRecordService borrowRecordService,
            IRoleService roleService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _tokenService = tokenService;
            _borrowRecordService = borrowRecordService;
            _roleService = roleService;
        }

        [HttpPost("login")]
        public async Task<ActionResult<UserDto>> Login(LoginDto loginDto)
        {
            var user = await _userManager.FindByEmailAsync(loginDto.Email);

            if (user == null)
                return Unauthorized("Invalid email or password");

            if (!user.IsActive)
                return Unauthorized("Account is deactivated. Please contact administrator.");

            var result = await _signInManager.CheckPasswordSignInAsync(user, loginDto.Password, false);

            if (!result.Succeeded)
                return Unauthorized("Invalid email or password");

            var roles = await _userManager.GetRolesAsync(user);
            var token = await _tokenService.CreateToken(user);

            var activeBorrows = await _borrowRecordService.GetActiveBorrowsByUserAsync(user.Id);
            var borrowHistory = await _borrowRecordService.GetUserBorrowHistoryAsync(user.Id);
            var overdueBooks = await _borrowRecordService.GetOverdueBooksAsync();
            var userOverdueBooks = overdueBooks.Count(b => b.UserId == user.Id);

            return new UserDto
            {
                Id = user.Id,
                Email = user.Email,
                Name = user.Name,
                Phone = user.Phone,
                Token = token,
                MembershipDate = user.MembershipDate,
                IsActive = user.IsActive,
                Roles = roles.ToList(),
                TotalBooksBorrowed = borrowHistory.Count(),
                ActiveBorrows = activeBorrows.Count(),
                OverdueBooks = userOverdueBooks,
                TotalFines = borrowHistory.Sum(b => b.FineAmount ?? 0)
            };
        }

        [HttpPost("register")]
        public async Task<ActionResult<UserDto>> Register(RegisterDto registerDto)
        {
            // Validate input
            if (string.IsNullOrWhiteSpace(registerDto.Email) ||
                string.IsNullOrWhiteSpace(registerDto.Password) ||
                string.IsNullOrWhiteSpace(registerDto.Name))
            {
                return BadRequest("Email, password, and name are required");
            }

            if (await _userManager.FindByEmailAsync(registerDto.Email) != null)
            {
                return BadRequest("Email address is already in use");
            }

            var user = new ApplicationUser
            {
                Name = registerDto.Name,
                Email = registerDto.Email,
                UserName = registerDto.Email,
                Phone = registerDto.Phone ?? string.Empty,
                MembershipDate = DateTime.UtcNow,
                IsActive = true
            };

            var result = await _userManager.CreateAsync(user, registerDto.Password);

            if (!result.Succeeded)
                return BadRequest(result.Errors.Select(e => e.Description));

            // Assign default role (Member)
            await _userManager.AddToRoleAsync(user, UserRoles.Member);

            // Get user roles for the response
            var roles = await _userManager.GetRolesAsync(user);
            var token = await _tokenService.CreateToken(user);

            return new UserDto
            {
                Id = user.Id,
                Email = user.Email,
                Name = user.Name,
                Phone = user.Phone,
                Token = token,
                MembershipDate = user.MembershipDate,
                IsActive = user.IsActive,
                Roles = roles.ToList(),
                TotalBooksBorrowed = 0,
                ActiveBorrows = 0,
                OverdueBooks = 0,
                TotalFines = 0
            };
        }

        [HttpGet("emailexists")]
        public async Task<ActionResult<bool>> CheckEmailExistsAsync([FromQuery] string email)
        {
            return await _userManager.FindByEmailAsync(email) != null;
        }

        [Authorize]
        [HttpGet]
        public async Task<ActionResult<UserDto>> GetCurrentUser()
        {
            var user = await _userManager.FindByEmailFromClaimsPrincipal(User);

            if (user == null)
                return NotFound("User not found");

            // Get user roles
            var roles = await _userManager.GetRolesAsync(user);

            // Get user statistics
            var activeBorrows = await _borrowRecordService.GetActiveBorrowsByUserAsync(user.Id);
            var borrowHistory = await _borrowRecordService.GetUserBorrowHistoryAsync(user.Id);
            var overdueBooks = await _borrowRecordService.GetOverdueBooksAsync();
            var userOverdueBooks = overdueBooks.Count(b => b.UserId == user.Id);

            return new UserDto
            {
                Id = user.Id,
                Email = user.Email,
                Name = user.Name,
                Phone = user.Phone,
                Token = await _tokenService.CreateToken(user),
                MembershipDate = user.MembershipDate,
                IsActive = user.IsActive,
                Roles = roles.ToList(),
                TotalBooksBorrowed = borrowHistory.Count(),
                ActiveBorrows = activeBorrows.Count(),
                OverdueBooks = userOverdueBooks,
                TotalFines = borrowHistory.Sum(b => b.FineAmount ?? 0)
            };
        }

        [Authorize]
        [HttpGet("profile")]
        public async Task<ActionResult<UserDto>> GetUserProfile()
        {
            var user = await _userManager.FindByEmailFromClaimsPrincipal(User);

            if (user == null)
                return NotFound("User not found");

            // Get user roles
            var roles = await _userManager.GetRolesAsync(user);

            return new UserDto
            {
                Id = user.Id,
                Email = user.Email,
                Name = user.Name,
                Phone = user.Phone,
                Token = await _tokenService.CreateToken(user),
                MembershipDate = user.MembershipDate,
                IsActive = user.IsActive,
                Roles = roles.ToList()
            };
        }

        [Authorize]
        [HttpPut("profile")]
        public async Task<ActionResult<UserDto>> UpdateUserProfile(UpdateProfileDto updateDto)
        {
            var user = await _userManager.FindByEmailFromClaimsPrincipal(User);

            if (user == null)
                return NotFound("User not found");

            user.Name = updateDto.Name;
            user.Phone = updateDto.Phone;

            var result = await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
                return BadRequest("Problem updating the user profile");

            // Get updated roles
            var roles = await _userManager.GetRolesAsync(user);

            return new UserDto
            {
                Id = user.Id,
                Email = user.Email,
                Name = user.Name,
                Phone = user.Phone,
                Token = await _tokenService.CreateToken(user),
                MembershipDate = user.MembershipDate,
                IsActive = user.IsActive,
                Roles = roles.ToList()
            };
        }

        [Authorize]
        [HttpPost("change-password")]
        public async Task<ActionResult> ChangePassword(ChangePasswordDto changePasswordDto)
        {
            var user = await _userManager.FindByEmailFromClaimsPrincipal(User);

            if (user == null)
                return NotFound("User not found");

            if (changePasswordDto.NewPassword != changePasswordDto.ConfirmNewPassword)
            {
                return BadRequest("New passwords do not match");
            }

            var result = await _userManager.ChangePasswordAsync(
                user,
                changePasswordDto.CurrentPassword,
                changePasswordDto.NewPassword
            );

            if (!result.Succeeded)
            {
                var errors = result.Errors.Select(e => e.Description);
                return BadRequest(new { errors });
            }

            return Ok(new { message = "Password changed successfully" });
        }

        [Authorize(Roles = $"{UserRoles.Librarian},{UserRoles.Admin},{UserRoles.SuperAdmin}")]
        [HttpGet("users-with-borrows")]
        public async Task<ActionResult<List<UserWithBorrowsDto>>> GetUsersWithBorrows()
        {
            var users = _userManager.Users.ToList();
            var result = new List<UserWithBorrowsDto>();

            foreach (var user in users)
            {
                var activeBorrows = await _borrowRecordService.GetActiveBorrowsByUserAsync(user.Id);
                var borrowHistory = await _borrowRecordService.GetUserBorrowHistoryAsync(user.Id);
                var roles = await _userManager.GetRolesAsync(user);
                var overdueBooks = await _borrowRecordService.GetOverdueBooksAsync();
                var userOverdueBooks = overdueBooks.Count(b => b.UserId == user.Id);

                result.Add(new UserWithBorrowsDto
                {
                    UserId = user.Id,
                    UserName = user.Name,
                    Email = user.Email,
                    Phone = user.Phone,
                    IsActive = user.IsActive,
                    Roles = roles.ToList(),
                    ActiveBorrowsCount = activeBorrows.Count(),
                    TotalBorrowsCount = borrowHistory.Count(),
                    OverdueBooksCount = userOverdueBooks,
                    HasOverdueBooks = userOverdueBooks > 0,
                    MembershipDate = user.MembershipDate
                });
            }

            return result;
        }

        [Authorize(Roles = $"{UserRoles.Librarian},{UserRoles.Admin},{UserRoles.SuperAdmin}")]
        [HttpGet("user/{userId}/borrow-history")]
        public async Task<ActionResult<UserBorrowHistoryDto>> GetUserBorrowHistory(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return NotFound("User not found");

            var roles = await _userManager.GetRolesAsync(user);
            var activeBorrows = await _borrowRecordService.GetActiveBorrowsByUserAsync(userId);
            var borrowHistory = await _borrowRecordService.GetUserBorrowHistoryAsync(userId);
            var overdueBooks = await _borrowRecordService.GetOverdueBooksAsync();
            var userOverdueBooks = overdueBooks.Count(b => b.UserId == userId);

            return new UserBorrowHistoryDto
            {
                UserId = user.Id,
                UserName = user.Name,
                Email = user.Email,
                IsActive = user.IsActive,
                Roles = roles.ToList(),
                ActiveBorrows = activeBorrows.ToList(),
                BorrowHistory = borrowHistory.ToList(),
                OverdueBooksCount = userOverdueBooks,
                TotalFines = borrowHistory.Sum(b => b.FineAmount ?? 0)
            };
        }

        [Authorize(Roles = $"{UserRoles.Admin},{UserRoles.SuperAdmin}")]
        [HttpGet("all-users")]
        public async Task<ActionResult<List<AdminUserDto>>> GetAllUsers()
        {
            var users = _userManager.Users.ToList();
            var userDtos = new List<AdminUserDto>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                var activeBorrows = await _borrowRecordService.GetActiveBorrowsByUserAsync(user.Id);
                var borrowHistory = await _borrowRecordService.GetUserBorrowHistoryAsync(user.Id);

                userDtos.Add(new AdminUserDto
                {
                    Id = user.Id,
                    Email = user.Email,
                    Name = user.Name,
                    Phone = user.Phone,
                    MembershipDate = user.MembershipDate,
                    IsActive = user.IsActive,
                    Roles = roles.ToList(),
                    TotalBorrows = borrowHistory.Count(),
                    ActiveBorrows = activeBorrows.Count(),
                    LastLogin = DateTime.UtcNow // You might want to store this in ApplicationUser
                });
            }

            return userDtos;
        }

        [Authorize(Roles = $"{UserRoles.Admin},{UserRoles.SuperAdmin}")]
        [HttpGet("user/{userId}")]
        public async Task<ActionResult<AdminUserDto>> GetUserById(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return NotFound("User not found");

            var roles = await _userManager.GetRolesAsync(user);
            var activeBorrows = await _borrowRecordService.GetActiveBorrowsByUserAsync(userId);
            var borrowHistory = await _borrowRecordService.GetUserBorrowHistoryAsync(userId);

            return new AdminUserDto
            {
                Id = user.Id,
                Email = user.Email,
                Name = user.Name,
                Phone = user.Phone,
                MembershipDate = user.MembershipDate,
                IsActive = user.IsActive,
                Roles = roles.ToList(),
                TotalBorrows = borrowHistory.Count(),
                ActiveBorrows = activeBorrows.Count(),
                LastLogin = DateTime.UtcNow
            };
        }

        [Authorize(Roles = $"{UserRoles.Admin},{UserRoles.SuperAdmin}")]
        [HttpPost("assign-role")]
        public async Task<ActionResult> AssignRoleToUser([FromBody] AssignRoleDto assignRoleDto)
        {
            try
            {
                var result = await _roleService.AssignRoleToUserAsync(assignRoleDto.UserId, assignRoleDto.RoleName);

                if (result.Succeeded)
                    return Ok(new { message = $"Role '{assignRoleDto.RoleName}' assigned successfully" });

                return BadRequest(result.Errors);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [Authorize(Roles = $"{UserRoles.Admin},{UserRoles.SuperAdmin}")]
        [HttpPost("remove-role")]
        public async Task<ActionResult> RemoveRoleFromUser([FromBody] AssignRoleDto removeRoleDto)
        {
            try
            {
                var result = await _roleService.RemoveRoleFromUserAsync(removeRoleDto.UserId, removeRoleDto.RoleName);

                if (result.Succeeded)
                    return Ok(new { message = $"Role '{removeRoleDto.RoleName}' removed successfully" });

                return BadRequest(result.Errors);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [Authorize(Roles = $"{UserRoles.Admin},{UserRoles.SuperAdmin}")]
        [HttpPut("deactivate/{userId}")]
        public async Task<ActionResult> DeactivateUser(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return NotFound("User not found");

            user.IsActive = false;
            var result = await _userManager.UpdateAsync(user);

            if (result.Succeeded)
                return Ok(new { message = "User deactivated successfully" });

            return BadRequest("Problem deactivating user");
        }

        [Authorize(Roles = $"{UserRoles.Admin},{UserRoles.SuperAdmin}")]
        [HttpPut("activate/{userId}")]
        public async Task<ActionResult> ActivateUser(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return NotFound("User not found");

            user.IsActive = true;
            var result = await _userManager.UpdateAsync(user);

            if (result.Succeeded)
                return Ok(new { message = "User activated successfully" });

            return BadRequest("Problem activating user");
        }

        [Authorize(Roles = UserRoles.SuperAdmin)]
        [HttpDelete("user/{userId}")]
        public async Task<ActionResult> DeleteUser(string userId)
        {
            var currentUser = await _userManager.FindByEmailFromClaimsPrincipal(User);
            if (currentUser?.Id == userId)
                return BadRequest("Cannot delete your own account");

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return NotFound("User not found");

            // Check if user has active borrows
            var activeBorrows = await _borrowRecordService.GetActiveBorrowsByUserAsync(userId);
            if (activeBorrows.Any())
                return BadRequest("Cannot delete user with active book borrows");

            var result = await _userManager.DeleteAsync(user);

            if (result.Succeeded)
                return Ok(new { message = "User deleted successfully" });

            return BadRequest("Problem deleting user");
        }

        [Authorize(Roles = UserRoles.SuperAdmin)]
        [HttpPost("create-librarian")]
        public async Task<ActionResult<UserDto>> CreateLibrarian(RegisterDto registerDto)
        {
            if (await _userManager.FindByEmailAsync(registerDto.Email) != null)
            {
                return BadRequest("Email address is already in use");
            }

            var user = new ApplicationUser
            {
                Name = registerDto.Name,
                Email = registerDto.Email,
                UserName = registerDto.Email,
                Phone = registerDto.Phone ?? string.Empty,
                MembershipDate = DateTime.UtcNow,
                IsActive = true
            };

            var result = await _userManager.CreateAsync(user, registerDto.Password);

            if (!result.Succeeded)
                return BadRequest(result.Errors.Select(e => e.Description));

            // Assign Librarian role instead of Member
            await _userManager.AddToRoleAsync(user, UserRoles.Librarian);

            var roles = await _userManager.GetRolesAsync(user);
            var token = await _tokenService.CreateToken(user);

            return new UserDto
            {
                Id = user.Id,
                Email = user.Email,
                Name = user.Name,
                Phone = user.Phone,
                Token = token,
                MembershipDate = user.MembershipDate,
                IsActive = user.IsActive,
                Roles = roles.ToList(),
                TotalBooksBorrowed = 0,
                ActiveBorrows = 0,
                OverdueBooks = 0,
                TotalFines = 0
            };
        }

        [Authorize(Roles = UserRoles.SuperAdmin)]
        [HttpPost("create-admin")]
        public async Task<ActionResult<UserDto>> CreateAdmin(RegisterDto registerDto)
        {
            if (await _userManager.FindByEmailAsync(registerDto.Email) != null)
            {
                return BadRequest("Email address is already in use");
            }

            var user = new ApplicationUser
            {
                Name = registerDto.Name,
                Email = registerDto.Email,
                UserName = registerDto.Email,
                Phone = registerDto.Phone ?? string.Empty,
                MembershipDate = DateTime.UtcNow,
                IsActive = true
            };

            var result = await _userManager.CreateAsync(user, registerDto.Password);

            if (!result.Succeeded)
                return BadRequest(result.Errors.Select(e => e.Description));

            // Assign Admin role
            await _userManager.AddToRoleAsync(user, UserRoles.Admin);

            var roles = await _userManager.GetRolesAsync(user);
            var token = await _tokenService.CreateToken(user);

            return new UserDto
            {
                Id = user.Id,
                Email = user.Email,
                Name = user.Name,
                Phone = user.Phone,
                Token = token,
                MembershipDate = user.MembershipDate,
                IsActive = user.IsActive,
                Roles = roles.ToList(),
                TotalBooksBorrowed = 0,
                ActiveBorrows = 0,
                OverdueBooks = 0,
                TotalFines = 0
            };
        }

        [Authorize(Roles = UserRoles.SuperAdmin)]
        [HttpGet("roles")]
        public async Task<ActionResult<List<RoleDto>>> GetAllRoles()
        {
            var roles = await _roleService.GetAllRolesAsync();
            return roles;
        }

        [Authorize(Roles = UserRoles.SuperAdmin)]
        [HttpGet("role/{roleName}/users")]
        public async Task<ActionResult<List<UserRoleDto>>> GetUsersInRole(string roleName)
        {
            if (!await _roleService.RoleExistsAsync(roleName))
                return NotFound($"Role '{roleName}' not found");

            var users = await _roleService.GetUsersInRoleAsync(roleName);
            return users;
        }
    }
}
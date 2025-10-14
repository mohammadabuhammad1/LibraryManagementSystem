using LibraryManagement.Domain.Entities;
using System.Threading.Tasks;

namespace LibraryManagement.Application.Interfaces
{
    public interface ITokenService
    {
        Task<string> CreateToken(ApplicationUser user);
        Task<string> GetUserIdFromToken(string token);
    }
}
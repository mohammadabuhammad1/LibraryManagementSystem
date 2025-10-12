using LibraryManagement.Domain.Entities;
using LibraryManagement.Domain.Interfaces;
using LibraryManagement.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagement.Infrastructure.Repositories
{
    public class MemberRepository : GenericRepository<Member>, IMemberRepository
    {
        public MemberRepository(LibraryDbContext context) : base(context) { }

        public async Task<Member?> GetByEmailAsync(string email)
        {
            return await _dbSet.FirstOrDefaultAsync(m => m.Email.ToLower() == email.ToLower());
        }

        public async Task<IEnumerable<Member>> GetActiveMembersAsync()
        {
            return await _dbSet
                .Where(m => m.IsActive)
                .ToListAsync();
        }
    }
}
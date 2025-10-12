using LibraryManagement.Domain.Entities;

namespace LibraryManagement.Domain.Interfaces
{
    //public interface IMemberRepository 
    //{
    //    Task<Member?> GetByIdAsync(int id);
    //    Task<IEnumerable<Member>> GetAllAsync();
    //    Task<Member> AddAsync(Member member);
    //    Task UpdateAsync(Member member);
    //    Task DeleteAsync(Member member);
    //    Task<Member?> GetByEmailAsync(string email);
    //}
    public interface IMemberRepository : IGenericRepository<Member>
    {
        Task<Member?> GetByEmailAsync(string email);
        Task<IEnumerable<Member>> GetActiveMembersAsync();
    }
}
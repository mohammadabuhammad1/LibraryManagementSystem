using LibraryManagement.Application.Dtos;

namespace LibraryManagement.Application.Interfaces
{
    public interface IMemberService
    {
        Task<MemberDto?> GetMemberByIdAsync(int id);
        Task<IEnumerable<MemberDto>> GetAllMembersAsync();
        Task<MemberDto> CreateMemberAsync(CreateMemberDto createMemberDto);
        Task<MemberDto> UpdateMemberAsync(int id, UpdateMemberDto updateMemberDto);
        Task<bool> DeleteMemberAsync(int id);
        Task<MemberDto?> GetMemberByEmailAsync(string email);
    }
}
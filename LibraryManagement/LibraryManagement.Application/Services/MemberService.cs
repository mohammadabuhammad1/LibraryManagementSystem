using LibraryManagement.Application.Dtos;
using LibraryManagement.Application.Interfaces;
using LibraryManagement.Domain.Entities;
using LibraryManagement.Domain.Interfaces;

namespace LibraryManagement.Application.Services
{
    public class MemberService : IMemberService
    {
        private readonly IMemberRepository _memberRepository;

        public MemberService(IMemberRepository memberRepository)
        {
            _memberRepository = memberRepository;
        }

        public async Task<MemberDto?> GetMemberByIdAsync(int id)
        {
            var member = await _memberRepository.GetByIdAsync(id);
            return member == null ? null : MapToMemberDto(member);
        }

        public async Task<IEnumerable<MemberDto>> GetAllMembersAsync()
        {
            var members = await _memberRepository.GetAllAsync();
            return members.Select(MapToMemberDto);
        }

        public async Task<MemberDto> CreateMemberAsync(CreateMemberDto createMemberDto)
        {
            // Check if email already exists
            var existingMember = await _memberRepository.GetByEmailAsync(createMemberDto.Email);
            if (existingMember != null)
            {
                throw new Exception($"Member with email {createMemberDto.Email} already exists.");
            }

            var member = new Member
            {
                Name = createMemberDto.Name,
                Email = createMemberDto.Email,
                Phone = createMemberDto.Phone,
                MembershipDate = DateTime.UtcNow,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            var createdMember = await _memberRepository.AddAsync(member);
            return MapToMemberDto(createdMember);
        }

        public async Task<MemberDto> UpdateMemberAsync(int id, UpdateMemberDto updateMemberDto)
        {
            var member = await _memberRepository.GetByIdAsync(id);
            if (member == null) return null;

            // Check if email is being changed and if it already exists
            if (member.Email != updateMemberDto.Email)
            {
                var existingMember = await _memberRepository.GetByEmailAsync(updateMemberDto.Email);
                if (existingMember != null)
                {
                    throw new Exception($"Member with email {updateMemberDto.Email} already exists.");
                }
            }

            member.Name = updateMemberDto.Name;
            member.Email = updateMemberDto.Email;
            member.Phone = updateMemberDto.Phone;
            member.IsActive = updateMemberDto.IsActive;

            await _memberRepository.UpdateAsync(member);
            return MapToMemberDto(member);
        }

        public async Task<bool> DeleteMemberAsync(int id)
        {
            var member = await _memberRepository.GetByIdAsync(id);
            if (member == null) return false;

            await _memberRepository.DeleteAsync(member);
            return true;
        }

        public async Task<MemberDto?> GetMemberByEmailAsync(string email)
        {
            var member = await _memberRepository.GetByEmailAsync(email);
            return member == null ? null : MapToMemberDto(member);
        }

        private static MemberDto MapToMemberDto(Member member)
        {
            return new MemberDto
            {
                Id = member.Id,
                Name = member.Name,
                Email = member.Email,
                Phone = member.Phone,
                MembershipDate = member.MembershipDate,
                IsActive = member.IsActive
            };
        }
    }
}
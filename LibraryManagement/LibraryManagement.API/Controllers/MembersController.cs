using LibraryManagement.API.Errors;
using LibraryManagement.Application.Dtos;
using LibraryManagement.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace LibraryManagement.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MembersController : ControllerBase
    {
        private readonly IMemberService _memberService;

        public MembersController(IMemberService memberService)
        {
            _memberService = memberService;
        }

        // Get all members
        [HttpGet("GetAllMembers")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<MemberDto>>> GetAllMembers()
        {
            var members = await _memberService.GetAllMembersAsync();
            return Ok(members);
        }

        // Get a member by ID
        [HttpGet("GetMemberById/{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<MemberDto>> GetMemberById(int id)
        {
            var member = await _memberService.GetMemberByIdAsync(id);
            if (member == null)
                return NotFound(new ApiResponse(404, $"Member with ID {id} not found"));

            return Ok(member);
        }

        // Get a member by email
        [HttpGet("GetMemberByEmail/{email}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<MemberDto>> GetMemberByEmail(string email)
        {
            var member = await _memberService.GetMemberByEmailAsync(email);
            if (member == null)
                return NotFound(new ApiResponse(404, $"Member with email {email} not found"));

            return Ok(member);
        }

        // Create a new member
        [HttpPost("CreateMember")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<MemberDto>> CreateMember([FromBody] CreateMemberDto createMemberDto)
        {
            try
            {
                var createdMember = await _memberService.CreateMemberAsync(createMemberDto);
                return CreatedAtAction(nameof(GetMemberById), new { id = createdMember.Id }, createdMember);
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse(400, ex.Message));
            }
        }

        // Update an existing member
        [HttpPut("UpdateMember/{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<MemberDto>> UpdateMember(int id, [FromBody] UpdateMemberDto updateMemberDto)
        {
            try
            {
                var updatedMember = await _memberService.UpdateMemberAsync(id, updateMemberDto);
                if (updatedMember == null)
                    return NotFound(new ApiResponse(404, $"Member with ID {id} not found"));

                return Ok(updatedMember);
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse(400, ex.Message));
            }
        }

        // Delete a member by ID
        [HttpDelete("DeleteMember/{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        public async Task<ActionResult> DeleteMember(int id)
        {
            var deleted = await _memberService.DeleteMemberAsync(id);
            if (!deleted)
                return NotFound(new ApiResponse(404, $"Member with ID {id} not found"));

            return NoContent();
        }
    }
}

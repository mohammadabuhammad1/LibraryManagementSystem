using LibraryManagement.API.Errors;
using LibraryManagement.Application.Dtos;
using LibraryManagement.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace LibraryManagement.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BorrowRecordsController : ControllerBase
    {
        private readonly IBorrowRecordService _borrowRecordService;

        public BorrowRecordsController(IBorrowRecordService borrowRecordService)
        {
            _borrowRecordService = borrowRecordService;
        }

        // Borrow a book
        [HttpPost("BorrowBook")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<BorrowRecordDto>> BorrowBook([FromBody] CreateBorrowRecordDto borrowDto)
        {
            try
            {
                var borrowRecord = await _borrowRecordService.BorrowBookAsync(borrowDto);
                return Ok(borrowRecord);
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse(400, ex.Message));
            }
        }

        // Return a borrowed book
        [HttpPost("ReturnBook")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<BorrowRecordDto>> ReturnBook([FromBody] ReturnBookDto returnDto)
        {
            try
            {
                var borrowRecord = await _borrowRecordService.ReturnBookAsync(returnDto);
                return Ok(borrowRecord);
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse(400, ex.Message));
            }
        }

        // Get borrow history of a member
        [HttpGet("MemberBorrowHistory/{memberId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<BorrowRecordDto>>> GetMemberBorrowHistory(int memberId)
        {
            var history = await _borrowRecordService.GetMemberBorrowHistoryAsync(memberId);
            return Ok(history);
        }

        // Get active borrow records of a member
        [HttpGet("ActiveBorrows/{memberId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<BorrowRecordDto>>> GetActiveBorrowsByMember(int memberId)
        {
            var activeBorrows = await _borrowRecordService.GetActiveBorrowsByMemberAsync(memberId);
            return Ok(activeBorrows);
        }

        // Get overdue books
        [HttpGet("OverdueBooks")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<BorrowRecordDto>>> GetOverdueBooks()
        {
            var overdueBooks = await _borrowRecordService.GetOverdueBooksAsync();
            return Ok(overdueBooks);
        }

        // Calculate fine for a borrow record
        [HttpGet("CalculateFine/{borrowRecordId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<decimal>> CalculateFine(int borrowRecordId)
        {
            try
            {
                var fine = await _borrowRecordService.CalculateFineAsync(borrowRecordId);
                return Ok(fine);
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse(400, ex.Message));
            }
        }
    }
}

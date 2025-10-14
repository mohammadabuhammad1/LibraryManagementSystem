using LibraryManagement.API.Errors;
using LibraryManagement.Application.Dtos.Book;
using LibraryManagement.Application.Dtos.Books;
using LibraryManagement.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LibraryManagement.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class BorrowRecordsController : ControllerBase
    {
        private readonly IBorrowRecordService _borrowRecordService;

        public BorrowRecordsController(IBorrowRecordService borrowRecordService)
        {
            _borrowRecordService = borrowRecordService;
        }


        [HttpPost("BorrowBook")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [Authorize(Roles = "Admin,Librarian,Member")]
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

        [HttpPost("ReturnBook")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [Authorize(Roles = "Admin,Librarian")]
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

        [HttpGet("MemberBorrowHistory/{userId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [Authorize(Roles = "Admin,Librarian,Member")]
        public async Task<ActionResult<IEnumerable<BorrowRecordDto>>> GetUserBorrowHistory(string userId)
        {
            if (User.IsInRole("Member") && User.Identity.Name != userId)
            {
                return Forbid();
            }

            var history = await _borrowRecordService.GetUserBorrowHistoryAsync(userId);
            return Ok(history);
        }

        [HttpGet("ActiveBorrows/{userId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [Authorize(Roles = "Admin,Librarian,Member")]
        public async Task<ActionResult<IEnumerable<BorrowRecordDto>>> GetActiveBorrowsByMember(string userId)
        {
            if (User.IsInRole("Member") && User.Identity.Name != userId)
            {
                return Forbid();
            }

            var activeBorrows = await _borrowRecordService.GetActiveBorrowsByUserAsync(userId);
            return Ok(activeBorrows);
        }

        [HttpGet("OverdueBooks")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [Authorize(Roles = "Admin,Librarian")]
        public async Task<ActionResult<IEnumerable<BorrowRecordDto>>> GetOverdueBooks()
        {
            var overdueBooks = await _borrowRecordService.GetOverdueBooksAsync();
            return Ok(overdueBooks);
        }


        [HttpGet("CalculateFine/{borrowRecordId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [Authorize(Roles = "Admin,Librarian,Member")]
        public async Task<ActionResult<decimal>> CalculateFine(int borrowRecordId)
        {
            try
            {
                if (User.IsInRole("Member"))
                {
                    var canViewFine = await _borrowRecordService.CanUserViewFineAsync(borrowRecordId, User.Identity.Name);
                    if (!canViewFine)
                    {
                        return Forbid();
                    }
                }

                var fine = await _borrowRecordService.CalculateFineAsync(borrowRecordId);
                return Ok(fine);
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse(400, ex.Message));
            }
        }


        [HttpGet("BookBorrowHistory/{bookId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [Authorize(Roles = "Admin,Librarian")]
        public async Task<ActionResult<IEnumerable<BorrowRecordDto>>> GetBookBorrowHistory(int bookId)
        {
            try
            {
                var history = await _borrowRecordService.GetBorrowHistoryByBookAsync(bookId);
                return Ok(history);
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse(400, ex.Message));
            }
        }

        [HttpPost("RenewBorrow/{borrowRecordId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [Authorize(Roles = "Admin,Librarian,Member")]
        public async Task<ActionResult<BorrowRecordDto>> RenewBorrow(int borrowRecordId, [FromBody] int additionalDays)
        {
            try
            {
                var userId = User.Identity.Name;
                var renewedRecord = await _borrowRecordService.RenewBorrowAsync(borrowRecordId, additionalDays, userId);
                return Ok(renewedRecord);
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse(400, ex.Message));
            }
        }

        [HttpGet("BorrowStats")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [Authorize(Roles = "Admin,Librarian")]
        public async Task<ActionResult<object>> GetBorrowStats()
        {
            try
            {
                var overdueBooks = await _borrowRecordService.GetOverdueBooksAsync();
                var stats = new
                {
                    TotalOverdue = overdueBooks.Count(),
                    TotalFines = overdueBooks.Sum(b => b.FineAmount ?? 0),
                    MostBorrowedBooks = "You could add this logic to service"
                };
                return Ok(stats);
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse(400, ex.Message));
            }
        }
    }
}
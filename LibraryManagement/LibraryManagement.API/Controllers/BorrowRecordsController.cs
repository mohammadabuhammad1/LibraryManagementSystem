using LibraryManagement.API.Errors;
using LibraryManagement.Application.Dtos.Book;
using LibraryManagement.Application.Dtos.Books;
using LibraryManagement.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LibraryManagement.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
internal sealed class BorrowRecordsController(IBorrowRecordService borrowRecordService) : ControllerBase
{
    [HttpPost("BorrowBook")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [Authorize(Roles = "Admin,Librarian,Member")]
    public async Task<ActionResult<BorrowRecordDto>> BorrowBook([FromBody] CreateBorrowRecordDto borrowDto)
    {
        BorrowRecordDto borrowRecord = await borrowRecordService
            .BorrowBookAsync(borrowDto)
            .ConfigureAwait(false);

        return Ok(borrowRecord);
    }

    [HttpPost("ReturnBook")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [Authorize(Roles = "Admin,Librarian")]
    public async Task<ActionResult<BorrowRecordDto>> ReturnBook([FromBody] ReturnBookDto returnDto)
    {
        BorrowRecordDto borrowRecord = await borrowRecordService
            .ReturnBookAsync(returnDto)
            .ConfigureAwait(false);

        return Ok(borrowRecord);
    }

    [HttpGet("MemberBorrowHistory/{userId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [Authorize(Roles = "Admin,Librarian,Member")]
    public async Task<ActionResult<IEnumerable<BorrowRecordDto>>> GetUserBorrowHistory(string userId)
    {
        if (User.IsInRole("Member") && User.Identity?.Name != userId)
        {
            return Forbid();
        }

        IEnumerable<BorrowRecordDto> history = await borrowRecordService
            .GetUserBorrowHistoryAsync(userId)
            .ConfigureAwait(false);

        return Ok(history);
    }

    [HttpGet("ActiveBorrows/{userId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [Authorize(Roles = "Admin,Librarian,Member")]
    public async Task<ActionResult<IEnumerable<BorrowRecordDto>>> GetActiveBorrowsByMember(string userId)
    {
        if (User.IsInRole("Member") && User.Identity?.Name != userId)
        {
            return Forbid();
        }

        IEnumerable<BorrowRecordDto> activeBorrows = await borrowRecordService
            .GetActiveBorrowsByUserAsync(userId)
            .ConfigureAwait(false);

        return Ok(activeBorrows);
    }

    [HttpGet("OverdueBooks")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [Authorize(Roles = "Admin,Librarian")]
    public async Task<ActionResult<IEnumerable<BorrowRecordDto>>> GetOverdueBooks()
    {
        IEnumerable<BorrowRecordDto> overdueBooks = await borrowRecordService
            .GetOverdueBooksAsync()
            .ConfigureAwait(false);

        return Ok(overdueBooks);
    }

    [HttpGet("CalculateFine/{borrowRecordId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [Authorize(Roles = "Admin,Librarian,Member")]
    public async Task<ActionResult<decimal>> CalculateFine(int borrowRecordId)
    {
        if (User.IsInRole("Member"))
        {
            string? userName = User.Identity?.Name;
            if (string.IsNullOrEmpty(userName))
            {
                return Unauthorized(new ApiResponse(401));
            }

            bool canViewFine = await borrowRecordService
                .CanUserViewFineAsync(borrowRecordId, userName)
                .ConfigureAwait(false);

            if (!canViewFine)
            {
                return Forbid();
            }
        }

        decimal fine = await borrowRecordService
            .CalculateFineAsync(borrowRecordId)
            .ConfigureAwait(false);

        return Ok(fine);
    }

    [HttpGet("BookBorrowHistory/{bookId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [Authorize(Roles = "Admin,Librarian")]
    public async Task<ActionResult<IEnumerable<BorrowRecordDto>>> GetBookBorrowHistory(int bookId)
    {
        IEnumerable<BorrowRecordDto> history = await borrowRecordService
            .GetBorrowHistoryByBookAsync(bookId)
            .ConfigureAwait(false);

        return Ok(history);
    }

    [HttpPost("RenewBorrow/{borrowRecordId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [Authorize(Roles = "Admin,Librarian,Member")]
    public async Task<ActionResult<BorrowRecordDto>> RenewBorrow(int borrowRecordId, [FromBody] int additionalDays)
    {
        string? userName = User.Identity?.Name;

        if (string.IsNullOrEmpty(userName))
        {
            return Unauthorized(new ApiResponse(401));
        }

        BorrowRecordDto renewedRecord = await borrowRecordService
            .RenewBorrowAsync(borrowRecordId, additionalDays, userName)
            .ConfigureAwait(false);

        return Ok(renewedRecord);
    }

    [HttpGet("BorrowStats")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [Authorize(Roles = "Admin,Librarian")]
    public async Task<ActionResult<BorrowStatsDto>> GetBorrowStats()
    {
        IEnumerable<BorrowRecordDto> overdueBooks = await borrowRecordService
            .GetOverdueBooksAsync()
            .ConfigureAwait(false);



        BorrowStatsDto stats = new BorrowStatsDto
        {
            TotalOverdue = overdueBooks.Count(),
            TotalFines = overdueBooks.Sum(b => b.FineAmount ?? 0),
            MostBorrowedBooks = "You could add this logic to service",
        };

        return Ok(stats);
    }
}
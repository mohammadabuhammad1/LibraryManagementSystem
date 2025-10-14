using LibraryManagement.API.Errors;
using LibraryManagement.Application.Dtos.Book;
using LibraryManagement.Application.Dtos.Books;
using LibraryManagement.Application.Interfaces;
using LibraryManagement.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace LibraryManagement.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
internal sealed class BooksController(
    IBookService bookService,
    IBorrowRecordService borrowRecordService,
    UserManager<ApplicationUser> userManager) : BaseApiController(userManager)
{
    // Get all 
    [HttpGet("GetAllBooks")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<BookDto>>> GetAllBooks()
    {
        IEnumerable<BookDto> books = await bookService.GetAllBooksAsync().ConfigureAwait(false);
        return Ok(books);
    }

    [HttpGet("GetBookById/{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [AllowAnonymous]
    public async Task<ActionResult<BookDto>> GetBookById(int id)
    {
        BookDto? book = await bookService.GetBookByIdAsync(id).ConfigureAwait(false);
        if (book == null)
            return NotFound(new ApiResponse(404, $"Book with ID {id} not found"));

        return Ok(book);
    }

    [HttpPost("CreateBook")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [Authorize(Roles = "Admin,Librarian")]
    public async Task<ActionResult<BookDto>> CreateBook(CreateBookDto createBookDto)
    {
        ApplicationUser? currentUser = await GetCurrentUserAsync().ConfigureAwait(false);
        if (currentUser == null)
            return Unauthorized(new ApiResponse(401));

        Console.WriteLine($"Book created by: {currentUser.Name} ({currentUser.Email})");

        BookDto? existingBook = await bookService.GetBookByIsbnAsync(createBookDto.ISBN).ConfigureAwait(false);
        if (existingBook != null)
            return BadRequest(new ApiResponse(400, $"Book with ISBN {createBookDto.ISBN} already exists"));

        BookDto? book = await bookService.CreateBookAsync(createBookDto).ConfigureAwait(false);
        if (book == null)
            return BadRequest(new ApiResponse(400, "Failed to create book"));

        return CreatedAtAction(nameof(GetBookById), new { id = book.Id }, book);
    }

    [HttpPut("UpdateBook/{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [Authorize(Roles = "Admin,Librarian")]
    public async Task<ActionResult<BookDto>> UpdateBook(int id, UpdateBookDto updateBookDto)
    {
        ApplicationUser? currentUser = await GetCurrentUserAsync().ConfigureAwait(false);
        if (currentUser == null)
            return Unauthorized(new ApiResponse(401));

        Console.WriteLine($"Book {id} updated by: {currentUser.Name}");

        if (!await bookService.BookExistsAsync(id).ConfigureAwait(false))
            return NotFound(new ApiResponse(404, $"Book with ID {id} not found"));

        BookDto? book = await bookService.UpdateBookAsync(id, updateBookDto).ConfigureAwait(false);
        if (book == null)
            return NotFound(new ApiResponse(404, $"Book with ID {id} not found"));

        return Ok(book);
    }

    [HttpDelete("DeleteBook/{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> DeleteBook(int id)
    {
        ApplicationUser? currentUser = await GetCurrentUserAsync().ConfigureAwait(false);
        if (currentUser == null)
            return Unauthorized(new ApiResponse(401));

        Console.WriteLine($"Book {id} deleted by: {currentUser.Name}");

        bool deleted = await bookService
            .DeleteBookAsync(id)
            .ConfigureAwait(false);

        if (!deleted)
            return NotFound(new ApiResponse(404, $"Book with ID {id} not found"));

        return NoContent();
    }

    [HttpGet("isbn/{isbn}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [AllowAnonymous]
    public async Task<ActionResult<BookDto>> GetBookByIsbn(string isbn)
    {
        BookDto? book = await bookService
            .GetBookByIsbnAsync(isbn)
            .ConfigureAwait(false);

        if (book == null)
            return NotFound(new ApiResponse(404, $"Book with ISBN {isbn} not found"));

        return Ok(book);
    }

    [HttpGet("available")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<BookDto>>> GetAvailableBooks()
    {
        IEnumerable<BookDto> books = await bookService
            .GetAvailableBooksAsync()
            .ConfigureAwait(false);

        return Ok(books);
    }

    [HttpGet("library/{libraryId:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<BookDto>>> GetBooksByLibrary(int libraryId)
    {
        IEnumerable<BookDto> books = await bookService
            .GetBooksByLibraryAsync(libraryId)
            .ConfigureAwait(false);

        return Ok(books);
    }

    [HttpGet("exists/{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [AllowAnonymous]
    public async Task<ActionResult<bool>> BookExists(int id)
    {
        bool exists = await bookService
            .BookExistsAsync(id)
            .ConfigureAwait(false);

        return Ok(exists);
    }

    [HttpPatch("{id:int}/update-copies")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [Authorize(Roles = "Admin,Librarian")]
    public async Task<ActionResult<BookDto>> UpdateBookCopies(int id, [FromBody] int totalCopies)
    {
        ApplicationUser? currentUser = await GetCurrentUserAsync().ConfigureAwait(false);
        if (currentUser == null)
            return Unauthorized(new ApiResponse(401));

        Console.WriteLine($"Book {id} copies updated by: {currentUser.Name}");

        BookDto? existingBook = await bookService
            .GetBookByIdAsync(id)
            .ConfigureAwait(false);

        if (existingBook == null)
            return NotFound(new ApiResponse(404, $"Book with ID {id} not found"));

        UpdateBookDto updateBookDto = new UpdateBookDto
        {
            Title = existingBook.Title ?? string.Empty,
            Author = existingBook.Author ?? string.Empty,
            PublishedYear = existingBook.PublishedYear,
            TotalCopies = totalCopies
        };

        BookDto? updatedBook = await bookService
            .UpdateBookAsync(id, updateBookDto)
            .ConfigureAwait(false);

        return Ok(updatedBook);
    }

    [HttpGet("stats")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [AllowAnonymous]
    public async Task<ActionResult<object>> GetBooksStats()
    {
        IEnumerable<BookDto> allBooks = await bookService
            .GetAllBooksAsync()
            .ConfigureAwait(false);
        IEnumerable<BookDto> availableBooks = await bookService
            .GetAvailableBooksAsync()
            .ConfigureAwait(false);

        BookStatsDto stats = new BookStatsDto
        {
            TotalBooks = allBooks.Count(),
            AvailableBooks = availableBooks.Count(),
            BorrowedBooks = allBooks.Count() - availableBooks.Count(),
            TotalCopies = allBooks.Sum(b => b.TotalCopies),
            AvailableCopies = allBooks.Sum(b => b.CopiesAvailable)
        };

        return Ok(stats);
    }

    [HttpGet("my-borrowed-books")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<BookDto>>> GetMyBorrowedBooks()
    {
        string? currentUserId = GetCurrentUserId();

        if (string.IsNullOrEmpty(currentUserId))
            return Unauthorized(new ApiResponse(401));

        IEnumerable<BookDto> borrowedBooks = await bookService
            .GetBorrowedBooksByUserAsync(currentUserId)
            .ConfigureAwait(false);

        return Ok(borrowedBooks);
    }

    [HttpGet("my-active-borrows")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<BorrowRecordDto>>> GetMyActiveBorrows()
    {
        string? currentUserId = GetCurrentUserId();

        if (string.IsNullOrEmpty(currentUserId))
            return Unauthorized(new ApiResponse(401));

        IEnumerable<BorrowRecordDto> activeBorrows = await borrowRecordService
            .GetActiveBorrowsByUserAsync(currentUserId)
            .ConfigureAwait(false);

        return Ok(activeBorrows);
    }

    [HttpPost("borrow/{bookId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<BorrowRecordDto>> BorrowBook(int bookId)
    {
        ApplicationUser? currentUser = await GetCurrentUserAsync().ConfigureAwait(false);
        if (currentUser == null)
            return Unauthorized(new ApiResponse(401));

        bool canBorrow = await borrowRecordService
            .CanUserBorrowAsync(currentUser.Id)
            .ConfigureAwait(false);

        if (!canBorrow)
            return BadRequest(new ApiResponse(400, "Cannot borrow book. Check if you have overdue books or reached borrowing limit."));

        CreateBorrowRecordDto borrowDto = new CreateBorrowRecordDto
        {
            UserId = currentUser.Id,
            BookId = bookId,
            BorrowDurationDays = 14,
            Notes = $"Borrowed by {currentUser.Name}"
        };

        BorrowRecordDto borrowRecord = await borrowRecordService
            .BorrowBookAsync(borrowDto)
            .ConfigureAwait(false);

        return Ok(borrowRecord);
    }

    [HttpPost("return/{bookId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [Authorize(Roles = "Admin,Librarian")]
    public async Task<ActionResult<BorrowRecordDto>> ReturnBook(int bookId)
    {
        ApplicationUser? currentUser = await GetCurrentUserAsync().ConfigureAwait(false);
        if (currentUser == null)
            return Unauthorized(new ApiResponse(401));

        Console.WriteLine($"Book return processed by: {currentUser.Name}");

        BorrowRecordDto? activeBorrow = await borrowRecordService
            .GetActiveBorrowByBookAsync(bookId)
            .ConfigureAwait(false);

        if (activeBorrow == null)
            return BadRequest(new ApiResponse(400, "No active borrow record found for this book"));

        ReturnBookDto returnDto = new ReturnBookDto
        {
            BookId = bookId,
            UserId = activeBorrow.UserId,
            Notes = $"Return processed by {currentUser.Name}"
        };

        BorrowRecordDto? borrowRecord = await borrowRecordService
            .ReturnBookAsync(returnDto)
            .ConfigureAwait(false);

        return Ok(borrowRecord);
    }

    [HttpGet("my-fines")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<decimal>> GetMyTotalFines()
    {
        string? currentUserId = GetCurrentUserId();
        if (string.IsNullOrEmpty(currentUserId))
            return Unauthorized(new ApiResponse(401));

        IEnumerable<BorrowRecordDto> borrowHistory = await borrowRecordService
            .GetUserBorrowHistoryAsync(currentUserId)
            .ConfigureAwait(false);

        decimal totalFines = borrowHistory.Sum(b => b.FineAmount ?? 0);

        return Ok(totalFines);
    }
}
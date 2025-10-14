using LibraryManagement.API.Errors;
using LibraryManagement.Application.Dtos.Book;
using LibraryManagement.Application.Dtos.Books;
using LibraryManagement.Application.Interfaces;
using LibraryManagement.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Text;

namespace LibraryManagement.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // All endpoints require authentication by default
    public class BooksController : BaseApiController
    {
        private readonly IBookService _bookService;
        private readonly IBorrowRecordService _borrowRecordService;

        public BooksController(
            IBookService bookService,
            IBorrowRecordService borrowRecordService,
            UserManager<ApplicationUser> userManager) : base(userManager)
        {
            _bookService = bookService;
            _borrowRecordService = borrowRecordService;
        }

        // Get all books
        [HttpGet("GetAllBooks")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<BookDto>>> GetAllBooks()
        {
            var books = await _bookService.GetAllBooksAsync();
            return Ok(books);
        }

        // Get a book by ID
        [HttpGet("GetBookById/{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        [AllowAnonymous]
        public async Task<ActionResult<BookDto>> GetBookById(int id)
        {
            var book = await _bookService.GetBookByIdAsync(id);
            if (book == null)
                return NotFound(new ApiResponse(404, $"Book with ID {id} not found"));

            return Ok(book);
        }

        // Create a new book
        [HttpPost("CreateBook")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [Authorize(Roles = "Admin,Librarian")]
        public async Task<ActionResult<BookDto>> CreateBook(CreateBookDto createBookDto)
        {
            try
            {
                // Get current user for auditing
                var currentUser = await GetCurrentUserAsync();
                if (currentUser == null)
                    return Unauthorized("User not found");

                Console.WriteLine($"Book created by: {currentUser.Name} ({currentUser.Email})");

                var existingBook = await _bookService.GetBookByIsbnAsync(createBookDto.ISBN);
                if (existingBook != null)
                    return BadRequest($"Book with ISBN {createBookDto.ISBN} already exists");

                var book = await _bookService.CreateBookAsync(createBookDto);
                return CreatedAtAction(nameof(GetBookById), new { id = book.Id }, book);
            }
            catch (Exception ex)
            {
                return BadRequest($"Error creating book: {ex.Message}");
            }
        }

        [HttpPut("UpdateBook/{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        [Authorize(Roles = "Admin,Librarian")]
        public async Task<ActionResult<BookDto>> UpdateBook(int id, UpdateBookDto updateBookDto)
        {
            try
            {
                // Get current user for auditing
                var currentUser = await GetCurrentUserAsync();
                if (currentUser == null)
                    return Unauthorized("User not found");

                Console.WriteLine($"Book {id} updated by: {currentUser.Name}");

                // Check if book exists
                if (!await _bookService.BookExistsAsync(id))
                    return NotFound($"Book with ID {id} not found");

                var book = await _bookService.UpdateBookAsync(id, updateBookDto);
                if (book == null)
                    return NotFound($"Book with ID {id} not found");

                return Ok(book);
            }
            catch (Exception ex)
            {
                return BadRequest($"Error updating book: {ex.Message}");
            }
        }

        // Delete a book by ID
        [HttpDelete("DeleteBook/{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        [Authorize(Roles = "Admin")] // Only Admin can delete books
        public async Task<ActionResult> DeleteBook(int id)
        {
            try
            {
                // Get current user for auditing
                var currentUser = await GetCurrentUserAsync();
                if (currentUser == null)
                    return Unauthorized("User not found");

                Console.WriteLine($"Book {id} deleted by: {currentUser.Name}");

                var deleted = await _bookService.DeleteBookAsync(id);
                if (!deleted)
                    return NotFound(new ApiResponse(404, $"Book with ID {id} not found"));

                return NoContent();
            }
            catch (Exception ex)
            {
                return BadRequest($"Error deleting book: {ex.Message}");
            }
        }

        [HttpGet("isbn/{isbn}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        [AllowAnonymous]
        public async Task<ActionResult<BookDto>> GetBookByIsbn(string isbn)
        {
            var book = await _bookService.GetBookByIsbnAsync(isbn);
            if (book == null)
                return NotFound($"Book with ISBN {isbn} not found");

            return Ok(book);
        }

        [HttpGet("available")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<BookDto>>> GetAvailableBooks()
        {
            var books = await _bookService.GetAvailableBooksAsync();
            return Ok(books);
        }

        [HttpGet("library/{libraryId:int}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<BookDto>>> GetBooksByLibrary(int libraryId)
        {
            var books = await _bookService.GetBooksByLibraryAsync(libraryId);
            return Ok(books);
        }

        [HttpGet("exists/{id:int}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [AllowAnonymous]
        public async Task<ActionResult<bool>> BookExists(int id)
        {
            var exists = await _bookService.BookExistsAsync(id);
            return Ok(exists);
        }

        [HttpPatch("{id:int}/update-copies")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        [Authorize(Roles = "Admin,Librarian")]
        public async Task<ActionResult<BookDto>> UpdateBookCopies(int id, [FromBody] int totalCopies)
        {
            try
            {
                // Get current user for auditing
                var currentUser = await GetCurrentUserAsync();
                if (currentUser == null)
                    return Unauthorized("User not found");

                Console.WriteLine($"Book {id} copies updated by: {currentUser.Name}");

                var existingBook = await _bookService.GetBookByIdAsync(id);
                if (existingBook == null)
                    return NotFound($"Book with ID {id} not found");

                var updateBookDto = new UpdateBookDto
                {
                    Title = existingBook.Title ?? string.Empty,
                    Author = existingBook.Author ?? string.Empty,
                    PublishedYear = existingBook.PublishedYear,
                    TotalCopies = totalCopies
                };

                var updatedBook = await _bookService.UpdateBookAsync(id, updateBookDto);
                return Ok(updatedBook);
            }
            catch (Exception ex)
            {
                return BadRequest($"Error updating book copies: {ex.Message}");
            }
        }

        [HttpGet("stats")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [AllowAnonymous]
        public async Task<ActionResult<object>> GetBooksStats()
        {
            var allBooks = await _bookService.GetAllBooksAsync();
            var availableBooks = await _bookService.GetAvailableBooksAsync();

            var stats = new
            {
                TotalBooks = allBooks.Count(),
                AvailableBooks = availableBooks.Count(),
                BorrowedBooks = allBooks.Count() - availableBooks.Count(),
                TotalCopies = allBooks.Sum(b => b.TotalCopies),
                AvailableCopies = allBooks.Sum(b => b.CopiesAvailable)
            };

            return Ok(stats);
        }

        // NEW ENDPOINTS WITH CURRENT USER CONTEXT

        [HttpGet("my-borrowed-books")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<BookDto>>> GetMyBorrowedBooks()
        {
            var currentUserId = GetCurrentUserId();
            if (string.IsNullOrEmpty(currentUserId))
                return Unauthorized("User not found");

            var borrowedBooks = await _bookService.GetBorrowedBooksByUserAsync(currentUserId);
            return Ok(borrowedBooks);
        }

        [HttpGet("my-active-borrows")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<BorrowRecordDto>>> GetMyActiveBorrows()
        {
            var currentUserId = GetCurrentUserId();
            if (string.IsNullOrEmpty(currentUserId))
                return Unauthorized("User not found");

            var activeBorrows = await _borrowRecordService.GetActiveBorrowsByUserAsync(currentUserId);
            return Ok(activeBorrows);
        }

        [HttpPost("borrow/{bookId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<BorrowRecordDto>> BorrowBook(int bookId)
        {
            try
            {
                var currentUser = await GetCurrentUserAsync();
                if (currentUser == null)
                    return Unauthorized("User not found");

                // Check if user can borrow (no overdue books, within limit, etc.)
                var canBorrow = await _borrowRecordService.CanUserBorrowAsync(currentUser.Id);
                if (!canBorrow)
                    return BadRequest("Cannot borrow book. Check if you have overdue books or reached borrowing limit.");

                // Use your existing CreateBorrowRecordDto
                var borrowDto = new CreateBorrowRecordDto
                {
                    UserId = currentUser.Id, // Automatically set from current user
                    BookId = bookId,
                    BorrowDurationDays = 14, // Default 2 weeks
                    Notes = $"Borrowed by {currentUser.Name}"
                };

                var borrowRecord = await _borrowRecordService.BorrowBookAsync(borrowDto);
                return Ok(borrowRecord);
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse(400, ex.Message));
            }
        }

        [HttpPost("return/{bookId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [Authorize(Roles = "Admin,Librarian")]
        public async Task<ActionResult<BorrowRecordDto>> ReturnBook(int bookId)
        {
            try
            {
                var currentUser = await GetCurrentUserAsync();
                if (currentUser == null)
                    return Unauthorized("User not found");

                Console.WriteLine($"Book return processed by: {currentUser.Name}");

                // Find the active borrow record for this book using the new method
                var activeBorrow = await _borrowRecordService.GetActiveBorrowByBookAsync(bookId);

                if (activeBorrow == null)
                    return BadRequest("No active borrow record found for this book");

                // Use your existing ReturnBookDto
                var returnDto = new ReturnBookDto
                {
                    BookId = bookId,
                    UserId = activeBorrow.UserId, // Get from the borrow record
                    Notes = $"Return processed by {currentUser.Name}"
                };

                var borrowRecord = await _borrowRecordService.ReturnBookAsync(returnDto);
                return Ok(borrowRecord);
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse(400, ex.Message));
            }
        }

        [HttpGet("my-fines")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<decimal>> GetMyTotalFines()
        {
            var currentUserId = GetCurrentUserId();
            if (string.IsNullOrEmpty(currentUserId))
                return Unauthorized("User not found");

            var borrowHistory = await _borrowRecordService.GetUserBorrowHistoryAsync(currentUserId);
            var totalFines = borrowHistory.Sum(b => b.FineAmount ?? 0);

            return Ok(totalFines);
        }


    }
}
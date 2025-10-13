using LibraryManagement.API.Errors;
using LibraryManagement.Application.Dtos;
using LibraryManagement.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LibraryManagement.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class BooksController : ControllerBase
    {
        private readonly IBookService _bookService;

        public BooksController(IBookService bookService)
        {
            _bookService = bookService;
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
        //public async Task<ActionResult<BookDto>> UpdateBook(int id, [FromBody] UpdateBookDto updateBookDto)
        //{
        //    try
        //    {
        //        var updatedBook = await _bookService.UpdateBookAsync(id, updateBookDto);
        //        if (updatedBook == null)
        //            return NotFound(new ApiResponse(404, $"Book with ID {id} not found"));

        //        return Ok(updatedBook);
        //    }
        //    catch (Exception ex)
        //    {
        //        return BadRequest(new ApiResponse(400, ex.Message));
        //    }
        //}
        public async Task<ActionResult<BookDto>> UpdateBook(int id, UpdateBookDto updateBookDto)
        {
            // Check if book exists
            if (!await _bookService.BookExistsAsync(id))
                return NotFound($"Book with ID {id} not found");

            var book = await _bookService.UpdateBookAsync(id, updateBookDto);
            if (book == null)
                return NotFound($"Book with ID {id} not found");

            return Ok(book);
        }

        // Delete a book by ID
        [HttpDelete("DeleteBook/{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        [Authorize(Roles = "Admin")] // Only Admin can delete books
        public async Task<ActionResult> DeleteBook(int id)
        {
            var deleted = await _bookService.DeleteBookAsync(id);
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
            var existingBook = await _bookService.GetBookByIdAsync(id);
            if (existingBook == null)
                return NotFound($"Book with ID {id} not found");

            var updateBookDto = new UpdateBookDto
            {
                Title = existingBook.Title,
                Author = existingBook.Author,
                PublishedYear = existingBook.PublishedYear,
                TotalCopies = totalCopies
            };

            var updatedBook = await _bookService.UpdateBookAsync(id, updateBookDto);
            return Ok(updatedBook);
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
    }
}
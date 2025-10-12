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
        public async Task<ActionResult<BookDto>> CreateBook([FromBody] CreateBookDto createBookDto)
        {
            try
            {
                var createdBook = await _bookService.CreateBookAsync(createBookDto);
                return CreatedAtAction(nameof(GetBookById), new { id = createdBook.Id }, createdBook);
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse(400, ex.Message));
            }
        }

        [HttpPut("UpdateBook/{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        [Authorize(Roles = "Admin,Librarian")] 
        public async Task<ActionResult<BookDto>> UpdateBook(int id, [FromBody] UpdateBookDto updateBookDto)
        {
            try
            {
                var updatedBook = await _bookService.UpdateBookAsync(id, updateBookDto);
                if (updatedBook == null)
                    return NotFound(new ApiResponse(404, $"Book with ID {id} not found"));

                return Ok(updatedBook);
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse(400, ex.Message));
            }
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
    }
}
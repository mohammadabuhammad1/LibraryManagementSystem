using LibraryManagement.API.Errors;
using LibraryManagement.Application.Dtos;
using LibraryManagement.Domain.Entities;
using LibraryManagement.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LibraryManagement.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class LibrariesController : ControllerBase
    {
        private readonly ILibraryRepository _libraryRepository;
        private readonly IBookRepository _bookRepository;

        public LibrariesController(ILibraryRepository libraryRepository, IBookRepository bookRepository)
        {
            _libraryRepository = libraryRepository;
            _bookRepository = bookRepository;
        }

        [HttpGet("GetAllLibraries")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [AllowAnonymous] 
        public async Task<ActionResult<IEnumerable<LibraryDto>>> GetAllLibraries()
        {
            var libraries = await _libraryRepository.GetAllAsync();
            var libraryDtos = libraries.Select(MapToLibraryDto);
            return Ok(libraryDtos);
        }

        [HttpGet("GetLibraryById/{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        [AllowAnonymous]
        public async Task<ActionResult<LibraryDetailsDto>> GetLibraryById(int id)
        {
            var library = await _libraryRepository.GetByIdAsync(id);
            if (library == null)
                return NotFound(new ApiResponse(404, $"Library with ID {id} not found"));

            var books = await _bookRepository.GetBooksByLibraryAsync(id);
            return Ok(MapToLibraryDetailsDto(library, books));
        }

        [HttpPost("CreateLibrary")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [Authorize(Roles = "Admin")] 
        public async Task<ActionResult<LibraryDto>> CreateLibrary([FromBody] CreateLibraryDto createLibraryDto)
        {
            try
            {
                var existingLibrary = await _libraryRepository.GetByNameAsync(createLibraryDto.Name);
                if (existingLibrary != null)
                    return BadRequest(new ApiResponse(400, $"Library with name '{createLibraryDto.Name}' already exists"));

                var library = new Library
                {
                    Name = createLibraryDto.Name,
                    Location = createLibraryDto.Location,
                    Description = createLibraryDto.Description
                };

                var createdLibrary = await _libraryRepository.AddAsync(library);
                return CreatedAtAction(nameof(GetLibraryById), new { id = createdLibrary.Id }, MapToLibraryDto(createdLibrary));
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse(400, ex.Message));
            }
        }

        [HttpPut("UpdateLibrary/{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        [Authorize(Roles = "Admin,Librarian")] 
        public async Task<ActionResult<LibraryDto>> UpdateLibrary(int id, [FromBody] UpdateLibraryDto updateLibraryDto)
        {
            try
            {
                var library = await _libraryRepository.GetByIdAsync(id);
                if (library == null)
                    return NotFound(new ApiResponse(404, $"Library with ID {id} not found"));

                library.Name = updateLibraryDto.Name;
                library.Location = updateLibraryDto.Location;
                library.Description = updateLibraryDto.Description;

                await _libraryRepository.UpdateAsync(library);
                return Ok(MapToLibraryDto(library));
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse(400, ex.Message));
            }
        }

        [HttpDelete("DeleteLibrary/{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        [Authorize(Roles = "Admin")] 
        public async Task<ActionResult> DeleteLibrary(int id)
        {
            var library = await _libraryRepository.GetByIdAsync(id);
            if (library == null)
                return NotFound(new ApiResponse(404, $"Library with ID {id} not found"));

            var books = await _bookRepository.GetBooksByLibraryAsync(id);
            if (books.Any())
                return BadRequest(new ApiResponse(400, "Cannot delete library that contains books"));

            await _libraryRepository.DeleteAsync(library);
            return NoContent();
        }

        [HttpPost("AddBookToLibrary/{libraryId}/{bookId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [Authorize(Roles = "Admin,Librarian")] 
        public async Task<ActionResult<BookDto>> AddBookToLibrary(int libraryId, int bookId)
        {
            try
            {
                var library = await _libraryRepository.GetByIdAsync(libraryId);
                if (library == null)
                    return NotFound(new ApiResponse(404, $"Library with ID {libraryId} not found"));

                var book = await _bookRepository.GetByIdAsync(bookId);
                if (book == null)
                    return NotFound(new ApiResponse(404, $"Book with ID {bookId} not found"));

                book.LibraryId = libraryId;
                await _bookRepository.UpdateAsync(book);

                return Ok(MapToBookDto(book));
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse(400, ex.Message));
            }
        }

        private static LibraryDto MapToLibraryDto(Library library)
        {
            return new LibraryDto
            {
                Id = library.Id,
                Name = library.Name,
                Location = library.Location,
                Description = library.Description,
                BookCount = library.Books?.Count ?? 0,
                CreatedAt = library.CreatedAt
            };
        }

        private static LibraryDetailsDto MapToLibraryDetailsDto(Library library, IEnumerable<Book> books)
        {
            return new LibraryDetailsDto
            {
                Id = library.Id,
                Name = library.Name,
                Location = library.Location,
                Description = library.Description,
                Books = books.Select(MapToBookDto),
                CreatedAt = library.CreatedAt
            };
        }

        private static BookDto MapToBookDto(Book book)
        {
            return new BookDto
            {
                Id = book.Id,
                Title = book.Title,
                Author = book.Author,
                ISBN = book.ISBN,
                PublishedYear = book.PublishedYear,
                TotalCopies = book.TotalCopies,
                CopiesAvailable = book.CopiesAvailable,
                LibraryId = book.LibraryId ?? 0
            };
        }
    }
}
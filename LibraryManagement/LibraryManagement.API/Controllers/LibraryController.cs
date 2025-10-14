using LibraryManagement.API.Errors;
using LibraryManagement.Application.Dtos.Books;
using LibraryManagement.Application.Dtos.Libraries;
using LibraryManagement.Domain.Entities;
using LibraryManagement.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LibraryManagement.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
internal class LibrariesController(ILibraryRepository libraryRepository, IBookRepository bookRepository) : ControllerBase
{
    [HttpGet("GetAllLibraries")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [AllowAnonymous] 
    public async Task<ActionResult<IEnumerable<LibraryDto>>> GetAllLibraries()
    {
        IEnumerable<Library> libraries = await libraryRepository.GetAllAsync().ConfigureAwait(false);
        IEnumerable<LibraryDto> libraryDtos = libraries.Select(MapToLibraryDto);
        return Ok(libraryDtos);
    }

    [HttpGet("GetLibraryById/{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [AllowAnonymous]
    public async Task<ActionResult<LibraryDetailsDto>> GetLibraryById(int id)
    {
        Library? library = await libraryRepository.GetByIdAsync(id).ConfigureAwait(false);
        if (library == null)
            return NotFound(new ApiResponse(404, $"Library with ID {id} not found"));

        IEnumerable<Book> books = await bookRepository.GetBooksByLibraryAsync(id).ConfigureAwait(false);

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
            Library? existingLibrary = await libraryRepository.GetByNameAsync(createLibraryDto.Name).ConfigureAwait(false);
            if (existingLibrary != null)
                return BadRequest(new ApiResponse(400, $"Library with name '{createLibraryDto.Name}' already exists"));

            var library = new Library
            {
                Name = createLibraryDto.Name,
                Location = createLibraryDto.Location,
                Description = createLibraryDto.Description
            };

            Library createdLibrary = await libraryRepository.AddAsync(library).ConfigureAwait(false);
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
            Library? library = await libraryRepository.GetByIdAsync(id).ConfigureAwait(false);
            if (library == null)
                return NotFound(new ApiResponse(404, $"Library with ID {id} not found"));

            library.Name = updateLibraryDto.Name;
            library.Location = updateLibraryDto.Location;
            library.Description = updateLibraryDto.Description;

            await libraryRepository.UpdateAsync(library).ConfigureAwait(false);
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
        Library? library = await libraryRepository.GetByIdAsync(id).ConfigureAwait(false);
        if (library == null)
            return NotFound(new ApiResponse(404, $"Library with ID {id} not found"));

        IEnumerable<Book> books = await bookRepository.GetBooksByLibraryAsync(id).ConfigureAwait(false);
        if (books.Any())
            return BadRequest(new ApiResponse(400, "Cannot delete library that contains books"));

        await libraryRepository.DeleteAsync(library).ConfigureAwait(false);
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
            Library? library = await libraryRepository.GetByIdAsync(libraryId).ConfigureAwait(false);
            if (library == null)
                return NotFound(new ApiResponse(404, $"Library with ID {libraryId} not found"));

            Book? book = await bookRepository.GetByIdAsync(bookId).ConfigureAwait(false);
            if (book == null)
                return NotFound(new ApiResponse(404, $"Book with ID {bookId} not found"));

            book.LibraryId = libraryId;
            await bookRepository.UpdateAsync(book).ConfigureAwait(false);

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
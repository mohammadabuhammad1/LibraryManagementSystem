using LibraryManagement.Application.Dtos;

namespace LibraryManagement.Application.Interfaces
{
    public interface IBookService
    {
        Task<BookDto?> GetBookByIdAsync(int id);
        Task<IEnumerable<BookDto>> GetAllBooksAsync();
        Task<BookDto> CreateBookAsync(CreateBookDto createBookDto);
        Task<BookDto> UpdateBookAsync(int id, UpdateBookDto updateBookDto);
        Task<bool> DeleteBookAsync(int id);
    }
}
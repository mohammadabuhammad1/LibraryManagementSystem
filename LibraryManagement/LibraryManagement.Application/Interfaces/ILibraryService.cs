using LibraryManagement.Application.Dtos.Books;

namespace LibraryManagement.Application.Interfaces;

public interface ILibraryService
{

    Task<BookDto> BorrowBookAsync(int bookId);
    Task<BookDto> ReturnBookAsync(int bookId);
    Task<IEnumerable<BookDto>> GetBorrowedBooksAsync();
    Task<IEnumerable<BookDto>> GetAvailableBooksAsync();


}

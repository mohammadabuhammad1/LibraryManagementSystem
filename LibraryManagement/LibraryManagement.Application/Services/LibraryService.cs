using LibraryManagement.Application.Dtos.Books;
using LibraryManagement.Application.Interfaces;
using LibraryManagement.Domain.Entities;
using LibraryManagement.Domain.Interfaces;

namespace LibraryManagement.Application.Services;

public class LibraryService(IBookRepository bookRepository) : ILibraryService
{
    public async Task<BookDto> BorrowBookAsync(int bookId)
    {
        Book? book = await bookRepository.GetByIdAsync(bookId).ConfigureAwait(false);

        if (book == null)
            throw new InvalidOperationException($"Book with ID {bookId} not found.");

        if (book.CopiesAvailable <= 0)
            throw new InvalidOperationException($"No copies available for book with ID {bookId}.");

        book.CopiesAvailable--;
        await bookRepository.UpdateAsync(book).ConfigureAwait(false);

        return MapToBookDto(book);
    }

    public async Task<BookDto> ReturnBookAsync(int bookId)
    {
        Book? book = await bookRepository.GetByIdAsync(bookId).ConfigureAwait(false);
        if (book == null)
            throw new InvalidOperationException($"Book with ID {bookId} not found.");

        if (book.CopiesAvailable >= book.TotalCopies)
            throw new InvalidOperationException($"'{book.Title}' is not currently borrowed.");

        book.CopiesAvailable++;
        await bookRepository.UpdateAsync(book).ConfigureAwait(false);

        return MapToBookDto(book);
    }

    public async Task<IEnumerable<BookDto>> GetAvailableBooksAsync()
    {
        IEnumerable<Book> books = await bookRepository.GetAvailableBooksAsync().ConfigureAwait(false);
        return books.Select(MapToBookDto);
    }

    public async Task<IEnumerable<BookDto>> GetBorrowedBooksAsync()
    {
        IEnumerable<Book> allBooks = await bookRepository.GetAllAsync().ConfigureAwait(false);

        IEnumerable<Book> borrowedBooks = allBooks.Where(book => book.CopiesAvailable < book.TotalCopies);

        return borrowedBooks.Select(MapToBookDto);
    }

    public async Task<IEnumerable<BookDto>> GetBooksByLibraryAsync(int libraryId)
    {
        IEnumerable<Book> books = await bookRepository.GetBooksByLibraryAsync(libraryId).ConfigureAwait(false);
        return books.Select(MapToBookDto);
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
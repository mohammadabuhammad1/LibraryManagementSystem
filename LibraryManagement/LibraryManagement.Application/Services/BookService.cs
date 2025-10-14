using LibraryManagement.Application.Dtos.Books;
using LibraryManagement.Application.Interfaces;
using LibraryManagement.Domain.Entities;
using LibraryManagement.Domain.Interfaces;

namespace LibraryManagement.Application.Services;

public class BookService(IBookRepository bookRepository) : IBookService
{
    public async Task<BookDto?> GetBookByIdAsync(int id)
    {
        Book? book = await bookRepository.GetByIdAsync(id).ConfigureAwait(false);
        return book == null ? null : MapToBookDto(book);
    }

    public async Task<IEnumerable<BookDto>> GetAllBooksAsync()
    {
        IEnumerable<Book> books = await bookRepository.GetAllAsync().ConfigureAwait(false);
        return books.Select(MapToBookDto);
    }

    public async Task<BookDto?> CreateBookAsync(CreateBookDto createBookDto)
    {
        if (createBookDto is null)
            return null;

        var book = new Book
        {
            Title = createBookDto.Title,
            Author = createBookDto.Author,
            ISBN = createBookDto.ISBN,
            PublishedYear = createBookDto.PublishedYear,
            TotalCopies = createBookDto.TotalCopies,
            CopiesAvailable = createBookDto.TotalCopies,
            CreatedAt = DateTime.UtcNow
            
        };

        Book? createdBook = await bookRepository.AddAsync(book).ConfigureAwait(false);
        return MapToBookDto(createdBook);
    }

    public async Task<BookDto?> UpdateBookAsync(int id, UpdateBookDto updateBookDto)
    {
        if (updateBookDto is null)
            return null;


        Book? book = await bookRepository.GetByIdAsync(id).ConfigureAwait(false);
        if (book == null) return null;

        book.Title = updateBookDto.Title;
        book.Author = updateBookDto.Author;
        book.PublishedYear = updateBookDto.PublishedYear;
        book.TotalCopies = updateBookDto.TotalCopies;

        await bookRepository.UpdateAsync(book).ConfigureAwait(false);
        return MapToBookDto(book);
    }

    public async Task<bool> DeleteBookAsync(int id)
    {
        Book? book = await bookRepository.GetByIdAsync(id).ConfigureAwait(false);
        if (book == null) return false;

        await bookRepository.DeleteAsync(book).ConfigureAwait(false);
        return true;
    }

    public async Task<BookDto?> GetBookByIsbnAsync(string isbn)
    {
        Book? book = await bookRepository.GetByIsbnAsync(isbn).ConfigureAwait(false);
        return book == null ? null : MapToBookDto(book);
    }

    public async Task<IEnumerable<BookDto>> GetAvailableBooksAsync()
    {
        IEnumerable<Book> books = await bookRepository.GetAvailableBooksAsync().ConfigureAwait(false);
        return books.Select(MapToBookDto);
    }

    public async Task<IEnumerable<BookDto>> GetBooksByLibraryAsync(int libraryId)
    {
        IEnumerable<Book> books = await bookRepository.GetBooksByLibraryAsync(libraryId).ConfigureAwait(false);
        return books.Select(MapToBookDto);
    }

    public async Task<bool> BookExistsAsync(int id)
    {
        return await bookRepository.ExistsAsync(id).ConfigureAwait(false);
    }

    public async Task<IEnumerable<BookDto>> GetBorrowedBooksByUserAsync(string userId)
    {
        IEnumerable<Book> books = await bookRepository.GetBorrowedBooksByUserAsync(userId).ConfigureAwait(false);
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
            CopiesAvailable = book.CopiesAvailable
        };
    }
}
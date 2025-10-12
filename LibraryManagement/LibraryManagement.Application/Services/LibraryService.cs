using LibraryManagement.Application.Dtos;
using LibraryManagement.Application.Interfaces;
using LibraryManagement.Domain.Entities;
using LibraryManagement.Domain.Interfaces;

namespace LibraryManagement.Application.Services
{
    public class LibraryService : ILibraryService
    {
        private readonly IBookRepository _bookRepository;
        private readonly ILibraryRepository _libraryRepository;

        public LibraryService(IBookRepository bookRepository, ILibraryRepository libraryRepository)
        {
            _bookRepository = bookRepository;
            _libraryRepository = libraryRepository;
        }

        public async Task<BookDto> BorrowBookAsync(int bookId)
        {
            var book = await _bookRepository.GetByIdAsync(bookId);
            if (book == null)
                throw new Exception($"Book with ID {bookId} not found.");

            if (book.CopiesAvailable <= 0)
                throw new Exception($"No copies available for book with ID {bookId}.");

            book.CopiesAvailable--;
            await _bookRepository.UpdateAsync(book);

            return MapToBookDto(book);
        }

        public async Task<BookDto> ReturnBookAsync(int bookId)
        {
            var book = await _bookRepository.GetByIdAsync(bookId);
            if (book == null)
                throw new Exception($"Book with ID {bookId} not found.");

            if (book.CopiesAvailable >= book.TotalCopies)
                throw new Exception($"'{book.Title}' is not currently borrowed.");

            book.CopiesAvailable++;
            await _bookRepository.UpdateAsync(book);

            return MapToBookDto(book);
        }

        public async Task<IEnumerable<BookDto>> GetAvailableBooksAsync()
        {
            var books = await _bookRepository.GetAvailableBooksAsync();
            return books.Select(MapToBookDto);
        }

        public async Task<IEnumerable<BookDto>> GetBorrowedBooksAsync()
        {
            var allBooks = await _bookRepository.GetAllAsync();
            var borrowedBooks = allBooks.Where(book => book.CopiesAvailable < book.TotalCopies);
            return borrowedBooks.Select(MapToBookDto);
        }

        public async Task<IEnumerable<BookDto>> GetBooksByLibraryAsync(int libraryId)
        {
            var books = await _bookRepository.GetBooksByLibraryAsync(libraryId);
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
                LibraryId = (int)book.LibraryId
            };
        }
    }
}
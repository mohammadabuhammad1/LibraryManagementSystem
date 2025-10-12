using LibraryManagement.Application.Dtos;
using LibraryManagement.Application.Interfaces;
using LibraryManagement.Domain.Entities;
using LibraryManagement.Domain.Interfaces;

namespace LibraryManagement.Application.Services
{
    public class BookService : IBookService
    {
        private readonly IBookRepository _bookRepository;

        public BookService(IBookRepository bookRepository)
        {
            _bookRepository = bookRepository;
        }

        public async Task<BookDto?> GetBookByIdAsync(int id)
        {
            var book = await _bookRepository.GetByIdAsync(id);
            return book == null ? null : MapToBookDto(book);
        }

        public async Task<IEnumerable<BookDto>> GetAllBooksAsync()
        {
            var books = await _bookRepository.GetAllAsync();
            return books.Select(MapToBookDto);
        }

        public async Task<BookDto> CreateBookAsync(CreateBookDto createBookDto)
        {
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

            var createdBook = await _bookRepository.AddAsync(book);
            return MapToBookDto(createdBook);
        }

        public async Task<BookDto> UpdateBookAsync(int id, UpdateBookDto updateBookDto)
        {
            var book = await _bookRepository.GetByIdAsync(id);
            if (book == null) return null;

            book.Title = updateBookDto.Title;
            book.Author = updateBookDto.Author;
            book.PublishedYear = updateBookDto.PublishedYear;
            book.TotalCopies = updateBookDto.TotalCopies;

            await _bookRepository.UpdateAsync(book);
            return MapToBookDto(book);
        }

        public async Task<bool> DeleteBookAsync(int id)
        {
            var book = await _bookRepository.GetByIdAsync(id);
            if (book == null) return false;

            await _bookRepository.DeleteAsync(book);
            return true;
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
}
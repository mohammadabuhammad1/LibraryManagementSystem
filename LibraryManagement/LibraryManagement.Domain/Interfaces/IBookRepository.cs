using LibraryManagement.Domain.Entities;

namespace LibraryManagement.Domain.Interfaces
{
    //public interface IBookRepository : IGenericRepository<Book>
    //{
    //    Task<Book?> GetByIdAsync(int id);
    //    Task<IEnumerable<Book>> GetAllAsync();
    //    Task<Book> AddAsync(Book book);
    //    Task UpdateAsync(Book book);
    //    Task DeleteAsync(Book book);
    //}
    public interface IBookRepository : IGenericRepository<Book>
    {
        Task<Book?> GetByIsbnAsync(string isbn);
        Task<IEnumerable<Book>> GetAvailableBooksAsync();
        Task<IEnumerable<Book>> GetBooksByLibraryAsync(int libraryId);
    }
}

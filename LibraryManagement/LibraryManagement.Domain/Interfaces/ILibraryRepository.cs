using LibraryManagement.Domain.Entities;

namespace LibraryManagement.Domain.Interfaces;

public interface ILibraryRepository : IGenericRepository<Library>
{
    Task<Library?> GetByNameAsync(string name);
    Task<IEnumerable<Library>> GetLibrariesWithBooksAsync();
}
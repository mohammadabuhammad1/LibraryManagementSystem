using LibraryManagement.Domain.Entities;
using LibraryManagement.Domain.Interfaces;
using LibraryManagement.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagement.Infrastructure.Repositories
{
    public class LibraryRepository : GenericRepository<Library>, ILibraryRepository
    {
        public LibraryRepository(LibraryDbContext context) : base(context) { }

        public async Task<Library?> GetByNameAsync(string name)
        {
            return await _dbSet.FirstOrDefaultAsync(l => l.Name.ToLower() == name.ToLower());
        }

        public async Task<IEnumerable<Library>> GetLibrariesWithBooksAsync()
        {
            return await _dbSet
                .Include(l => l.Books)
                .ToListAsync();
        }
    }
}
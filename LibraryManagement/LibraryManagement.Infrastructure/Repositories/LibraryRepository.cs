using LibraryManagement.Domain.Entities;
using LibraryManagement.Domain.Interfaces;
using LibraryManagement.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagement.Infrastructure.Repositories;

public class LibraryRepository : GenericRepository<Library>, ILibraryRepository
{
    public LibraryRepository(LibraryDbContext context) : base(context) { }

    public async Task<Library?> GetByNameAsync(string name)
    {
        return await _dbSet.FirstOrDefaultAsync(l => l.Name.Equals(name, StringComparison.OrdinalIgnoreCase)).ConfigureAwait(false);

        //  Uses StringComparison.OrdinalIgnoreCase for case-insensitive comparison
        //  No string allocation (unlike ToLower/ToUpper which create new strings)
        //  Culture-invariant and safe for all locales
        //  Better performance - direct comparison without temporary strings
        //  EF Core translates this efficiently to SQL

    }

    public async Task<IEnumerable<Library>> GetLibrariesWithBooksAsync()
    {
        return await _dbSet
            .Include(l => l.Books)
            .ToListAsync().ConfigureAwait(false);
    }
}
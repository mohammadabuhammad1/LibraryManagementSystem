using LibraryManagement.Domain.Entities;
using LibraryManagement.Domain.Interfaces;
using LibraryManagement.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagement.Infrastructure.Repositories;

public class GenericRepository<T>(LibraryDbContext context) : IGenericRepository<T> where T : BaseEntity
{
    protected LibraryDbContext Context { get; } = context;// review

    public async Task<T?> GetByIdAsync(int id)
    {
        return await Context.Set<T>().FindAsync(id).ConfigureAwait(false);
    }

    public async Task<IEnumerable<T>> GetAllAsync()
    {
        return await Context.Set<T>().ToListAsync().ConfigureAwait(false);
    }

    public async Task<T> AddAsync(T entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        entity.CreatedAt = DateTime.UtcNow;
        await Context.Set<T>().AddAsync(entity).ConfigureAwait(false);
        await Context.SaveChangesAsync().ConfigureAwait(false);
        return entity;
    }

    public async Task UpdateAsync(T entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        entity.UpdatedAt = DateTime.UtcNow;
        Context.Set<T>().Update(entity);
        await Context.SaveChangesAsync().ConfigureAwait(false);
    }

    public async Task DeleteAsync(T entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        Context.Set<T>().Remove(entity);
        await Context.SaveChangesAsync().ConfigureAwait(false);
    }

    public async Task<bool> ExistsAsync(int id)
    {
        return await Context.Set<T>().AnyAsync(e => e.Id == id).ConfigureAwait(false);
    }
}
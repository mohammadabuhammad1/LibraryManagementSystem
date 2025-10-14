using LibraryManagement.Domain.Entities;
using LibraryManagement.Domain.Interfaces;
using LibraryManagement.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagement.Infrastructure.Repositories;

public class GenericRepository<T>(LibraryDbContext context) : IGenericRepository<T> where T : BaseEntity
{
    protected readonly LibraryDbContext _context = context;
    protected readonly DbSet<T> _dbSet = context.Set<T>();

    public async Task<T?> GetByIdAsync(int id)
    {
        return await _dbSet.FindAsync(id).ConfigureAwait(false);
    }

    public async Task<IEnumerable<T>> GetAllAsync()
    {
        return await _dbSet.ToListAsync().ConfigureAwait(false);
    }

    public async Task<T> AddAsync(T entity)
    {
        entity.CreatedAt = DateTime.UtcNow;
        await _dbSet.AddAsync(entity).ConfigureAwait(false);
        await _context.SaveChangesAsync().ConfigureAwait(false);
        return entity;
    }

    public async Task UpdateAsync(T entity)
    {
        entity.UpdatedAt = DateTime.UtcNow;
        _dbSet.Update(entity);
        await _context.SaveChangesAsync().ConfigureAwait(false);
    }

    public async Task DeleteAsync(T entity)
    {
        _dbSet.Remove(entity);
        await _context.SaveChangesAsync().ConfigureAwait(false);
    }

    public async Task<bool> ExistsAsync(int id)
    {
        return await _dbSet.AnyAsync(e => e.Id == id).ConfigureAwait(false);
    }
}

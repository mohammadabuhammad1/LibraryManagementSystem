using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace LibraryManagement.Infrastructure.Data;

public partial class DatabaseInitializer(IServiceProvider serviceProvider, ILogger<DatabaseInitializer> logger)
{
    // LoggerMessage delegates for high-performance logging
    private static readonly Action<ILogger, Exception?> _databaseMigrated =
        LoggerMessage.Define(LogLevel.Information, new EventId(1, "DatabaseMigrated"),
            "Database migrated successfully");

    private static readonly Action<ILogger, Exception?> _initializationCompleted =
        LoggerMessage.Define(LogLevel.Information, new EventId(2, "InitializationCompleted"),
            "Database initialization completed successfully");

    private static readonly Action<ILogger, string, Exception?> _initializationError =
        LoggerMessage.Define<string>(LogLevel.Error, new EventId(3, "InitializationError"),
            "An error occurred while initializing the database: {ErrorMessage}");

    public async Task InitializeAsync()
    {
        using IServiceScope scope = serviceProvider.CreateScope();  // ✓ Explicit type
        IServiceProvider scopeServiceProvider = scope.ServiceProvider;  // ✓ Renamed to avoid hiding

        try
        {
            LibraryDbContext context = scopeServiceProvider.GetRequiredService<LibraryDbContext>();  // ✓ Explicit type

            // Apply migrations
            await context.Database.MigrateAsync().ConfigureAwait(false);   
            _databaseMigrated(logger, null);

            // Seed roles and super admin
            RoleSeeder roleSeeder = scopeServiceProvider.GetRequiredService<RoleSeeder>();  // ✓ Explicit type
            await roleSeeder.SeedRolesAsync().ConfigureAwait(false);   
            await roleSeeder.SeedSuperAdminAsync().ConfigureAwait(false);   

            // Seed initial data
            DataSeeder dataSeeder = scopeServiceProvider.GetRequiredService<DataSeeder>();  // ✓ Explicit type
            await dataSeeder.SeedAsync().ConfigureAwait(false);   

            _initializationCompleted(logger, null);
        }
        catch (DbUpdateException dbEx)
        {
            _initializationError(logger, $"Database update failed: {dbEx.Message}", dbEx);
            throw new InvalidOperationException("Database initialization failed due to migration error", dbEx);
        }
        catch (InvalidOperationException invalidOpEx)
        {
            _initializationError(logger, $"Service resolution failed: {invalidOpEx.Message}", invalidOpEx);
            throw new InvalidOperationException("Database initialization failed due to service resolution error", invalidOpEx);
        }
        catch (Exception ex)
        {
            _initializationError(logger, $"Unexpected error: {ex.Message}", ex);
            throw new DatabaseInitializationException("Database initialization failed", ex);// ask eng moath
        }
    }
}
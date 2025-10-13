using LibraryManagement.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace LibraryManagement.Infrastructure.Data
{
    public class DatabaseInitializer
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<DatabaseInitializer> _logger;

        public DatabaseInitializer(IServiceProvider serviceProvider, ILogger<DatabaseInitializer> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public async Task InitializeAsync()
        {
            using var scope = _serviceProvider.CreateScope();
            var serviceProvider = scope.ServiceProvider;

            try
            {
                var context = serviceProvider.GetRequiredService<LibraryDbContext>();

                // Apply migrations
                await context.Database.MigrateAsync();
                _logger.LogInformation("Database migrated successfully");

                // Seed roles and super admin
                var roleSeeder = serviceProvider.GetRequiredService<RoleSeeder>();
                await roleSeeder.SeedRolesAsync();
                await roleSeeder.SeedSuperAdminAsync();

                // Seed initial data
                var dataSeeder = serviceProvider.GetRequiredService<DataSeeder>();
                await dataSeeder.SeedAsync();

                _logger.LogInformation("Database initialization completed successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while initializing the database.");
                throw;
            }
        }
    }
}
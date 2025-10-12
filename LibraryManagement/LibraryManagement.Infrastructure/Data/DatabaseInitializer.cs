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
                await context.Database.MigrateAsync();
                _logger.LogInformation("Main database migrated successfully");

                var roleSeeder = serviceProvider.GetRequiredService<RoleSeeder>();
                await roleSeeder.SeedRolesAsync();     
                await roleSeeder.SeedSuperAdminAsync();

                await SeedLibraryDataAsync(context);

                _logger.LogInformation("Database initialization completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while initializing the database");
                throw;
            }
        }

        private async Task SeedLibraryDataAsync(LibraryDbContext context)
        {
            // Seed initial libraries if none exist
            if (!await context.Libraries.AnyAsync())
            {
                var libraries = new List<Library>
                {
                    new Library
                    {
                        Name = "Main Library",
                        Location = "City Center",
                        Description = "Main public library",
                        CreatedAt = DateTime.UtcNow
                    },
                    new Library
                    {
                        Name = "Community Library",
                        Location = "West District",
                        Description = "Community branch library",
                        CreatedAt = DateTime.UtcNow
                    }
                };

                await context.Libraries.AddRangeAsync(libraries);
                await context.SaveChangesAsync();
                _logger.LogInformation("Initial libraries seeded");
            }
        }
    }
}
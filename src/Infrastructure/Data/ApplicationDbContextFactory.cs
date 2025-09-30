using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace stibe.api.Data
{
    /// <summary>
    /// Design-time factory for Entity Framework migrations and tooling
    /// This enables EF migrations to work without a running application
    /// </summary>
    public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
    {
        public ApplicationDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
            
            // Build configuration to read from appsettings files
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false)
                .AddJsonFile("appsettings.Development.json", optional: true)
                .AddJsonFile("appsettings.Production.json", optional: true)
                .AddEnvironmentVariables()
                .Build();
            
            // Get connection string from configuration (same as runtime)
            var connectionString = configuration.GetConnectionString("DefaultConnection");
            
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException(
                    "Connection string 'DefaultConnection' not found. " +
                    "Please ensure it's configured in appsettings.json or environment variables.");
            }
            
            optionsBuilder.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 40)));
            
            return new ApplicationDbContext(optionsBuilder.Options);
        }
    }
}

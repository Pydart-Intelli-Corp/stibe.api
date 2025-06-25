using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace stibe.api.Data
{
    public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
    {
        public ApplicationDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
            
            // Use the connection string directly for design-time operations
            var connectionString = "Server=localhost;Database=stibe_booking;Uid=root;Pwd=2232;Port=3306;SslMode=none;AllowPublicKeyRetrieval=true;";
            
            optionsBuilder.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
            
            return new ApplicationDbContext(optionsBuilder.Options);
        }
    }
}

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
            var connectionString = "Server=psrazuredb.mysql.database.azure.com;Port=3306;UserID=psrcloud;Password=Access@LRC2404;Database=Stibe_db;SslMode=Required;SslCa=D:\\MY PROJECTS\\Azure DB\\DigiCertGlobalRootCA.crt.pem";
            
            optionsBuilder.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 40)));
            
            return new ApplicationDbContext(optionsBuilder.Options);
        }
    }
}

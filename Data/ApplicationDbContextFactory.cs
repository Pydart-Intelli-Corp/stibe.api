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
            var connectionString = "Server=psrazuredb.mysql.database.azure.com;Port=3306;UserID=psrcloud;Password=Access@LRC2404;Database=psrtest;SslMode=Required;SslCa=D:\\MY PROJECTS\\Azure DB\\DigiCertGlobalRootCA.crt.pem";
            
            optionsBuilder.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
            
            return new ApplicationDbContext(optionsBuilder.Options);
        }
    }
}

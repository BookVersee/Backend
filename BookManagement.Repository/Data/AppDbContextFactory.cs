using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;


using BookStore.BE2.Infrastructure.Persistence;

namespace BookManagement.Repository.Data
{
    public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
            optionsBuilder.UseSqlServer("Server=localhost;Database=BookManagementDb;User Id=sa;Password=12345;TrustServerCertificate=True;");

            return new AppDbContext(optionsBuilder.Options);
        }
    }
}

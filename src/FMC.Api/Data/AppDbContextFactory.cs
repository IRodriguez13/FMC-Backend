using Microsoft.EntityFrameworkCore.Design;

namespace Fmc.Api.Data;

public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite("Data Source=fmc.db")
            .Options;
        return new AppDbContext(options);
    }
}

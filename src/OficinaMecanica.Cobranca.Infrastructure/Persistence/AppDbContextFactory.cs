using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace OficinaMecanica.Cobranca.Infrastructure.Persistence;

public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var connection = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? "Server=localhost;Port=3308;Database=cobranca;User=cobranca_app";
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseMySql(connection, new MySqlServerVersion(new Version(8, 4, 0))).Options;
        return new AppDbContext(options);
    }
}

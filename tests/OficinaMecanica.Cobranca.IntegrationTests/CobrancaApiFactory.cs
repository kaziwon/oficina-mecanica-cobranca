using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using MySqlConnector;
using OficinaMecanica.Cobranca.Infrastructure.Persistence;

namespace OficinaMecanica.Cobranca.IntegrationTests;

public class CobrancaApiFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection? _connection;
    private readonly string? _mysql;

    public CobrancaApiFactory()
    {
        var connection = Environment.GetEnvironmentVariable("MYSQL_TEST_CONNECTION_STRING");
        if (!string.IsNullOrWhiteSpace(connection))
        {
            var settings = new MySqlConnectionStringBuilder(connection)
            {
                Database = $"cobranca_test_{Guid.NewGuid():N}"
            };
            _mysql = settings.ConnectionString;
        }
        else
        {
            _connection = new SqliteConnection("Data Source=:memory:");
            _connection.Open();
        }
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                ["JwtSettings:SecretKey"] = "segredo-exclusivo-dos-testes-fase4-1234567890",
                ["AdminUserSettings:Username"] = "admin",
                ["AdminUserSettings:Password"] = "SenhaDosTestes@123",
                ["Database:ApplyMigrations"] = "false"
            }));
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<AppDbContext>>();
            services.RemoveAll<AppDbContext>();
            services.AddDbContext<AppDbContext>(options =>
            {
                if (_mysql is not null) options.UseMySql(_mysql, new MySqlServerVersion(new Version(8, 4, 0)));
                else options.UseSqlite(_connection!);
            });
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);
        using var scope = host.Services.CreateScope();
        var database = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        if (_mysql is not null) database.Database.Migrate();
        else database.Database.EnsureCreated();
        return host;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing && _mysql is not null)
        {
            using var scope = Services.CreateScope();
            scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.EnsureDeleted();
        }
        base.Dispose(disposing);
        if (disposing) _connection?.Dispose();
    }
}

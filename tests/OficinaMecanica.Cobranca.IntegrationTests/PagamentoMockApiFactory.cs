using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using OficinaMecanica.PagamentoMock;

namespace OficinaMecanica.Cobranca.IntegrationTests;

public class PagamentoMockApiFactory : WebApplicationFactory<MockProgram>
{
    public string DatabasePath { get; }
    private readonly string _resultado;
    private readonly bool _apagar;
    public string ConnectionString => "Data Source=" + DatabasePath + ";Default Timeout=5";
    public PagamentoMockApiFactory(string resultado = "approved", string? path = null, bool apagar = true)
    { _resultado = resultado; DatabasePath = path ?? Path.Combine(Path.GetTempPath(), "oficina-mock-" + Guid.NewGuid().ToString("N") + ".db"); _apagar = apagar; }
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?>
        { ["Mock:ConnectionString"] = ConnectionString, ["Mock:Resultado"] = _resultado }));
    }
    public int ContarPagamentos()
    {
        using var db = new SqliteConnection(ConnectionString); db.Open(); using var cmd = db.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM Payments"; return Convert.ToInt32(cmd.ExecuteScalar());
    }
    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing && _apagar)
        {
            using var db = new SqliteConnection(ConnectionString); SqliteConnection.ClearPool(db);
            foreach (var suffix in new[] { "", "-wal", "-shm" }) File.Delete(DatabasePath + suffix);
        }
    }
}

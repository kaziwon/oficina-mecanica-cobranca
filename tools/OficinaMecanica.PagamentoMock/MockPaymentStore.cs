using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Options;

namespace OficinaMecanica.PagamentoMock;

public class MockPaymentStore
{
    private readonly string _connection;
    public MockPaymentStore(IOptions<MockSettings> settings) { _connection = settings.Value.ConnectionString; }
    public void Initialize()
    {
        using var db = new SqliteConnection(_connection); db.Open();
        using var command = db.CreateCommand();
        command.CommandText = """
            PRAGMA journal_mode=WAL;
            CREATE TABLE IF NOT EXISTS Payments (
                IdempotencyKey TEXT PRIMARY KEY,
                ProviderId TEXT NOT NULL UNIQUE,
                AmountCents INTEGER NOT NULL,
                Status TEXT NOT NULL,
                CorrelationId TEXT NOT NULL);
            """;
        command.ExecuteNonQuery();
    }
    public PaymentRecord? Find(Guid id)
    {
        using var db = new SqliteConnection(_connection); db.Open();
        using var command = db.CreateCommand();
        command.CommandText = "SELECT ProviderId, AmountCents, Status, CorrelationId FROM Payments WHERE IdempotencyKey = $id";
        command.Parameters.AddWithValue("$id", id.ToString());
        using var reader = command.ExecuteReader();
        return !reader.Read() ? null : new PaymentRecord(reader.GetString(0), id, reader.GetInt64(1) / 100m, reader.GetString(2), reader.GetString(3));
    }
    public PaymentRecord GetOrCreate(Guid id, decimal amount, string status, string correlationId)
    {
        using (var db = new SqliteConnection(_connection))
        {
            db.Open(); using var command = db.CreateCommand();
            command.CommandText = "INSERT OR IGNORE INTO Payments (IdempotencyKey, ProviderId, AmountCents, Status, CorrelationId) VALUES ($key, $id, $amount, $status, $correlation)";
            command.Parameters.AddWithValue("$key", id.ToString());
            command.Parameters.AddWithValue("$id", "mock-" + id.ToString("N"));
            command.Parameters.AddWithValue("$amount", checked((long)(amount * 100)));
            command.Parameters.AddWithValue("$status", status);
            command.Parameters.AddWithValue("$correlation", correlationId);
            command.ExecuteNonQuery();
        }
        var saved = Find(id)!;
        if (saved.Amount != amount) throw new InvalidOperationException("Chave reutilizada com outro valor.");
        return saved;
    }
}

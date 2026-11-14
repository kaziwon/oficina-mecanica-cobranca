using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Options;
namespace OficinaMecanica.PagamentoMock;

public class MockRefundStore
{
    private readonly string _connection;
    public MockRefundStore(IOptions<MockSettings> settings) { _connection = settings.Value.ConnectionString; }
    public void Initialize()
    {
        using var db = new SqliteConnection(_connection); db.Open(); using var command = db.CreateCommand();
        command.CommandText = """
            CREATE TABLE IF NOT EXISTS Refunds (
                IdempotencyKey TEXT PRIMARY KEY, ProviderId TEXT NOT NULL UNIQUE,
                PaymentProviderId TEXT NOT NULL UNIQUE, AmountCents INTEGER NOT NULL, Status TEXT NOT NULL,
                CorrelationId TEXT NOT NULL);
            """;
        command.ExecuteNonQuery();
    }
    public RefundRecord? Find(Guid id)
    {
        using var db = new SqliteConnection(_connection); db.Open(); using var command = db.CreateCommand();
        command.CommandText = "SELECT ProviderId, PaymentProviderId, AmountCents, Status FROM Refunds WHERE IdempotencyKey=$key";
        command.Parameters.AddWithValue("$key", id.ToString()); using var reader = command.ExecuteReader();
        return reader.Read() ? new RefundRecord(reader.GetString(0), id, reader.GetString(1), reader.GetInt64(2) / 100m, reader.GetString(3)) : null;
    }
    public RefundRecord GetOrCreate(Guid id, string paymentId, decimal amount, string status, string correlation)
    {
        using (var db = new SqliteConnection(_connection))
        {
            db.Open(); using var command = db.CreateCommand();
            command.CommandText = """
                INSERT OR IGNORE INTO Refunds (IdempotencyKey, ProviderId, PaymentProviderId, AmountCents, Status, CorrelationId)
                SELECT $key,$provider,$payment,$amount,$status,$correlation FROM Payments
                WHERE ProviderId=$payment AND AmountCents=$amount AND Status='approved'
                """;
            command.Parameters.AddWithValue("$key", id.ToString()); command.Parameters.AddWithValue("$provider", "mock-refund-" + id.ToString("N"));
            command.Parameters.AddWithValue("$payment", paymentId); command.Parameters.AddWithValue("$amount", checked((long)(amount * 100)));
            command.Parameters.AddWithValue("$status", status); command.Parameters.AddWithValue("$correlation", correlation); command.ExecuteNonQuery();
        }
        var saved = Find(id);
        if (saved is null || saved.PaymentId != paymentId || saved.Amount != amount)
            throw new InvalidOperationException("Pagamento nao estornavel ou chave de estorno reutilizada.");
        return saved;
    }
}

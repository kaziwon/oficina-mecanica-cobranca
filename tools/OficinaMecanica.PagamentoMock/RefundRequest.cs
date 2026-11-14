using System.Text.Json.Serialization;
namespace OficinaMecanica.PagamentoMock;

public record RefundRequest(
    [property: JsonPropertyName("external_reference")] Guid ExternalReference,
    [property: JsonPropertyName("payment_id")] string PaymentId,
    [property: JsonPropertyName("transaction_amount")] decimal Amount,
    [property: JsonPropertyName("currency_id")] string Currency);

using System.Text.Json.Serialization;

namespace OficinaMecanica.PagamentoMock;

public record PaymentRequest(
    [property: JsonPropertyName("external_reference")] Guid ExternalReference,
    [property: JsonPropertyName("transaction_amount")] decimal Amount,
    [property: JsonPropertyName("currency_id")] string? Currency);

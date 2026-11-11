using System.Text.Json.Serialization;

namespace OficinaMecanica.PagamentoMock;

public record TokenRequest(
    [property: JsonPropertyName("grant_type")] string? GrantType,
    [property: JsonPropertyName("client_id")] string? ClientId,
    [property: JsonPropertyName("client_secret")] string? ClientSecret);

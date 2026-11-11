using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace OficinaMecanica.Cobranca.IntegrationTests;

public class PagamentoRespostaInvalidaHandler : HttpMessageHandler
{
    public string Modo { get; set; } = "status";
    public List<string> Caminhos { get; } = [];
    public string? Chave { get; private set; }
    public string? CorrelationId { get; private set; }
    public bool Autenticado { get; private set; }
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Caminhos.Add(request.RequestUri!.AbsolutePath);
        if (Caminhos.Last() == "/oauth/token")
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(new { access_token = "token-teste" }) };
        var body = await request.Content!.ReadFromJsonAsync<JsonElement>(cancellationToken);
        Chave = request.Headers.GetValues("X-Idempotency-Key").Single();
        CorrelationId = request.Headers.GetValues("X-Correlation-ID").Single();
        Autenticado = request.Headers.Authorization?.ToString() == "Bearer token-teste";
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new
            {
                id = "mock-id",
                status = Modo == "status" ? "pending" : "approved",
                external_reference = Modo == "referencia" ? Guid.NewGuid() : body.GetProperty("external_reference").GetGuid(),
                transaction_amount = body.GetProperty("transaction_amount").GetDecimal() + (Modo == "valor" ? 1m : 0m),
                currency_id = "BRL"
            })
        };
    }
}

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
namespace OficinaMecanica.Cobranca.IntegrationTests;

public class EstornoRespostaInvalidaHandler(string modo) : HttpMessageHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (request.RequestUri!.AbsolutePath == "/oauth/token")
            return new(HttpStatusCode.OK) { Content = JsonContent.Create(new { access_token = "token-teste" }) };
        var body = await request.Content!.ReadFromJsonAsync<JsonElement>(cancellationToken);
        return new(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new
            {
                id = "refund-123",
                status = modo == "status" ? "pending" : "refunded",
                external_reference = modo == "referencia" ? Guid.NewGuid() : body.GetProperty("external_reference").GetGuid(),
                payment_id = modo == "pagamento" ? "outro" : body.GetProperty("payment_id").GetString(),
                transaction_amount = body.GetProperty("transaction_amount").GetDecimal() + (modo == "valor" ? 1 : 0),
                currency_id = "BRL"
            })
        };
    }
}

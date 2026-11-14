using System.Net;
using System.Net.Http.Json;

namespace OficinaMecanica.Cobranca.IntegrationTests;

public class ConsultaFinanceiraRespostaHandler : HttpMessageHandler
{
    public string Modo { get; set; } = "approved";
    public List<string> Chamadas { get; } = [];
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var path = request.RequestUri!.AbsolutePath; Chamadas.Add(request.Method + " " + path);
        if (path == "/oauth/token")
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            { Content = JsonContent.Create(new { access_token = Modo == "token" ? "\n" : "token-consulta" }) });
        Assert.Equal(HttpMethod.Get, request.Method);
        Assert.Equal("Bearer token-consulta", request.Headers.Authorization!.ToString());
        Assert.Equal("consulta", request.Headers.GetValues("X-Correlation-ID").Single());
        Assert.False(request.Headers.Contains("X-Idempotency-Key")); Assert.Null(request.Content);
        if (Modo is "404" or "503")
            return Task.FromResult(new HttpResponseMessage((HttpStatusCode)int.Parse(Modo)));
        if (Modo == "json")
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{") });
        var body = new Dictionary<string, object?>
        {
            ["id"] = Modo == "id" ? "" : "provider-id",
            ["external_reference"] = Modo == "referencia" ? Guid.NewGuid() : Guid.Parse(path.Split('/').Last()),
            ["transaction_amount"] = Modo == "valor" ? 99m : 100m,
            ["currency_id"] = Modo == "moeda" ? "USD" : "BRL",
            ["payment_id"] = Modo == "pagamento" ? "outro" : "charge",
            ["status"] = Modo is "pending" or "rejected" ? Modo : path.StartsWith("/refunds/") ? "refunded" : "approved"
        };
        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(body) });
    }
}

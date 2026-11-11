using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;
using OficinaMecanica.Cobranca.Application.Exceptions;
using OficinaMecanica.Cobranca.Application.Inputs.Pagamentos;
using OficinaMecanica.Cobranca.Application.Outputs.Pagamentos;
using OficinaMecanica.Cobranca.Infrastructure.Settings;
using OficinaMecanica.Cobranca.InterfaceAdapters.DataSources;

namespace OficinaMecanica.Cobranca.Infrastructure.Sources;

public class HttpPagamentoMockSource : IPagamentoProvedorSource
{
    private readonly HttpClient _http;
    private readonly PagamentoMockSettings _settings;
    public HttpPagamentoMockSource(HttpClient http, IOptions<PagamentoMockSettings> settings)
    { _http = http; _settings = settings.Value; }

    public async Task<ResultadoPagamentoProvedor> Cobrar(SolicitacaoPagamentoProvedor solicitacao, CancellationToken cancellationToken)
    {
        try
        {
            using var tokenResponse = await _http.PostAsJsonAsync("oauth/token", new
            { grant_type = "client_credentials", client_id = _settings.ClientId, client_secret = _settings.ClientSecret }, cancellationToken);
            tokenResponse.EnsureSuccessStatusCode();
            using var tokenJson = await JsonDocument.ParseAsync(await tokenResponse.Content.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken);
            var token = tokenJson.RootElement.GetProperty("access_token").GetString();
            if (string.IsNullOrWhiteSpace(token) || token.Length > 2048 || token.Any(char.IsControl))
                throw new IntegracaoPagamentoException("Token do mock invalido.");
            using var request = new HttpRequestMessage(HttpMethod.Post, "payments");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            request.Headers.Add("X-Idempotency-Key", solicitacao.PagamentoId.ToString());
            request.Headers.Add("X-Correlation-ID", solicitacao.CorrelationId);
            request.Content = JsonContent.Create(new
            {
                external_reference = solicitacao.PagamentoId,
                transaction_amount = solicitacao.Valor,
                currency_id = solicitacao.Moeda
            });
            using var response = await _http.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();
            using var json = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken);
            var body = json.RootElement;
            var id = body.GetProperty("id").GetString(); var status = body.GetProperty("status").GetString();
            if (string.IsNullOrWhiteSpace(id) || id.Length > 100 || id.Any(char.IsControl)
                || status is not ("approved" or "rejected")
                || body.GetProperty("external_reference").GetGuid() != solicitacao.PagamentoId
                || body.GetProperty("transaction_amount").GetDecimal() != solicitacao.Valor
                || body.GetProperty("currency_id").GetString() != solicitacao.Moeda)
                throw new IntegracaoPagamentoException("Resposta do mock nao corresponde ao pagamento.");
            return new ResultadoPagamentoProvedor(id, status == "approved");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception exception) when (exception is HttpRequestException or JsonException or KeyNotFoundException
            or InvalidOperationException or FormatException or OperationCanceledException or IOException or OverflowException)
        { throw new IntegracaoPagamentoException("Nao foi possivel confirmar a resposta do mock.", exception); }
    }
}

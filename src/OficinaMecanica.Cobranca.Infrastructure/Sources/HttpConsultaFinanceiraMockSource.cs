using System.Net;
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

public class HttpConsultaFinanceiraMockSource : IConsultaFinanceiraSource
{
    private readonly HttpClient _http;
    private readonly PagamentoMockSettings _settings;
    public HttpConsultaFinanceiraMockSource(HttpClient http, IOptions<PagamentoMockSettings> settings)
    { _http = http; _settings = settings.Value; }

    public async Task<ResultadoPagamentoProvedor?> Consultar(SolicitacaoPagamentoProvedor pagamento, CancellationToken cancellationToken)
    {
        using var json = await Consultar("payments", pagamento.PagamentoId, pagamento.Valor, pagamento.Moeda,
            pagamento.CorrelationId, null, cancellationToken);
        if (json is null) return null;
        return new ResultadoPagamentoProvedor(json.RootElement.GetProperty("id").GetString()!,
            json.RootElement.GetProperty("status").GetString() == "approved");
    }

    public async Task<ResultadoEstornoProvedor?> Consultar(SolicitacaoEstornoProvedor estorno, CancellationToken cancellationToken)
    {
        using var json = await Consultar("refunds", estorno.EstornoId, estorno.Valor, estorno.Moeda,
            estorno.CorrelationId, estorno.PagamentoProvedorId, cancellationToken);
        if (json is null) return null;
        return new ResultadoEstornoProvedor(json.RootElement.GetProperty("id").GetString()!,
            json.RootElement.GetProperty("status").GetString() == "refunded");
    }

    private async Task<JsonDocument?> Consultar(string recurso, Guid id, decimal valor, string moeda,
        string correlationId, string? pagamentoProvedorId, CancellationToken cancellationToken)
    {
        try
        {
            if (!_settings.Enabled) throw new IntegracaoPagamentoException("Consulta ao provedor nao habilitada.");
            using var tokenResponse = await _http.PostAsJsonAsync("oauth/token", new
            { grant_type = "client_credentials", client_id = _settings.ClientId, client_secret = _settings.ClientSecret }, cancellationToken);
            tokenResponse.EnsureSuccessStatusCode();
            await tokenResponse.Content.LoadIntoBufferAsync(1048576, cancellationToken);
            using var tokenJson = await JsonDocument.ParseAsync(await tokenResponse.Content.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken);
            var token = tokenJson.RootElement.GetProperty("access_token").GetString();
            if (string.IsNullOrWhiteSpace(token) || token.Length > 2048 || token.Any(char.IsControl))
                throw new IntegracaoPagamentoException("Token do mock invalido.");
            using var request = new HttpRequestMessage(HttpMethod.Get, $"{recurso}/{id}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            request.Headers.Add("X-Correlation-ID", correlationId);
            using var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            if (response.StatusCode == HttpStatusCode.NotFound) return null;
            response.EnsureSuccessStatusCode();
            await response.Content.LoadIntoBufferAsync(1048576, cancellationToken);
            var json = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken);
            try
            {
                var body = json.RootElement;
                var provedorId = body.GetProperty("id").GetString();
                var status = body.GetProperty("status").GetString();
                if (string.IsNullOrWhiteSpace(provedorId) || provedorId.Length > 100 || provedorId.Any(char.IsControl)
                    || body.GetProperty("external_reference").GetGuid() != id
                    || body.GetProperty("transaction_amount").GetDecimal() != valor
                    || body.GetProperty("currency_id").GetString() != moeda
                    || (pagamentoProvedorId is null ? status is not ("approved" or "rejected")
                        : status is not ("refunded" or "rejected") || body.GetProperty("payment_id").GetString() != pagamentoProvedorId))
                    throw new IntegracaoPagamentoException("Consulta do mock nao corresponde a operacao original.");
                return json;
            }
            catch { json.Dispose(); throw; }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception exception) when (exception is HttpRequestException or JsonException or KeyNotFoundException
            or InvalidOperationException or FormatException or OperationCanceledException or IOException or OverflowException)
        { throw new IntegracaoPagamentoException("Nao foi possivel consultar o resultado do mock.", exception); }
    }
}

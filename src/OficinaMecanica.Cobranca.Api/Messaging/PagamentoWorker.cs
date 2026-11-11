using Microsoft.Extensions.Options;
using OficinaMecanica.Cobranca.Application.Gateways;
using OficinaMecanica.Cobranca.Application.UseCases.Pagamentos;
using OficinaMecanica.Cobranca.Infrastructure.Settings;

namespace OficinaMecanica.Cobranca.Messaging;

public class PagamentoWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopes;
    private readonly PagamentoMockSettings _settings;
    private readonly ILogger<PagamentoWorker> _logger;
    public PagamentoWorker(IServiceScopeFactory scopes, IOptions<PagamentoMockSettings> settings, ILogger<PagamentoWorker> logger)
    { _scopes = scopes; _settings = settings.Value; _logger = logger; }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_settings.Enabled) return;
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                List<Guid> pendentes;
                using (var scope = _scopes.CreateScope())
                    pendentes = scope.ServiceProvider.GetRequiredService<IPagamentoProcessamentoGateway>().ObterPendentes(DateTime.UtcNow, 10);
                foreach (var id in pendentes)
                {
                    try
                    {
                        using var scope = _scopes.CreateScope();
                        var pagamento = scope.ServiceProvider.GetRequiredService<IPagamentoProcessamentoGateway>().ObterPorId(id);
                        using var logScope = _logger.BeginScope(new Dictionary<string, object?>
                        { ["PagamentoId"] = id, ["SagaId"] = pagamento?.SagaId, ["CorrelationId"] = pagamento?.CorrelationId });
                        await scope.ServiceProvider.GetRequiredService<ProcessarPagamentoUseCase>().Executar(id,
                            _settings.MaxTentativas, TimeSpan.FromSeconds(_settings.IntervaloRetentativaSegundos), stoppingToken);
                        if (pagamento?.CodigoFalha is not null)
                            _logger.LogWarning("Pagamento {PagamentoId} em {Status}, codigo {CodigoFalha}, tentativa {Tentativas}",
                                id, pagamento.Status, pagamento.CodigoFalha, pagamento.Tentativas);
                        else _logger.LogInformation("Processamento do pagamento {PagamentoId} verificado", id);
                    }
                    catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { throw; }
                    catch (Exception exception) { _logger.LogError(exception, "Falha local ao processar pagamento {PagamentoId}; mesma chave sera reutilizada", id); }
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception exception) { _logger.LogError(exception, "Nao foi possivel consultar as pendencias de pagamento"); }
            try { await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
        }
    }
}

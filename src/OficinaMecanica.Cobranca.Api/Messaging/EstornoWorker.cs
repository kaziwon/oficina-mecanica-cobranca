using Microsoft.Extensions.Options;
using OficinaMecanica.Cobranca.Application.Gateways;
using OficinaMecanica.Cobranca.Application.UseCases.Pagamentos;
using OficinaMecanica.Cobranca.Infrastructure.Settings;

namespace OficinaMecanica.Cobranca.Messaging;

public class EstornoWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopes;
    private readonly PagamentoMockSettings _settings;
    private readonly ILogger<EstornoWorker> _logger;
    public EstornoWorker(IServiceScopeFactory scopes, IOptions<PagamentoMockSettings> settings, ILogger<EstornoWorker> logger)
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
                    pendentes = scope.ServiceProvider.GetRequiredService<IEstornoGateway>().ObterPendentes(DateTime.UtcNow, 10);
                foreach (var id in pendentes)
                {
                    try
                    {
                        using var scope = _scopes.CreateScope();
                        var estorno = scope.ServiceProvider.GetRequiredService<IEstornoGateway>().ObterPorId(id);
                        using var logScope = _logger.BeginScope(new Dictionary<string, object?>
                        { ["EstornoId"] = id, ["SagaId"] = estorno?.SagaId, ["CorrelationId"] = estorno?.CorrelationId });
                        await scope.ServiceProvider.GetRequiredService<ProcessarEstornoUseCase>().Executar(id,
                            _settings.MaxTentativas, TimeSpan.FromSeconds(_settings.IntervaloRetentativaSegundos), stoppingToken);
                        if (estorno?.CodigoFalha is not null)
                            _logger.LogWarning("Estorno {EstornoId} em {Status}, codigo {CodigoFalha}, tentativa {Tentativas}",
                                id, estorno.Status, estorno.CodigoFalha, estorno.Tentativas);
                        else _logger.LogInformation("Processamento do estorno {EstornoId} verificado", id);
                    }
                    catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { throw; }
                    catch (Exception exception) { _logger.LogError(exception, "Falha local ao processar estorno {EstornoId}; mesma chave sera reutilizada", id); }
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception exception) { _logger.LogError(exception, "Nao foi possivel consultar as pendencias de estorno"); }
            try { await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
        }
    }
}

using OficinaMecanica.Cobranca.Application.Gateways;
using OficinaMecanica.Cobranca.Application.Exceptions;
using OficinaMecanica.Cobranca.Application.Inputs.Pagamentos;
using OficinaMecanica.Cobranca.Application.Mappers.Pagamentos;

namespace OficinaMecanica.Cobranca.Application.UseCases.Pagamentos;

public class ProcessarPagamentoUseCase
{
    private readonly IPagamentoProcessamentoGateway _pagamentos;
    private readonly IPagamentoProvedorGateway _provedor;
    public ProcessarPagamentoUseCase(IPagamentoProcessamentoGateway pagamentos, IPagamentoProvedorGateway provedor)
    { _pagamentos = pagamentos; _provedor = provedor; }

    public async Task Executar(Guid id, int maxTentativas, TimeSpan intervalo, CancellationToken cancellationToken)
    {
        var pagamento = _pagamentos.ObterPorId(id);
        if (pagamento is null || !pagamento.PodeProcessar(DateTime.UtcNow)) return;
        var versao = pagamento.Versao;
        Outputs.Pagamentos.ResultadoPagamentoProvedor? resultado = null;
        try
        {
            resultado = await _provedor.Cobrar(new SolicitacaoPagamentoProvedor(
                pagamento.Id, pagamento.Valor, pagamento.Moeda, pagamento.CorrelationId!), cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (IntegracaoPagamentoException)
        {
            // Timeout nao comprova recusa: o provedor pode ter efetivado a cobranca.
            pagamento.RegistrarFalhaTecnica(DateTime.UtcNow, maxTentativas, intervalo);
        }
        if (resultado is not null)
            pagamento.RegistrarResultado(resultado.Aprovado, resultado.ProvedorId, DateTime.UtcNow);
        // Falha de persistencia nao e capturada como falha do provedor. Uma retentativa usa o mesmo ID.
        _pagamentos.Salvar(pagamento, versao, ResultadoPagamentoMensagemMapper.Mapear(pagamento));
    }
}

using OficinaMecanica.Cobranca.Application.Exceptions;
using OficinaMecanica.Cobranca.Application.Gateways;
using OficinaMecanica.Cobranca.Application.Inputs.Pagamentos;
using OficinaMecanica.Cobranca.Application.Mappers.Pagamentos;
using OficinaMecanica.Cobranca.Application.Outputs.Pagamentos;
namespace OficinaMecanica.Cobranca.Application.UseCases.Pagamentos;

public class ProcessarEstornoUseCase
{
    private readonly IEstornoGateway _estornos;
    private readonly IEstornoProvedorGateway _provedor;
    public ProcessarEstornoUseCase(IEstornoGateway estornos, IEstornoProvedorGateway provedor) { _estornos = estornos; _provedor = provedor; }
    public async Task Executar(Guid id, int maxTentativas, TimeSpan intervalo, CancellationToken cancellationToken)
    {
        var estorno = _estornos.ObterPorId(id);
        if (estorno is null || !estorno.PodeProcessar(DateTime.UtcNow)) return;
        var versao = estorno.Versao; ResultadoEstornoProvedor? resultado = null;
        try
        {
            resultado = await _provedor.Estornar(new SolicitacaoEstornoProvedor(estorno.Id, estorno.PagamentoProvedorId,
            estorno.Valor, estorno.Moeda, estorno.CorrelationId), cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (IntegracaoPagamentoException) { estorno.RegistrarFalhaTecnica(DateTime.UtcNow, maxTentativas, intervalo); }
        if (resultado is not null) estorno.RegistrarResultado(resultado.Confirmado, resultado.ProvedorId, DateTime.UtcNow);
        _estornos.Salvar(estorno, versao, ResultadoEstornoMensagemMapper.Mapear(estorno));
    }
}

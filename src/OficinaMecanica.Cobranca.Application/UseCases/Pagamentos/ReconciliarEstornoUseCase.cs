using OficinaMecanica.Cobranca.Application.Gateways;
using OficinaMecanica.Cobranca.Application.Inputs.Pagamentos;
using OficinaMecanica.Cobranca.Application.Mappers.Pagamentos;
using OficinaMecanica.Cobranca.Application.Outputs.Pagamentos;
using OficinaMecanica.Cobranca.Domain.Entities;
using OficinaMecanica.Cobranca.Domain.Exceptions;

namespace OficinaMecanica.Cobranca.Application.UseCases.Pagamentos;

public class ReconciliarEstornoUseCase
{
    private readonly IEstornoGateway _estornos;
    private readonly IConsultaFinanceiraGateway _consulta;
    public ReconciliarEstornoUseCase(IEstornoGateway estornos, IConsultaFinanceiraGateway consulta)
    { _estornos = estornos; _consulta = consulta; }

    public async Task<EstornoOutput?> Executar(Guid pagamentoId, CancellationToken cancellationToken)
    {
        var estorno = _estornos.ObterPorPagamento(pagamentoId);
        if (estorno is null) return null;
        if (estorno.Status is StatusEstorno.Confirmado or StatusEstorno.Recusado)
            return EstornoOutputMapper.Mapear(estorno);
        if (estorno.Status != StatusEstorno.FalhaRequerIntervencao)
            throw new RegraNegocioException("Aguarde o processamento do estorno antes de reconciliar.");
        var versao = estorno.Versao;
        var resultado = await _consulta.Consultar(new SolicitacaoEstornoProvedor(
            estorno.Id, estorno.PagamentoProvedorId, estorno.Valor, estorno.Moeda, estorno.CorrelationId), cancellationToken);
        estorno.RegistrarConsulta(resultado?.Confirmado, resultado?.ProvedorId, DateTime.UtcNow);
        _estornos.Salvar(estorno, versao, resultado is null ? null : ResultadoEstornoMensagemMapper.Mapear(estorno));
        return EstornoOutputMapper.Mapear(estorno);
    }
}

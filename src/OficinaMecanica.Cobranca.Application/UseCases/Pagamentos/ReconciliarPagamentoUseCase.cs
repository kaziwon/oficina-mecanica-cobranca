using OficinaMecanica.Cobranca.Application.Gateways;
using OficinaMecanica.Cobranca.Application.Inputs.Pagamentos;
using OficinaMecanica.Cobranca.Application.Mappers.Pagamentos;
using OficinaMecanica.Cobranca.Application.Outputs.Pagamentos;
using OficinaMecanica.Cobranca.Domain.Entities;
using OficinaMecanica.Cobranca.Domain.Exceptions;

namespace OficinaMecanica.Cobranca.Application.UseCases.Pagamentos;

public class ReconciliarPagamentoUseCase
{
    private readonly IPagamentoProcessamentoGateway _pagamentos;
    private readonly IConsultaFinanceiraGateway _consulta;
    public ReconciliarPagamentoUseCase(IPagamentoProcessamentoGateway pagamentos, IConsultaFinanceiraGateway consulta)
    { _pagamentos = pagamentos; _consulta = consulta; }

    public async Task<PagamentoOutput?> Executar(Guid id, CancellationToken cancellationToken)
    {
        var pagamento = _pagamentos.ObterPorId(id);
        if (pagamento is null) return null;
        if (pagamento.Status is StatusPagamento.Confirmado or StatusPagamento.Recusado)
            return PagamentoOutputMapper.Mapear(pagamento);
        if (pagamento.Status != StatusPagamento.FalhaRequerIntervencao)
            throw new RegraNegocioException("Aguarde o processamento do pagamento antes de reconciliar.");
        var versao = pagamento.Versao;
        var resultado = await _consulta.Consultar(new SolicitacaoPagamentoProvedor(
            pagamento.Id, pagamento.Valor, pagamento.Moeda, pagamento.CorrelationId!), cancellationToken);
        pagamento.RegistrarConsulta(resultado?.Aprovado, resultado?.ProvedorId, DateTime.UtcNow);
        _pagamentos.Salvar(pagamento, versao, resultado is null ? null : ResultadoPagamentoMensagemMapper.Mapear(pagamento));
        return PagamentoOutputMapper.Mapear(pagamento);
    }
}

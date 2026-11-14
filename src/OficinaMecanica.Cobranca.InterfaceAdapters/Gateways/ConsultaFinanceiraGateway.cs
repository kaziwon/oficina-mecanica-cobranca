using OficinaMecanica.Cobranca.Application.Gateways;
using OficinaMecanica.Cobranca.Application.Inputs.Pagamentos;
using OficinaMecanica.Cobranca.Application.Outputs.Pagamentos;
using OficinaMecanica.Cobranca.InterfaceAdapters.DataSources;

namespace OficinaMecanica.Cobranca.InterfaceAdapters.Gateways;

public class ConsultaFinanceiraGateway : IConsultaFinanceiraGateway
{
    private readonly IConsultaFinanceiraSource _source;
    public ConsultaFinanceiraGateway(IConsultaFinanceiraSource source) { _source = source; }
    public Task<ResultadoPagamentoProvedor?> Consultar(SolicitacaoPagamentoProvedor pagamento, CancellationToken cancellationToken)
        => _source.Consultar(pagamento, cancellationToken);
    public Task<ResultadoEstornoProvedor?> Consultar(SolicitacaoEstornoProvedor estorno, CancellationToken cancellationToken)
        => _source.Consultar(estorno, cancellationToken);
}

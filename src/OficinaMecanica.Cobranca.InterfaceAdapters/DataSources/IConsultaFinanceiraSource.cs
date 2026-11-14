using OficinaMecanica.Cobranca.Application.Inputs.Pagamentos;
using OficinaMecanica.Cobranca.Application.Outputs.Pagamentos;

namespace OficinaMecanica.Cobranca.InterfaceAdapters.DataSources;

public interface IConsultaFinanceiraSource
{
    Task<ResultadoPagamentoProvedor?> Consultar(SolicitacaoPagamentoProvedor pagamento, CancellationToken cancellationToken);
    Task<ResultadoEstornoProvedor?> Consultar(SolicitacaoEstornoProvedor estorno, CancellationToken cancellationToken);
}

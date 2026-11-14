using OficinaMecanica.Cobranca.Application.Inputs.Pagamentos;
using OficinaMecanica.Cobranca.Application.Outputs.Pagamentos;

namespace OficinaMecanica.Cobranca.Application.Gateways;

public interface IConsultaFinanceiraGateway
{
    Task<ResultadoPagamentoProvedor?> Consultar(SolicitacaoPagamentoProvedor pagamento, CancellationToken cancellationToken);
    Task<ResultadoEstornoProvedor?> Consultar(SolicitacaoEstornoProvedor estorno, CancellationToken cancellationToken);
}

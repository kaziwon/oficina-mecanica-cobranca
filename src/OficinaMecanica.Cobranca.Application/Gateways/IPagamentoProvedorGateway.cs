using OficinaMecanica.Cobranca.Application.Inputs.Pagamentos;
using OficinaMecanica.Cobranca.Application.Outputs.Pagamentos;

namespace OficinaMecanica.Cobranca.Application.Gateways;

public interface IPagamentoProvedorGateway
{
    Task<ResultadoPagamentoProvedor> Cobrar(SolicitacaoPagamentoProvedor solicitacao, CancellationToken cancellationToken);
}

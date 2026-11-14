using OficinaMecanica.Cobranca.Application.Inputs.Pagamentos;
using OficinaMecanica.Cobranca.Application.Outputs.Pagamentos;
namespace OficinaMecanica.Cobranca.Application.Gateways;

public interface IEstornoProvedorGateway
{
    Task<ResultadoEstornoProvedor> Estornar(SolicitacaoEstornoProvedor solicitacao, CancellationToken cancellationToken);
}

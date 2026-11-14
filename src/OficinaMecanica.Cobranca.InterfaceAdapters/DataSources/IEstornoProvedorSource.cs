using OficinaMecanica.Cobranca.Application.Inputs.Pagamentos;
using OficinaMecanica.Cobranca.Application.Outputs.Pagamentos;
namespace OficinaMecanica.Cobranca.InterfaceAdapters.DataSources;

public interface IEstornoProvedorSource
{
    Task<ResultadoEstornoProvedor> Estornar(SolicitacaoEstornoProvedor solicitacao, CancellationToken cancellationToken);
}

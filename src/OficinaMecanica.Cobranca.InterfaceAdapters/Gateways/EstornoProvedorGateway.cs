using OficinaMecanica.Cobranca.Application.Gateways;
using OficinaMecanica.Cobranca.Application.Inputs.Pagamentos;
using OficinaMecanica.Cobranca.Application.Outputs.Pagamentos;
using OficinaMecanica.Cobranca.InterfaceAdapters.DataSources;
namespace OficinaMecanica.Cobranca.InterfaceAdapters.Gateways;

public class EstornoProvedorGateway : IEstornoProvedorGateway
{
    private readonly IEstornoProvedorSource _source;
    public EstornoProvedorGateway(IEstornoProvedorSource source) { _source = source; }
    public Task<ResultadoEstornoProvedor> Estornar(SolicitacaoEstornoProvedor solicitacao, CancellationToken cancellationToken)
        => _source.Estornar(solicitacao, cancellationToken);
}

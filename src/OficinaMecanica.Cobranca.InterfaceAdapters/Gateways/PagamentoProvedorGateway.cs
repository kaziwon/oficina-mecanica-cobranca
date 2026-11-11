using OficinaMecanica.Cobranca.Application.Gateways;
using OficinaMecanica.Cobranca.Application.Inputs.Pagamentos;
using OficinaMecanica.Cobranca.Application.Outputs.Pagamentos;
using OficinaMecanica.Cobranca.InterfaceAdapters.DataSources;

namespace OficinaMecanica.Cobranca.InterfaceAdapters.Gateways;

public class PagamentoProvedorGateway : IPagamentoProvedorGateway
{
    private readonly IPagamentoProvedorSource _source;
    public PagamentoProvedorGateway(IPagamentoProvedorSource source) { _source = source; }
    public Task<ResultadoPagamentoProvedor> Cobrar(SolicitacaoPagamentoProvedor solicitacao, CancellationToken cancellationToken)
        => _source.Cobrar(solicitacao, cancellationToken);
}

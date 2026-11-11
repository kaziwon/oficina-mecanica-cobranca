using OficinaMecanica.Cobranca.Application.Inputs.Pagamentos;
using OficinaMecanica.Cobranca.Application.Outputs.Pagamentos;

namespace OficinaMecanica.Cobranca.InterfaceAdapters.DataSources;

public interface IPagamentoProvedorSource
{
    Task<ResultadoPagamentoProvedor> Cobrar(SolicitacaoPagamentoProvedor solicitacao, CancellationToken cancellationToken);
}

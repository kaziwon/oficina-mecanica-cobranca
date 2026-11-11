using OficinaMecanica.Cobranca.Application.Exceptions;
using OficinaMecanica.Cobranca.Application.Gateways;
using OficinaMecanica.Cobranca.Application.Inputs.Pagamentos;
using OficinaMecanica.Cobranca.Application.Outputs.Pagamentos;

namespace OficinaMecanica.Cobranca.UnitTests.Fakes;

public class PagamentoProvedorGatewayFake : IPagamentoProvedorGateway
{
    public bool Falhar { get; set; }
    public bool Aprovado { get; set; } = true;
    public List<Guid> Chaves { get; } = [];
    public Task<ResultadoPagamentoProvedor> Cobrar(SolicitacaoPagamentoProvedor solicitacao, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Chaves.Add(solicitacao.PagamentoId);
        if (Falhar) throw new IntegracaoPagamentoException("Mock indisponivel");
        return Task.FromResult(new ResultadoPagamentoProvedor("mock-" + solicitacao.PagamentoId.ToString("N"), Aprovado));
    }
}

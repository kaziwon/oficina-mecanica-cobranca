using OficinaMecanica.Cobranca.Application.Gateways;
using OficinaMecanica.Cobranca.Application.Messaging;
using OficinaMecanica.Cobranca.Domain.Entities;

namespace OficinaMecanica.Cobranca.IntegrationTests;

public class PagamentoProcessamentoComFalhaGateway : IPagamentoProcessamentoGateway
{
    private readonly IPagamentoProcessamentoGateway _real;
    public PagamentoProcessamentoComFalhaGateway(IPagamentoProcessamentoGateway real) { _real = real; }
    public List<Guid> ObterPendentes(DateTime agora, int limite) => _real.ObterPendentes(agora, limite);
    public Pagamento? ObterPorId(Guid id) => _real.ObterPorId(id);
    public void Salvar(Pagamento pagamento, long versaoEsperada, MensagemIntegracao? evento)
        => throw new InvalidOperationException("Falha local simulada depois da resposta HTTP.");
}

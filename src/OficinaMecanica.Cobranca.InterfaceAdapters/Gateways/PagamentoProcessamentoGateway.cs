using OficinaMecanica.Cobranca.Application.Gateways;
using OficinaMecanica.Cobranca.Application.Messaging;
using OficinaMecanica.Cobranca.Domain.Entities;
using OficinaMecanica.Cobranca.InterfaceAdapters.DataSources;

namespace OficinaMecanica.Cobranca.InterfaceAdapters.Gateways;

public class PagamentoProcessamentoGateway : IPagamentoProcessamentoGateway
{
    private readonly IPagamentoProcessamentoDataSource _source;
    public PagamentoProcessamentoGateway(IPagamentoProcessamentoDataSource source) { _source = source; }
    public List<Guid> ObterPendentes(DateTime agora, int limite) => _source.ObterPendentes(agora, limite);
    public Pagamento? ObterPorId(Guid id) => _source.ObterPorId(id);
    public void Salvar(Pagamento pagamento, long versaoEsperada, MensagemIntegracao? evento) => _source.Salvar(pagamento, versaoEsperada, evento);
}

using OficinaMecanica.Cobranca.Application.Gateways;
using OficinaMecanica.Cobranca.Application.Messaging;
using OficinaMecanica.Cobranca.Domain.Entities;
using OficinaMecanica.Cobranca.InterfaceAdapters.DataSources;

namespace OficinaMecanica.Cobranca.InterfaceAdapters.Gateways;

public class PagamentoIntegracaoGateway : IPagamentoIntegracaoGateway
{
    private readonly IPagamentoIntegracaoDataSource _source;
    public PagamentoIntegracaoGateway(IPagamentoIntegracaoDataSource source) { _source = source; }
    public bool FoiProcessada(MensagemIntegracao pedido) => _source.FoiProcessada(pedido);
    public Orcamento? ObterOrcamento(Guid id) => _source.ObterOrcamento(id);
    public MensagemIntegracao? ObterEventoAprovacao(Guid id) => _source.ObterEventoAprovacao(id);
    public void Registrar(MensagemIntegracao pedido, Orcamento orcamento) => _source.Registrar(pedido, orcamento);
}

using OficinaMecanica.Cobranca.Application.Gateways;
using OficinaMecanica.Cobranca.Application.Messaging;
using OficinaMecanica.Cobranca.Domain.Entities;
using OficinaMecanica.Cobranca.InterfaceAdapters.DataSources;

namespace OficinaMecanica.Cobranca.InterfaceAdapters.Gateways;

public class OrcamentoIntegracaoGateway : IOrcamentoIntegracaoGateway
{
    private readonly IOrcamentoIntegracaoDataSource _source;
    public OrcamentoIntegracaoGateway(IOrcamentoIntegracaoDataSource source) { _source = source; }
    public bool FoiProcessada(MensagemIntegracao pedido) => _source.FoiProcessada(pedido);
    public bool ExisteOrcamento(Guid os, Guid execucao) => _source.ExisteOrcamento(os, execucao);
    public void Registrar(MensagemIntegracao pedido, Orcamento? orcamento, MensagemIntegracao resposta)
        => _source.Registrar(pedido, orcamento, resposta);
}

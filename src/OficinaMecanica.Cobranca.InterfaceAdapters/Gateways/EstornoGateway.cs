using OficinaMecanica.Cobranca.Application.Gateways;
using OficinaMecanica.Cobranca.Application.Messaging;
using OficinaMecanica.Cobranca.Domain.Entities;
using OficinaMecanica.Cobranca.InterfaceAdapters.DataSources;
namespace OficinaMecanica.Cobranca.InterfaceAdapters.Gateways;

public class EstornoGateway : IEstornoGateway
{
    private readonly IEstornoDataSource _source;
    public EstornoGateway(IEstornoDataSource source) { _source = source; }
    public bool FoiProcessada(MensagemIntegracao mensagem) => _source.FoiProcessada(mensagem);
    public Pagamento? ObterPagamento(Guid id) => _source.ObterPagamento(id);
    public Estorno? ObterPorPagamento(Guid id) => _source.ObterPorPagamento(id);
    public Estorno? ObterPorId(Guid id) => _source.ObterPorId(id);
    public List<Guid> ObterPendentes(DateTime agora, int limite) => _source.ObterPendentes(agora, limite);
    public void Registrar(MensagemIntegracao mensagem, Estorno estorno) => _source.Registrar(mensagem, estorno);
    public void Salvar(Estorno estorno, long versaoEsperada, MensagemIntegracao? evento) => _source.Salvar(estorno, versaoEsperada, evento);
}

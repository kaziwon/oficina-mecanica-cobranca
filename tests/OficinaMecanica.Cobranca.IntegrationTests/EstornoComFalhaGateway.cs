using OficinaMecanica.Cobranca.Application.Gateways;
using OficinaMecanica.Cobranca.Application.Messaging;
using OficinaMecanica.Cobranca.Domain.Entities;

namespace OficinaMecanica.Cobranca.IntegrationTests;

public class EstornoComFalhaGateway(IEstornoGateway inner) : IEstornoGateway
{
    public bool FoiProcessada(MensagemIntegracao mensagem) => inner.FoiProcessada(mensagem);
    public Pagamento? ObterPagamento(Guid id) => inner.ObterPagamento(id);
    public Estorno? ObterPorPagamento(Guid id) => inner.ObterPorPagamento(id);
    public Estorno? ObterPorId(Guid id) => inner.ObterPorId(id);
    public List<Guid> ObterPendentes(DateTime agora, int limite) => inner.ObterPendentes(agora, limite);
    public void Registrar(MensagemIntegracao mensagem, Estorno estorno) => inner.Registrar(mensagem, estorno);
    public void Salvar(Estorno estorno, long versaoEsperada, MensagemIntegracao? evento) => throw new InvalidOperationException("Falha de gravacao simulada");
}

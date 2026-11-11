using OficinaMecanica.Cobranca.Application.Gateways;
using OficinaMecanica.Cobranca.Application.Messaging;
using OficinaMecanica.Cobranca.Domain.Entities;

namespace OficinaMecanica.Cobranca.UnitTests.Fakes;

public class PagamentoProcessamentoGatewayFake : IPagamentoProcessamentoGateway
{
    public Pagamento Pagamento { get; set; } = null!;
    public int Gravacoes { get; private set; }
    public List<MensagemIntegracao> Eventos { get; } = [];
    public List<Guid> ObterPendentes(DateTime agora, int limite) => Pagamento.PodeProcessar(agora) ? [Pagamento.Id] : [];
    public Pagamento? ObterPorId(Guid id) => Pagamento.Id == id ? Pagamento : null;
    public void Salvar(Pagamento pagamento, long versaoEsperada, MensagemIntegracao? evento)
    { Gravacoes++; if (evento is not null) Eventos.Add(evento); }
}

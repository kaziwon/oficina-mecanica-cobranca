using OficinaMecanica.Cobranca.Application.Gateways;
using OficinaMecanica.Cobranca.Application.Messaging;
using OficinaMecanica.Cobranca.InterfaceAdapters.DataSources;

namespace OficinaMecanica.Cobranca.InterfaceAdapters.Gateways;

public class OutboxGateway : IOutboxGateway
{
    private readonly IOutboxDataSource _source;
    public OutboxGateway(IOutboxDataSource source) { _source = source; }
    public List<MensagemIntegracao> ObterPendentes(int limite) => _source.ObterPendentes(limite);
    public void ConfirmarPublicacao(Guid messageId) => _source.ConfirmarPublicacao(messageId);
}

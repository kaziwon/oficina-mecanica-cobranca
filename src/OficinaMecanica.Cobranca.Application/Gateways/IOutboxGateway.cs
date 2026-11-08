using OficinaMecanica.Cobranca.Application.Messaging;

namespace OficinaMecanica.Cobranca.Application.Gateways;

public interface IOutboxGateway
{
    List<MensagemIntegracao> ObterPendentes(int limite);
    void ConfirmarPublicacao(Guid messageId);
}

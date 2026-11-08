using OficinaMecanica.Cobranca.Application.Messaging;

namespace OficinaMecanica.Cobranca.InterfaceAdapters.DataSources;

public interface IOutboxDataSource
{
    List<MensagemIntegracao> ObterPendentes(int limite);
    void ConfirmarPublicacao(Guid messageId);
}

using OficinaMecanica.Cobranca.Application.Messaging;
using OficinaMecanica.Cobranca.Domain.Entities;

namespace OficinaMecanica.Cobranca.InterfaceAdapters.DataSources;

public interface IPagamentoProcessamentoDataSource
{
    List<Guid> ObterPendentes(DateTime agora, int limite);
    Pagamento? ObterPorId(Guid id);
    void Salvar(Pagamento pagamento, long versaoEsperada, MensagemIntegracao? evento);
}

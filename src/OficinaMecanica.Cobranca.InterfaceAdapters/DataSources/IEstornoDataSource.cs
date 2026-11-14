using OficinaMecanica.Cobranca.Application.Messaging;
using OficinaMecanica.Cobranca.Domain.Entities;
namespace OficinaMecanica.Cobranca.InterfaceAdapters.DataSources;

public interface IEstornoDataSource
{
    bool FoiProcessada(MensagemIntegracao mensagem);
    Pagamento? ObterPagamento(Guid id);
    Estorno? ObterPorPagamento(Guid id);
    Estorno? ObterPorId(Guid id);
    List<Guid> ObterPendentes(DateTime agora, int limite);
    void Registrar(MensagemIntegracao mensagem, Estorno estorno);
    void Salvar(Estorno estorno, long versaoEsperada, MensagemIntegracao? evento);
}

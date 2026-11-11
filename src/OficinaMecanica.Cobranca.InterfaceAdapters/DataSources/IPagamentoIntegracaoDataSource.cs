using OficinaMecanica.Cobranca.Application.Messaging;
using OficinaMecanica.Cobranca.Domain.Entities;

namespace OficinaMecanica.Cobranca.InterfaceAdapters.DataSources;

public interface IPagamentoIntegracaoDataSource
{
    bool FoiProcessada(MensagemIntegracao pedido);
    Orcamento? ObterOrcamento(Guid id);
    MensagemIntegracao? ObterEventoAprovacao(Guid id);
    void Registrar(MensagemIntegracao pedido, Orcamento orcamento);
}

using OficinaMecanica.Cobranca.Domain.Entities;

namespace OficinaMecanica.Cobranca.InterfaceAdapters.DataSources;

public interface IOrcamentoDataSource
{
    Orcamento? ObterPorId(Guid id);
    Orcamento? ObterPorOrdemServicoId(Guid ordemServicoId);
    List<Orcamento> ObterTodos(StatusOrcamento? status, Guid? ordemServicoId);
    void Inserir(Orcamento orcamento);
    void Salvar(Orcamento orcamento, Application.Messaging.MensagemIntegracao? evento = null);
}

using OficinaMecanica.Cobranca.Domain.Entities;

namespace OficinaMecanica.Cobranca.Application.Gateways;

public interface IOrcamentoGateway
{
    Orcamento? ObterPorId(Guid id);
    Orcamento? ObterPorOrdemServicoId(Guid ordemServicoId);
    List<Orcamento> ObterTodos(StatusOrcamento? status, Guid? ordemServicoId);
    void Inserir(Orcamento orcamento);
    void Salvar(Orcamento orcamento, Messaging.MensagemIntegracao? evento = null);
}

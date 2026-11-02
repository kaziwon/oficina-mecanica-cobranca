using OficinaMecanica.Cobranca.Application.Gateways;
using OficinaMecanica.Cobranca.Domain.Entities;

namespace OficinaMecanica.Cobranca.UnitTests.Fakes;

public class OrcamentoGatewayFake : IOrcamentoGateway, IPagamentoGateway
{
    public List<Orcamento> Orcamentos { get; } = [];
    public int Gravacoes { get; private set; }
    public Orcamento? ObterPorId(Guid id) => Orcamentos.SingleOrDefault(item => item.Id == id);
    public Orcamento? ObterPorOrdemServicoId(Guid ordemServicoId)
        => Orcamentos.SingleOrDefault(item => item.OrdemServicoId == ordemServicoId);
    public List<Orcamento> ObterTodos(StatusOrcamento? status, Guid? ordemServicoId) => Orcamentos
        .Where(item => !status.HasValue || item.Status == status)
        .Where(item => !ordemServicoId.HasValue || item.OrdemServicoId == ordemServicoId).ToList();
    public void Inserir(Orcamento orcamento) { Orcamentos.Add(orcamento); Gravacoes++; }
    public void Salvar(Orcamento orcamento) { Gravacoes++; }
    Pagamento? IPagamentoGateway.ObterPorId(Guid id) => Orcamentos.Select(item => item.Pagamento).SingleOrDefault(item => item?.Id == id);
}

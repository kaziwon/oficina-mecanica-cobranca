using OficinaMecanica.Cobranca.Application.Gateways;
using OficinaMecanica.Cobranca.Domain.Entities;
using OficinaMecanica.Cobranca.InterfaceAdapters.DataSources;

namespace OficinaMecanica.Cobranca.InterfaceAdapters.Gateways;

public class OrcamentoGateway : IOrcamentoGateway
{
    private readonly IOrcamentoDataSource _source;
    public OrcamentoGateway(IOrcamentoDataSource source) { _source = source; }
    public Orcamento? ObterPorId(Guid id) => _source.ObterPorId(id);
    public Orcamento? ObterPorOrdemServicoId(Guid ordemServicoId) => _source.ObterPorOrdemServicoId(ordemServicoId);
    public List<Orcamento> ObterTodos(StatusOrcamento? status, Guid? ordemServicoId) => _source.ObterTodos(status, ordemServicoId);
    public void Inserir(Orcamento orcamento) => _source.Inserir(orcamento);
    public void Salvar(Orcamento orcamento) => _source.Salvar(orcamento);
}

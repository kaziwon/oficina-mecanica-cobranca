using Microsoft.EntityFrameworkCore;
using MySqlConnector;
using OficinaMecanica.Cobranca.Application.Exceptions;
using OficinaMecanica.Cobranca.Domain.Entities;
using OficinaMecanica.Cobranca.Infrastructure.Persistence;
using OficinaMecanica.Cobranca.InterfaceAdapters.DataSources;

namespace OficinaMecanica.Cobranca.Infrastructure.Sources;

public class EfOrcamentoDataSource : IOrcamentoDataSource
{
    private readonly AppDbContext _database;
    public EfOrcamentoDataSource(AppDbContext database) { _database = database; }

    private IQueryable<Orcamento> ConsultaCompleta() => _database.Orcamentos
        .Include(item => item.Itens).Include(item => item.Historico).Include(item => item.Pagamento).AsSplitQuery();

    public Orcamento? ObterPorId(Guid id) => ConsultaCompleta().SingleOrDefault(item => item.Id == id);

    public Orcamento? ObterPorOrdemServicoId(Guid ordemServicoId)
        => ConsultaCompleta().SingleOrDefault(item => item.OrdemServicoId == ordemServicoId);

    public List<Orcamento> ObterTodos(StatusOrcamento? status, Guid? ordemServicoId)
    {
        var query = ConsultaCompleta().AsNoTracking();
        if (status.HasValue) query = query.Where(item => item.Status == status.Value);
        if (ordemServicoId.HasValue) query = query.Where(item => item.OrdemServicoId == ordemServicoId.Value);
        return query.OrderBy(item => item.CriadoEm).ThenBy(item => item.Id).ToList();
    }

    public void Inserir(Orcamento orcamento)
    {
        _database.Orcamentos.Add(orcamento);
        Salvar(orcamento);
    }

    public void Salvar(Orcamento orcamento)
    {
        try
        {
            _database.SaveChanges();
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConflitoException("O orcamento foi alterado por outra requisicao. Consulte novamente.");
        }
        catch (DbUpdateException exception) when (exception.InnerException is MySqlException { Number: 1062 })
        {
            throw new ConflitoException("A OS, execucao ou pagamento ja foi registrado.");
        }
    }
}

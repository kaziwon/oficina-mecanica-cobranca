using Microsoft.EntityFrameworkCore;
using OficinaMecanica.Cobranca.Domain.Entities;
using OficinaMecanica.Cobranca.Infrastructure.Persistence;
using OficinaMecanica.Cobranca.InterfaceAdapters.DataSources;

namespace OficinaMecanica.Cobranca.Infrastructure.Sources;

public class EfPagamentoDataSource : IPagamentoDataSource
{
    private readonly AppDbContext _database;
    public EfPagamentoDataSource(AppDbContext database) { _database = database; }
    public Pagamento? ObterPorId(Guid id) => _database.Pagamentos.AsNoTracking().SingleOrDefault(item => item.Id == id);
}

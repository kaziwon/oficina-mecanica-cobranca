using Microsoft.EntityFrameworkCore;
using OficinaMecanica.Cobranca.Application.Messaging;
using OficinaMecanica.Cobranca.Domain.Entities;
using OficinaMecanica.Cobranca.Infrastructure.Messaging;
using OficinaMecanica.Cobranca.Infrastructure.Persistence;
using OficinaMecanica.Cobranca.Infrastructure.Persistence.Models;
using OficinaMecanica.Cobranca.InterfaceAdapters.DataSources;

namespace OficinaMecanica.Cobranca.Infrastructure.Sources;

public class EfPagamentoProcessamentoDataSource : IPagamentoProcessamentoDataSource
{
    private readonly AppDbContext _database;
    public EfPagamentoProcessamentoDataSource(AppDbContext database) { _database = database; }
    public List<Guid> ObterPendentes(DateTime agora, int limite) => _database.Pagamentos.AsNoTracking()
        .Where(item => item.Status == StatusPagamento.Pendente && item.PedidoPagamentoId != null
            && (item.ProximaTentativaEm == null || item.ProximaTentativaEm <= agora))
        .OrderBy(item => item.CriadoEm).ThenBy(item => item.Id).Take(limite).Select(item => item.Id).ToList();
    public Pagamento? ObterPorId(Guid id) => _database.Pagamentos.SingleOrDefault(item => item.Id == id);
    public void Salvar(Pagamento pagamento, long versaoEsperada, MensagemIntegracao? evento)
    {
        _database.Entry(pagamento).Property(item => item.Versao).OriginalValue = versaoEsperada;
        if (evento is not null)
            _database.Outbox.Add(new MensagemOutbox
            { Id = evento.MessageId, SagaId = evento.SagaId, Body = MensagemSerializer.Serializar(evento), CriadaEm = evento.OccurredAt });
        _database.SaveChanges();
    }
}

using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using OficinaMecanica.Cobranca.Application.Messaging;
using OficinaMecanica.Cobranca.Domain.Entities;
using OficinaMecanica.Cobranca.Infrastructure.Messaging;
using OficinaMecanica.Cobranca.Infrastructure.Persistence;
using OficinaMecanica.Cobranca.Infrastructure.Persistence.Models;
using OficinaMecanica.Cobranca.InterfaceAdapters.DataSources;
namespace OficinaMecanica.Cobranca.Infrastructure.Sources;

public class EfEstornoDataSource : IEstornoDataSource
{
    private readonly AppDbContext _database;
    public EfEstornoDataSource(AppDbContext database) { _database = database; }
    public bool FoiProcessada(MensagemIntegracao mensagem) => new EfOrcamentoIntegracaoDataSource(_database).FoiProcessada(mensagem);
    public Pagamento? ObterPagamento(Guid id) => _database.Pagamentos.SingleOrDefault(item => item.Id == id);
    public Estorno? ObterPorPagamento(Guid id) => _database.Estornos.SingleOrDefault(item => item.PagamentoId == id);
    public Estorno? ObterPorId(Guid id) => _database.Estornos.SingleOrDefault(item => item.Id == id);
    public List<Guid> ObterPendentes(DateTime agora, int limite) => _database.Estornos.AsNoTracking()
        .Where(item => item.Status == StatusEstorno.Pendente && (item.ProximaTentativaEm == null || item.ProximaTentativaEm <= agora))
        .OrderBy(item => item.CriadoEm).ThenBy(item => item.Id).Take(limite).Select(item => item.Id).ToList();
    public void Registrar(MensagemIntegracao mensagem, Estorno estorno)
    {
        _database.Estornos.Add(estorno);
        _database.Inbox.Add(new MensagemInbox
        {
            Id = mensagem.MessageId,
            SagaId = mensagem.SagaId,
            ProcessadaEm = DateTime.UtcNow,
            Hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(MensagemSerializer.Serializar(mensagem))))
        });
        _database.SaveChanges();
    }
    public void Salvar(Estorno estorno, long versaoEsperada, MensagemIntegracao? evento)
    {
        _database.Entry(estorno).Property(item => item.Versao).OriginalValue = versaoEsperada;
        if (evento is not null) _database.Outbox.Add(new MensagemOutbox
        { Id = evento.MessageId, SagaId = evento.SagaId, Body = MensagemSerializer.Serializar(evento), CriadaEm = evento.OccurredAt });
        _database.SaveChanges();
    }
}

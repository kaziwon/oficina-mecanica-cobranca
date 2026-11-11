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

public class EfPagamentoIntegracaoDataSource : IPagamentoIntegracaoDataSource
{
    private readonly AppDbContext _database;
    public EfPagamentoIntegracaoDataSource(AppDbContext database) { _database = database; }
    public bool FoiProcessada(MensagemIntegracao pedido) => new EfOrcamentoIntegracaoDataSource(_database).FoiProcessada(pedido);
    public Orcamento? ObterOrcamento(Guid id) => _database.Orcamentos.Include(item => item.Pagamento).SingleOrDefault(item => item.Id == id);
    public MensagemIntegracao? ObterEventoAprovacao(Guid id)
    {
        var evento = _database.Outbox.AsNoTracking().SingleOrDefault(item => item.Id == id);
        return evento is null ? null : MensagemSerializer.Desserializar(evento.Body);
    }
    public void Registrar(MensagemIntegracao pedido, Orcamento orcamento)
    {
        _database.Inbox.Add(new MensagemInbox
        {
            Id = pedido.MessageId,
            SagaId = pedido.SagaId,
            ProcessadaEm = DateTime.UtcNow,
            Hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(MensagemSerializer.Serializar(pedido))))
        });
        _database.SaveChanges();
    }
}

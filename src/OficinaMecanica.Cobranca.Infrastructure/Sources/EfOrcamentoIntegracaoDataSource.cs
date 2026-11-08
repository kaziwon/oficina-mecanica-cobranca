using System.Security.Cryptography;
using System.Text;
using OficinaMecanica.Cobranca.Application.Exceptions;
using OficinaMecanica.Cobranca.Application.Messaging;
using OficinaMecanica.Cobranca.Domain.Entities;
using OficinaMecanica.Cobranca.Infrastructure.Messaging;
using OficinaMecanica.Cobranca.Infrastructure.Persistence;
using OficinaMecanica.Cobranca.Infrastructure.Persistence.Models;
using OficinaMecanica.Cobranca.InterfaceAdapters.DataSources;

namespace OficinaMecanica.Cobranca.Infrastructure.Sources;

public class EfOrcamentoIntegracaoDataSource : IOrcamentoIntegracaoDataSource
{
    private readonly AppDbContext _database;
    public EfOrcamentoIntegracaoDataSource(AppDbContext database) { _database = database; }
    private static string Hash(MensagemIntegracao pedido)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(MensagemSerializer.Serializar(pedido))));

    public bool FoiProcessada(MensagemIntegracao pedido)
    {
        var recebida = _database.Inbox.SingleOrDefault(item => item.Id == pedido.MessageId);
        if (recebida is null) return false;
        if (recebida.SagaId != pedido.SagaId || recebida.Hash != Hash(pedido))
            throw new ValidacaoException("MessageId reutilizado com conteudo diferente.");
        return true;
    }

    public bool ExisteOrcamento(Guid os, Guid execucao)
        => _database.Orcamentos.Any(item => item.OrdemServicoId == os || item.ExecucaoId == execucao);

    public void Registrar(MensagemIntegracao pedido, Orcamento? orcamento, MensagemIntegracao resposta)
    {
        if (orcamento is not null) _database.Orcamentos.Add(orcamento);
        _database.Inbox.Add(new MensagemInbox
        { Id = pedido.MessageId, SagaId = pedido.SagaId, Hash = Hash(pedido), ProcessadaEm = DateTime.UtcNow });
        _database.Outbox.Add(new MensagemOutbox
        { Id = resposta.MessageId, SagaId = resposta.SagaId, Body = MensagemSerializer.Serializar(resposta), CriadaEm = resposta.OccurredAt });
        // Orcamento, deduplicacao e resposta pertencem a mesma transacao local.
        _database.SaveChanges();
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using MySqlConnector;
using OficinaMecanica.Cobranca.Application.Messaging;
using OficinaMecanica.Cobranca.Infrastructure.Messaging;
using OficinaMecanica.Cobranca.Infrastructure.Persistence;
using OficinaMecanica.Cobranca.Infrastructure.Persistence.Models;

namespace OficinaMecanica.Cobranca.IntegrationTests;

public class CorrelacaoMigrationTests
{
    [MySqlFact]
    public void Migration_RecuperaCorrelacaoDoOrcamentoJaExistente()
    {
        var settings = new MySqlConnectionStringBuilder(Environment.GetEnvironmentVariable("MYSQL_TEST_CONNECTION_STRING")!)
        { Database = CobrancaApiFactory.CriarNomeBancoTeste() };
        using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseMySql(settings.ConnectionString, new MySqlServerVersion(new Version(8, 4, 0))).Options);
        try
        {
            db.GetService<IMigrator>().Migrate("20261108212920_DiagnosticoOrcamento");
            var id = Guid.NewGuid(); var os = Guid.NewGuid(); var execucao = Guid.NewGuid();
            var saga = Guid.NewGuid(); var pedido = Guid.NewGuid(); var agora = DateTime.UtcNow;
            db.Database.ExecuteSqlInterpolated($"""
                INSERT INTO Orcamentos (Id, OrdemServicoId, ExecucaoId, Diagnostico, Status, ValorTotal, CriadoEm, Versao, SagaId, PedidoGeracaoId)
                VALUES ({id.ToString()}, {os.ToString()}, {execucao.ToString()}, {"Revisao"}, {"AguardandoAprovacao"}, {100m}, {agora}, {1L}, {saga.ToString()}, {pedido.ToString()})
                """);
            var evento = new MensagemIntegracao
            {
                MessageId = Guid.NewGuid(),
                Type = "OrcamentoGerado",
                SagaId = saga,
                CausationId = pedido,
                OccurredAt = agora,
                CorrelationId = "correlacao-legada",
                Payload = new DadosMensagem { OrdemServicoId = os, ExecucaoId = execucao, OrcamentoId = id, ValorTotal = 100m }
            };
            db.Outbox.Add(new MensagemOutbox { Id = evento.MessageId, SagaId = saga, Body = MensagemSerializer.Serializar(evento), CriadaEm = agora });
            db.SaveChanges();
            db.Database.Migrate();
            Assert.Equal("correlacao-legada", db.Orcamentos.Single().CorrelationId);
        }
        finally { db.Database.EnsureDeleted(); }
    }
}

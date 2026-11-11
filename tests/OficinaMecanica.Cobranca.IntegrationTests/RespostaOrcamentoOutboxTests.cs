using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OficinaMecanica.Cobranca.Application.Exceptions;
using OficinaMecanica.Cobranca.Application.Mappers.Orcamentos;
using OficinaMecanica.Cobranca.Application.Messaging;
using OficinaMecanica.Cobranca.Domain.Entities;
using OficinaMecanica.Cobranca.Infrastructure.Persistence;
using OficinaMecanica.Cobranca.Infrastructure.Sources;
using OficinaMecanica.Cobranca.InterfaceAdapters.Controllers;

namespace OficinaMecanica.Cobranca.IntegrationTests;

public class RespostaOrcamentoOutboxTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task RespostaHttp_GravaEstadoEEventoUmaVez(bool aprovado)
    {
        using var factory = new CobrancaApiFactory(); using var client = factory.CreateClient();
        await ApiTestHelper.Autenticar(client);
        var id = Criar(factory);
        for (var i = 0; i < 2; i++)
        {
            var resposta = aprovado ? await client.PostAsync($"/api/orcamentos/{id}/aprovar", null)
                : await client.PostAsJsonAsync($"/api/orcamentos/{id}/recusar", new { motivo = "Valor alto" });
            resposta.EnsureSuccessStatusCode();
        }
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Equal(2, db.Outbox.Count()); Assert.Equal(2, db.HistoricoOrcamento.Count());
        var evento = new EfOutboxDataSource(db).ObterPendentes(10).Single(item => item.Type != "OrcamentoGerado");
        Assert.Equal(aprovado ? "OrcamentoAprovado" : "OrcamentoRecusado", evento.Type);
        Assert.Equal("correlacao-teste", evento.CorrelationId);
        Assert.Equal(DateTimeKind.Utc, evento.Payload.OrcamentoCriadoEm!.Value.Kind);
        Assert.Equal(id, evento.Payload.OrcamentoId); Assert.Empty(db.Pagamentos);
        Assert.Equal(aprovado ? StatusOrcamento.Aprovado : StatusOrcamento.Recusado, db.Orcamentos.Single().Status);
    }

    [Fact]
    public void RespostasConcorrentes_NaoGravamEventoContraditorio()
    {
        using var factory = new CobrancaApiFactory(); using var client = factory.CreateClient();
        var id = Criar(factory);
        using var a = factory.Services.CreateScope(); using var b = factory.Services.CreateScope();
        var dbA = a.ServiceProvider.GetRequiredService<AppDbContext>(); var dbB = b.ServiceProvider.GetRequiredService<AppDbContext>();
        var sourceA = new EfOrcamentoDataSource(dbA); var sourceB = new EfOrcamentoDataSource(dbB);
        var primeiro = sourceA.ObterPorId(id)!; var segundo = sourceB.ObterPorId(id)!;
        primeiro.Aprovar(); segundo.Recusar("Valor alto");
        sourceA.Salvar(primeiro, RespostaOrcamentoMensagemMapper.Mapear(primeiro));
        Assert.Throws<ConflitoException>(() => sourceB.Salvar(segundo, RespostaOrcamentoMensagemMapper.Mapear(segundo)));
        using var verificar = factory.Services.CreateScope(); var db = verificar.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Equal(StatusOrcamento.Aprovado, db.Orcamentos.Single().Status);
        Assert.Equal(2, db.Outbox.Count()); Assert.Equal(2, db.HistoricoOrcamento.Count());
        Assert.DoesNotContain(new EfOutboxDataSource(db).ObterPendentes(10), item => item.Type == "OrcamentoRecusado");
    }

    [Fact]
    public void FalhaAoGravarEvento_ReverteRespostaEHistorico()
    {
        using var factory = new CobrancaApiFactory(); using var client = factory.CreateClient();
        var id = Criar(factory);
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var source = new EfOrcamentoDataSource(db); var orcamento = source.ObterPorId(id)!;
            orcamento.Aprovar(); var evento = RespostaOrcamentoMensagemMapper.Mapear(orcamento)!;
            evento.MessageId = db.Outbox.AsNoTracking().Single().Id;
            var erro = Record.Exception(() => source.Salvar(orcamento, evento));
            Assert.True(erro is DbUpdateException or ConflitoException);
        }
        using var verificar = factory.Services.CreateScope(); var banco = verificar.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Equal(StatusOrcamento.AguardandoAprovacao, banco.Orcamentos.Single().Status);
        Assert.Single(banco.Outbox); Assert.Single(banco.HistoricoOrcamento);
    }

    private static Guid Criar(CobrancaApiFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var mensagem = new MensagemIntegracao
        {
            MessageId = Guid.NewGuid(),
            Type = "GerarOrcamento",
            SagaId = Guid.NewGuid(),
            CausationId = Guid.NewGuid(),
            OccurredAt = DateTime.UtcNow,
            CorrelationId = "correlacao-teste",
            Payload = new DadosMensagem
            {
                OrdemServicoId = Guid.NewGuid(),
                ExecucaoId = Guid.NewGuid(),
                Diagnostico = "Revisao",
                Itens = [new ItemMensagem { Tipo = "Servico", ReferenciaId = Guid.NewGuid(), Nome = "Revisao", Quantidade = 1, PrecoUnitario = 100m }]
            }
        };
        scope.ServiceProvider.GetRequiredService<IntegracaoCleanController>().Processar(mensagem);
        return scope.ServiceProvider.GetRequiredService<AppDbContext>().Orcamentos.Single().Id;
    }
}

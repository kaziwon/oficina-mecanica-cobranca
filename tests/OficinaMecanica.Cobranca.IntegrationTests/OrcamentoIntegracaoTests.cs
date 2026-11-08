using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OficinaMecanica.Cobranca.Application.Exceptions;
using OficinaMecanica.Cobranca.Application.Messaging;
using OficinaMecanica.Cobranca.Domain.Entities;
using OficinaMecanica.Cobranca.Infrastructure.Messaging;
using OficinaMecanica.Cobranca.Infrastructure.Persistence;
using OficinaMecanica.Cobranca.Infrastructure.Persistence.Models;
using OficinaMecanica.Cobranca.Infrastructure.Sources;
using OficinaMecanica.Cobranca.InterfaceAdapters.Controllers;

namespace OficinaMecanica.Cobranca.IntegrationTests;

public class OrcamentoIntegracaoTests
{
    [Fact]
    public void PedidoRepetido_CriaUmOrcamentoComSnapshotEUmaResposta()
    {
        using var factory = new CobrancaApiFactory();
        using var client = factory.CreateClient();
        var pedido = Pedido();
        Processar(factory, pedido); Processar(factory, pedido);
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var orcamento = db.Orcamentos.Include(item => item.Itens).Include(item => item.Historico).Single();
        Assert.Equal(200m, orcamento.ValorTotal);
        Assert.Equal(pedido.SagaId, orcamento.SagaId);
        Assert.Equal(pedido.MessageId, orcamento.PedidoGeracaoId);
        Assert.Equal(StatusOrcamento.AguardandoAprovacao, orcamento.Status);
        Assert.Equal(2, orcamento.Itens.Count);
        Assert.Single(orcamento.Historico);
        Assert.Single(db.Inbox);
        var outbox = new EfOutboxDataSource(db);
        var resposta = Assert.Single(outbox.ObterPendentes(10));
        Assert.Equal("OrcamentoGerado", resposta.Type);
        Assert.Equal(pedido.MessageId, resposta.CausationId);
        Assert.Equal(orcamento.Id, resposta.Payload.OrcamentoId);
        Assert.Equal(orcamento.ValorTotal, resposta.Payload.ValorTotal);
        outbox.ConfirmarPublicacao(resposta.MessageId);
        Processar(factory, pedido);
        Assert.Empty(outbox.ObterPendentes(10));
        Assert.Single(db.Outbox);
    }

    [Theory]
    [InlineData("vazio")]
    [InlineData("preco")]
    [InlineData("tipo")]
    [InlineData("duplicado")]
    public void RegraDeNegocioInvalida_GravaRecusaSemOrcamento(string falha)
    {
        using var factory = new CobrancaApiFactory();
        using var client = factory.CreateClient();
        var pedido = Pedido();
        if (falha == "vazio") pedido.Payload.Itens = [];
        if (falha == "preco") pedido.Payload.Itens![0].PrecoUnitario = -1;
        if (falha == "tipo") pedido.Payload.Itens![0].Tipo = "Desconhecido";
        if (falha == "duplicado") pedido.Payload.Itens!.Add(pedido.Payload.Itens[0]);
        Processar(factory, pedido); Processar(factory, pedido);
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Empty(db.Orcamentos); Assert.Empty(db.ItensOrcamento);
        Assert.Single(db.Inbox);
        var resposta = Assert.Single(new EfOutboxDataSource(db).ObterPendentes(10));
        Assert.Equal("GeracaoOrcamentoRecusada", resposta.Type);
        Assert.Equal("DadosInvalidos", resposta.Payload.Codigo);
        Assert.Null(resposta.Payload.OrcamentoId);
        Assert.Null(resposta.Payload.ValorTotal);
    }

    [Fact]
    public void OutroPedidoParaMesmaOS_NaoAdotaOrcamentoNemRecalculaPreco()
    {
        using var factory = new CobrancaApiFactory();
        using var client = factory.CreateClient();
        var pedido = Pedido();
        Processar(factory, pedido);
        pedido.MessageId = Guid.NewGuid(); pedido.SagaId = Guid.NewGuid();
        pedido.Payload.Itens![0].PrecoUnitario = 900m;
        Processar(factory, pedido);
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Equal(200m, Assert.Single(db.Orcamentos).ValorTotal);
        var recusada = db.Outbox.AsEnumerable().Select(item => MensagemSerializer.Desserializar(item.Body))
            .Single(item => item.Type == "GeracaoOrcamentoRecusada");
        Assert.Equal("ConflitoDeVinculo", recusada.Payload.Codigo);
    }

    [Fact]
    public void MesmoMessageIdComDadosAlterados_EhRejeitado()
    {
        using var factory = new CobrancaApiFactory();
        using var client = factory.CreateClient();
        var pedido = Pedido(); Processar(factory, pedido);
        pedido.Payload.Itens![0].PrecoUnitario = 900m;
        Assert.Throws<ValidacaoException>(() => Processar(factory, pedido));
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Single(db.Inbox); Assert.Single(db.Outbox);
        Assert.Equal(200m, db.Orcamentos.Single().ValorTotal);
    }

    [Fact]
    public void ErroDePersistencia_ReverteOrcamentoItensEOutbox()
    {
        using var factory = new CobrancaApiFactory();
        using var client = factory.CreateClient();
        var pedido = Pedido();
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Inbox.Add(new MensagemInbox { Id = pedido.MessageId, SagaId = pedido.SagaId, Hash = "preexistente" });
            db.SaveChanges();
        }
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var orcamento = Orcamento.Criar(pedido.Payload.OrdemServicoId, pedido.Payload.ExecucaoId!.Value,
                "Diagnostico", [new ItemOrcamento(TipoItemOrcamento.Servico, Guid.NewGuid(), "Revisao", 1, 10m)]);
            var resposta = Pedido(); resposta.Type = "OrcamentoGerado";
            Assert.Throws<DbUpdateException>(() => new EfOrcamentoIntegracaoDataSource(db).Registrar(pedido, orcamento, resposta));
        }
        using var verificar = factory.Services.CreateScope();
        var banco = verificar.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Empty(banco.Orcamentos); Assert.Empty(banco.ItensOrcamento);
        Assert.Empty(banco.Outbox); Assert.Single(banco.Inbox);
    }

    [Fact]
    public void ContratoInvalido_NaoGravaRespostaDeNegocio()
    {
        using var factory = new CobrancaApiFactory();
        using var client = factory.CreateClient();
        var pedido = Pedido(); pedido.CausationId = null;
        Assert.Throws<ValidacaoException>(() => Processar(factory, pedido));
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Empty(db.Inbox); Assert.Empty(db.Outbox);
    }

    private static MensagemIntegracao Pedido() => new()
    {
        MessageId = Guid.NewGuid(),
        Type = "GerarOrcamento",
        SagaId = Guid.NewGuid(),
        CausationId = Guid.NewGuid(),
        OccurredAt = DateTime.UtcNow,
        CorrelationId = "diagnostico-teste",
        Payload = new DadosMensagem
        {
            OrdemServicoId = Guid.NewGuid(),
            ExecucaoId = Guid.NewGuid(),
            Diagnostico = "Revisao dos freios",
            Itens = [new ItemMensagem { Tipo = "Servico", ReferenciaId = Guid.NewGuid(), Nome = "Revisao", Quantidade = 1, PrecoUnitario = 100m },
                new ItemMensagem { Tipo = "PecaInsumo", ReferenciaId = Guid.NewGuid(), Nome = "Pastilha", Quantidade = 2, PrecoUnitario = 50m }]
        }
    };

    private static void Processar(CobrancaApiFactory factory, MensagemIntegracao mensagem)
    {
        using var scope = factory.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<IntegracaoCleanController>().Processar(mensagem);
    }
}

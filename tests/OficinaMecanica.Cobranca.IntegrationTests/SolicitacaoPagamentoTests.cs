using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OficinaMecanica.Cobranca.Application.Exceptions;
using OficinaMecanica.Cobranca.Application.UseCases.Pagamentos;
using OficinaMecanica.Cobranca.Domain.Entities;
using OficinaMecanica.Cobranca.Infrastructure.Persistence;
using OficinaMecanica.Cobranca.Infrastructure.Sources;

namespace OficinaMecanica.Cobranca.IntegrationTests;

public class SolicitacaoPagamentoTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void PedidoRepetido_GravaUmaPendenciaDuravelEReaproveitaRegistroManual(bool manual)
    {
        using var factory = new CobrancaApiFactory(); using var client = factory.CreateClient(); var pedido = PagamentoTestHelper.CriarPedido(factory);
        Guid? pagamentoManual = null;
        if (manual)
        {
            using var scope = factory.Services.CreateScope();
            pagamentoManual = scope.ServiceProvider.GetRequiredService<RegistrarPagamentoPendenteUseCase>().Executar(pedido.Payload.OrcamentoId!.Value)!.Id;
        }
        PagamentoTestHelper.Processar(factory, pedido); PagamentoTestHelper.Processar(factory, pedido);
        using var verificar = factory.Services.CreateScope(); var db = verificar.ServiceProvider.GetRequiredService<AppDbContext>();
        var pagamento = Assert.Single(db.Pagamentos);
        Assert.Equal(pedido.MessageId, pagamento.PedidoPagamentoId); Assert.Equal(pedido.SagaId, pagamento.SagaId);
        Assert.Equal(100m, pagamento.Valor); Assert.Equal(StatusPagamento.Pendente, pagamento.Status);
        if (manual) Assert.Equal(pagamentoManual, pagamento.Id);
        Assert.Equal(2, db.Inbox.Count()); Assert.Equal(2, db.Outbox.Count());
    }

    [Theory]
    [InlineData("valor")]
    [InlineData("causa")]
    [InlineData("correlacao")]
    [InlineData("moeda")]
    public void PedidoDivergente_NaoCriaPagamento(string erro)
    {
        using var factory = new CobrancaApiFactory(); using var client = factory.CreateClient(); var pedido = PagamentoTestHelper.CriarPedido(factory);
        if (erro == "valor") pedido.Payload.ValorTotal = 101m;
        if (erro == "causa") pedido.CausationId = Guid.NewGuid();
        if (erro == "correlacao") pedido.CorrelationId = "outra";
        if (erro == "moeda") pedido.Payload.Moeda = "USD";
        Assert.Throws<ValidacaoException>(() => PagamentoTestHelper.Processar(factory, pedido));
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Empty(db.Pagamentos); Assert.Single(db.Inbox);
    }

    [Fact]
    public void MessageIdAlterado_NaoPodeSerReutilizado()
    {
        using var factory = new CobrancaApiFactory(); using var client = factory.CreateClient(); var pedido = PagamentoTestHelper.CriarPedido(factory);
        PagamentoTestHelper.Processar(factory, pedido); pedido.Payload.ValorTotal = 101m;
        Assert.Throws<ValidacaoException>(() => PagamentoTestHelper.Processar(factory, pedido));
    }

    [Fact]
    public void FalhaDeInbox_ReverteCriacaoDoPagamento()
    {
        using var factory = new CobrancaApiFactory(); using var client = factory.CreateClient(); var pedido = PagamentoTestHelper.CriarPedido(factory);
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>(); var source = new EfPagamentoIntegracaoDataSource(db);
            var orcamento = source.ObterOrcamento(pedido.Payload.OrcamentoId!.Value)!;
            orcamento.RegistrarPagamentoPendente(); orcamento.Pagamento!.VincularSaga(pedido.SagaId, orcamento.ExecucaoId, pedido.MessageId, pedido.CorrelationId);
            pedido.MessageId = db.Inbox.AsNoTracking().Single().Id;
            Assert.Throws<DbUpdateException>(() => source.Registrar(pedido, orcamento));
        }
        using var verificar = factory.Services.CreateScope(); var banco = verificar.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Empty(banco.Pagamentos); Assert.Single(banco.Inbox);
    }
}

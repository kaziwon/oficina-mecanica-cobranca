using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OficinaMecanica.Cobranca.Application.Gateways;
using OficinaMecanica.Cobranca.Application.Inputs.Pagamentos;
using OficinaMecanica.Cobranca.Application.Mappers.Pagamentos;
using OficinaMecanica.Cobranca.Application.UseCases.Pagamentos;
using OficinaMecanica.Cobranca.Domain.Entities;
using OficinaMecanica.Cobranca.Infrastructure.Persistence;
using OficinaMecanica.Cobranca.Infrastructure.Sources;
using OficinaMecanica.Cobranca.InterfaceAdapters.Gateways;

namespace OficinaMecanica.Cobranca.IntegrationTests;

public class ProcessamentoPagamentoTests
{
    [Theory]
    [InlineData("approved", StatusPagamento.Confirmado, "PagamentoConfirmado")]
    [InlineData("rejected", StatusPagamento.Recusado, "PagamentoRecusado")]
    [InlineData("unavailable", StatusPagamento.FalhaRequerIntervencao, "FalhaProcessamentoPagamento")]
    public async Task ResultadoHttp_GravaPagamentoEOutboxAtomicamente(string modo, StatusPagamento esperado, string tipo)
    {
        using var factory = new CobrancaApiFactory(); using var client = factory.CreateClient();
        using var mock = new PagamentoMockApiFactory(modo); using var http = mock.CreateClient();
        var pedido = PagamentoTestHelper.CriarPedido(factory); PagamentoTestHelper.Processar(factory, pedido);
        Guid pagamentoId;
        using (var scope = factory.Services.CreateScope())
        {
            pagamentoId = scope.ServiceProvider.GetRequiredService<AppDbContext>().Pagamentos.Single().Id;
            var useCase = new ProcessarPagamentoUseCase(scope.ServiceProvider.GetRequiredService<IPagamentoProcessamentoGateway>(),
                new PagamentoProvedorGateway(new HttpPagamentoMockSource(http, Options.Create(PagamentoMockHttpTests.Settings()))));
            await useCase.Executar(pagamentoId, 1, TimeSpan.FromSeconds(1), CancellationToken.None);
            await useCase.Executar(pagamentoId, 1, TimeSpan.FromSeconds(1), CancellationToken.None);
        }
        using var verificar = factory.Services.CreateScope(); var db = verificar.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Equal(esperado, db.Pagamentos.Single().Status); Assert.Equal(1, db.Pagamentos.Single().Tentativas);
        var mensagem = new EfOutboxDataSource(db).ObterPendentes(10).Single(item => item.Type == tipo);
        Assert.Equal(pedido.MessageId, mensagem.CausationId); Assert.Equal(pagamentoId, mensagem.Payload.PagamentoId);
        Assert.Equal(3, db.Outbox.Count()); Assert.Equal(modo == "unavailable" ? 0 : 1, mock.ContarPagamentos());
    }

    [Fact]
    public async Task RespostaRecebidaMasGravacaoFalhou_RepetirNaoDuplicaCobranca()
    {
        using var factory = new CobrancaApiFactory(); using var client = factory.CreateClient();
        using var mock = new PagamentoMockApiFactory(); using var http = mock.CreateClient();
        var pedido = PagamentoTestHelper.CriarPedido(factory); PagamentoTestHelper.Processar(factory, pedido);
        var provedor = new PagamentoProvedorGateway(new HttpPagamentoMockSource(http, Options.Create(PagamentoMockHttpTests.Settings())));
        Guid id;
        using (var scope = factory.Services.CreateScope())
        {
            id = scope.ServiceProvider.GetRequiredService<AppDbContext>().Pagamentos.Single().Id;
            var fake = new PagamentoProcessamentoComFalhaGateway(scope.ServiceProvider.GetRequiredService<IPagamentoProcessamentoGateway>());
            await Assert.ThrowsAsync<InvalidOperationException>(() => new ProcessarPagamentoUseCase(fake, provedor)
                .Executar(id, 3, TimeSpan.FromSeconds(1), CancellationToken.None));
        }
        using (var scope = factory.Services.CreateScope())
        {
            Assert.Equal(StatusPagamento.Pendente, scope.ServiceProvider.GetRequiredService<AppDbContext>().Pagamentos.Single().Status);
            await new ProcessarPagamentoUseCase(scope.ServiceProvider.GetRequiredService<IPagamentoProcessamentoGateway>(), provedor)
                .Executar(id, 3, TimeSpan.FromSeconds(1), CancellationToken.None);
        }
        using var verificar = factory.Services.CreateScope(); var db = verificar.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Equal(StatusPagamento.Confirmado, db.Pagamentos.Single().Status); Assert.Equal(1, mock.ContarPagamentos());
        Assert.Equal(3, db.Outbox.Count());
    }

    [Fact]
    public async Task ProcessamentosConcorrentes_NaoDuplicamEventoDeResultado()
    {
        using var factory = new CobrancaApiFactory(); using var client = factory.CreateClient();
        using var mock = new PagamentoMockApiFactory(); using var http = mock.CreateClient();
        var pedido = PagamentoTestHelper.CriarPedido(factory); PagamentoTestHelper.Processar(factory, pedido);
        using var a = factory.Services.CreateScope(); using var b = factory.Services.CreateScope();
        var dbA = a.ServiceProvider.GetRequiredService<AppDbContext>(); var dbB = b.ServiceProvider.GetRequiredService<AppDbContext>();
        var sourceA = new EfPagamentoProcessamentoDataSource(dbA); var sourceB = new EfPagamentoProcessamentoDataSource(dbB);
        var id = dbA.Pagamentos.AsNoTracking().Single().Id; var primeiro = sourceA.ObterPorId(id)!; var segundo = sourceB.ObterPorId(id)!;
        var versao = primeiro.Versao; var provider = new HttpPagamentoMockSource(http, Options.Create(PagamentoMockHttpTests.Settings()));
        var input = new SolicitacaoPagamentoProvedor(id, 100m, "BRL", pedido.CorrelationId);
        var resultadoA = await provider.Cobrar(input, CancellationToken.None); var resultadoB = await provider.Cobrar(input, CancellationToken.None);
        primeiro.RegistrarResultado(resultadoA.Aprovado, resultadoA.ProvedorId, DateTime.UtcNow);
        segundo.RegistrarResultado(resultadoB.Aprovado, resultadoB.ProvedorId, DateTime.UtcNow);
        sourceA.Salvar(primeiro, versao, ResultadoPagamentoMensagemMapper.Mapear(primeiro));
        Assert.Throws<DbUpdateConcurrencyException>(() => sourceB.Salvar(segundo, versao, ResultadoPagamentoMensagemMapper.Mapear(segundo)));
        using var verificar = factory.Services.CreateScope(); var db = verificar.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Equal(3, db.Outbox.Count()); Assert.Equal(1, mock.ContarPagamentos());
    }
}

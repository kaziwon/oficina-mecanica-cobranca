using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OficinaMecanica.Cobranca.Application.Inputs.Pagamentos;
using OficinaMecanica.Cobranca.Application.Gateways;
using OficinaMecanica.Cobranca.Application.UseCases.Pagamentos;
using OficinaMecanica.Cobranca.Application.Mappers.Pagamentos;
using OficinaMecanica.Cobranca.Application.Outputs.Pagamentos;
using OficinaMecanica.Cobranca.Domain.Entities;
using OficinaMecanica.Cobranca.Infrastructure.Persistence;
using OficinaMecanica.Cobranca.Infrastructure.Sources;
using OficinaMecanica.Cobranca.InterfaceAdapters.Gateways;

namespace OficinaMecanica.Cobranca.IntegrationTests;

public class ReconciliacaoFinanceiraTests
{
    [Theory]
    [InlineData("approved", StatusPagamento.Confirmado)]
    [InlineData("rejected", StatusPagamento.Recusado)]
    public async Task Pagamento_ConsultaOperacaoOriginalEPublicaSomenteUmNovoResultado(string modo, StatusPagamento esperado)
    {
        using var mock = new PagamentoMockApiFactory(modo); using var http = mock.CreateClient();
        using var factory = new ConsultaFinanceiraApiFactory(http); using var client = factory.CreateClient();
        var pedido = PagamentoTestHelper.CriarPedido(factory); PagamentoTestHelper.Processar(factory, pedido);
        Guid id; string provider;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>(); var pagamento = db.Pagamentos.Single(); id = pagamento.Id;
            provider = (await new HttpPagamentoMockSource(http, Options.Create(PagamentoMockHttpTests.Settings()))
                .Cobrar(new(id, pagamento.Valor, pagamento.Moeda, pagamento.CorrelationId!), CancellationToken.None)).ProvedorId;
            MarcarIncerto(db, pagamento);
        }
        var rota = $"/api/pagamentos/{id}/reconciliar";
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsync(rota, null)).StatusCode);
        await ApiTestHelper.Autenticar(client);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync(rota, new { status = "Confirmado" })).StatusCode);
        var resposta = await client.PostAsJsonAsync(rota, new { }); resposta.EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.NoContent, resposta.StatusCode);
        var output = await client.GetFromJsonAsync<PagamentoOutput>($"/api/pagamentos/{id}", ApiTestHelper.Json);
        Assert.Equal(esperado, output!.Status); Assert.Equal(provider, output.ProvedorId);
        (await client.PostAsync(rota, null)).EnsureSuccessStatusCode();
        using var verificar = factory.Services.CreateScope(); var salvo = verificar.ServiceProvider.GetRequiredService<AppDbContext>();
        var atual = salvo.Pagamentos.Single(); Assert.Equal(id, atual.Id); Assert.Equal(1, atual.Tentativas);
        Assert.Equal(1, atual.ConsultasReconciliacao); Assert.NotNull(atual.ReconciliadoEm); Assert.Equal(1, mock.ContarPagamentos());
        Assert.Equal(4, salvo.Outbox.Count());
        Assert.Equal(pedido.MessageId, new EfOutboxDataSource(salvo).ObterPendentes(10).Last().CausationId);
    }

    [Fact]
    public async Task AusenciaNoProvedor_MantemIntervencaoSemCobrarOuPublicarRecusa()
    {
        using var mock = new PagamentoMockApiFactory(); using var http = mock.CreateClient();
        using var factory = new ConsultaFinanceiraApiFactory(http); using var client = factory.CreateClient();
        var pedido = PagamentoTestHelper.CriarPedido(factory); PagamentoTestHelper.Processar(factory, pedido); Guid id;
        using (var scope = factory.Services.CreateScope())
        { var db = scope.ServiceProvider.GetRequiredService<AppDbContext>(); var p = db.Pagamentos.Single(); id = p.Id; MarcarIncerto(db, p); }
        await ApiTestHelper.Autenticar(client); (await client.PostAsync($"/api/pagamentos/{id}/reconciliar", null)).EnsureSuccessStatusCode();
        using var verificar = factory.Services.CreateScope(); var salvo = verificar.ServiceProvider.GetRequiredService<AppDbContext>();
        var pagamento = salvo.Pagamentos.Single(); Assert.Equal(StatusPagamento.FalhaRequerIntervencao, pagamento.Status);
        Assert.Equal(1, pagamento.ConsultasReconciliacao); Assert.Null(pagamento.ReconciliadoEm);
        Assert.Equal(3, salvo.Outbox.Count()); Assert.Equal(0, mock.ContarPagamentos());
    }

    [Theory]
    [InlineData("refunded", StatusEstorno.Confirmado)]
    [InlineData("rejected", StatusEstorno.Recusado)]
    public async Task Estorno_ConsultaMesmoIdPreservaCobrancaENaoDuplicaReembolso(string modo, StatusEstorno esperado)
    {
        using var mock = new PagamentoMockApiFactory(resultadoEstorno: modo); using var http = mock.CreateClient();
        using var factory = new ConsultaFinanceiraApiFactory(http); using var client = factory.CreateClient();
        var pedido = await EstornoTests.Pagar(factory, http); PagamentoTestHelper.Processar(factory, pedido); Guid pagamentoId;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>(); var estorno = db.Estornos.Single(); pagamentoId = estorno.PagamentoId;
            await new HttpEstornoMockSource(http, Options.Create(PagamentoMockHttpTests.Settings())).Estornar(
                new(estorno.Id, estorno.PagamentoProvedorId, estorno.Valor, estorno.Moeda, estorno.CorrelationId), CancellationToken.None);
            var versao = estorno.Versao; estorno.RegistrarFalhaTecnica(DateTime.UtcNow, 1, TimeSpan.FromSeconds(1));
            new EfEstornoDataSource(db).Salvar(estorno, versao, ResultadoEstornoMensagemMapper.Mapear(estorno));
        }
        await ApiTestHelper.Autenticar(client); var rota = $"/api/pagamentos/{pagamentoId}/estorno/reconciliar";
        (await client.PostAsync(rota, null)).EnsureSuccessStatusCode(); (await client.PostAsJsonAsync(rota, new { })).EnsureSuccessStatusCode();
        using var verificar = factory.Services.CreateScope(); var salvo = verificar.ServiceProvider.GetRequiredService<AppDbContext>();
        var atual = salvo.Estornos.Single(); Assert.Equal(pedido.MessageId, atual.Id); Assert.Equal(esperado, atual.Status);
        Assert.Equal(1, atual.Tentativas); Assert.Equal(1, atual.ConsultasReconciliacao); Assert.NotNull(atual.ReconciliadoEm);
        Assert.Equal(StatusPagamento.Confirmado, salvo.Pagamentos.Single().Status);
        Assert.Equal(1, mock.ContarEstornos()); Assert.Equal(1, mock.ContarPagamentos()); Assert.Equal(5, salvo.Outbox.Count());
    }

    [Fact]
    public async Task PendenciaOuIdInexistente_NaoConsultaProvedor()
    {
        using var factory = new CobrancaApiFactory(); using var client = factory.CreateClient(); await ApiTestHelper.Autenticar(client);
        var pedido = PagamentoTestHelper.CriarPedido(factory); PagamentoTestHelper.Processar(factory, pedido);
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<AppDbContext>(); var id = db.Pagamentos.Single().Id;
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsync($"/api/pagamentos/{id}/reconciliar", null)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsync($"/api/pagamentos/{Guid.NewGuid()}/reconciliar", null)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsync($"/api/pagamentos/{id}/estorno/reconciliar", null)).StatusCode);
        MarcarIncerto(db, db.Pagamentos.Single());
        Assert.Equal(HttpStatusCode.ServiceUnavailable, (await client.PostAsync($"/api/pagamentos/{id}/reconciliar", null)).StatusCode);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RespostaPerdidaAposEfetivar_ConsultaRecuperaSemRepetirOperacao(bool estornar)
    {
        using var mock = new PagamentoMockApiFactory(estornar ? "approved" : "approved_response_lost",
            resultadoEstorno: "refunded_response_lost"); using var http = mock.CreateClient();
        using var factory = new ConsultaFinanceiraApiFactory(http); using var client = factory.CreateClient();
        Guid pagamentoId;
        if (estornar)
        {
            var pedido = await EstornoTests.Pagar(factory, http); PagamentoTestHelper.Processar(factory, pedido);
            using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            pagamentoId = db.Pagamentos.Single().Id;
            await new ProcessarEstornoUseCase(scope.ServiceProvider.GetRequiredService<IEstornoGateway>(),
                new EstornoProvedorGateway(new HttpEstornoMockSource(http, Options.Create(PagamentoMockHttpTests.Settings()))))
                .Executar(pedido.MessageId, 1, TimeSpan.FromSeconds(1), CancellationToken.None);
            Assert.Equal(StatusEstorno.FalhaRequerIntervencao, db.Estornos.Single().Status);
        }
        else
        {
            var pedido = PagamentoTestHelper.CriarPedido(factory); PagamentoTestHelper.Processar(factory, pedido);
            using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            pagamentoId = db.Pagamentos.Single().Id;
            await new ProcessarPagamentoUseCase(scope.ServiceProvider.GetRequiredService<IPagamentoProcessamentoGateway>(),
                new PagamentoProvedorGateway(new HttpPagamentoMockSource(http, Options.Create(PagamentoMockHttpTests.Settings()))))
                .Executar(pagamentoId, 1, TimeSpan.FromSeconds(1), CancellationToken.None);
            Assert.Equal(StatusPagamento.FalhaRequerIntervencao, db.Pagamentos.Single().Status);
        }
        await ApiTestHelper.Autenticar(client);
        var rota = $"/api/pagamentos/{pagamentoId}" + (estornar ? "/estorno" : "") + "/reconciliar";
        (await client.PostAsync(rota, null)).EnsureSuccessStatusCode();
        (await client.PostAsync(rota, null)).EnsureSuccessStatusCode();
        Assert.Equal(1, mock.ContarPagamentos()); Assert.Equal(estornar ? 1 : 0, mock.ContarEstornos());
        using var verificar = factory.Services.CreateScope(); var salvo = verificar.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Equal(StatusPagamento.Confirmado, salvo.Pagamentos.Single().Status);
        if (estornar) Assert.Equal(StatusEstorno.Confirmado, salvo.Estornos.Single().Status);
        Assert.Equal(estornar ? 5 : 4, salvo.Outbox.Count());
    }

    [Fact]
    public async Task FalhaAoSalvarReconciliacao_NaoPerdeIntervencaoNemDuplicaOperacaoNoProvedor()
    {
        using var mock = new PagamentoMockApiFactory(); using var http = mock.CreateClient();
        using var factory = new ConsultaFinanceiraApiFactory(http); using var client = factory.CreateClient();
        var pedido = PagamentoTestHelper.CriarPedido(factory); PagamentoTestHelper.Processar(factory, pedido); Guid id;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>(); var p = db.Pagamentos.Single(); id = p.Id;
            await new HttpPagamentoMockSource(http, Options.Create(PagamentoMockHttpTests.Settings()))
                .Cobrar(new(id, p.Valor, p.Moeda, p.CorrelationId!), CancellationToken.None);
            MarcarIncerto(db, p);
        }
        using (var scope = factory.Services.CreateScope())
        {
            var useCase = new ReconciliarPagamentoUseCase(new PagamentoProcessamentoComFalhaGateway(
                scope.ServiceProvider.GetRequiredService<IPagamentoProcessamentoGateway>()),
                scope.ServiceProvider.GetRequiredService<IConsultaFinanceiraGateway>());
            await Assert.ThrowsAsync<InvalidOperationException>(() => useCase.Executar(id, CancellationToken.None));
        }
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>(); Assert.Equal(StatusPagamento.FalhaRequerIntervencao, db.Pagamentos.Single().Status);
            Assert.Equal(0, db.Pagamentos.Single().ConsultasReconciliacao); Assert.Equal(3, db.Outbox.Count());
        }
        await ApiTestHelper.Autenticar(client); (await client.PostAsync($"/api/pagamentos/{id}/reconciliar", null)).EnsureSuccessStatusCode();
        Assert.Equal(1, mock.ContarPagamentos());
    }

    private static void MarcarIncerto(AppDbContext db, Pagamento pagamento)
    {
        var versao = pagamento.Versao; pagamento.RegistrarFalhaTecnica(DateTime.UtcNow, 1, TimeSpan.FromSeconds(1));
        new EfPagamentoProcessamentoDataSource(db).Salvar(pagamento, versao, ResultadoPagamentoMensagemMapper.Mapear(pagamento));
    }
}

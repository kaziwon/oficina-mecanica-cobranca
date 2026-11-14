using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OficinaMecanica.Cobranca.Application.Exceptions;
using OficinaMecanica.Cobranca.Application.Gateways;
using OficinaMecanica.Cobranca.Application.Inputs.Pagamentos;
using OficinaMecanica.Cobranca.Application.Mappers.Pagamentos;
using OficinaMecanica.Cobranca.Application.Messaging;
using OficinaMecanica.Cobranca.Application.UseCases.Pagamentos;
using OficinaMecanica.Cobranca.Domain.Entities;
using OficinaMecanica.Cobranca.Infrastructure.Persistence;
using OficinaMecanica.Cobranca.Infrastructure.Sources;
using OficinaMecanica.Cobranca.InterfaceAdapters.Gateways;

namespace OficinaMecanica.Cobranca.IntegrationTests;

public class EstornoTests
{
    [Theory]
    [InlineData("refunded", StatusEstorno.Confirmado, "EstornoConfirmado")]
    [InlineData("rejected", StatusEstorno.Recusado, "EstornoRecusado")]
    [InlineData("unavailable", StatusEstorno.FalhaRequerIntervencao, "FalhaProcessamentoEstorno")]
    public async Task Resultado_SeparaEstornoDaCobrancaOriginalSemDuplicar(string modo, StatusEstorno status, string tipo)
    {
        using var factory = new CobrancaApiFactory(); using var client = factory.CreateClient();
        using var mock = new PagamentoMockApiFactory(resultadoEstorno: modo); using var http = mock.CreateClient();
        var pedido = await Pagar(factory, http); PagamentoTestHelper.Processar(factory, pedido); PagamentoTestHelper.Processar(factory, pedido);
        using (var scope = factory.Services.CreateScope())
        {
            var useCase = new ProcessarEstornoUseCase(scope.ServiceProvider.GetRequiredService<IEstornoGateway>(), Provedor(http));
            await useCase.Executar(pedido.MessageId, 1, TimeSpan.FromSeconds(1), CancellationToken.None);
            await useCase.Executar(pedido.MessageId, 1, TimeSpan.FromSeconds(1), CancellationToken.None);
        }
        using var verificar = factory.Services.CreateScope(); var db = verificar.ServiceProvider.GetRequiredService<AppDbContext>(); var estorno = db.Estornos.Single();
        Assert.Equal(status, estorno.Status); Assert.Equal(1, estorno.Tentativas); Assert.Equal(100m, estorno.Valor);
        Assert.Equal(StatusPagamento.Confirmado, db.Pagamentos.Single().Status); Assert.Equal(1, db.Pagamentos.Single().Tentativas);
        Assert.Equal(1, mock.ContarPagamentos()); Assert.Equal(modo == "unavailable" ? 0 : 1, mock.ContarEstornos());
        var evento = new EfOutboxDataSource(db).ObterPendentes(10).Single(item => item.Type == tipo);
        Assert.Equal(pedido.MessageId, evento.CausationId); Assert.Equal(pedido.MessageId, evento.Payload.EstornoId); Assert.Equal(4, db.Outbox.Count());
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync($"/api/pagamentos/{estorno.PagamentoId}/estorno")).StatusCode);
        await ApiTestHelper.Autenticar(client);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"/api/pagamentos/{estorno.PagamentoId}/estorno")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/pagamentos/{Guid.NewGuid()}/estorno")).StatusCode);
    }

    [Fact]
    public async Task EstornoConfirmadoMasGravacaoFalhou_RetomaMesmaOperacaoNoMock()
    {
        using var factory = new CobrancaApiFactory(); using var client = factory.CreateClient();
        using var mock = new PagamentoMockApiFactory(); using var http = mock.CreateClient();
        var pedido = await Pagar(factory, http); PagamentoTestHelper.Processar(factory, pedido);
        using (var scope = factory.Services.CreateScope())
            await Assert.ThrowsAsync<InvalidOperationException>(() => new ProcessarEstornoUseCase(
                new EstornoComFalhaGateway(scope.ServiceProvider.GetRequiredService<IEstornoGateway>()), Provedor(http))
                .Executar(pedido.MessageId, 3, TimeSpan.FromSeconds(1), CancellationToken.None));
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>(); Assert.Equal(StatusEstorno.Pendente, db.Estornos.Single().Status);
            await new ProcessarEstornoUseCase(scope.ServiceProvider.GetRequiredService<IEstornoGateway>(), Provedor(http))
                .Executar(pedido.MessageId, 3, TimeSpan.FromSeconds(1), CancellationToken.None);
        }
        using var verificar = factory.Services.CreateScope(); var salvo = verificar.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Equal(StatusEstorno.Confirmado, salvo.Estornos.Single().Status); Assert.Equal(1, mock.ContarEstornos());
        Assert.Equal(1, mock.ContarPagamentos()); Assert.Equal(4, salvo.Outbox.Count());
    }

    [Fact]
    public async Task ComandosRepetidosOuDivergentes_NaoCriamSegundoEstorno()
    {
        using var factory = new CobrancaApiFactory(); using var client = factory.CreateClient();
        using var mock = new PagamentoMockApiFactory(); using var http = mock.CreateClient();
        var pedido = await Pagar(factory, http); PagamentoTestHelper.Processar(factory, pedido); PagamentoTestHelper.Processar(factory, pedido);
        pedido.Payload.ValorTotal = 99m; Assert.Throws<ValidacaoException>(() => PagamentoTestHelper.Processar(factory, pedido));
        pedido.Payload.ValorTotal = 100m; pedido.MessageId = Guid.NewGuid(); pedido.Payload.EstornoId = pedido.MessageId;
        Assert.Throws<ValidacaoException>(() => PagamentoTestHelper.Processar(factory, pedido));
        using var scope = factory.Services.CreateScope(); Assert.Single(scope.ServiceProvider.GetRequiredService<AppDbContext>().Estornos);
    }

    [Theory]
    [InlineData("pagamento")]
    [InlineData("provedor")]
    [InlineData("valor")]
    [InlineData("correlacao")]
    public async Task PedidoSemVinculoFinanceiro_NaoGravaEstorno(string erro)
    {
        using var factory = new CobrancaApiFactory(); using var client = factory.CreateClient();
        using var mock = new PagamentoMockApiFactory(); using var http = mock.CreateClient(); var pedido = await Pagar(factory, http);
        if (erro == "pagamento") pedido.Payload.PagamentoId = Guid.NewGuid();
        if (erro == "provedor") pedido.Payload.ProvedorId = "outra-cobranca";
        if (erro == "valor") pedido.Payload.ValorTotal = 99m;
        if (erro == "correlacao") pedido.CorrelationId = "outra";
        Assert.Throws<ValidacaoException>(() => PagamentoTestHelper.Processar(factory, pedido));
        using var scope = factory.Services.CreateScope(); Assert.Empty(scope.ServiceProvider.GetRequiredService<AppDbContext>().Estornos);
    }

    [Fact]
    public async Task DuasVersoes_NaoDuplicamResultadoNemEstornoExterno()
    {
        using var factory = new CobrancaApiFactory(); using var client = factory.CreateClient();
        using var mock = new PagamentoMockApiFactory(); using var http = mock.CreateClient(); var pedido = await Pagar(factory, http);
        PagamentoTestHelper.Processar(factory, pedido); using var a = factory.Services.CreateScope(); using var b = factory.Services.CreateScope();
        var da = a.ServiceProvider.GetRequiredService<AppDbContext>(); var db = b.ServiceProvider.GetRequiredService<AppDbContext>();
        var primeiro = da.Estornos.Single(); var segundo = db.Estornos.Single(); var versao = primeiro.Versao;
        var input = new SolicitacaoEstornoProvedor(primeiro.Id, primeiro.PagamentoProvedorId, primeiro.Valor, primeiro.Moeda, primeiro.CorrelationId);
        var ra = await Provedor(http).Estornar(input, CancellationToken.None); var rb = await Provedor(http).Estornar(input, CancellationToken.None);
        primeiro.RegistrarResultado(ra.Confirmado, ra.ProvedorId, DateTime.UtcNow); segundo.RegistrarResultado(rb.Confirmado, rb.ProvedorId, DateTime.UtcNow);
        new EfEstornoDataSource(da).Salvar(primeiro, versao, ResultadoEstornoMensagemMapper.Mapear(primeiro));
        Assert.Throws<DbUpdateConcurrencyException>(() => new EfEstornoDataSource(db).Salvar(segundo, versao, ResultadoEstornoMensagemMapper.Mapear(segundo)));
        using var verificar = factory.Services.CreateScope(); Assert.Equal(4, verificar.ServiceProvider.GetRequiredService<AppDbContext>().Outbox.Count());
        Assert.Equal(1, mock.ContarEstornos());
    }

    private static IEstornoProvedorGateway Provedor(HttpClient http) => new EstornoProvedorGateway(new HttpEstornoMockSource(http, Options.Create(PagamentoMockHttpTests.Settings())));
    internal static async Task<MensagemIntegracao> Pagar(CobrancaApiFactory factory, HttpClient http)
    {
        var pagamento = PagamentoTestHelper.CriarPedido(factory); PagamentoTestHelper.Processar(factory, pagamento);
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<AppDbContext>(); var salvo = db.Pagamentos.Single();
        await new ProcessarPagamentoUseCase(scope.ServiceProvider.GetRequiredService<IPagamentoProcessamentoGateway>(),
            new PagamentoProvedorGateway(new HttpPagamentoMockSource(http, Options.Create(PagamentoMockHttpTests.Settings()))))
            .Executar(salvo.Id, 3, TimeSpan.FromSeconds(1), CancellationToken.None);
        var id = Guid.NewGuid();
        return new MensagemIntegracao
        {
            MessageId = id,
            Type = "SolicitarEstorno",
            SagaId = salvo.SagaId!.Value,
            CausationId = Guid.NewGuid(),
            CorrelationId = salvo.CorrelationId!,
            OccurredAt = DateTime.UtcNow,
            Payload = new DadosMensagem
            {
                OrdemServicoId = salvo.OrdemServicoId,
                OrcamentoId = salvo.OrcamentoId,
                ExecucaoId = salvo.ExecucaoId,
                PagamentoId = salvo.Id,
                ProvedorId = salvo.ProvedorId,
                ValorTotal = salvo.Valor,
                Moeda = salvo.Moeda,
                EstornoId = id
            }
        };
    }
}

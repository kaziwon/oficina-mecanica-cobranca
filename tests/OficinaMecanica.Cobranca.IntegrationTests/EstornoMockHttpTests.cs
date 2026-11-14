using Microsoft.Extensions.Options;
using OficinaMecanica.Cobranca.Application.Exceptions;
using OficinaMecanica.Cobranca.Application.Inputs.Pagamentos;
using OficinaMecanica.Cobranca.Infrastructure.Sources;
namespace OficinaMecanica.Cobranca.IntegrationTests;

public class EstornoMockHttpTests
{
    [Theory]
    [InlineData("valor")]
    [InlineData("referencia")]
    [InlineData("pagamento")]
    [InlineData("status")]
    public async Task RespostaDivergente_NaoConfirmaEstorno(string modo)
    {
        using var handler = new EstornoRespostaInvalidaHandler(modo); using var http = new HttpClient(handler) { BaseAddress = new("http://mock/") };
        var source = new HttpEstornoMockSource(http, Options.Create(PagamentoMockHttpTests.Settings()));
        await Assert.ThrowsAsync<IntegracaoPagamentoException>(() => source.Estornar(new(Guid.NewGuid(), "charge-123", 100m, "BRL", "teste"), CancellationToken.None));
    }
    [Fact]
    public async Task ReinicioDoMock_MantemResultadoERecusaNovoIdParaMesmaCobranca()
    {
        string path; string providerId; SolicitacaoEstornoProvedor pedido;
        using (var factory = new PagamentoMockApiFactory(apagar: false))
        {
            path = factory.DatabasePath; using var http = factory.CreateClient();
            var pagamento = await new HttpPagamentoMockSource(http, Options.Create(PagamentoMockHttpTests.Settings()))
                .Cobrar(new(Guid.NewGuid(), 100m, "BRL", "teste"), CancellationToken.None);
            pedido = new(Guid.NewGuid(), pagamento.ProvedorId, 100m, "BRL", "teste");
            var source = new HttpEstornoMockSource(http, Options.Create(PagamentoMockHttpTests.Settings()));
            providerId = (await source.Estornar(pedido, CancellationToken.None)).ProvedorId;
            Assert.True((await source.Estornar(pedido, CancellationToken.None)).Confirmado); Assert.Equal(1, factory.ContarEstornos());
        }
        using var reiniciado = new PagamentoMockApiFactory(path: path, resultadoEstorno: "rejected"); using var client = reiniciado.CreateClient();
        var estorno = new HttpEstornoMockSource(client, Options.Create(PagamentoMockHttpTests.Settings()));
        var repetido = await estorno.Estornar(pedido, CancellationToken.None); Assert.True(repetido.Confirmado); Assert.Equal(providerId, repetido.ProvedorId);
        await Assert.ThrowsAsync<IntegracaoPagamentoException>(() => estorno.Estornar(pedido with { EstornoId = Guid.NewGuid() }, CancellationToken.None));
        await Assert.ThrowsAsync<IntegracaoPagamentoException>(() => estorno.Estornar(pedido with { Valor = 99m }, CancellationToken.None));
        Assert.Equal(1, reiniciado.ContarEstornos()); Assert.Equal(1, reiniciado.ContarPagamentos());
    }
}

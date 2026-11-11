using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Options;
using OficinaMecanica.Cobranca.Application.Exceptions;
using OficinaMecanica.Cobranca.Application.Inputs.Pagamentos;
using OficinaMecanica.Cobranca.Infrastructure.Settings;
using OficinaMecanica.Cobranca.Infrastructure.Sources;

namespace OficinaMecanica.Cobranca.IntegrationTests;

public class PagamentoMockHttpTests
{
    internal static PagamentoMockSettings Settings() => new()
    { Enabled = true, BaseUrl = "http://localhost/", ClientId = "oficina-local", ClientSecret = "segredo-do-mock-somente-local" };

    [Fact]
    public async Task Cliente_ObtemTokenERepeteMesmaCobrancaAposReinicioDoMock()
    {
        var pedido = new SolicitacaoPagamentoProvedor(Guid.NewGuid(), 100m, "BRL", "correlacao-original");
        string id; string path;
        using (var factory = new PagamentoMockApiFactory(apagar: false))
        {
            path = factory.DatabasePath; using var http = factory.CreateClient();
            var source = new HttpPagamentoMockSource(http, Options.Create(Settings()));
            var primeira = await source.Cobrar(pedido, CancellationToken.None);
            var segunda = await source.Cobrar(pedido, CancellationToken.None);
            Assert.True(primeira.Aprovado); Assert.Equal(primeira, segunda); Assert.Equal(1, factory.ContarPagamentos()); id = primeira.ProvedorId;
        }
        // Mudar o modo do servidor nao muda uma transacao ja persistida com a mesma chave.
        using var reiniciado = new PagamentoMockApiFactory("rejected", path);
        using var client = reiniciado.CreateClient();
        var resposta = await new HttpPagamentoMockSource(client, Options.Create(Settings())).Cobrar(pedido, CancellationToken.None);
        Assert.True(resposta.Aprovado); Assert.Equal(id, resposta.ProvedorId); Assert.Equal(1, reiniciado.ContarPagamentos());
    }

    [Theory]
    [InlineData("rejected")]
    [InlineData("unavailable")]
    public async Task Cliente_DistingueRecusaDeIndisponibilidade(string modo)
    {
        using var factory = new PagamentoMockApiFactory(modo); using var http = factory.CreateClient();
        var source = new HttpPagamentoMockSource(http, Options.Create(Settings()));
        var pedido = new SolicitacaoPagamentoProvedor(Guid.NewGuid(), 100m, "BRL", "teste");
        if (modo == "rejected")
        { Assert.False((await source.Cobrar(pedido, CancellationToken.None)).Aprovado); Assert.Equal(1, factory.ContarPagamentos()); }
        else
        { await Assert.ThrowsAsync<IntegracaoPagamentoException>(() => source.Cobrar(pedido, CancellationToken.None)); Assert.Equal(0, factory.ContarPagamentos()); }
    }

    [Fact]
    public async Task CredencialErrada_NaoGeraTransacao()
    {
        using var factory = new PagamentoMockApiFactory(); using var http = factory.CreateClient(); var settings = Settings(); settings.ClientSecret = "errado";
        await Assert.ThrowsAsync<IntegracaoPagamentoException>(() => new HttpPagamentoMockSource(http, Options.Create(settings))
            .Cobrar(new SolicitacaoPagamentoProvedor(Guid.NewGuid(), 100m, "BRL", "teste"), CancellationToken.None));
        Assert.Equal(0, factory.ContarPagamentos());
    }

    [Theory]
    [InlineData("valor")]
    [InlineData("referencia")]
    [InlineData("status")]
    public async Task RespostaDivergente_NaoEhTratadaComoPagamentoConfirmado(string modo)
    {
        using var handler = new PagamentoRespostaInvalidaHandler { Modo = modo };
        using var http = new HttpClient(handler) { BaseAddress = new Uri("http://mock/") };
        var id = Guid.NewGuid(); var source = new HttpPagamentoMockSource(http, Options.Create(Settings()));
        await Assert.ThrowsAsync<IntegracaoPagamentoException>(() => source.Cobrar(new(id, 100m, "BRL", "correlacao-original"), CancellationToken.None));
        Assert.Equal(new[] { "/oauth/token", "/payments" }, handler.Caminhos);
        Assert.Equal(id.ToString(), handler.Chave); Assert.Equal("correlacao-original", handler.CorrelationId); Assert.True(handler.Autenticado);
    }

    [Fact]
    public async Task Mock_ExigeBearerERejeitaMesmaChaveComOutroValor()
    {
        using var factory = new PagamentoMockApiFactory(); using var http = factory.CreateClient(); var id = Guid.NewGuid();
        var body = new { external_reference = id, transaction_amount = 100m, currency_id = "BRL" };
        Assert.Equal(HttpStatusCode.Unauthorized, (await http.PostAsJsonAsync("/payments", body)).StatusCode);
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "token-ficticio-do-mock-local");
        http.DefaultRequestHeaders.Add("X-Idempotency-Key", id.ToString()); http.DefaultRequestHeaders.Add("X-Correlation-ID", "teste");
        (await http.PostAsJsonAsync("/payments", body)).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.Conflict, (await http.PostAsJsonAsync("/payments", new { external_reference = id, transaction_amount = 101m, currency_id = "BRL" })).StatusCode);
        Assert.Equal(1, factory.ContarPagamentos());
        (await http.GetAsync("/payments/" + id)).EnsureSuccessStatusCode();
    }
}

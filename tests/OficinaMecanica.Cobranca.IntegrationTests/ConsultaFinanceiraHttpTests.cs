using Microsoft.Extensions.Options;
using OficinaMecanica.Cobranca.Application.Exceptions;
using OficinaMecanica.Cobranca.Application.Inputs.Pagamentos;
using OficinaMecanica.Cobranca.Infrastructure.Sources;

namespace OficinaMecanica.Cobranca.IntegrationTests;

public class ConsultaFinanceiraHttpTests
{
    [Theory]
    [InlineData("approved")]
    [InlineData("rejected")]
    [InlineData("404")]
    public async Task ConsultaSomenteGet_DistingueResultadoFinalDeAusencia(string modo)
    {
        using var handler = new ConsultaFinanceiraRespostaHandler { Modo = modo };
        using var http = new HttpClient(handler) { BaseAddress = new Uri("http://mock/") };
        var source = new HttpConsultaFinanceiraMockSource(http, Options.Create(PagamentoMockHttpTests.Settings()));
        var id = Guid.NewGuid(); var resultado = await source.Consultar(new SolicitacaoPagamentoProvedor(id, 100m, "BRL", "consulta"), CancellationToken.None);
        if (modo == "404") Assert.Null(resultado); else Assert.Equal(modo == "approved", resultado!.Aprovado);
        var estorno = await source.Consultar(new SolicitacaoEstornoProvedor(id, "charge", 100m, "BRL", "consulta"), CancellationToken.None);
        if (modo == "404") Assert.Null(estorno); else Assert.Equal(modo != "rejected", estorno!.Confirmado);
        Assert.Equal(new[] { "POST /oauth/token", $"GET /payments/{id}", "POST /oauth/token", $"GET /refunds/{id}" }, handler.Chamadas);
    }

    [Theory]
    [InlineData("token")]
    [InlineData("id")]
    [InlineData("referencia")]
    [InlineData("valor")]
    [InlineData("moeda")]
    [InlineData("pending")]
    [InlineData("json")]
    [InlineData("503")]
    public async Task RespostaInvalidaOuIndisponivel_NaoEhConfirmacaoOuRecusa(string modo)
    {
        using var handler = new ConsultaFinanceiraRespostaHandler { Modo = modo };
        using var http = new HttpClient(handler) { BaseAddress = new Uri("http://mock/") };
        var source = new HttpConsultaFinanceiraMockSource(http, Options.Create(PagamentoMockHttpTests.Settings()));
        await Assert.ThrowsAsync<IntegracaoPagamentoException>(() => source.Consultar(
            new SolicitacaoPagamentoProvedor(Guid.NewGuid(), 100m, "BRL", "consulta"), CancellationToken.None));
        await Assert.ThrowsAsync<IntegracaoPagamentoException>(() => source.Consultar(
            new SolicitacaoEstornoProvedor(Guid.NewGuid(), "charge", 100m, "BRL", "consulta"), CancellationToken.None));
    }

    [Fact]
    public async Task EstornoDeOutraCobranca_NaoEhAceito()
    {
        using var handler = new ConsultaFinanceiraRespostaHandler { Modo = "pagamento" };
        using var http = new HttpClient(handler) { BaseAddress = new Uri("http://mock/") };
        var source = new HttpConsultaFinanceiraMockSource(http, Options.Create(PagamentoMockHttpTests.Settings()));
        await Assert.ThrowsAsync<IntegracaoPagamentoException>(() => source.Consultar(
            new SolicitacaoEstornoProvedor(Guid.NewGuid(), "charge", 100m, "BRL", "consulta"), CancellationToken.None));
    }
}

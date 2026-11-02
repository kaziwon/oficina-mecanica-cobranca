using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using OficinaMecanica.Cobranca.Domain.Entities;
using OficinaMecanica.Cobranca.InterfaceAdapters.DTOs;

namespace OficinaMecanica.Cobranca.IntegrationTests;

public class FluxoCobrancaTests
{
    [Fact]
    public async Task FluxoCompleto_CriaAprovaERegistraUmaUnicaPendencia()
    {
        using var factory = new CobrancaApiFactory();
        using var client = factory.CreateClient();
        await ApiTestHelper.Autenticar(client);
        var orcamento = await ApiTestHelper.CriarOrcamento(client);
        Assert.Equal(261.15m, orcamento.ValorTotal);
        Assert.Equal(StatusOrcamento.AguardandoAprovacao, orcamento.Status);
        var aprovado = await client.PostAsync($"/api/orcamentos/{orcamento.Id}/aprovar", null);
        aprovado.EnsureSuccessStatusCode();
        Assert.Equal(StatusOrcamento.Aprovado, (await aprovado.Content.ReadFromJsonAsync<OrcamentoResponseDto>(ApiTestHelper.Json))!.Status);
        var repetido = await client.PostAsync($"/api/orcamentos/{orcamento.Id}/aprovar", null);
        repetido.EnsureSuccessStatusCode();
        Assert.Equal(2, (await repetido.Content.ReadFromJsonAsync<OrcamentoResponseDto>(ApiTestHelper.Json))!.Historico.Count);
        var resposta = await client.PostAsync($"/api/orcamentos/{orcamento.Id}/pagamento", null);
        resposta.EnsureSuccessStatusCode();
        var pagamento = (await resposta.Content.ReadFromJsonAsync<PagamentoResponseDto>(ApiTestHelper.Json))!;
        Assert.Equal(StatusPagamento.Pendente, pagamento.Status);
        Assert.Equal(261.15m, pagamento.Valor);
        Assert.Equal("BRL", pagamento.Moeda);
        var segundaResposta = await client.PostAsync($"/api/orcamentos/{orcamento.Id}/pagamento", null);
        segundaResposta.EnsureSuccessStatusCode();
        Assert.Equal(pagamento.Id, (await segundaResposta.Content.ReadFromJsonAsync<PagamentoResponseDto>(ApiTestHelper.Json))!.Id);
        var consulta = await client.GetFromJsonAsync<PagamentoResponseDto>($"/api/pagamentos/{pagamento.Id}", ApiTestHelper.Json);
        Assert.Equal(pagamento.Id, consulta!.Id);
        var atualizado = await client.GetFromJsonAsync<OrcamentoResponseDto>($"/api/orcamentos/{orcamento.Id}", ApiTestHelper.Json);
        Assert.Equal(pagamento.Id, atualizado!.PagamentoId);
        Assert.Equal(3, atualizado.Versao);
    }

    [Fact]
    public async Task Recusa_PersisteMotivoImpedeAprovacaoEPagamento()
    {
        using var factory = new CobrancaApiFactory();
        using var client = factory.CreateClient();
        await ApiTestHelper.Autenticar(client);
        var orcamento = await ApiTestHelper.CriarOrcamento(client);
        var response = await client.PostAsJsonAsync($"/api/orcamentos/{orcamento.Id}/recusar", new { motivo = "Valor alto" });
        response.EnsureSuccessStatusCode();
        var recusado = (await response.Content.ReadFromJsonAsync<OrcamentoResponseDto>(ApiTestHelper.Json))!;
        Assert.Equal(StatusOrcamento.Recusado, recusado.Status);
        Assert.Equal("Valor alto", recusado.MotivoRecusa);
        var repetida = await client.PostAsJsonAsync($"/api/orcamentos/{orcamento.Id}/recusar", new { motivo = "Valor alto" });
        repetida.EnsureSuccessStatusCode();
        Assert.Equal(2, (await repetida.Content.ReadFromJsonAsync<OrcamentoResponseDto>(ApiTestHelper.Json))!.Historico.Count);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsync($"/api/orcamentos/{orcamento.Id}/aprovar", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsync($"/api/orcamentos/{orcamento.Id}/pagamento", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict,
            (await client.PostAsJsonAsync($"/api/orcamentos/{orcamento.Id}/recusar", new { motivo = "Outro motivo" })).StatusCode);
    }

    [Fact]
    public async Task Aprovado_NaoPodeSerRecusadoEPendenteNaoPodeReceberPagamento()
    {
        using var factory = new CobrancaApiFactory();
        using var client = factory.CreateClient();
        await ApiTestHelper.Autenticar(client);
        var orcamento = await ApiTestHelper.CriarOrcamento(client);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsync($"/api/orcamentos/{orcamento.Id}/pagamento", null)).StatusCode);
        (await client.PostAsync($"/api/orcamentos/{orcamento.Id}/aprovar", null)).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.Conflict,
            (await client.PostAsJsonAsync($"/api/orcamentos/{orcamento.Id}/recusar", new { motivo = "Recusa" })).StatusCode);
    }

    [Fact]
    public async Task Lista_FiltraPorStatusEOrdemServico()
    {
        using var factory = new CobrancaApiFactory();
        using var client = factory.CreateClient();
        await ApiTestHelper.Autenticar(client);
        var primeiro = await ApiTestHelper.CriarOrcamento(client);
        var segundo = await ApiTestHelper.CriarOrcamento(client);
        (await client.PostAsync($"/api/orcamentos/{segundo.Id}/aprovar", null)).EnsureSuccessStatusCode();
        var todos = await client.GetFromJsonAsync<List<OrcamentoResponseDto>>("/api/orcamentos", ApiTestHelper.Json);
        Assert.Equal(new[] { primeiro.Id, segundo.Id }, todos!.Select(item => item.Id));
        var filtrados = await client.GetFromJsonAsync<List<OrcamentoResponseDto>>(
            $"/api/orcamentos?status=Aprovado&ordemServicoId={segundo.OrdemServicoId}", ApiTestHelper.Json);
        Assert.Equal(segundo.Id, Assert.Single(filtrados!).Id);
        var vazios = await client.GetFromJsonAsync<List<OrcamentoResponseDto>>("/api/orcamentos?status=Recusado", ApiTestHelper.Json);
        Assert.Empty(vazios!);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync("/api/orcamentos?status=99")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync($"/api/orcamentos?ordemServicoId={Guid.Empty}")).StatusCode);
    }

    [Fact]
    public async Task CriacaoDuplicadaParaOS_RetornaConflito()
    {
        using var factory = new CobrancaApiFactory();
        using var client = factory.CreateClient();
        await ApiTestHelper.Autenticar(client);
        var body = ApiTestHelper.Orcamento();
        (await client.PostAsJsonAsync("/api/orcamentos", body)).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync("/api/orcamentos", body)).StatusCode);
    }

    [Theory]
    [InlineData("valorTotal", "0.01")]
    [InlineData("status", "\"Aprovado\"")]
    [InlineData("itens", "null")]
    [InlineData("itens", "[]")]
    [InlineData("itens", "[null]")]
    [InlineData("ordemServicoId", "\"00000000-0000-0000-0000-000000000000\"")]
    public async Task Criacao_RejeitaValoresForjadosOuEstruturaInvalida(string campo, string valor)
    {
        using var factory = new CobrancaApiFactory();
        using var client = factory.CreateClient();
        await ApiTestHelper.Autenticar(client);
        var json = System.Text.Json.JsonSerializer.SerializeToNode(ApiTestHelper.Orcamento())!;
        json[campo] = JsonNode.Parse(valor);
        var response = await client.PostAsJsonAsync("/api/orcamentos", json);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Recusa_RejeitaMotivoVazio()
    {
        using var factory = new CobrancaApiFactory();
        using var client = factory.CreateClient();
        await ApiTestHelper.Autenticar(client);
        var orcamento = await ApiTestHelper.CriarOrcamento(client);
        Assert.Equal(HttpStatusCode.BadRequest,
            (await client.PostAsJsonAsync($"/api/orcamentos/{orcamento.Id}/recusar", new { motivo = " " })).StatusCode);
    }

    [Theory]
    [InlineData("GET", "/api/orcamentos")]
    [InlineData("POST", "/api/orcamentos")]
    [InlineData("GET", "/api/pagamentos/11111111-1111-1111-1111-111111111111")]
    [InlineData("POST", "/api/orcamentos/11111111-1111-1111-1111-111111111111/aprovar")]
    [InlineData("POST", "/api/orcamentos/11111111-1111-1111-1111-111111111111/recusar")]
    [InlineData("POST", "/api/orcamentos/11111111-1111-1111-1111-111111111111/pagamento")]
    public async Task RotasDeNegocio_ExigemAutenticacao(string metodo, string rota)
    {
        using var factory = new CobrancaApiFactory();
        using var client = factory.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.SendAsync(new HttpRequestMessage(new HttpMethod(metodo), rota))).StatusCode);
    }

    [Fact]
    public async Task RecursosInexistentes_Retornam404()
    {
        using var factory = new CobrancaApiFactory();
        using var client = factory.CreateClient();
        await ApiTestHelper.Autenticar(client);
        var id = Guid.NewGuid();
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/orcamentos/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/pagamentos/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsync($"/api/orcamentos/{id}/aprovar", null)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsJsonAsync($"/api/orcamentos/{id}/recusar", new { motivo = "Motivo" })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsync($"/api/orcamentos/{id}/pagamento", null)).StatusCode);
    }

    [Fact]
    public async Task HealthSwaggerLoginECorrelacao_EstaoDisponiveis()
    {
        using var factory = new CobrancaApiFactory();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Correlation-ID", "teste-cobranca-123");
        var health = await client.GetAsync("/health");
        health.EnsureSuccessStatusCode();
        Assert.Equal("teste-cobranca-123", Assert.Single(health.Headers.GetValues("X-Correlation-ID")));
        (await client.GetAsync("/health/ready")).EnsureSuccessStatusCode();
        (await client.GetAsync("/swagger/v1/swagger.json")).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.Unauthorized,
            (await client.PostAsJsonAsync("/api/auth/login", new { username = "admin", password = "Errada" })).StatusCode);
    }
}

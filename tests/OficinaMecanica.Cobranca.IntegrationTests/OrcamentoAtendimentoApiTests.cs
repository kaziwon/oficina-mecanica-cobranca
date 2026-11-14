using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using OficinaMecanica.Cobranca.Application.Inputs.Orcamentos;
using OficinaMecanica.Cobranca.Domain.Entities;
using OficinaMecanica.Cobranca.Infrastructure.Persistence;
using OficinaMecanica.Cobranca.InterfaceAdapters.DTOs;

namespace OficinaMecanica.Cobranca.IntegrationTests;

public class OrcamentoAtendimentoApiTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Decisao_GravaUmaVezEUsaEventoOriginalDaSaga(bool aprovado)
    {
        using var factory = new AtendimentoApiFactory(); using var client = factory.CreateClient(); var (id, vinculo) = Criar(factory);
        client.DefaultRequestHeaders.Authorization = new("Bearer", AtendimentoTokenTestHelper.Gerar());
        var url = $"/internal/orcamentos/{id}"; var body = new { vinculo, motivo = aprovado ? null : "Valor alto" };
        var original = (await client.GetFromJsonAsync<OrcamentoResponseDto>(await Query(id, vinculo), ApiTestHelper.Json))!;
        Assert.Equal(200m, original.ValorTotal); Assert.True(original.CriadoEm.Kind == DateTimeKind.Utc);
        for (var i = 0; i < 2; i++) Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsJsonAsync(url + (aprovado ? "/aprovar" : "/recusar"), body)).StatusCode);
        var final = (await client.GetFromJsonAsync<OrcamentoResponseDto>(await Query(id, vinculo), ApiTestHelper.Json))!;
        Assert.Equal(aprovado ? StatusOrcamento.Aprovado : StatusOrcamento.Recusado, final.Status); Assert.Equal(2, final.Historico.Count);
        Assert.NotNull(final.RespondidoEm); Assert.Null(final.PagamentoId); Assert.Equal(original.ValorTotal, final.ValorTotal);
        var contraditoria = new { vinculo, motivo = aprovado ? "Mudei de ideia" : null };
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync(url + (aprovado ? "/recusar" : "/aprovar"), contraditoria)).StatusCode);
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Single(db.Outbox); Assert.Empty(db.Pagamentos); Assert.Equal(2, db.HistoricoOrcamento.Count());
    }
    [Theory]
    [InlineData("os")]
    [InlineData("saga")]
    [InlineData("execucao")]
    [InlineData("pedido")]
    [InlineData("valor")]
    [InlineData("correlacao")]
    public async Task VinculoAlterado_NaoLeNemResponde(string erro)
    {
        using var factory = new AtendimentoApiFactory(); using var client = factory.CreateClient(); var (id, v) = Criar(factory);
        client.DefaultRequestHeaders.Authorization = new("Bearer", AtendimentoTokenTestHelper.Gerar());
        v = erro switch
        {
            "os" => v with { OrdemServicoId = Guid.NewGuid() },
            "saga" => v with { SagaId = Guid.NewGuid() },
            "execucao" => v with { ExecucaoId = Guid.NewGuid() },
            "pedido" => v with { PedidoGeracaoId = Guid.NewGuid() },
            "valor" => v with { ValorTotal = 1m },
            _ => v with { CorrelationId = "outra" }
        };
        Assert.Equal(HttpStatusCode.Conflict, (await client.GetAsync(await Query(id, v))).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync($"/internal/orcamentos/{id}/aprovar", new { vinculo = v })).StatusCode);
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Empty(db.Outbox); Assert.Equal(StatusOrcamento.AguardandoAprovacao, db.Orcamentos.Single().Status);
    }
    [Fact]
    public async Task SemCredencialOuTokenAdmin_NaoAcessaAtendimento()
    {
        using var factory = new AtendimentoApiFactory(); using var client = factory.CreateClient(); var (id, v) = Criar(factory);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync(await Query(id, v))).StatusCode);
        await ApiTestHelper.Autenticar(client);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync($"/internal/orcamentos/{id}/aprovar", new { vinculo = v })).StatusCode);
        client.DefaultRequestHeaders.Authorization = new("Bearer", AtendimentoTokenTestHelper.Gerar());
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/orcamentos")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/pagamentos/" + Guid.NewGuid())).StatusCode);
    }
    [Theory]
    [InlineData("assinatura", HttpStatusCode.Unauthorized)]
    [InlineData("issuer", HttpStatusCode.Unauthorized)]
    [InlineData("audience", HttpStatusCode.Unauthorized)]
    [InlineData("expirado", HttpStatusCode.Unauthorized)]
    [InlineData("sub", HttpStatusCode.Unauthorized)]
    [InlineData("sem-assinatura", HttpStatusCode.Unauthorized)]
    [InlineData("role", HttpStatusCode.Forbidden)]
    public async Task CredencialForaDoEscopo_NaoAceita(string erro, HttpStatusCode status)
    {
        using var factory = new AtendimentoApiFactory(); using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new("Bearer", AtendimentoTokenTestHelper.Gerar(erro));
        Assert.Equal(status, (await client.GetAsync($"/internal/orcamentos/{Guid.NewGuid()}")).StatusCode);
    }
    [Fact]
    public async Task IntegracaoDesabilitada_RejeitaMesmoTokenDeServicoAssinado()
    {
        using var factory = new CobrancaApiFactory(); using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new("Bearer", AtendimentoTokenTestHelper.Gerar());
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync($"/internal/orcamentos/{Guid.NewGuid()}")).StatusCode);
    }
    private static (Guid, VinculoOrcamentoAtendimentoInput) Criar(CobrancaApiFactory factory)
    {
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var orcamento = Orcamento.Criar(Guid.NewGuid(), Guid.NewGuid(), "Revisao", [new(TipoItemOrcamento.Servico, Guid.NewGuid(), "Revisao", 1, 200m)]);
        orcamento.VincularSaga(Guid.NewGuid(), Guid.NewGuid(), "atendimento-teste"); db.Orcamentos.Add(orcamento); db.SaveChanges();
        return (orcamento.Id, new(orcamento.OrdemServicoId, orcamento.ExecucaoId, orcamento.SagaId!.Value, orcamento.PedidoGeracaoId!.Value, 200m, "atendimento-teste"));
    }
    private static async Task<string> Query(Guid id, VinculoOrcamentoAtendimentoInput vinculo)
    {
        using var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["ordemServicoId"] = vinculo.OrdemServicoId.ToString(),
            ["execucaoId"] = vinculo.ExecucaoId.ToString(),
            ["sagaId"] = vinculo.SagaId.ToString(),
            ["pedidoGeracaoId"] = vinculo.PedidoGeracaoId.ToString(),
            ["valorTotal"] = vinculo.ValorTotal.ToString(CultureInfo.InvariantCulture),
            ["correlationId"] = vinculo.CorrelationId
        });
        return $"/internal/orcamentos/{id}?" + await content.ReadAsStringAsync();
    }
}

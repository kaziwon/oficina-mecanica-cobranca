using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using OficinaMecanica.Cobranca.InterfaceAdapters.DTOs;

namespace OficinaMecanica.Cobranca.IntegrationTests;

internal static class ApiTestHelper
{
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public static async Task Autenticar(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/auth/login", new { username = "admin", password = "SenhaDosTestes@123" });
        response.EnsureSuccessStatusCode();
        var login = await response.Content.ReadFromJsonAsync<LoginResponseDto>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login!.Token);
    }

    public static object Orcamento(Guid? ordemServicoId = null, Guid? execucaoId = null) => new
    {
        ordemServicoId = ordemServicoId ?? Guid.NewGuid(),
        execucaoId = execucaoId ?? Guid.NewGuid(),
        diagnostico = "Trocar pastilhas",
        itens = new[]
        {
            new { tipo = "Servico", referenciaId = Guid.NewGuid(), nome = "Revisao", quantidade = 1, precoUnitario = 100.15m },
            new { tipo = "PecaInsumo", referenciaId = Guid.NewGuid(), nome = "Pastilha", quantidade = 2, precoUnitario = 80.50m }
        }
    };

    public static async Task<OrcamentoResponseDto> CriarOrcamento(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/orcamentos", Orcamento());
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<OrcamentoResponseDto>(Json))!;
    }
}

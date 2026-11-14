using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace OficinaMecanica.Cobranca.IntegrationTests;

public class AtendimentoApiFactory : CobrancaApiFactory
{
    public const string Secret = "chave-exclusiva-de-integracao-ordens-cobranca-nos-testes-1234567890";
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(new Dictionary<string, string?>
        { ["OrdensAtendimento:Enabled"] = "true", ["OrdensAtendimento:SecretKey"] = Secret }));
    }
}

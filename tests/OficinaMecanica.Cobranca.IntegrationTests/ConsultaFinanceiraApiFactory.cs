using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using OficinaMecanica.Cobranca.Infrastructure.Sources;
using OficinaMecanica.Cobranca.InterfaceAdapters.DataSources;

namespace OficinaMecanica.Cobranca.IntegrationTests;

public class ConsultaFinanceiraApiFactory : CobrancaApiFactory
{
    private readonly HttpClient _mock;
    public ConsultaFinanceiraApiFactory(HttpClient mock) { _mock = mock; }
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IConsultaFinanceiraSource>();
            services.AddScoped<IConsultaFinanceiraSource>(_ => new HttpConsultaFinanceiraMockSource(
                _mock, Options.Create(PagamentoMockHttpTests.Settings())));
        });
    }
}

using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using OficinaMecanica.Cobranca.Application.Gateways;
using OficinaMecanica.Cobranca.Application.UseCases.Auth;
using OficinaMecanica.Cobranca.Application.UseCases.Orcamentos;
using OficinaMecanica.Cobranca.Application.UseCases.Pagamentos;
using OficinaMecanica.Cobranca.Infrastructure.Persistence;
using OficinaMecanica.Cobranca.Infrastructure.Messaging;
using OficinaMecanica.Cobranca.Messaging;
using OficinaMecanica.Cobranca.Infrastructure.Settings;
using OficinaMecanica.Cobranca.Infrastructure.Sources;
using OficinaMecanica.Cobranca.InterfaceAdapters.Controllers;
using OficinaMecanica.Cobranca.InterfaceAdapters.DataSources;
using OficinaMecanica.Cobranca.InterfaceAdapters.Gateways;

namespace OficinaMecanica.Cobranca;

public static class DependencyInjection
{
    public static IServiceCollection AddCobranca(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>((provider, options) =>
        {
            var connection = provider.GetRequiredService<IConfiguration>().GetConnectionString("DefaultConnection");
            options.UseMySql(connection, new MySqlServerVersion(new Version(8, 4, 0)));
        });
        services.AddOptions<JwtSettings>().Bind(configuration.GetSection("JwtSettings"))
            .Validate(settings => Encoding.UTF8.GetByteCount(settings.SecretKey) >= 32,
                "Configure JwtSettings:SecretKey com pelo menos 32 bytes.")
            .Validate(settings => !string.IsNullOrWhiteSpace(settings.Issuer)
                && !string.IsNullOrWhiteSpace(settings.Audience), "Configure issuer e audience do JWT.")
            .ValidateOnStart();
        services.AddOptions<AdminUserSettings>().Bind(configuration.GetSection("AdminUserSettings"))
            .Validate(settings => !string.IsNullOrWhiteSpace(settings.Username)
                && !string.IsNullOrWhiteSpace(settings.Password), "Configure as credenciais administrativas.")
            .ValidateOnStart();
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtSettings>>((options, configured) =>
            {
                var jwt = configured.Value;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwt.Issuer,
                    ValidAudience = jwt.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SecretKey)),
                    ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
                    ClockSkew = TimeSpan.Zero
                };
            });
        services.AddAuthorization();
        Authentication.OrdensAtendimentoAuthentication.Adicionar(services, configuration);
        services.AddScoped<ObterOrcamentoAtendimentoUseCase>();
        services.AddScoped<ResponderOrcamentoAtendimentoUseCase>();
        services.AddScoped<OrcamentoAtendimentoCleanController>();
        services.AddOptions<PagamentoMockSettings>().Bind(configuration.GetSection("PagamentoMock"))
            .Validate(settings => !settings.Enabled || (Uri.TryCreate(settings.BaseUrl, UriKind.Absolute, out var uri)
                && uri.Scheme is "http" or "https" && string.IsNullOrEmpty(uri.UserInfo)
                && !string.IsNullOrWhiteSpace(settings.ClientId) && !string.IsNullOrWhiteSpace(settings.ClientSecret)
                && settings.MaxTentativas is >= 1 and <= 20 && settings.IntervaloRetentativaSegundos is >= 1 and <= 3600),
                "Configure URL, credenciais e retentativas do pagamento mock.").ValidateOnStart();
        services.AddHttpClient<IPagamentoProvedorSource, HttpPagamentoMockSource>((provider, http) =>
        {
            var settings = provider.GetRequiredService<IOptions<PagamentoMockSettings>>().Value;
            if (settings.Enabled) http.BaseAddress = new Uri(settings.BaseUrl.TrimEnd('/') + "/");
            http.Timeout = TimeSpan.FromSeconds(8);
        });
        services.AddScoped<IPagamentoProvedorGateway, PagamentoProvedorGateway>();
        services.AddScoped<IConsultaFinanceiraGateway, ConsultaFinanceiraGateway>();
        services.AddHttpClient<IConsultaFinanceiraSource, HttpConsultaFinanceiraMockSource>((provider, http) =>
        {
            var settings = provider.GetRequiredService<IOptions<PagamentoMockSettings>>().Value;
            if (settings.Enabled) http.BaseAddress = new Uri(settings.BaseUrl.TrimEnd('/') + "/");
            http.Timeout = TimeSpan.FromSeconds(8);
        }).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false });
        services.AddScoped<ReconciliarPagamentoUseCase>();
        services.AddScoped<ReconciliarEstornoUseCase>();
        services.AddScoped<IPagamentoIntegracaoDataSource, EfPagamentoIntegracaoDataSource>();
        services.AddScoped<IPagamentoIntegracaoGateway, PagamentoIntegracaoGateway>();
        services.AddScoped<IPagamentoProcessamentoDataSource, EfPagamentoProcessamentoDataSource>();
        services.AddScoped<IPagamentoProcessamentoGateway, PagamentoProcessamentoGateway>();
        services.AddScoped<SolicitarPagamentoUseCase>();
        services.AddScoped<ProcessarPagamentoUseCase>();
        services.AddScoped<IEstornoDataSource, EfEstornoDataSource>();
        services.AddScoped<IEstornoGateway, EstornoGateway>();
        services.AddScoped<IEstornoProvedorGateway, EstornoProvedorGateway>();
        services.AddScoped<SolicitarEstornoUseCase>();
        services.AddScoped<ProcessarEstornoUseCase>();
        services.AddScoped<ObterEstornoUseCase>();
        services.AddScoped<EstornosCleanController>();
        services.AddHttpClient<IEstornoProvedorSource, HttpEstornoMockSource>((provider, http) =>
        {
            var settings = provider.GetRequiredService<IOptions<PagamentoMockSettings>>().Value;
            if (settings.Enabled) http.BaseAddress = new Uri(settings.BaseUrl.TrimEnd('/') + "/");
            http.Timeout = TimeSpan.FromSeconds(8);
        });
        services.AddHostedService<EstornoWorker>();
        services.AddHostedService<PagamentoWorker>();
        services.Configure<MessagingSettings>(configuration.GetSection("Messaging"));
        services.AddSingleton<SqsTransport>();
        services.AddHostedService<IntegracaoWorker>();
        services.AddScoped<IOutboxDataSource, EfOutboxDataSource>();
        services.AddScoped<IOutboxGateway, OutboxGateway>();
        services.AddScoped<IOrcamentoIntegracaoDataSource, EfOrcamentoIntegracaoDataSource>();
        services.AddScoped<IOrcamentoIntegracaoGateway, OrcamentoIntegracaoGateway>();
        services.AddScoped<ProcessarPedidoOrcamentoUseCase>();
        services.AddScoped<IntegracaoCleanController>();
        services.AddScoped<IAdminUserSource, AdminUserSettingsSource>();
        services.AddScoped<ITokenSource, JwtTokenSource>();
        services.AddScoped<IOrcamentoDataSource, EfOrcamentoDataSource>();
        services.AddScoped<IPagamentoDataSource, EfPagamentoDataSource>();
        services.AddScoped<IAdminUserGateway, AdminUserGateway>();
        services.AddScoped<ITokenGateway, TokenGateway>();
        services.AddScoped<IOrcamentoGateway, OrcamentoGateway>();
        services.AddScoped<IPagamentoGateway, PagamentoGateway>();
        services.AddScoped<LoginUseCase>();
        services.AddScoped<CriarOrcamentoUseCase>();
        services.AddScoped<ObterOrcamentoPorIdUseCase>();
        services.AddScoped<ObterTodosOrcamentosUseCase>();
        services.AddScoped<AprovarOrcamentoUseCase>();
        services.AddScoped<RecusarOrcamentoUseCase>();
        services.AddScoped<RegistrarPagamentoPendenteUseCase>();
        services.AddScoped<ObterPagamentoPorIdUseCase>();
        services.AddScoped<AuthCleanController>();
        services.AddScoped<OrcamentosCleanController>();
        services.AddScoped<PagamentosCleanController>();
        return services;
    }
}

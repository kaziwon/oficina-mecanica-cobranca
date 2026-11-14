using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using OficinaMecanica.Cobranca.Infrastructure.Settings;

namespace OficinaMecanica.Cobranca.Authentication;

public static class OrdensAtendimentoAuthentication
{
    public const string Scheme = "OrdensAtendimento";
    public static void Adicionar(IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<OrdensAtendimentoSettings>().Bind(configuration.GetSection("OrdensAtendimento"))
            .Validate(settings => !settings.Enabled || (!string.IsNullOrWhiteSpace(settings.SecretKey)
                && Encoding.UTF8.GetByteCount(settings.SecretKey) >= 32 && settings.SecretKey != configuration["JwtSettings:SecretKey"]),
                "Configure uma chave exclusiva de pelo menos 32 bytes para OrdensAtendimento.").ValidateOnStart();
        services.AddAuthentication().AddJwtBearer(Scheme);
        services.AddOptions<JwtBearerOptions>(Scheme).Configure<IOptions<OrdensAtendimentoSettings>>((options, configured) =>
        {
            var settings = configured.Value;
            options.MapInboundClaims = false;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = "OficinaMecanica.Ordens.Integracao",
                ValidAudience = "OficinaMecanica.Cobranca.Orcamentos",
                IssuerSigningKey = settings.Enabled ? new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.SecretKey)) : null,
                ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
                ClockSkew = TimeSpan.Zero,
                RoleClaimType = "role",
                NameClaimType = "sub"
            };
            options.Events = new JwtBearerEvents
            {
                OnTokenValidated = context =>
            {
                var identidades = context.Principal?.FindAll("sub").ToList();
                if (!settings.Enabled || identidades?.Count != 1 || identidades[0].Value != "ordens") context.Fail("Integracao indisponivel.");
                return Task.CompletedTask;
            }
            };
        });
    }
}

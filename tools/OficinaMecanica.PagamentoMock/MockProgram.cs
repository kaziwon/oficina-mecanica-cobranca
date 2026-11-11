using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;

namespace OficinaMecanica.PagamentoMock;

public partial class MockProgram
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.Logging.ClearProviders(); builder.Logging.AddJsonConsole(options => options.IncludeScopes = true);
        builder.Services.ConfigureHttpJsonOptions(options => options.SerializerOptions.UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow);
        builder.Services.AddOptions<MockSettings>().Bind(builder.Configuration.GetSection("Mock"))
            .Validate(settings => settings.Resultado is "approved" or "rejected" or "unavailable"
                && !string.IsNullOrWhiteSpace(settings.ClientId) && !string.IsNullOrWhiteSpace(settings.ClientSecret)
                && !string.IsNullOrWhiteSpace(settings.Token), "Configure o mock local de pagamento.").ValidateOnStart();
        builder.Services.AddSingleton<MockPaymentStore>();
        var app = builder.Build();
        app.Services.GetRequiredService<MockPaymentStore>().Initialize();
        app.MapGet("/health", () => Results.Ok(new { status = "Healthy", mock = true }));
        app.MapPost("/oauth/token", (TokenRequest input, IOptions<MockSettings> configured) =>
        {
            var settings = configured.Value;
            return input.GrantType != "client_credentials" || input.ClientId != settings.ClientId || input.ClientSecret != settings.ClientSecret
                ? Results.Unauthorized() : Results.Ok(new { access_token = settings.Token, token_type = "Bearer", expires_in = 3600 });
        });
        app.MapPost("/payments", (PaymentRequest input, HttpContext context, IOptions<MockSettings> configured, MockPaymentStore store) =>
        {
            var settings = configured.Value;
            if (context.Request.Headers.Authorization != "Bearer " + settings.Token) return Results.Unauthorized();
            if (!Guid.TryParse(context.Request.Headers["X-Idempotency-Key"], out var key) || key == Guid.Empty
                || key != input.ExternalReference || input.Amount <= 0 || input.Amount > 999999999.99m
                || decimal.Round(input.Amount, 2) != input.Amount || input.Currency != "BRL")
                return Results.BadRequest(new { message = "Pedido de pagamento mock invalido." });
            var correlation = context.Request.Headers["X-Correlation-ID"].ToString();
            if (string.IsNullOrWhiteSpace(correlation) || correlation.Length > 100 || correlation.Any(char.IsControl))
                return Results.BadRequest(new { message = "Correlacao invalida." });
            var existing = store.Find(key);
            if (existing is not null && existing.Amount != input.Amount) return Results.Conflict();
            if (existing is null && settings.Resultado == "unavailable") return Results.StatusCode(503);
            try { return Response(existing ?? store.GetOrCreate(key, input.Amount, settings.Resultado, correlation)); }
            catch (InvalidOperationException) { return Results.Conflict(); }
        });
        app.MapGet("/payments/{id:guid}", (Guid id, HttpContext context, IOptions<MockSettings> configured, MockPaymentStore store) =>
        {
            if (context.Request.Headers.Authorization != "Bearer " + configured.Value.Token) return Results.Unauthorized();
            var saved = store.Find(id);
            return saved is null ? Results.NotFound() : Response(saved);
        });
        app.Run();
    }

    private static IResult Response(PaymentRecord saved) => Results.Ok(new
    { id = saved.Id, external_reference = saved.ExternalReference, transaction_amount = saved.Amount, currency_id = "BRL", status = saved.Status, mock = true });
}

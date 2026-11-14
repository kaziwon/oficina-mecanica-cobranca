using Microsoft.Extensions.Options;
namespace OficinaMecanica.PagamentoMock;

public static class MockRefundEndpoints
{
    public static void Map(WebApplication app)
    {
        app.MapPost("/refunds", (RefundRequest input, HttpContext context, IOptions<MockSettings> configured, MockRefundStore store) =>
        {
            var settings = configured.Value;
            if (context.Request.Headers.Authorization != "Bearer " + settings.Token) return Results.Unauthorized();
            var correlation = context.Request.Headers["X-Correlation-ID"].ToString();
            if (!Guid.TryParse(context.Request.Headers["X-Idempotency-Key"], out var id) || id == Guid.Empty || id != input.ExternalReference
                || string.IsNullOrWhiteSpace(input.PaymentId) || input.PaymentId.Length > 100 || input.PaymentId.Any(char.IsControl)
                || input.Amount <= 0 || input.Amount > 999999999.99m || decimal.Round(input.Amount, 2) != input.Amount || input.Currency != "BRL"
                || string.IsNullOrWhiteSpace(correlation) || correlation.Length > 100 || correlation.Any(char.IsControl)) return Results.BadRequest();
            var existing = store.Find(id);
            var lost = settings.ResultadoEstorno == "refunded_response_lost";
            if (existing is not null) return existing.PaymentId != input.PaymentId || existing.Amount != input.Amount
                ? Results.Conflict() : lost ? Results.StatusCode(503) : Response(existing);
            if (settings.ResultadoEstorno == "unavailable") return Results.StatusCode(503);
            try
            {
                var saved = store.GetOrCreate(id, input.PaymentId, input.Amount, lost ? "refunded" : settings.ResultadoEstorno, correlation);
                return lost ? Results.StatusCode(503) : Response(saved);
            }
            catch (InvalidOperationException) { return Results.Conflict(); }
        });
        app.MapGet("/refunds/{id:guid}", (Guid id, HttpContext context, IOptions<MockSettings> configured, MockRefundStore store) =>
        {
            if (context.Request.Headers.Authorization != "Bearer " + configured.Value.Token) return Results.Unauthorized();
            var saved = store.Find(id); return saved is null ? Results.NotFound() : Response(saved);
        });
    }
    private static IResult Response(RefundRecord saved) => Results.Ok(new
    {
        id = saved.Id,
        external_reference = saved.ExternalReference,
        payment_id = saved.PaymentId,
        transaction_amount = saved.Amount,
        currency_id = "BRL",
        status = saved.Status,
        mock = true
    });
}

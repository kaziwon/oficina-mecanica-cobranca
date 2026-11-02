using Microsoft.EntityFrameworkCore;
using OficinaMecanica.Cobranca.Application.Exceptions;
using OficinaMecanica.Cobranca.Domain.Exceptions;

namespace OficinaMecanica.Cobranca.Middleware;

public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;
    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
    {
        _next = next; _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try { await _next(context); }
        catch (Exception exception) when (exception is ValidacaoException or DadosInvalidosException)
        {
            await EscreverErro(context, 400, exception.Message);
        }
        catch (Exception exception) when (exception is RegraNegocioException or ConflitoException)
        {
            _logger.LogWarning(exception, "Conflito ao processar cobranca");
            await EscreverErro(context, 409, exception.Message);
        }
        catch (DbUpdateException exception)
        {
            _logger.LogError(exception, "Falha ao persistir cobranca");
            await EscreverErro(context, 500, "Nao foi possivel persistir os dados.");
        }
    }

    private static Task EscreverErro(HttpContext context, int status, string mensagem)
    {
        context.Response.StatusCode = status;
        return context.Response.WriteAsJsonAsync(new { message = mensagem, correlationId = context.TraceIdentifier });
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OficinaMecanica.Cobranca.InterfaceAdapters.Controllers;
using OficinaMecanica.Cobranca.InterfaceAdapters.DTOs;
namespace OficinaMecanica.Cobranca.Controllers;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/pagamentos/{pagamentoId:guid}/estorno")]
public class EstornosController : ControllerBase
{
    private readonly EstornosCleanController _controller;
    public EstornosController(EstornosCleanController controller) { _controller = controller; }
    [HttpGet]
    [ProducesResponseType(typeof(EstornoResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult Get(Guid pagamentoId)
    {
        var resultado = _controller.Obter(pagamentoId);
        return resultado is null ? NotFound() : Ok(resultado);
    }

    [HttpPost("reconciliar")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Reconciliar(Guid pagamentoId,
        [FromBody(EmptyBodyBehavior = Microsoft.AspNetCore.Mvc.ModelBinding.EmptyBodyBehavior.Allow)] ReconciliacaoRequestDto? request,
        CancellationToken cancellationToken)
    {
        var resultado = await _controller.Reconciliar(pagamentoId, cancellationToken);
        return resultado is null ? NotFound() : NoContent();
    }
}

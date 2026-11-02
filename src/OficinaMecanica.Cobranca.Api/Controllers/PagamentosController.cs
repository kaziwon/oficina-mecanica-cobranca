using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OficinaMecanica.Cobranca.InterfaceAdapters.Controllers;
using OficinaMecanica.Cobranca.InterfaceAdapters.DTOs;

namespace OficinaMecanica.Cobranca.Controllers;

[ApiController]
[Route("api/pagamentos")]
[Authorize(Roles = "Admin")]
public class PagamentosController : ControllerBase
{
    private readonly PagamentosCleanController _controller;
    public PagamentosController(PagamentosCleanController controller) { _controller = controller; }

    [HttpPost("/api/orcamentos/{orcamentoId:guid}/pagamento")]
    [ProducesResponseType(typeof(PagamentoResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public IActionResult RegistrarPendente(Guid orcamentoId)
    {
        var pagamento = _controller.RegistrarPendente(orcamentoId);
        return pagamento is null ? NotFound() : Ok(pagamento);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(PagamentoResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult GetById(Guid id)
    {
        var pagamento = _controller.ObterPorId(id);
        return pagamento is null ? NotFound() : Ok(pagamento);
    }
}

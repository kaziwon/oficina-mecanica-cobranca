using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OficinaMecanica.Cobranca.Authentication;
using OficinaMecanica.Cobranca.Controllers.DTOs;
using OficinaMecanica.Cobranca.Controllers.Mappers;
using OficinaMecanica.Cobranca.InterfaceAdapters.Controllers;
using OficinaMecanica.Cobranca.InterfaceAdapters.DTOs;

namespace OficinaMecanica.Cobranca.Controllers;

[ApiController]
[Route("internal/orcamentos/{id:guid}")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
[Authorize(AuthenticationSchemes = OrdensAtendimentoAuthentication.Scheme, Roles = "OrcamentosCliente")]
public class OrcamentosAtendimentoController(OrcamentoAtendimentoCleanController controller) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(OrcamentoResponseDto), StatusCodes.Status200OK)]
    public IActionResult Get(Guid id, [FromQuery] VinculoOrcamentoAtendimentoRequestDto vinculo)
    {
        var response = controller.Obter(id, vinculo.ParaCleanDto());
        return response is null ? NotFound() : Ok(response);
    }
    [HttpPost("aprovar")]
    public IActionResult Aprovar(Guid id, [FromBody] RespostaOrcamentoAtendimentoRequestDto request)
        => controller.Responder(id, request.Vinculo.ParaCleanDto(), true, request.Motivo) ? NoContent() : NotFound();
    [HttpPost("recusar")]
    public IActionResult Recusar(Guid id, [FromBody] RespostaOrcamentoAtendimentoRequestDto request)
        => controller.Responder(id, request.Vinculo.ParaCleanDto(), false, request.Motivo) ? NoContent() : NotFound();
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OficinaMecanica.Cobranca.Controllers.Mappers;
using OficinaMecanica.Cobranca.Domain.Entities;
using OficinaMecanica.Cobranca.InterfaceAdapters.Controllers;
using OficinaMecanica.Cobranca.InterfaceAdapters.DTOs;
using HttpDtos = OficinaMecanica.Cobranca.Controllers.DTOs;

namespace OficinaMecanica.Cobranca.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class OrcamentosController : ControllerBase
{
    private readonly OrcamentosCleanController _controller;
    public OrcamentosController(OrcamentosCleanController controller) { _controller = controller; }

    [HttpPost]
    [ProducesResponseType(typeof(OrcamentoResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public IActionResult Post([FromBody] HttpDtos.CriarOrcamentoRequestDto dto)
    {
        var orcamento = _controller.Criar(dto.ParaCleanDto());
        return CreatedAtAction(nameof(GetById), new { id = orcamento.Id }, orcamento);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(OrcamentoResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult GetById(Guid id)
    {
        var orcamento = _controller.ObterPorId(id);
        return orcamento is null ? NotFound() : Ok(orcamento);
    }

    [HttpGet]
    [ProducesResponseType(typeof(List<OrcamentoResponseDto>), StatusCodes.Status200OK)]
    public IActionResult Get([FromQuery] StatusOrcamento? status, [FromQuery] Guid? ordemServicoId)
        => Ok(_controller.ObterTodos(status, ordemServicoId));

    [HttpPost("{id:guid}/aprovar")]
    [ProducesResponseType(typeof(OrcamentoResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public IActionResult Aprovar(Guid id)
    {
        var orcamento = _controller.Aprovar(id);
        return orcamento is null ? NotFound() : Ok(orcamento);
    }

    [HttpPost("{id:guid}/recusar")]
    [ProducesResponseType(typeof(OrcamentoResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public IActionResult Recusar(Guid id, [FromBody] HttpDtos.RecusarOrcamentoRequestDto dto)
    {
        var orcamento = _controller.Recusar(id, dto.ParaCleanDto());
        return orcamento is null ? NotFound() : Ok(orcamento);
    }
}

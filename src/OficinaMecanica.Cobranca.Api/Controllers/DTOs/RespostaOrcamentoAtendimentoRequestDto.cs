using System.ComponentModel.DataAnnotations;

namespace OficinaMecanica.Cobranca.Controllers.DTOs;

public class RespostaOrcamentoAtendimentoRequestDto
{
    [Required]
    public VinculoOrcamentoAtendimentoRequestDto Vinculo { get; set; } = null!;
    [StringLength(1000)]
    public string? Motivo { get; set; }
}

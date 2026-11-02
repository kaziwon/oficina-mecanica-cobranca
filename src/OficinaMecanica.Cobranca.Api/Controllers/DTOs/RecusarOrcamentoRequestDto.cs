using System.ComponentModel.DataAnnotations;

namespace OficinaMecanica.Cobranca.Controllers.DTOs;

public class RecusarOrcamentoRequestDto
{
    [Required, StringLength(1000)]
    public string Motivo { get; set; } = string.Empty;
}

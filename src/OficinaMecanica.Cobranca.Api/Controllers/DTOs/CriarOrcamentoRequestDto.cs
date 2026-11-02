using System.ComponentModel.DataAnnotations;

namespace OficinaMecanica.Cobranca.Controllers.DTOs;

public class CriarOrcamentoRequestDto : IValidatableObject
{
    public Guid OrdemServicoId { get; set; }
    public Guid ExecucaoId { get; set; }
    [Required, StringLength(2000)]
    public string Diagnostico { get; set; } = string.Empty;
    [Required, MinLength(1), MaxLength(100)]
    public List<ItemOrcamentoRequestDto> Itens { get; set; } = [];

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Itens is not null && Itens.Any(item => item is null))
            yield return new ValidationResult("A lista nao pode conter itens nulos.", [nameof(Itens)]);
    }
}

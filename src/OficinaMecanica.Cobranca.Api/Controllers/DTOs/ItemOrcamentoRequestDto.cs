using System.ComponentModel.DataAnnotations;
using OficinaMecanica.Cobranca.Domain.Entities;

namespace OficinaMecanica.Cobranca.Controllers.DTOs;

public class ItemOrcamentoRequestDto
{
    [EnumDataType(typeof(TipoItemOrcamento))]
    public TipoItemOrcamento Tipo { get; set; }
    public Guid ReferenciaId { get; set; }
    [Required, StringLength(150)]
    public string Nome { get; set; } = string.Empty;
    [Range(1, 10000)]
    public int Quantidade { get; set; }
    public decimal PrecoUnitario { get; set; }
}

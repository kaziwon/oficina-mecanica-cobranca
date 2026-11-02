using OficinaMecanica.Cobranca.Domain.Entities;

namespace OficinaMecanica.Cobranca.InterfaceAdapters.DTOs;

public class ItemOrcamentoResponseDto
{
    public TipoItemOrcamento Tipo { get; set; }
    public Guid ReferenciaId { get; set; }
    public string Nome { get; set; } = string.Empty;
    public int Quantidade { get; set; }
    public decimal PrecoUnitario { get; set; }
    public decimal Total { get; set; }
}

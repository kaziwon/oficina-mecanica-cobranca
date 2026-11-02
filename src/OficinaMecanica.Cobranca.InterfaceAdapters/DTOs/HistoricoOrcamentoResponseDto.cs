using OficinaMecanica.Cobranca.Domain.Entities;

namespace OficinaMecanica.Cobranca.InterfaceAdapters.DTOs;

public class HistoricoOrcamentoResponseDto
{
    public StatusOrcamento Status { get; set; }
    public DateTime RegistradoEm { get; set; }
    public string? Motivo { get; set; }
}

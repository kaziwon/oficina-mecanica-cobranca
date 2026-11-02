using OficinaMecanica.Cobranca.Domain.Entities;

namespace OficinaMecanica.Cobranca.Application.Outputs.Orcamentos;

public class HistoricoOrcamentoOutput
{
    public StatusOrcamento Status { get; set; }
    public DateTime RegistradoEm { get; set; }
    public string? Motivo { get; set; }
}

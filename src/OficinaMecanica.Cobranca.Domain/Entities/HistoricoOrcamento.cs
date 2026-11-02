namespace OficinaMecanica.Cobranca.Domain.Entities;

public class HistoricoOrcamento
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid OrcamentoId { get; private set; }
    public StatusOrcamento Status { get; private set; }
    public DateTime RegistradoEm { get; private set; }
    public string? Motivo { get; private set; }

    private HistoricoOrcamento() { }

    internal HistoricoOrcamento(StatusOrcamento status, DateTime registradoEm, string? motivo = null)
    {
        Status = status;
        RegistradoEm = registradoEm;
        Motivo = motivo;
    }
}

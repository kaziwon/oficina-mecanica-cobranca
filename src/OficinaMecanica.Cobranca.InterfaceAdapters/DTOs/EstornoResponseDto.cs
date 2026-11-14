using OficinaMecanica.Cobranca.Domain.Entities;
namespace OficinaMecanica.Cobranca.InterfaceAdapters.DTOs;

public class EstornoResponseDto
{
    public Guid Id { get; set; }
    public Guid PagamentoId { get; set; }
    public Guid OrdemServicoId { get; set; }
    public StatusEstorno Status { get; set; }
    public decimal Valor { get; set; }
    public string Moeda { get; set; } = string.Empty;
    public string? ProvedorId { get; set; }
    public string? CodigoFalha { get; set; }
    public int Tentativas { get; set; }
    public int ConsultasReconciliacao { get; set; }
    public DateTime? UltimaConsultaReconciliacaoEm { get; set; }
    public DateTime? ReconciliadoEm { get; set; }
    public DateTime CriadoEm { get; set; }
    public DateTime? FinalizadoEm { get; set; }
}

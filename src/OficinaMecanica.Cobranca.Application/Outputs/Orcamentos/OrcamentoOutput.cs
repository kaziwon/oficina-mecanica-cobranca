using OficinaMecanica.Cobranca.Domain.Entities;

namespace OficinaMecanica.Cobranca.Application.Outputs.Orcamentos;

public class OrcamentoOutput
{
    public Guid Id { get; set; }
    public Guid OrdemServicoId { get; set; }
    public Guid ExecucaoId { get; set; }
    public string Diagnostico { get; set; } = string.Empty;
    public StatusOrcamento Status { get; set; }
    public decimal ValorTotal { get; set; }
    public DateTime CriadoEm { get; set; }
    public DateTime? RespondidoEm { get; set; }
    public string? MotivoRecusa { get; set; }
    public Guid? PagamentoId { get; set; }
    public long Versao { get; set; }
    public List<ItemOrcamentoOutput> Itens { get; set; } = [];
    public List<HistoricoOrcamentoOutput> Historico { get; set; } = [];
}

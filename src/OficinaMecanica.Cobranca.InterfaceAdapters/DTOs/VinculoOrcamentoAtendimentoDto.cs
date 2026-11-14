namespace OficinaMecanica.Cobranca.InterfaceAdapters.DTOs;

public class VinculoOrcamentoAtendimentoDto
{
    public Guid OrdemServicoId { get; set; }
    public Guid ExecucaoId { get; set; }
    public Guid SagaId { get; set; }
    public Guid PedidoGeracaoId { get; set; }
    public decimal ValorTotal { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
}

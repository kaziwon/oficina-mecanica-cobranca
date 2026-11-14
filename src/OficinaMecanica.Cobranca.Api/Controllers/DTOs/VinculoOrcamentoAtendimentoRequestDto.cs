using System.ComponentModel.DataAnnotations;

namespace OficinaMecanica.Cobranca.Controllers.DTOs;

public class VinculoOrcamentoAtendimentoRequestDto
{
    public Guid OrdemServicoId { get; set; }
    public Guid ExecucaoId { get; set; }
    public Guid SagaId { get; set; }
    public Guid PedidoGeracaoId { get; set; }
    public decimal ValorTotal { get; set; }
    [Required, StringLength(100)]
    public string CorrelationId { get; set; } = string.Empty;
}

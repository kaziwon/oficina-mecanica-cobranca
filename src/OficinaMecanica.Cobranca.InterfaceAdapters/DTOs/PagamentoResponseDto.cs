using OficinaMecanica.Cobranca.Domain.Entities;

namespace OficinaMecanica.Cobranca.InterfaceAdapters.DTOs;

public class PagamentoResponseDto
{
    public Guid Id { get; set; }
    public Guid OrcamentoId { get; set; }
    public Guid OrdemServicoId { get; set; }
    public decimal Valor { get; set; }
    public string Moeda { get; set; } = string.Empty;
    public StatusPagamento Status { get; set; }
    public DateTime CriadoEm { get; set; }
}

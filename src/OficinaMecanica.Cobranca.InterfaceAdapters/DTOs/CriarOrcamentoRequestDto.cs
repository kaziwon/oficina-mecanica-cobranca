namespace OficinaMecanica.Cobranca.InterfaceAdapters.DTOs;

public class CriarOrcamentoRequestDto
{
    public Guid OrdemServicoId { get; set; }
    public Guid ExecucaoId { get; set; }
    public string Diagnostico { get; set; } = string.Empty;
    public List<ItemOrcamentoRequestDto> Itens { get; set; } = [];
}

namespace OficinaMecanica.Cobranca.Application.Inputs.Orcamentos;

public class CriarOrcamentoInput
{
    public Guid OrdemServicoId { get; set; }
    public Guid ExecucaoId { get; set; }
    public string Diagnostico { get; set; } = string.Empty;
    public List<ItemOrcamentoInput> Itens { get; set; } = [];
}

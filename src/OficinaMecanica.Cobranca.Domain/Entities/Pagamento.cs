namespace OficinaMecanica.Cobranca.Domain.Entities;

public class Pagamento
{
    public Guid Id { get; private set; }
    public Guid OrcamentoId { get; private set; }
    public Guid OrdemServicoId { get; private set; }
    public decimal Valor { get; private set; }
    public string Moeda { get; private set; } = "BRL";
    public StatusPagamento Status { get; private set; } = StatusPagamento.Pendente;
    public DateTime CriadoEm { get; private set; }

    private Pagamento() { }

    internal Pagamento(Guid orcamentoId, Guid ordemServicoId, decimal valor)
    {
        Id = Guid.NewGuid();
        OrcamentoId = orcamentoId;
        OrdemServicoId = ordemServicoId;
        Valor = valor;
        CriadoEm = DateTime.UtcNow;
    }
}

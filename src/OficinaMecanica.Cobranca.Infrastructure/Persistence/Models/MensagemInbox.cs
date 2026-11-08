namespace OficinaMecanica.Cobranca.Infrastructure.Persistence.Models;

public class MensagemInbox
{
    public Guid Id { get; set; }
    public Guid SagaId { get; set; }
    public string Hash { get; set; } = string.Empty;
    public DateTime ProcessadaEm { get; set; }
}

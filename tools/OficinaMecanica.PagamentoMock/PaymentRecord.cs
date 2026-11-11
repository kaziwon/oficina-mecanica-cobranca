namespace OficinaMecanica.PagamentoMock;

public record PaymentRecord(string Id, Guid ExternalReference, decimal Amount, string Status, string CorrelationId);

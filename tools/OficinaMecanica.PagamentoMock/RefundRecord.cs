namespace OficinaMecanica.PagamentoMock;

public record RefundRecord(string Id, Guid ExternalReference, string PaymentId, decimal Amount, string Status);

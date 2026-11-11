namespace OficinaMecanica.Cobranca.Application.Inputs.Pagamentos;

public record SolicitacaoPagamentoProvedor(Guid PagamentoId, decimal Valor, string Moeda, string CorrelationId);

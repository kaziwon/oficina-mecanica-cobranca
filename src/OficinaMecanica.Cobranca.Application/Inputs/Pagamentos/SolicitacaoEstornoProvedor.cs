namespace OficinaMecanica.Cobranca.Application.Inputs.Pagamentos;

public record SolicitacaoEstornoProvedor(Guid EstornoId, string PagamentoProvedorId, decimal Valor, string Moeda, string CorrelationId);

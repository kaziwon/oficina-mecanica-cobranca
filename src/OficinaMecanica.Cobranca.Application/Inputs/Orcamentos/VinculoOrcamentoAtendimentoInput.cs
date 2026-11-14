namespace OficinaMecanica.Cobranca.Application.Inputs.Orcamentos;

public record VinculoOrcamentoAtendimentoInput(Guid OrdemServicoId, Guid ExecucaoId, Guid SagaId,
    Guid PedidoGeracaoId, decimal ValorTotal, string CorrelationId);

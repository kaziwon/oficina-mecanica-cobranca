using OficinaMecanica.Cobranca.Application.Exceptions;
using OficinaMecanica.Cobranca.Application.Inputs.Orcamentos;
using OficinaMecanica.Cobranca.Domain.Entities;

namespace OficinaMecanica.Cobranca.Application.Validators;

public static class OrcamentoAtendimentoValidator
{
    public static void Validar(Orcamento orcamento, VinculoOrcamentoAtendimentoInput vinculo)
    {
        if (vinculo.OrdemServicoId == Guid.Empty || vinculo.ExecucaoId == Guid.Empty || vinculo.SagaId == Guid.Empty
            || vinculo.PedidoGeracaoId == Guid.Empty || vinculo.ValorTotal <= 0 || vinculo.ValorTotal > 999999999.99m
            || decimal.Round(vinculo.ValorTotal, 2) != vinculo.ValorTotal
            || string.IsNullOrWhiteSpace(vinculo.CorrelationId) || vinculo.CorrelationId.Length > 100 || vinculo.CorrelationId.Any(char.IsControl))
            throw new ValidacaoException("Vinculo do orcamento invalido.");
        if (orcamento.OrdemServicoId != vinculo.OrdemServicoId || orcamento.ExecucaoId != vinculo.ExecucaoId
            || orcamento.SagaId != vinculo.SagaId || orcamento.PedidoGeracaoId != vinculo.PedidoGeracaoId
            || orcamento.ValorTotal != vinculo.ValorTotal || orcamento.CorrelationId != vinculo.CorrelationId)
            throw new ConflitoException("O orcamento nao pertence ao pedido informado por Ordens.");
    }
}

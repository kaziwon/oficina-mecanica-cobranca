using OficinaMecanica.Cobranca.Application.Exceptions;
using OficinaMecanica.Cobranca.Application.Messaging;
using OficinaMecanica.Cobranca.Domain.Entities;

namespace OficinaMecanica.Cobranca.Application.Mappers.Orcamentos;

public static class RespostaOrcamentoMensagemMapper
{
    public static MensagemIntegracao? Mapear(Orcamento orcamento)
    {
        if (!orcamento.SagaId.HasValue) return null;
        if (!orcamento.PedidoGeracaoId.HasValue || string.IsNullOrWhiteSpace(orcamento.CorrelationId)
            || !orcamento.RespondidoEm.HasValue
            || orcamento.Status is not (StatusOrcamento.Aprovado or StatusOrcamento.Recusado))
            throw new ValidacaoException("O orcamento nao possui os dados necessarios para notificar a Saga.");
        return new MensagemIntegracao
        {
            MessageId = Guid.NewGuid(),
            Type = orcamento.Status == StatusOrcamento.Aprovado ? "OrcamentoAprovado" : "OrcamentoRecusado",
            SagaId = orcamento.SagaId.Value,
            CausationId = orcamento.PedidoGeracaoId,
            CorrelationId = orcamento.CorrelationId,
            OccurredAt = orcamento.RespondidoEm.Value,
            Payload = new DadosMensagem
            {
                OrdemServicoId = orcamento.OrdemServicoId,
                ExecucaoId = orcamento.ExecucaoId,
                OrcamentoId = orcamento.Id,
                ValorTotal = orcamento.ValorTotal,
                OrcamentoCriadoEm = DateTime.SpecifyKind(orcamento.CriadoEm, DateTimeKind.Utc),
                Motivo = orcamento.MotivoRecusa
            }
        };
    }
}

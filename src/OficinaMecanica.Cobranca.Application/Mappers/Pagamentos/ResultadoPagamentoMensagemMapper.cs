using OficinaMecanica.Cobranca.Application.Messaging;
using OficinaMecanica.Cobranca.Domain.Entities;

namespace OficinaMecanica.Cobranca.Application.Mappers.Pagamentos;

public static class ResultadoPagamentoMensagemMapper
{
    public static MensagemIntegracao? Mapear(Pagamento pagamento)
    {
        if (pagamento.Status == StatusPagamento.Pendente) return null;
        return new MensagemIntegracao
        {
            MessageId = Guid.NewGuid(),
            Type = pagamento.Status switch
            { StatusPagamento.Confirmado => "PagamentoConfirmado", StatusPagamento.Recusado => "PagamentoRecusado", _ => "FalhaProcessamentoPagamento" },
            SagaId = pagamento.SagaId!.Value,
            CausationId = pagamento.PedidoPagamentoId,
            CorrelationId = pagamento.CorrelationId!,
            OccurredAt = pagamento.FinalizadoEm!.Value,
            Payload = new DadosMensagem
            {
                OrdemServicoId = pagamento.OrdemServicoId,
                ExecucaoId = pagamento.ExecucaoId,
                OrcamentoId = pagamento.OrcamentoId,
                PagamentoId = pagamento.Id,
                ValorTotal = pagamento.Valor,
                Moeda = pagamento.Moeda,
                ProvedorId = pagamento.ProvedorId,
                Codigo = pagamento.CodigoFalha,
                Motivo = pagamento.Status switch
                {
                    StatusPagamento.Confirmado => null,
                    StatusPagamento.Recusado => "Pagamento recusado pelo provedor simulado.",
                    _ => "Nao foi possivel confirmar o resultado do pagamento. Reconciliacao necessaria."
                }
            }
        };
    }
}

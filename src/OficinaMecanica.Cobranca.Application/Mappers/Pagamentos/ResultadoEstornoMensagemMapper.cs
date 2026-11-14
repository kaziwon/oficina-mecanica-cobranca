using OficinaMecanica.Cobranca.Application.Messaging;
using OficinaMecanica.Cobranca.Domain.Entities;
namespace OficinaMecanica.Cobranca.Application.Mappers.Pagamentos;

public static class ResultadoEstornoMensagemMapper
{
    public static MensagemIntegracao? Mapear(Estorno estorno)
    {
        if (estorno.Status == StatusEstorno.Pendente) return null;
        return new MensagemIntegracao
        {
            MessageId = Guid.NewGuid(),
            Type = estorno.Status switch
            { StatusEstorno.Confirmado => "EstornoConfirmado", StatusEstorno.Recusado => "EstornoRecusado", _ => "FalhaProcessamentoEstorno" },
            SagaId = estorno.SagaId,
            CausationId = estorno.Id,
            CorrelationId = estorno.CorrelationId,
            OccurredAt = estorno.FinalizadoEm!.Value,
            Payload = new DadosMensagem
            {
                OrdemServicoId = estorno.OrdemServicoId,
                ExecucaoId = estorno.ExecucaoId,
                OrcamentoId = estorno.OrcamentoId,
                PagamentoId = estorno.PagamentoId,
                ProvedorId = estorno.PagamentoProvedorId,
                ValorTotal = estorno.Valor,
                Moeda = estorno.Moeda,
                EstornoId = estorno.Id,
                EstornoProvedorId = estorno.ProvedorId,
                Codigo = estorno.CodigoFalha,
                Motivo = estorno.Status == StatusEstorno.Confirmado ? null : "Estorno recusado ou nao confirmado; reconciliacao necessaria."
            }
        };
    }
}

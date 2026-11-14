using OficinaMecanica.Cobranca.Application.Outputs.Pagamentos;
using OficinaMecanica.Cobranca.Domain.Entities;
namespace OficinaMecanica.Cobranca.Application.Mappers.Pagamentos;

public static class EstornoOutputMapper
{
    public static EstornoOutput Mapear(Estorno estorno) => new()
    {
        Id = estorno.Id,
        PagamentoId = estorno.PagamentoId,
        OrdemServicoId = estorno.OrdemServicoId,
        Status = estorno.Status,
        Valor = estorno.Valor,
        Moeda = estorno.Moeda,
        ProvedorId = estorno.ProvedorId,
        CodigoFalha = estorno.CodigoFalha,
        Tentativas = estorno.Tentativas,
        ConsultasReconciliacao = estorno.ConsultasReconciliacao,
        UltimaConsultaReconciliacaoEm = estorno.UltimaConsultaReconciliacaoEm,
        ReconciliadoEm = estorno.ReconciliadoEm,
        CriadoEm = estorno.CriadoEm,
        FinalizadoEm = estorno.FinalizadoEm
    };
}

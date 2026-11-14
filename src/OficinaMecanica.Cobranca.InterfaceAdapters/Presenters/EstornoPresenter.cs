using OficinaMecanica.Cobranca.Application.Outputs.Pagamentos;
using OficinaMecanica.Cobranca.InterfaceAdapters.DTOs;
namespace OficinaMecanica.Cobranca.InterfaceAdapters.Presenters;

public static class EstornoPresenter
{
    public static EstornoResponseDto Apresentar(EstornoOutput output) => new()
    {
        Id = output.Id,
        PagamentoId = output.PagamentoId,
        OrdemServicoId = output.OrdemServicoId,
        Status = output.Status,
        Valor = output.Valor,
        Moeda = output.Moeda,
        ProvedorId = output.ProvedorId,
        CodigoFalha = output.CodigoFalha,
        Tentativas = output.Tentativas,
        ConsultasReconciliacao = output.ConsultasReconciliacao,
        UltimaConsultaReconciliacaoEm = output.UltimaConsultaReconciliacaoEm,
        ReconciliadoEm = output.ReconciliadoEm,
        CriadoEm = output.CriadoEm,
        FinalizadoEm = output.FinalizadoEm
    };
}

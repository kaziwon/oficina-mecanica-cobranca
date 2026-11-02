using OficinaMecanica.Cobranca.Application.Outputs.Pagamentos;
using OficinaMecanica.Cobranca.InterfaceAdapters.DTOs;

namespace OficinaMecanica.Cobranca.InterfaceAdapters.Presenters;

public static class PagamentoPresenter
{
    public static PagamentoResponseDto Apresentar(PagamentoOutput output) => new()
    {
        Id = output.Id,
        OrcamentoId = output.OrcamentoId,
        OrdemServicoId = output.OrdemServicoId,
        Valor = output.Valor,
        Moeda = output.Moeda,
        Status = output.Status,
        CriadoEm = output.CriadoEm
    };
}

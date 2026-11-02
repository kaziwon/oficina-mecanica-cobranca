using OficinaMecanica.Cobranca.Application.Outputs.Orcamentos;
using OficinaMecanica.Cobranca.InterfaceAdapters.DTOs;

namespace OficinaMecanica.Cobranca.InterfaceAdapters.Presenters;

public static class OrcamentoPresenter
{
    public static OrcamentoResponseDto Apresentar(OrcamentoOutput output) => new()
    {
        Id = output.Id,
        OrdemServicoId = output.OrdemServicoId,
        ExecucaoId = output.ExecucaoId,
        Diagnostico = output.Diagnostico,
        Status = output.Status,
        ValorTotal = output.ValorTotal,
        CriadoEm = output.CriadoEm,
        RespondidoEm = output.RespondidoEm,
        MotivoRecusa = output.MotivoRecusa,
        PagamentoId = output.PagamentoId,
        Versao = output.Versao,
        Itens = output.Itens.Select(item => new ItemOrcamentoResponseDto
        {
            Tipo = item.Tipo,
            ReferenciaId = item.ReferenciaId,
            Nome = item.Nome,
            Quantidade = item.Quantidade,
            PrecoUnitario = item.PrecoUnitario,
            Total = item.Total
        }).ToList(),
        Historico = output.Historico.Select(item => new HistoricoOrcamentoResponseDto
        {
            Status = item.Status,
            RegistradoEm = item.RegistradoEm,
            Motivo = item.Motivo
        }).ToList()
    };
}

using OficinaMecanica.Cobranca.Application.Outputs.Orcamentos;
using OficinaMecanica.Cobranca.Domain.Entities;

namespace OficinaMecanica.Cobranca.Application.Mappers.Orcamentos;

public static class OrcamentoOutputMapper
{
    public static OrcamentoOutput Mapear(Orcamento orcamento) => new()
    {
        Id = orcamento.Id,
        OrdemServicoId = orcamento.OrdemServicoId,
        ExecucaoId = orcamento.ExecucaoId,
        Diagnostico = orcamento.Diagnostico,
        Status = orcamento.Status,
        ValorTotal = orcamento.ValorTotal,
        CriadoEm = orcamento.CriadoEm,
        RespondidoEm = orcamento.RespondidoEm,
        MotivoRecusa = orcamento.MotivoRecusa,
        Versao = orcamento.Versao,
        PagamentoId = orcamento.Pagamento?.Id,
        Itens = orcamento.Itens.OrderBy(item => item.Tipo).ThenBy(item => item.Nome).Select(item => new ItemOrcamentoOutput
        {
            Tipo = item.Tipo,
            ReferenciaId = item.ReferenciaId,
            Nome = item.Nome,
            Quantidade = item.Quantidade,
            PrecoUnitario = item.PrecoUnitario,
            Total = item.Total
        }).ToList(),
        Historico = orcamento.Historico.OrderBy(item => item.RegistradoEm).Select(item => new HistoricoOrcamentoOutput
        {
            Status = item.Status,
            RegistradoEm = item.RegistradoEm,
            Motivo = item.Motivo
        }).ToList()
    };
}

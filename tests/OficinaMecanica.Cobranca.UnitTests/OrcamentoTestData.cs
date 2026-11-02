using OficinaMecanica.Cobranca.Application.Inputs.Orcamentos;
using OficinaMecanica.Cobranca.Domain.Entities;

namespace OficinaMecanica.Cobranca.UnitTests;

internal static class OrcamentoTestData
{
    public static CriarOrcamentoInput Input() => new()
    {
        OrdemServicoId = Guid.NewGuid(),
        ExecucaoId = Guid.NewGuid(),
        Diagnostico = " Trocar pastilhas ",
        Itens =
        [
            new() { Tipo = TipoItemOrcamento.Servico, ReferenciaId = Guid.NewGuid(), Nome = "Revisao", Quantidade = 1, PrecoUnitario = 100.15m },
            new() { Tipo = TipoItemOrcamento.PecaInsumo, ReferenciaId = Guid.NewGuid(), Nome = "Pastilha", Quantidade = 2, PrecoUnitario = 80.50m }
        ]
    };

    public static Orcamento Entidade()
    {
        var input = Input();
        return Orcamento.Criar(input.OrdemServicoId, input.ExecucaoId, input.Diagnostico,
            input.Itens.Select(item => new ItemOrcamento(item.Tipo, item.ReferenciaId, item.Nome, item.Quantidade, item.PrecoUnitario)).ToList());
    }
}

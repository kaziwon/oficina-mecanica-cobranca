using OficinaMecanica.Cobranca.Application.Exceptions;
using OficinaMecanica.Cobranca.Application.Inputs.Orcamentos;
using OficinaMecanica.Cobranca.Domain.Entities;

namespace OficinaMecanica.Cobranca.Application.Mappers.Orcamentos;

public static class OrcamentoInputMapper
{
    public static Orcamento Mapear(CriarOrcamentoInput input)
    {
        if (input.Itens is null || input.Itens.Count is < 1 or > 100 || input.Itens.Any(item => item is null))
            throw new ValidacaoException("Informe entre 1 e 100 itens validos.");
        var itens = input.Itens.Select(item => new ItemOrcamento(
            item.Tipo, item.ReferenciaId, item.Nome, item.Quantidade, item.PrecoUnitario)).ToList();
        return Orcamento.Criar(input.OrdemServicoId, input.ExecucaoId, input.Diagnostico, itens);
    }
}

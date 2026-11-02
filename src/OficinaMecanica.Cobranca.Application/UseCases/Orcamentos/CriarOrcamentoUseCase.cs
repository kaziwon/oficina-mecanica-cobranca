using OficinaMecanica.Cobranca.Application.Exceptions;
using OficinaMecanica.Cobranca.Application.Gateways;
using OficinaMecanica.Cobranca.Application.Inputs.Orcamentos;
using OficinaMecanica.Cobranca.Application.Mappers.Orcamentos;
using OficinaMecanica.Cobranca.Application.Outputs.Orcamentos;
using OficinaMecanica.Cobranca.Domain.Entities;

namespace OficinaMecanica.Cobranca.Application.UseCases.Orcamentos;

public class CriarOrcamentoUseCase
{
    private readonly IOrcamentoGateway _orcamentos;
    public CriarOrcamentoUseCase(IOrcamentoGateway orcamentos) { _orcamentos = orcamentos; }

    public OrcamentoOutput Executar(CriarOrcamentoInput input)
    {
        if (input.Itens is null || input.Itens.Count is < 1 or > 100 || input.Itens.Any(item => item is null))
            throw new ValidacaoException("Informe entre 1 e 100 itens validos.");
        var itens = input.Itens.Select(item => new ItemOrcamento(
            item.Tipo, item.ReferenciaId, item.Nome, item.Quantidade, item.PrecoUnitario)).ToList();
        var orcamento = Orcamento.Criar(input.OrdemServicoId, input.ExecucaoId, input.Diagnostico, itens);
        if (_orcamentos.ObterPorOrdemServicoId(input.OrdemServicoId) is not null)
            throw new ConflitoException("Esta OS ja possui um orcamento.");
        _orcamentos.Inserir(orcamento);
        return OrcamentoOutputMapper.Mapear(orcamento);
    }
}

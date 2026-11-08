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
        var orcamento = OrcamentoInputMapper.Mapear(input);
        if (_orcamentos.ObterPorOrdemServicoId(input.OrdemServicoId) is not null)
            throw new ConflitoException("Esta OS ja possui um orcamento.");
        _orcamentos.Inserir(orcamento);
        return OrcamentoOutputMapper.Mapear(orcamento);
    }
}

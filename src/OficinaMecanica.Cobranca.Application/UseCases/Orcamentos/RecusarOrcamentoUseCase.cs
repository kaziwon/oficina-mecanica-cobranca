using OficinaMecanica.Cobranca.Application.Gateways;
using OficinaMecanica.Cobranca.Application.Inputs.Orcamentos;
using OficinaMecanica.Cobranca.Application.Mappers.Orcamentos;
using OficinaMecanica.Cobranca.Application.Outputs.Orcamentos;

namespace OficinaMecanica.Cobranca.Application.UseCases.Orcamentos;

public class RecusarOrcamentoUseCase
{
    private readonly IOrcamentoGateway _orcamentos;
    public RecusarOrcamentoUseCase(IOrcamentoGateway orcamentos) { _orcamentos = orcamentos; }

    public OrcamentoOutput? Executar(Guid id, RecusarOrcamentoInput input)
    {
        var orcamento = _orcamentos.ObterPorId(id);
        if (orcamento is null) return null;
        if (orcamento.Recusar(input.Motivo)) _orcamentos.Salvar(orcamento);
        return OrcamentoOutputMapper.Mapear(orcamento);
    }
}

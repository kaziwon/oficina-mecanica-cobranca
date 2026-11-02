using OficinaMecanica.Cobranca.Application.Gateways;
using OficinaMecanica.Cobranca.Application.Mappers.Orcamentos;
using OficinaMecanica.Cobranca.Application.Outputs.Orcamentos;

namespace OficinaMecanica.Cobranca.Application.UseCases.Orcamentos;

public class ObterOrcamentoPorIdUseCase
{
    private readonly IOrcamentoGateway _orcamentos;
    public ObterOrcamentoPorIdUseCase(IOrcamentoGateway orcamentos) { _orcamentos = orcamentos; }

    public OrcamentoOutput? Executar(Guid id)
    {
        var orcamento = _orcamentos.ObterPorId(id);
        return orcamento is null ? null : OrcamentoOutputMapper.Mapear(orcamento);
    }
}

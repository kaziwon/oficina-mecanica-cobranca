using OficinaMecanica.Cobranca.Application.Gateways;
using OficinaMecanica.Cobranca.Application.Inputs.Orcamentos;
using OficinaMecanica.Cobranca.Application.Mappers.Orcamentos;
using OficinaMecanica.Cobranca.Application.Outputs.Orcamentos;
using OficinaMecanica.Cobranca.Application.Validators;

namespace OficinaMecanica.Cobranca.Application.UseCases.Orcamentos;

public class ObterOrcamentoAtendimentoUseCase(IOrcamentoGateway orcamentos)
{
    public OrcamentoOutput? Executar(Guid id, VinculoOrcamentoAtendimentoInput vinculo)
    {
        var orcamento = orcamentos.ObterPorId(id);
        if (orcamento is null) return null;
        OrcamentoAtendimentoValidator.Validar(orcamento, vinculo);
        return OrcamentoOutputMapper.Mapear(orcamento);
    }
}

using OficinaMecanica.Cobranca.Application.Gateways;
using OficinaMecanica.Cobranca.Application.Mappers.Orcamentos;
using OficinaMecanica.Cobranca.Application.Outputs.Orcamentos;

namespace OficinaMecanica.Cobranca.Application.UseCases.Orcamentos;

public class AprovarOrcamentoUseCase
{
    private readonly IOrcamentoGateway _orcamentos;
    public AprovarOrcamentoUseCase(IOrcamentoGateway orcamentos) { _orcamentos = orcamentos; }

    public OrcamentoOutput? Executar(Guid id)
    {
        var orcamento = _orcamentos.ObterPorId(id);
        if (orcamento is null) return null;
        if (orcamento.Aprovar()) _orcamentos.Salvar(orcamento, RespostaOrcamentoMensagemMapper.Mapear(orcamento));
        return OrcamentoOutputMapper.Mapear(orcamento);
    }
}

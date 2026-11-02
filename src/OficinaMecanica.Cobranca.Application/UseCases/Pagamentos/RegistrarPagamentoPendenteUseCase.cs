using OficinaMecanica.Cobranca.Application.Gateways;
using OficinaMecanica.Cobranca.Application.Mappers.Pagamentos;
using OficinaMecanica.Cobranca.Application.Outputs.Pagamentos;

namespace OficinaMecanica.Cobranca.Application.UseCases.Pagamentos;

public class RegistrarPagamentoPendenteUseCase
{
    private readonly IOrcamentoGateway _orcamentos;
    public RegistrarPagamentoPendenteUseCase(IOrcamentoGateway orcamentos) { _orcamentos = orcamentos; }

    public PagamentoOutput? Executar(Guid orcamentoId)
    {
        var orcamento = _orcamentos.ObterPorId(orcamentoId);
        if (orcamento is null) return null;
        if (orcamento.RegistrarPagamentoPendente()) _orcamentos.Salvar(orcamento);
        return PagamentoOutputMapper.Mapear(orcamento.Pagamento!);
    }
}

using OficinaMecanica.Cobranca.Application.Gateways;
using OficinaMecanica.Cobranca.Application.Mappers.Pagamentos;
using OficinaMecanica.Cobranca.Application.Outputs.Pagamentos;

namespace OficinaMecanica.Cobranca.Application.UseCases.Pagamentos;

public class ObterPagamentoPorIdUseCase
{
    private readonly IPagamentoGateway _pagamentos;
    public ObterPagamentoPorIdUseCase(IPagamentoGateway pagamentos) { _pagamentos = pagamentos; }

    public PagamentoOutput? Executar(Guid id)
    {
        var pagamento = _pagamentos.ObterPorId(id);
        return pagamento is null ? null : PagamentoOutputMapper.Mapear(pagamento);
    }
}

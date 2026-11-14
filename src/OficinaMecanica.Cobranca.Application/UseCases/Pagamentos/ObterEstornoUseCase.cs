using OficinaMecanica.Cobranca.Application.Gateways;
using OficinaMecanica.Cobranca.Application.Mappers.Pagamentos;
using OficinaMecanica.Cobranca.Application.Outputs.Pagamentos;
namespace OficinaMecanica.Cobranca.Application.UseCases.Pagamentos;

public class ObterEstornoUseCase
{
    private readonly IEstornoGateway _estornos;
    public ObterEstornoUseCase(IEstornoGateway estornos) { _estornos = estornos; }
    public EstornoOutput? Executar(Guid pagamentoId)
    {
        var estorno = _estornos.ObterPorPagamento(pagamentoId);
        return estorno is null ? null : EstornoOutputMapper.Mapear(estorno);
    }
}

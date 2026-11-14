using OficinaMecanica.Cobranca.Application.UseCases.Pagamentos;
using OficinaMecanica.Cobranca.InterfaceAdapters.DTOs;
using OficinaMecanica.Cobranca.InterfaceAdapters.Presenters;
namespace OficinaMecanica.Cobranca.InterfaceAdapters.Controllers;

public class EstornosCleanController
{
    private readonly ObterEstornoUseCase _obter;
    private readonly ReconciliarEstornoUseCase _reconciliar;
    public EstornosCleanController(ObterEstornoUseCase obter, ReconciliarEstornoUseCase reconciliar)
    { _obter = obter; _reconciliar = reconciliar; }
    public EstornoResponseDto? Obter(Guid pagamentoId)
    {
        var output = _obter.Executar(pagamentoId);
        return output is null ? null : EstornoPresenter.Apresentar(output);
    }

    public async Task<EstornoResponseDto?> Reconciliar(Guid pagamentoId, CancellationToken cancellationToken)
    {
        var output = await _reconciliar.Executar(pagamentoId, cancellationToken);
        return output is null ? null : EstornoPresenter.Apresentar(output);
    }
}

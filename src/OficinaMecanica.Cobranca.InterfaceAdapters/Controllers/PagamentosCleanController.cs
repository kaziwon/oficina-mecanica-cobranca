using OficinaMecanica.Cobranca.Application.UseCases.Pagamentos;
using OficinaMecanica.Cobranca.InterfaceAdapters.DTOs;
using OficinaMecanica.Cobranca.InterfaceAdapters.Presenters;

namespace OficinaMecanica.Cobranca.InterfaceAdapters.Controllers;

public class PagamentosCleanController
{
    private readonly RegistrarPagamentoPendenteUseCase _registrar;
    private readonly ObterPagamentoPorIdUseCase _obter;
    private readonly ReconciliarPagamentoUseCase _reconciliar;
    public PagamentosCleanController(RegistrarPagamentoPendenteUseCase registrar, ObterPagamentoPorIdUseCase obter,
        ReconciliarPagamentoUseCase reconciliar)
    {
        _registrar = registrar; _obter = obter; _reconciliar = reconciliar;
    }

    public PagamentoResponseDto? RegistrarPendente(Guid orcamentoId)
    {
        var output = _registrar.Executar(orcamentoId);
        return output is null ? null : PagamentoPresenter.Apresentar(output);
    }

    public PagamentoResponseDto? ObterPorId(Guid id)
    {
        var output = _obter.Executar(id);
        return output is null ? null : PagamentoPresenter.Apresentar(output);
    }

    public async Task<PagamentoResponseDto?> Reconciliar(Guid id, CancellationToken cancellationToken)
    {
        var output = await _reconciliar.Executar(id, cancellationToken);
        return output is null ? null : PagamentoPresenter.Apresentar(output);
    }
}

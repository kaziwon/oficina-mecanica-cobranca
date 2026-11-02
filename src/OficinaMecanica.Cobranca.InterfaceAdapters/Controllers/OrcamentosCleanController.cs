using OficinaMecanica.Cobranca.Application.Inputs.Orcamentos;
using OficinaMecanica.Cobranca.Application.UseCases.Orcamentos;
using OficinaMecanica.Cobranca.Domain.Entities;
using OficinaMecanica.Cobranca.InterfaceAdapters.DTOs;
using OficinaMecanica.Cobranca.InterfaceAdapters.Presenters;

namespace OficinaMecanica.Cobranca.InterfaceAdapters.Controllers;

public class OrcamentosCleanController
{
    private readonly CriarOrcamentoUseCase _criar;
    private readonly ObterOrcamentoPorIdUseCase _obter;
    private readonly ObterTodosOrcamentosUseCase _listar;
    private readonly AprovarOrcamentoUseCase _aprovar;
    private readonly RecusarOrcamentoUseCase _recusar;

    public OrcamentosCleanController(CriarOrcamentoUseCase criar, ObterOrcamentoPorIdUseCase obter,
        ObterTodosOrcamentosUseCase listar, AprovarOrcamentoUseCase aprovar, RecusarOrcamentoUseCase recusar)
    {
        _criar = criar; _obter = obter; _listar = listar; _aprovar = aprovar; _recusar = recusar;
    }

    public OrcamentoResponseDto Criar(CriarOrcamentoRequestDto request) => OrcamentoPresenter.Apresentar(
        _criar.Executar(new CriarOrcamentoInput
        {
            OrdemServicoId = request.OrdemServicoId,
            ExecucaoId = request.ExecucaoId,
            Diagnostico = request.Diagnostico,
            Itens = request.Itens.Select(item => new ItemOrcamentoInput
            {
                Tipo = item.Tipo,
                ReferenciaId = item.ReferenciaId,
                Nome = item.Nome,
                Quantidade = item.Quantidade,
                PrecoUnitario = item.PrecoUnitario
            }).ToList()
        }));

    public OrcamentoResponseDto? ObterPorId(Guid id)
    {
        var output = _obter.Executar(id);
        return output is null ? null : OrcamentoPresenter.Apresentar(output);
    }

    public List<OrcamentoResponseDto> ObterTodos(StatusOrcamento? status, Guid? ordemServicoId)
        => _listar.Executar(status, ordemServicoId).Select(OrcamentoPresenter.Apresentar).ToList();

    public OrcamentoResponseDto? Aprovar(Guid id)
    {
        var output = _aprovar.Executar(id);
        return output is null ? null : OrcamentoPresenter.Apresentar(output);
    }

    public OrcamentoResponseDto? Recusar(Guid id, RecusarOrcamentoRequestDto request)
    {
        var output = _recusar.Executar(id, new RecusarOrcamentoInput(request.Motivo));
        return output is null ? null : OrcamentoPresenter.Apresentar(output);
    }
}

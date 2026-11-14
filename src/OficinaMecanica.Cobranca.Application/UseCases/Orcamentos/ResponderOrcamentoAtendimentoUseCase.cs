using OficinaMecanica.Cobranca.Application.Exceptions;
using OficinaMecanica.Cobranca.Application.Gateways;
using OficinaMecanica.Cobranca.Application.Inputs.Orcamentos;
using OficinaMecanica.Cobranca.Application.Mappers.Orcamentos;
using OficinaMecanica.Cobranca.Application.Validators;

namespace OficinaMecanica.Cobranca.Application.UseCases.Orcamentos;

public class ResponderOrcamentoAtendimentoUseCase(IOrcamentoGateway orcamentos)
{
    public bool Executar(Guid id, VinculoOrcamentoAtendimentoInput vinculo, bool aprovado, string? motivo)
    {
        if (aprovado ? motivo is not null : string.IsNullOrWhiteSpace(motivo) || motivo.Trim().Length > 1000 || motivo.Any(char.IsControl))
            throw new ValidacaoException("Informe um motivo valido de recusa; aprovacao nao recebe motivo.");
        var orcamento = orcamentos.ObterPorId(id);
        if (orcamento is null) return false;
        OrcamentoAtendimentoValidator.Validar(orcamento, vinculo);
        var alterado = aprovado ? orcamento.Aprovar() : orcamento.Recusar(motivo!);
        if (alterado) orcamentos.Salvar(orcamento, RespostaOrcamentoMensagemMapper.Mapear(orcamento));
        return true;
    }
}

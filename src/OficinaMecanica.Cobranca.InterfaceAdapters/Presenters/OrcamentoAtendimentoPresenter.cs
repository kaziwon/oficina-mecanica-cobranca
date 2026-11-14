using OficinaMecanica.Cobranca.Application.Outputs.Orcamentos;
using OficinaMecanica.Cobranca.InterfaceAdapters.DTOs;

namespace OficinaMecanica.Cobranca.InterfaceAdapters.Presenters;

public static class OrcamentoAtendimentoPresenter
{
    public static OrcamentoResponseDto Apresentar(OrcamentoOutput output)
    {
        var response = OrcamentoPresenter.Apresentar(output);
        response.CriadoEm = DateTime.SpecifyKind(response.CriadoEm, DateTimeKind.Utc);
        if (response.RespondidoEm.HasValue) response.RespondidoEm = DateTime.SpecifyKind(response.RespondidoEm.Value, DateTimeKind.Utc);
        foreach (var item in response.Historico) item.RegistradoEm = DateTime.SpecifyKind(item.RegistradoEm, DateTimeKind.Utc);
        return response;
    }
}

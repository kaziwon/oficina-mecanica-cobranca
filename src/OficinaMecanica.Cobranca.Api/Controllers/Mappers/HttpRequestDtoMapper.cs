using CleanDtos = OficinaMecanica.Cobranca.InterfaceAdapters.DTOs;
using HttpDtos = OficinaMecanica.Cobranca.Controllers.DTOs;

namespace OficinaMecanica.Cobranca.Controllers.Mappers;

public static class HttpRequestDtoMapper
{
    public static CleanDtos.LoginRequestDto ParaCleanDto(this HttpDtos.LoginRequestDto dto)
        => new() { Username = dto.Username, Password = dto.Password };

    public static CleanDtos.CriarOrcamentoRequestDto ParaCleanDto(this HttpDtos.CriarOrcamentoRequestDto dto) => new()
    {
        OrdemServicoId = dto.OrdemServicoId,
        ExecucaoId = dto.ExecucaoId,
        Diagnostico = dto.Diagnostico,
        Itens = dto.Itens.Select(item => new CleanDtos.ItemOrcamentoRequestDto
        {
            Tipo = item.Tipo,
            ReferenciaId = item.ReferenciaId,
            Nome = item.Nome,
            Quantidade = item.Quantidade,
            PrecoUnitario = item.PrecoUnitario
        }).ToList()
    };

    public static CleanDtos.RecusarOrcamentoRequestDto ParaCleanDto(this HttpDtos.RecusarOrcamentoRequestDto dto)
        => new(dto.Motivo);
}

using OficinaMecanica.Cobranca.Application.Inputs.Auth;
using OficinaMecanica.Cobranca.Application.Outputs.Auth;
using OficinaMecanica.Cobranca.InterfaceAdapters.DTOs;
using OficinaMecanica.Cobranca.Application.UseCases.Auth;

namespace OficinaMecanica.Cobranca.InterfaceAdapters.Controllers;

public class AuthCleanController
{
    private readonly LoginUseCase _loginUseCase;

    public AuthCleanController(LoginUseCase loginUseCase)
    {
        _loginUseCase = loginUseCase;
    }

    public LoginResponseDto? Login(LoginRequestDto loginRequestDto)
    {
        var output = _loginUseCase.Executar(new LoginInput(loginRequestDto.Username, loginRequestDto.Password));

        return output is null
            ? null
            : new LoginResponseDto
            {
                Token = output.Token,
                ExpiresAt = output.ExpiresAt
            };
    }
}

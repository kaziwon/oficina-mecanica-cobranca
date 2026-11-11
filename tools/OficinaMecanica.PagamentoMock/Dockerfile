FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /source
COPY Directory.Build.props ./
COPY tools/OficinaMecanica.PagamentoMock/ ./tools/OficinaMecanica.PagamentoMock/
RUN dotnet publish tools/OficinaMecanica.PagamentoMock/OficinaMecanica.PagamentoMock.csproj --configuration Release --output /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_HTTP_PORTS=8080
USER $APP_UID
ENTRYPOINT ["dotnet", "OficinaMecanica.PagamentoMock.dll"]

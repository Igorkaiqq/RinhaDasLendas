FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
WORKDIR /src

COPY BackEnd/RinhaDasLendas.sln BackEnd/
COPY BackEnd/Directory.Packages.props BackEnd/
COPY BackEnd/src/RinhaDasLendas.Api/RinhaDasLendas.Api.csproj BackEnd/src/RinhaDasLendas.Api/
COPY BackEnd/src/RinhaDasLendas.Application/RinhaDasLendas.Application.csproj BackEnd/src/RinhaDasLendas.Application/
COPY BackEnd/src/RinhaDasLendas.Domain/RinhaDasLendas.Domain.csproj BackEnd/src/RinhaDasLendas.Domain/
COPY BackEnd/src/RinhaDasLendas.Infrastructure/RinhaDasLendas.Infrastructure.csproj BackEnd/src/RinhaDasLendas.Infrastructure/
RUN dotnet restore "BackEnd/src/RinhaDasLendas.Api/RinhaDasLendas.Api.csproj"

COPY BackEnd/src BackEnd/src
RUN dotnet publish "BackEnd/src/RinhaDasLendas.Api/RinhaDasLendas.Api.csproj" -c Release --no-restore -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS runtime
WORKDIR /app
RUN apk add --no-cache krb5-libs
ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080
COPY --from=build /app/publish .
USER $APP_UID
HEALTHCHECK --interval=30s --timeout=5s --start-period=30s --retries=3 CMD wget -qO- http://localhost:8080/health || exit 1
ENTRYPOINT ["dotnet", "RinhaDasLendas.Api.dll"]

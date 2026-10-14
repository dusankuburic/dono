FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY Directory.Build.props Directory.Packages.props .editorconfig ./
COPY src/BuildingManager.Core/BuildingManager.Core.csproj src/BuildingManager.Core/
COPY src/BuildingManager.Infrastructure/BuildingManager.Infrastructure.csproj src/BuildingManager.Infrastructure/
COPY src/BuildingManager.Orleans.Interfaces/BuildingManager.Orleans.Interfaces.csproj src/BuildingManager.Orleans.Interfaces/
COPY src/BuildingManager.Orleans.Grains/BuildingManager.Orleans.Grains.csproj src/BuildingManager.Orleans.Grains/
COPY src/BuildingManager.Api/BuildingManager.Api.csproj src/BuildingManager.Api/
RUN dotnet restore src/BuildingManager.Api/BuildingManager.Api.csproj

COPY src/ src/
RUN dotnet publish src/BuildingManager.Api/BuildingManager.Api.csproj -c Release -o /app --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app .

ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080 11111 30000

USER $APP_UID
ENTRYPOINT ["dotnet", "BuildingManager.Api.dll"]

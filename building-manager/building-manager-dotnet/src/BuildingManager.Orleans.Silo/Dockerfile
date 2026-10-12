FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY Directory.Build.props Directory.Packages.props BuildingManager.sln ./
COPY src/BuildingManager.Core/BuildingManager.Core.csproj src/BuildingManager.Core/
COPY src/BuildingManager.Infrastructure/BuildingManager.Infrastructure.csproj src/BuildingManager.Infrastructure/
COPY src/BuildingManager.Orleans.Interfaces/BuildingManager.Orleans.Interfaces.csproj src/BuildingManager.Orleans.Interfaces/
COPY src/BuildingManager.Orleans.Grains/BuildingManager.Orleans.Grains.csproj src/BuildingManager.Orleans.Grains/
COPY src/BuildingManager.Orleans.Silo/BuildingManager.Orleans.Silo.csproj src/BuildingManager.Orleans.Silo/
RUN dotnet restore src/BuildingManager.Orleans.Silo/BuildingManager.Orleans.Silo.csproj

COPY src/ src/
RUN dotnet publish src/BuildingManager.Orleans.Silo/BuildingManager.Orleans.Silo.csproj -c Release --no-restore -o /app

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app .

ENV Orleans__SiloPort=11111 \
    Orleans__GatewayPort=30000 \
    Orleans__HttpPort=8080

EXPOSE 11111 30000 8080
ENTRYPOINT ["dotnet", "BuildingManager.Orleans.Silo.dll"]

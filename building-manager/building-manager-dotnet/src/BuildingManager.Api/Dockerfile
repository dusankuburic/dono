FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY Directory.Build.props Directory.Packages.props BuildingManager.sln ./
COPY src/BuildingManager.Core/BuildingManager.Core.csproj src/BuildingManager.Core/
COPY src/BuildingManager.Infrastructure/BuildingManager.Infrastructure.csproj src/BuildingManager.Infrastructure/
COPY src/BuildingManager.Orleans.Interfaces/BuildingManager.Orleans.Interfaces.csproj src/BuildingManager.Orleans.Interfaces/
COPY src/BuildingManager.Api/BuildingManager.Api.csproj src/BuildingManager.Api/
RUN dotnet restore src/BuildingManager.Api/BuildingManager.Api.csproj

COPY src/ src/
RUN dotnet publish src/BuildingManager.Api/BuildingManager.Api.csproj -c Release --no-restore -o /app

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "BuildingManager.Api.dll"]

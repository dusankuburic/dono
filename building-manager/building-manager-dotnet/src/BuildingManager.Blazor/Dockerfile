FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY Directory.Build.props Directory.Packages.props BuildingManager.sln ./
COPY src/BuildingManager.Core/BuildingManager.Core.csproj src/BuildingManager.Core/
COPY src/BuildingManager.Orleans.Interfaces/BuildingManager.Orleans.Interfaces.csproj src/BuildingManager.Orleans.Interfaces/
COPY src/BuildingManager.Blazor/BuildingManager.Blazor.csproj src/BuildingManager.Blazor/
RUN dotnet restore src/BuildingManager.Blazor/BuildingManager.Blazor.csproj

COPY src/ src/
RUN dotnet publish src/BuildingManager.Blazor/BuildingManager.Blazor.csproj -c Release --no-restore -o /app

FROM nginx:alpine AS runtime
COPY --from=build /app/wwwroot /usr/share/nginx/html
COPY src/BuildingManager.Blazor/nginx.conf /etc/nginx/conf.d/default.conf
EXPOSE 80

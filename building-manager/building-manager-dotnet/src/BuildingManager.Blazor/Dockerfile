FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY Directory.Build.props Directory.Packages.props .editorconfig ./
COPY src/BuildingManager.Core/BuildingManager.Core.csproj src/BuildingManager.Core/
COPY src/BuildingManager.Orleans.Interfaces/BuildingManager.Orleans.Interfaces.csproj src/BuildingManager.Orleans.Interfaces/
COPY src/BuildingManager.Blazor/BuildingManager.Blazor.csproj src/BuildingManager.Blazor/
RUN dotnet restore src/BuildingManager.Blazor/BuildingManager.Blazor.csproj

COPY src/ src/
RUN dotnet publish src/BuildingManager.Blazor/BuildingManager.Blazor.csproj -c Release -o /app --no-restore

FROM nginx:1.27-alpine AS final
COPY src/BuildingManager.Blazor/nginx/default.conf /etc/nginx/conf.d/default.conf
COPY src/BuildingManager.Blazor/nginx/40-api-base-url.sh /docker-entrypoint.d/40-api-base-url.sh
RUN chmod +x /docker-entrypoint.d/40-api-base-url.sh
COPY --from=build /app/wwwroot /usr/share/nginx/html
EXPOSE 80

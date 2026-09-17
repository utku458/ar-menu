# syntax=docker/dockerfile:1
# Build from the repository root:
#   docker build -f src/ArMenu.Api/Dockerfile -t armenu-api .
#   docker build -f src/ArMenu.Api/Dockerfile --target migrations -t armenu-migrations .

FROM mcr.microsoft.com/dotnet/sdk:10.0-noble AS build
WORKDIR /src

# Restore first, so source changes reuse the package layer.
COPY global.json Directory.Build.props Directory.Packages.props dotnet-tools.json .editorconfig ./
COPY src/ArMenu.Domain/ArMenu.Domain.csproj src/ArMenu.Domain/
COPY src/ArMenu.Application/ArMenu.Application.csproj src/ArMenu.Application/
COPY src/ArMenu.Infrastructure/ArMenu.Infrastructure.csproj src/ArMenu.Infrastructure/
COPY src/ArMenu.Api/ArMenu.Api.csproj src/ArMenu.Api/
RUN dotnet restore src/ArMenu.Api/ArMenu.Api.csproj && dotnet tool restore

COPY src/ src/
RUN dotnet publish src/ArMenu.Api/ArMenu.Api.csproj --configuration Release --no-restore --output /app -p:UseAppHost=false

# Schema changes ship as a reviewed, self-contained migration bundle run as the schema owner before the new API starts,
# never as a side effect of application startup.
RUN dotnet ef migrations bundle \
        --project src/ArMenu.Infrastructure --startup-project src/ArMenu.Infrastructure \
        --configuration Release --output /bundle/efbundle --verbose

# Migration job: docker run armenu-migrations --connection "Host=…;Username=<schema owner>;…"
FROM mcr.microsoft.com/dotnet/runtime:10.0-noble-chiseled AS migrations
WORKDIR /bundle
COPY --from=build /bundle/efbundle ./
ENTRYPOINT ["./efbundle"]

# The API. Chiseled Ubuntu: no shell, no package manager, a non-root user; orchestrators probe /health/live and
# /health/ready over HTTP.
FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble-chiseled AS api
WORKDIR /app
# Chiseled images carry no time zone database; businesses count their days in their own IANA time zone.
COPY --from=build /usr/share/zoneinfo /usr/share/zoneinfo
COPY --from=build /app ./
ENV ASPNETCORE_HTTP_PORTS=8080 \
    DOTNET_gcServer=0
EXPOSE 8080
ENTRYPOINT ["dotnet", "ArMenu.Api.dll"]

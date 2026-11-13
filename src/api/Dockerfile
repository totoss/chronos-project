# syntax=docker/dockerfile:1
FROM mcr.microsoft.com/dotnet/sdk:11.0 AS build
WORKDIR /source

COPY --link global.json Directory.Build.props Directory.Packages.props ./
COPY --link --from=racine .editorconfig ./
COPY --link Chronos.Domain/Chronos.Domain.csproj Chronos.Domain/packages.lock.json Chronos.Domain/
COPY --link Chronos.Infrastructure/Chronos.Infrastructure.csproj Chronos.Infrastructure/packages.lock.json Chronos.Infrastructure/
COPY --link Chronos.Migrations.PostgreSql/Chronos.Migrations.PostgreSql.csproj Chronos.Migrations.PostgreSql/packages.lock.json Chronos.Migrations.PostgreSql/
COPY --link Chronos.Migrations.SqlServer/Chronos.Migrations.SqlServer.csproj Chronos.Migrations.SqlServer/packages.lock.json Chronos.Migrations.SqlServer/
COPY --link Chronos.Api/Chronos.Api.csproj Chronos.Api/packages.lock.json Chronos.Api/
RUN --mount=type=cache,target=/root/.nuget/packages \
    dotnet restore Chronos.Api/Chronos.Api.csproj --locked-mode

COPY --link Chronos.Domain/ Chronos.Domain/
COPY --link Chronos.Infrastructure/ Chronos.Infrastructure/
COPY --link Chronos.Migrations.PostgreSql/ Chronos.Migrations.PostgreSql/
COPY --link Chronos.Migrations.SqlServer/ Chronos.Migrations.SqlServer/
COPY --link Chronos.Api/ Chronos.Api/
RUN --mount=type=cache,target=/root/.nuget/packages \
    dotnet publish Chronos.Api/Chronos.Api.csproj --no-restore -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0
RUN --mount=type=cache,target=/var/lib/apt/lists,sharing=locked \
    apt-get update && apt-get install -y --no-install-recommends curl
EXPOSE 8080
WORKDIR /app
COPY --link --from=build /app .
# Dossier des clés de protection des données : un volume nommé monté ici hérite de ce propriétaire.
RUN mkdir /cles && chown $APP_UID /cles
USER $APP_UID
ENTRYPOINT ["dotnet", "Chronos.Api.dll"]

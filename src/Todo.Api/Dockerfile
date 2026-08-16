# syntax=docker/dockerfile:1
#
# The API image. Built from the repository root, not from this directory:
#
#   docker build -f src/Todo.Api/Dockerfile -t todo-api .
#
# `docker-compose.yml` does exactly that (`context: .`). The context has to be the root
# because central package management puts Directory.Packages.props and
# Directory.Build.props there, and a build that cannot see them fails.

# ---------------------------------------------------------------------------
# Build
# ---------------------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Central package management first, and before any restore.
#
#   Directory.Packages.props - every package version in the repository
#   Directory.Build.props    - TargetFramework, nullable, warnings-as-errors
#   .editorconfig            - the analyser severities those warnings come from
#
# Miss the first and restore fails outright (NU1008: a project using central package
# version management must not carry a Version on its PackageReference, which is exactly
# what every .csproj here relies on). Miss the second and there is no TargetFramework to
# build against. Miss the third and the image builds under weaker rules than CI does,
# which is the worst outcome of the three because it is silent.
COPY Directory.Packages.props Directory.Build.props .editorconfig ./

# Project files on their own, so the restore layer is cached until a dependency actually
# changes. Copying the sources first would re-restore on every edit to a .cs file.
COPY src/Todo.Domain/Todo.Domain.csproj src/Todo.Domain/
COPY src/Todo.Application/Todo.Application.csproj src/Todo.Application/
COPY src/Todo.Infrastructure/Todo.Infrastructure.csproj src/Todo.Infrastructure/
COPY src/Todo.Api/Todo.Api.csproj src/Todo.Api/

# Restoring the API project pulls in Application, Infrastructure and Domain through its
# ProjectReferences. The solution - Todo.slnx, the .NET 10 XML format, not a .sln - would
# also drag in the three test projects and their packages (Testcontainers, xUnit, Respawn),
# none of which belong in a runtime image.
RUN dotnet restore src/Todo.Api/Todo.Api.csproj

COPY src/ src/

# --no-restore because the layer above already did it; without the flag this step would
# quietly re-restore and the caching above would buy nothing.
RUN dotnet publish src/Todo.Api/Todo.Api.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish

# ---------------------------------------------------------------------------
# Runtime
# ---------------------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

COPY --from=build /app/publish .

# The ASP.NET Core images listen on 8080 by default (ASPNETCORE_HTTP_PORTS), and they
# define APP_UID for a pre-created non-root account. Running as root inside a container is
# a habit worth not teaching.
EXPOSE 8080
USER $APP_UID

ENTRYPOINT ["dotnet", "Todo.Api.dll"]

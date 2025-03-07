FROM mcr.microsoft.com/dotnet/aspnet:9.0.2@sha256:69d0eb9d3182372d0509630cb4996085ccafb3bed75e9adec68604640bf725d3 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:9.0.200@sha256:1025bed126a7b85c56b960215ab42a99db97a319a72b5d902383ebf6c6e62bbe AS build
WORKDIR /src

COPY [".editorconfig", "."]
COPY ["Directory.Build.props", "."]

# Main project and dependencies
COPY ["src/Digdir.Domain.Dialogporten.Janitor/Digdir.Domain.Dialogporten.Janitor.csproj", "src/Digdir.Domain.Dialogporten.Janitor/"]
COPY ["src/Digdir.Domain.Dialogporten.Application/Digdir.Domain.Dialogporten.Application.csproj", "src/Digdir.Domain.Dialogporten.Application/"]
COPY ["src/Digdir.Domain.Dialogporten.Domain/Digdir.Domain.Dialogporten.Domain.csproj", "src/Digdir.Domain.Dialogporten.Domain/"]
COPY ["src/Digdir.Library.Entity.Abstractions/Digdir.Library.Entity.Abstractions.csproj", "src/Digdir.Library.Entity.Abstractions/"]
COPY ["src/Digdir.Library.Entity.EntityFrameworkCore/Digdir.Library.Entity.EntityFrameworkCore.csproj", "src/Digdir.Library.Entity.EntityFrameworkCore/"]
COPY ["src/Digdir.Domain.Dialogporten.Infrastructure/Digdir.Domain.Dialogporten.Infrastructure.csproj", "src/Digdir.Domain.Dialogporten.Infrastructure/"]

# Restore project
RUN dotnet restore "src/Digdir.Domain.Dialogporten.Janitor/Digdir.Domain.Dialogporten.Janitor.csproj"

# Copy source
COPY ["src/", "."]

# Publish
WORKDIR "/src/Digdir.Domain.Dialogporten.Janitor"
RUN dotnet publish "Digdir.Domain.Dialogporten.Janitor.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Digdir.Domain.Dialogporten.Janitor.dll"]
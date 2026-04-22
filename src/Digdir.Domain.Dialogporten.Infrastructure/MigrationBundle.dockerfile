FROM mcr.microsoft.com/dotnet/aspnet:10.0.6@sha256:902cb7375e347f20c93d959bf1feba884e0a9232e0087991ace8eb4f82d2986d AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:10.0.203@sha256:6d7f69bc7bc9d4510ca255977b1f53ce52a79307e048a91450b2aecd63627cc3 AS build
WORKDIR /src

ENV PATH="/root/.dotnet/tools:${PATH}"
RUN dotnet tool install --global dotnet-ef --version 10.0.5

COPY [".editorconfig", "."]
COPY ["Directory.Build.props", "."]

# Main project
COPY ["src/Digdir.Domain.Dialogporten.Infrastructure/Digdir.Domain.Dialogporten.Infrastructure.csproj", "Digdir.Domain.Dialogporten.Infrastructure/"]
# Dependencies
COPY ["src/Digdir.Domain.Dialogporten.Domain/Digdir.Domain.Dialogporten.Domain.csproj", "Digdir.Domain.Dialogporten.Domain/"]
COPY ["src/Digdir.Domain.Dialogporten.Application/Digdir.Domain.Dialogporten.Application.csproj", "Digdir.Domain.Dialogporten.Application/"]
COPY ["src/Digdir.Library.Entity.Abstractions/Digdir.Library.Entity.Abstractions.csproj", "Digdir.Library.Entity.Abstractions/"]
COPY ["src/Digdir.Library.Entity.EntityFrameworkCore/Digdir.Library.Entity.EntityFrameworkCore.csproj", "Digdir.Library.Entity.EntityFrameworkCore/"]

# Restore
RUN dotnet restore "Digdir.Domain.Dialogporten.Infrastructure/Digdir.Domain.Dialogporten.Infrastructure.csproj"

# Copy source
COPY ["src/", "."]

WORKDIR "/src/Digdir.Domain.Dialogporten.Infrastructure"
RUN dotnet build -c Release --no-restore
RUN mkdir -p /app/publish
RUN dotnet ef migrations -v bundle -o /app/publish/efbundle

FROM base AS final
ENV Infrastructure__DialogDbConnectionString=""
WORKDIR /app
USER $APP_UID
COPY --from=build /app/publish .
ENTRYPOINT ["sh", "-c", "./efbundle -v --connection \"${Infrastructure__DialogDbConnectionString}Command Timeout=0;\""]

FROM mcr.microsoft.com/dotnet/aspnet:10.0.3@sha256:13fda51806e4b84fccf84abf86b35313edf89200a17a7b01c4ea587c88a0fa20 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:10.0.103@sha256:0a506ab0c8aa077361af42f82569d364ab1b8741e967955d883e3f23683d473a AS build
WORKDIR /src

ENV PATH="/root/.dotnet/tools:${PATH}"
RUN dotnet tool install --global dotnet-ef --version 10.0.3

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

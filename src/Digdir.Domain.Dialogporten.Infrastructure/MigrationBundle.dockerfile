﻿FROM mcr.microsoft.com/dotnet/aspnet:8.0@sha256:3ff67792728179308c4bf06799d8b45d155c60fddc347acf69465a496d9a20b8 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0@sha256:b246fc9a92feefe3aff49beb165fda4fc5a29122cd30768bafdc2d3e606c565c AS build
WORKDIR /src

# Main project
COPY ["src/Digdir.Domain.Dialogporten.Infrastructure/Digdir.Domain.Dialogporten.Infrastructure.csproj", "src/Digdir.Domain.Dialogporten.Infrastructure/"]
# Dependencies 
COPY ["src/Digdir.Domain.Dialogporten.Domain/Digdir.Domain.Dialogporten.Domain.csproj", "src/Digdir.Domain.Dialogporten.Domain/"]
COPY ["src/Digdir.Library.Entity.Abstractions/Digdir.Library.Entity.Abstractions.csproj", "src/Digdir.Library.Entity.Abstractions/"]
COPY ["src/Digdir.Library.Entity.EntityFrameworkCore/Digdir.Library.Entity.EntityFrameworkCore.csproj", "src/Digdir.Library.Entity.EntityFrameworkCore/"]
# Restore
RUN dotnet restore "src/Digdir.Domain.Dialogporten.Infrastructure/Digdir.Domain.Dialogporten.Infrastructure.csproj"
# Copy source
COPY ["src/", "."]

WORKDIR "/src/Digdir.Domain.Dialogporten.Infrastructure"
RUN mkdir -p /app/publish
RUN dotnet tool install --global dotnet-ef --version 7.0.14
ENV PATH $PATH:/root/.dotnet/tools
RUN dotnet ef migrations -v bundle -o /app/publish/efbundle

FROM base AS final
ENV Infrastructure__DialogDbConnectionString=""
WORKDIR /app
USER $APP_UID
COPY --from=build /app/publish .
ENTRYPOINT ./efbundle -v --connection "${Infrastructure__DialogDbConnectionString}"

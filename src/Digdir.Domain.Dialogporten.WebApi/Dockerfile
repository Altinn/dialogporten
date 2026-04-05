FROM mcr.microsoft.com/dotnet/aspnet:10.0.5@sha256:ccdca44cd4f256d50187f920dc8ccc2a9ea7a8a4597ac1d51e08fddb2e3b3205 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0.201@sha256:f061e5a7532b36fa1d1b684857fe1f504ba92115b9934f154643266613c44c62 AS build
WORKDIR /src

COPY [".editorconfig", "."]
COPY ["Directory.Build.props", "."]

# Main project
COPY ["src/Digdir.Domain.Dialogporten.WebApi/Digdir.Domain.Dialogporten.WebApi.csproj", "Digdir.Domain.Dialogporten.WebApi/"]
# Dependencies
COPY ["src/Digdir.Domain.Dialogporten.Application/Digdir.Domain.Dialogporten.Application.csproj", "Digdir.Domain.Dialogporten.Application/"]
COPY ["src/Digdir.Domain.Dialogporten.Domain/Digdir.Domain.Dialogporten.Domain.csproj", "Digdir.Domain.Dialogporten.Domain/"]
COPY ["src/Digdir.Library.Entity.Abstractions/Digdir.Library.Entity.Abstractions.csproj", "Digdir.Library.Entity.Abstractions/"]
COPY ["src/Digdir.Library.Entity.EntityFrameworkCore/Digdir.Library.Entity.EntityFrameworkCore.csproj", "Digdir.Library.Entity.EntityFrameworkCore/"]
COPY ["src/Digdir.Domain.Dialogporten.Infrastructure/Digdir.Domain.Dialogporten.Infrastructure.csproj", "Digdir.Domain.Dialogporten.Infrastructure/"]
COPY ["src/Digdir.Library.Utils.AspNet/Digdir.Library.Utils.AspNet.csproj", "Digdir.Library.Utils.AspNet/"]

# Restore project
RUN dotnet restore "Digdir.Domain.Dialogporten.WebApi/Digdir.Domain.Dialogporten.WebApi.csproj"

# Copy source
COPY ["src/", "."]

# Publish
WORKDIR "/src/Digdir.Domain.Dialogporten.WebApi"
RUN dotnet publish "Digdir.Domain.Dialogporten.WebApi.csproj" -c Release -o /app/publish /p:UseAppHost=false --no-restore

FROM base AS final
WORKDIR /app
USER $APP_UID
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Digdir.Domain.Dialogporten.WebApi.dll"]

FROM mcr.microsoft.com/dotnet/aspnet:9.0.10@sha256:bf48e8b328707fae0e63a1b7d764d770221def59b97468c8cdee68f4e38ddfb9 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:9.0.306@sha256:ca77338a19f87a7d24494a3656cb7d878a040c158621337b9cd3ab811c5eb057 AS build
WORKDIR /src

COPY [".editorconfig", "."]
COPY ["Directory.Build.props", "."]

# Main project
COPY ["src/Digdir.Domain.Dialogporten.Service/Digdir.Domain.Dialogporten.Service.csproj", "src/Digdir.Domain.Dialogporten.Service/"]
# Dependencies
COPY ["src/Digdir.Domain.Dialogporten.Application/Digdir.Domain.Dialogporten.Application.csproj", "src/Digdir.Domain.Dialogporten.Application/"]
COPY ["src/Digdir.Domain.Dialogporten.Domain/Digdir.Domain.Dialogporten.Domain.csproj", "src/Digdir.Domain.Dialogporten.Domain/"]
COPY ["src/Digdir.Library.Entity.Abstractions/Digdir.Library.Entity.Abstractions.csproj", "src/Digdir.Library.Entity.Abstractions/"]
COPY ["src/Digdir.Library.Entity.EntityFrameworkCore/Digdir.Library.Entity.EntityFrameworkCore.csproj", "src/Digdir.Library.Entity.EntityFrameworkCore/"]
COPY ["src/Digdir.Domain.Dialogporten.Infrastructure/Digdir.Domain.Dialogporten.Infrastructure.csproj", "src/Digdir.Domain.Dialogporten.Infrastructure/"]
# Restore
RUN dotnet restore "src/Digdir.Domain.Dialogporten.Service/Digdir.Domain.Dialogporten.Service.csproj"
# Copy rest of source
COPY ["src/", "/src/"]
# Publish
WORKDIR "/src/Digdir.Domain.Dialogporten.Service"
RUN dotnet publish "Digdir.Domain.Dialogporten.Service.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
USER $APP_UID
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Digdir.Domain.Dialogporten.Service.dll"]

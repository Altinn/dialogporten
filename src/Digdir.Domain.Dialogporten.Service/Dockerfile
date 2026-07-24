FROM mcr.microsoft.com/dotnet/aspnet:10.0.10@sha256:1fa23fc4872d95fd71c2833ebe65d7e84a43b2d51a31d119516852f13d9505a7 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:10.0.302@sha256:ed034a8bf0b24ded0cbbac07e17825d8e9ebfe21e308191d0f7421eaf5ad4664 AS build
WORKDIR /src

COPY [".editorconfig", "."]
COPY ["Directory.Build.props", "."]

# Main project
COPY ["src/Digdir.Domain.Dialogporten.Service/Digdir.Domain.Dialogporten.Service.csproj", "Digdir.Domain.Dialogporten.Service/"]
COPY ["src/Digdir.Domain.Dialogporten.Service/packages.lock.json", "Digdir.Domain.Dialogporten.Service/"]
# Dependencies
COPY ["src/Digdir.Domain.Dialogporten.Application/Digdir.Domain.Dialogporten.Application.csproj", "Digdir.Domain.Dialogporten.Application/"]
COPY ["src/Digdir.Domain.Dialogporten.Application/packages.lock.json", "Digdir.Domain.Dialogporten.Application/"]
COPY ["src/Digdir.Domain.Dialogporten.Domain/Digdir.Domain.Dialogporten.Domain.csproj", "Digdir.Domain.Dialogporten.Domain/"]
COPY ["src/Digdir.Domain.Dialogporten.Domain/packages.lock.json", "Digdir.Domain.Dialogporten.Domain/"]
COPY ["src/Digdir.Library.Entity.Abstractions/Digdir.Library.Entity.Abstractions.csproj", "Digdir.Library.Entity.Abstractions/"]
COPY ["src/Digdir.Library.Entity.Abstractions/packages.lock.json", "Digdir.Library.Entity.Abstractions/"]
COPY ["src/Digdir.Library.Entity.EntityFrameworkCore/Digdir.Library.Entity.EntityFrameworkCore.csproj", "Digdir.Library.Entity.EntityFrameworkCore/"]
COPY ["src/Digdir.Library.Entity.EntityFrameworkCore/packages.lock.json", "Digdir.Library.Entity.EntityFrameworkCore/"]
COPY ["src/Digdir.Domain.Dialogporten.Infrastructure/Digdir.Domain.Dialogporten.Infrastructure.csproj", "Digdir.Domain.Dialogporten.Infrastructure/"]
COPY ["src/Digdir.Domain.Dialogporten.Infrastructure/packages.lock.json", "Digdir.Domain.Dialogporten.Infrastructure/"]
COPY ["src/Digdir.Library.Utils.AspNet/Digdir.Library.Utils.AspNet.csproj", "Digdir.Library.Utils.AspNet/"]
COPY ["src/Digdir.Library.Utils.AspNet/packages.lock.json", "Digdir.Library.Utils.AspNet/"]

# Restore
RUN dotnet restore "Digdir.Domain.Dialogporten.Service/Digdir.Domain.Dialogporten.Service.csproj" --locked-mode

# Copy rest of source
COPY ["src/", "."]

# Publish
WORKDIR "/src/Digdir.Domain.Dialogporten.Service"
RUN dotnet publish "Digdir.Domain.Dialogporten.Service.csproj" -c Release -o /app/publish /p:UseAppHost=false --no-restore

FROM base AS final
WORKDIR /app
USER $APP_UID
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Digdir.Domain.Dialogporten.Service.dll"]

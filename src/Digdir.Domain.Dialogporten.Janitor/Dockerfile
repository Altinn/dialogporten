FROM mcr.microsoft.com/dotnet/aspnet:9.0.9@sha256:366204b1d249aa2615f4942c8549677a1f3e6231829f274aa8829fc048f38d8c AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:9.0.305@sha256:123b43e4d9775451d8ed63af324a42132707b4edb14770bcdf5c85cf55bc45f1 AS build
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
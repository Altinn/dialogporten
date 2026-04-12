FROM mcr.microsoft.com/dotnet/aspnet:10.0.5@sha256:c433886fdfe33c6427966a412328867b2be9a64f540a105d08943c2dc6fba39b AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:10.0.201@sha256:127d7d4d601ae26b8e04c54efb37e9ce8766931bded0ee59fcd799afd21d6850 AS build
WORKDIR /src

COPY [".editorconfig", "."]
COPY ["Directory.Build.props", "."]

# Main project and dependencies
COPY ["src/Digdir.Domain.Dialogporten.Janitor/Digdir.Domain.Dialogporten.Janitor.csproj", "Digdir.Domain.Dialogporten.Janitor/"]
COPY ["src/Digdir.Domain.Dialogporten.Application/Digdir.Domain.Dialogporten.Application.csproj", "Digdir.Domain.Dialogporten.Application/"]
COPY ["src/Digdir.Domain.Dialogporten.Domain/Digdir.Domain.Dialogporten.Domain.csproj", "Digdir.Domain.Dialogporten.Domain/"]
COPY ["src/Digdir.Library.Entity.Abstractions/Digdir.Library.Entity.Abstractions.csproj", "Digdir.Library.Entity.Abstractions/"]
COPY ["src/Digdir.Library.Entity.EntityFrameworkCore/Digdir.Library.Entity.EntityFrameworkCore.csproj", "Digdir.Library.Entity.EntityFrameworkCore/"]
COPY ["src/Digdir.Domain.Dialogporten.Infrastructure/Digdir.Domain.Dialogporten.Infrastructure.csproj", "Digdir.Domain.Dialogporten.Infrastructure/"]
COPY ["src/Digdir.Library.Utils.AspNet/Digdir.Library.Utils.AspNet.csproj", "Digdir.Library.Utils.AspNet/"]

# Restore project
RUN dotnet restore "Digdir.Domain.Dialogporten.Janitor/Digdir.Domain.Dialogporten.Janitor.csproj"

# Copy source
COPY ["src/", "."]

# Publish
WORKDIR "/src/Digdir.Domain.Dialogporten.Janitor"
RUN dotnet publish "Digdir.Domain.Dialogporten.Janitor.csproj" -c Release -o /app/publish --no-restore

FROM base AS final
WORKDIR /app
USER $APP_UID
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Digdir.Domain.Dialogporten.Janitor.dll"]

FROM mcr.microsoft.com/dotnet/aspnet:9.0.5@sha256:d5507d8292fb2469ec0b9913f3cd4bb8d5a014bd6dc00c33fd4c4e7310229f07 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0.300@sha256:c5b188baf837b0180a14f988815b9cc7a55b836dd6cbe1b6e6523cf3098faaa8 AS build
WORKDIR /src

COPY [".editorconfig", "."]
COPY ["Directory.Build.props", "."]

# Main project
COPY ["src/Digdir.Domain.Dialogporten.GraphQL/Digdir.Domain.Dialogporten.GraphQL.csproj", "src/Digdir.Domain.Dialogporten.GraphQL/"]
# Dependencies
COPY ["src/Digdir.Domain.Dialogporten.Application/Digdir.Domain.Dialogporten.Application.csproj", "src/Digdir.Domain.Dialogporten.Application/"]
COPY ["src/Digdir.Domain.Dialogporten.Domain/Digdir.Domain.Dialogporten.Domain.csproj", "src/Digdir.Domain.Dialogporten.Domain/"]
COPY ["src/Digdir.Library.Entity.Abstractions/Digdir.Library.Entity.Abstractions.csproj", "src/Digdir.Library.Entity.Abstractions/"]
COPY ["src/Digdir.Library.Entity.EntityFrameworkCore/Digdir.Library.Entity.EntityFrameworkCore.csproj", "src/Digdir.Library.Entity.EntityFrameworkCore/"]
COPY ["src/Digdir.Domain.Dialogporten.Infrastructure/Digdir.Domain.Dialogporten.Infrastructure.csproj", "src/Digdir.Domain.Dialogporten.Infrastructure/"]
# Restore project
RUN dotnet restore "src/Digdir.Domain.Dialogporten.GraphQL/Digdir.Domain.Dialogporten.GraphQL.csproj"
# Copy source
COPY ["src/", "."]
# Publish
WORKDIR "/src/Digdir.Domain.Dialogporten.GraphQL"
RUN dotnet publish "Digdir.Domain.Dialogporten.GraphQL.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
USER $APP_UID
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Digdir.Domain.Dialogporten.GraphQL.dll"]

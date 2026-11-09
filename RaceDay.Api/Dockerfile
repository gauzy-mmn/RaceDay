
# ==========================================
# RACEDAY API - DOCKERFILE
# ==========================================

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /src

COPY ["RaceDay.Api/RaceDay.Api.csproj", "RaceDay.Api/"]

RUN dotnet restore "RaceDay.Api/RaceDay.Api.csproj"

COPY . .

WORKDIR /src/RaceDay.Api

RUN dotnet publish "RaceDay.Api.csproj" \
    -c Release \
    -o /app/publish \
    --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final

WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_HTTP_PORTS=8080

EXPOSE 8080

ENTRYPOINT ["dotnet", "RaceDay.Api.dll"]

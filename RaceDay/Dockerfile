
# Stage 1: Build RaceDay MVC
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /src

COPY ["RaceDay/RaceDay.csproj", "RaceDay/"]

RUN dotnet restore "RaceDay/RaceDay.csproj"

COPY . .

WORKDIR /src/RaceDay

RUN dotnet publish "RaceDay.csproj" \
    -c Release \
    -o /app/publish \
    --no-restore

# Stage 2: Run RaceDay MVC
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final

WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_HTTP_PORTS=8080

EXPOSE 8080

ENTRYPOINT ["dotnet", "RaceDay.dll"]

# ── Build Stage ──
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["src/Planeroo.Domain/Planeroo.Domain.csproj", "Planeroo.Domain/"]
COPY ["src/Planeroo.Application/Planeroo.Application.csproj", "Planeroo.Application/"]
COPY ["src/Planeroo.Infrastructure/Planeroo.Infrastructure.csproj", "Planeroo.Infrastructure/"]
COPY ["src/Planeroo.API/Planeroo.API.csproj", "Planeroo.API/"]

RUN dotnet restore "Planeroo.API/Planeroo.API.csproj"

COPY src/ .
RUN dotnet build "Planeroo.API/Planeroo.API.csproj" -c Release -o /app/build
RUN dotnet publish "Planeroo.API/Planeroo.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# ── Runtime Stage ──
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

EXPOSE 8080
EXPOSE 8081

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

COPY --from=build /app/publish .

HEALTHCHECK --interval=30s --timeout=10s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "Planeroo.API.dll"]

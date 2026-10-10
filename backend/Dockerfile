# ---------- build ----------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copia só os .csproj primeiro para aproveitar o cache do restore.
COPY src/Fraga.Domain/Fraga.Domain.csproj src/Fraga.Domain/
COPY src/Fraga.Application/Fraga.Application.csproj src/Fraga.Application/
COPY src/Fraga.Infrastructure/Fraga.Infrastructure.csproj src/Fraga.Infrastructure/
COPY src/Fraga.Api/Fraga.Api.csproj src/Fraga.Api/
RUN dotnet restore src/Fraga.Api/Fraga.Api.csproj

COPY src/ src/
RUN dotnet publish src/Fraga.Api/Fraga.Api.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish

# ---------- runtime ----------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

# A imagem aspnet escuta na porta 8080 por padrão.
EXPOSE 8080
ENTRYPOINT ["dotnet", "Fraga.Api.dll"]
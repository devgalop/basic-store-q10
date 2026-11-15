FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
WORKDIR /src

# Copiar solo los archivos de proyecto y restaurar dependencias
COPY Store.Core/Store.Core.csproj Store.Core/
COPY Store.Infrastructure/Store.Infrastructure.csproj Store.Infrastructure/
COPY Store.Inventory.Worker/Store.Inventory.Worker.csproj Store.Inventory.Worker/
RUN dotnet restore Store.Inventory.Worker/Store.Inventory.Worker.csproj

# Copiar el resto del código fuente y publicar la aplicación
COPY Store.Core/ Store.Core/
COPY Store.Infrastructure/ Store.Infrastructure/
COPY Store.Inventory.Worker/ Store.Inventory.Worker/
# Publicar la aplicación en modo Release sin restaurar dependencias nuevamente
RUN dotnet publish Store.Inventory.Worker/Store.Inventory.Worker.csproj -c Release -o /app --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/runtime:10.0-alpine AS final
WORKDIR /app
COPY --from=build /app .

USER $APP_UID
ENTRYPOINT ["dotnet", "Store.Inventory.Worker.dll"]

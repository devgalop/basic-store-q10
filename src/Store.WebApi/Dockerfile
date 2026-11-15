FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
WORKDIR /src

# Copiar solo los archivos de proyecto y restaurar dependencias
COPY Store.Core/Store.Core.csproj Store.Core/
COPY Store.Infrastructure/Store.Infrastructure.csproj Store.Infrastructure/
COPY Store.WebApi/Store.WebApi.csproj Store.WebApi/
RUN dotnet restore Store.WebApi/Store.WebApi.csproj

# Copiar el resto del código fuente y publicar la aplicación
COPY Store.Core/ Store.Core/
COPY Store.Infrastructure/ Store.Infrastructure/
COPY Store.WebApi/ Store.WebApi/
# Publicar la aplicación en modo Release sin restaurar dependencias nuevamente
RUN dotnet publish Store.WebApi/Store.WebApi.csproj -c Release -o /app --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS final
WORKDIR /app
COPY --from=build /app .

USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "Store.WebApi.dll"]

# Imagen para correr los tests dentro de Docker: `docker compose run --rm tests`.
# Contexto de build: la carpeta src/ (ver docker-compose.yml).
FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine
WORKDIR /src

COPY . .
RUN dotnet restore Store.slnx
# Se compila al construir la imagen: al ejecutar, los tests arrancan sin esperar al compilador.
RUN dotnet build Store.slnx --no-restore

# La base de datos y sus credenciales llegan por variables POSTGRES_* (ver docker-compose.yml).
CMD ["dotnet", "test", "Store.slnx", "--no-build"]

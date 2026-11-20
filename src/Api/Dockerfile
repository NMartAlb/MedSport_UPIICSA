# Se construye desde la RAÍZ del repositorio:  docker build -f src/Api/Dockerfile .
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS compilar
WORKDIR /origen
COPY src/ src/
RUN dotnet publish src/Api/ServiciosEstudiantiles.Api.csproj -c Release -o /publicado

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=compilar /publicado .
# Carpeta de archivos subidos (con volumen) y ejecución sin privilegios de administrador
RUN mkdir -p /data/archivos && chown $APP_UID /data/archivos
USER $APP_UID
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "ServiciosEstudiantiles.Api.dll"]

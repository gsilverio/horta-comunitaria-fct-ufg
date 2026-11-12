# API da horta (.NET 10) e bundle de migrations do EF Core. Contexto de build: raiz do repositório.
#   target "api":     servidor HTTP e comandos explícitos (--bootstrap-admin, --seed-embrapa, --seed-demo)
#   target "migrate": aplica as migrations versionadas; executado sob demanda, nunca no startup
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY global.json Directory.Build.props dotnet-tools.json ./
COPY HortaComunitaria.Api/HortaComunitaria.Api.csproj HortaComunitaria.Api/packages.lock.json HortaComunitaria.Api/
RUN dotnet restore HortaComunitaria.Api/HortaComunitaria.Api.csproj --locked-mode && dotnet tool restore
COPY HortaComunitaria.Api/ HortaComunitaria.Api/
RUN dotnet publish HortaComunitaria.Api/HortaComunitaria.Api.csproj -c Release --no-restore -o /out/api
# Conexão fictícia só para o EF montar o modelo; o bundle não acessa banco no build.
RUN Persistence__ConnectionString="Host=build;Database=build" dotnet ef migrations bundle --project HortaComunitaria.Api --startup-project HortaComunitaria.Api \
      --context AppDbContext --configuration Release --self-contained -r linux-x64 -o /out/migrate/efbundle \
    && cp HortaComunitaria.Api/appsettings.json /out/migrate/

FROM mcr.microsoft.com/dotnet/runtime-deps:10.0 AS migrate
WORKDIR /migrate
COPY --from=build /out/migrate/ ./
USER $APP_UID
# A factory de design lê appsettings.json desta pasta e Persistence__ConnectionString do ambiente.
ENTRYPOINT ["./efbundle"]

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS api
WORKDIR /app
COPY --from=build /out/api/ ./
# App_Data guarda as chaves do Data Protection e as fotografias; persistido em volume.
RUN mkdir -p /app/App_Data && chown -R $APP_UID /app/App_Data
USER $APP_UID
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "HortaComunitaria.Api.dll"]

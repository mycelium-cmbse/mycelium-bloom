FROM mcr.microsoft.com/dotnet/sdk:10.0.400-noble AS build

WORKDIR /src

COPY Nuget.Config ./
COPY NOTICE ./
COPY Mycelium.Bloom/ ./Mycelium.Bloom/

RUN dotnet restore Mycelium.Bloom/Mycelium.Bloom.csproj --configfile Nuget.Config

WORKDIR /src/Mycelium.Bloom

RUN dotnet publish Mycelium.Bloom.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish \
    /p:UseAppHost=false

RUN test -s /app/publish/wwwroot/css/app.css.gz
RUN test -s /app/publish/wwwroot/css/tokens.css.gz
RUN grep -q "css/app.css" /app/publish/Mycelium.Bloom.staticwebassets.endpoints.json
RUN test -s /app/publish/wwwroot/_framework/blazor.web.js
RUN test -s "/app/publish/Resources/Domain Libraries/Quantities and Units/Quantities.json"

FROM mcr.microsoft.com/dotnet/aspnet:10.0.11-noble AS final

WORKDIR /app

COPY --from=build /app/publish .

EXPOSE 8080

ENV ASPNETCORE_URLS=http://+:8080

USER $APP_UID

ENTRYPOINT ["dotnet", "Mycelium.Bloom.dll"]

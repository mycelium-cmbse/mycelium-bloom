FROM node:24.20.0-bookworm-slim@sha256:ba849c60be29959425b8734d57b8b4b7d56f98edd9504c9af091d5281095a71e AS frontend

WORKDIR /src/Mycelium.Bloom

RUN corepack enable \
    && corepack prepare pnpm@10.34.5+sha512.a4ee05f2f73658255bd6a89859c065a45c28a57daefae2c893a168ee2b73168c37b91e83e57ea67654ad03f03031746430e8bce38e362e042605fb8abc80192e --activate

COPY Mycelium.Bloom/package.json ./
COPY Mycelium.Bloom/pnpm-lock.yaml ./

RUN pnpm config set node-linker hoisted \
    && pnpm install --frozen-lockfile --ignore-scripts

COPY Mycelium.Bloom/ ./

RUN pnpm run css:build

FROM mcr.microsoft.com/dotnet/sdk:10.0.400-noble@sha256:4beef5b8919dcaa2dc924233bd069257e883cc7a061e09088a97d152d6a48510 AS build

WORKDIR /src

COPY NOTICE Nuget.Config ./
COPY Mycelium.Bloom/ ./Mycelium.Bloom/
COPY --from=frontend /src/Mycelium.Bloom/wwwroot/css/app.css ./Mycelium.Bloom/wwwroot/css/app.css

WORKDIR /src/Mycelium.Bloom

RUN dotnet restore Mycelium.Bloom.csproj

RUN dotnet publish Mycelium.Bloom.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish \
    /p:UseAppHost=false

RUN test -s /app/publish/wwwroot/css/app.css.gz
RUN grep -q "css/app.css" /app/publish/Mycelium.Bloom.staticwebassets.endpoints.json
RUN test -s /app/publish/wwwroot/_framework/blazor.web.js
RUN test -s "/app/publish/Resources/Domain Libraries/Quantities and Units/Quantities.json"

FROM mcr.microsoft.com/dotnet/aspnet:10.0.11-noble@sha256:011bb5f30180717b1c8b65822ff2c99bcb96bc65af0164589751b83c7b4949f7 AS final

WORKDIR /app

COPY --from=build /app/publish .

EXPOSE 8080

ENV ASPNETCORE_URLS=http://+:8080

USER $APP_UID

ENTRYPOINT ["dotnet", "Mycelium.Bloom.dll"]

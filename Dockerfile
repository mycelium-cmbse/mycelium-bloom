# ---------- Build stage ----------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /src

# Install Node.js + pnpm for Tailwind build
RUN apt-get update \
    && apt-get install -y curl ca-certificates gnupg \
    && curl -fsSL https://deb.nodesource.com/setup_22.x | bash - \
    && apt-get install -y nodejs \
    && corepack enable \
    && corepack prepare pnpm@10.33.0 --activate \
    && rm -rf /var/lib/apt/lists/*

# Copy project files first for better Docker cache
COPY MyCeliumBloom/MyCeliumBloom.csproj ./MyCeliumBloom/
COPY MyCeliumBloom/package.json ./MyCeliumBloom/
COPY MyCeliumBloom/pnpm-lock.yaml* ./MyCeliumBloom/

WORKDIR /src/MyCeliumBloom

# Install Tailwind dependencies
RUN pnpm config set node-linker hoisted \
    && pnpm install

# Copy the rest of the source code
WORKDIR /src
COPY MyCeliumBloom/ ./MyCeliumBloom/

WORKDIR /src/MyCeliumBloom


# Build Tailwind into wwwroot/css/app.css
RUN pnpm run css:build

# Publish the .NET app
RUN dotnet publish MyCeliumBloom.csproj -c Release -o /app/publish /p:UseAppHost=false


# ---------- Runtime stage ----------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final

WORKDIR /app

COPY --from=build /app/publish .

EXPOSE 8080

ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "MyCeliumBloom.dll"]
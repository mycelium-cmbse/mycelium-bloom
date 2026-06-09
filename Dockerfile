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
COPY MyceliumBloom/MyceliumBloom.csproj ./MyceliumBloom/
COPY MyceliumBloom/package.json ./MyceliumBloom/
COPY MyceliumBloom/pnpm-lock.yaml* ./MyceliumBloom/

WORKDIR /src/MyceliumBloom

# Install Tailwind dependencies
RUN pnpm config set node-linker hoisted \
    && pnpm install

# Copy the rest of the source code
WORKDIR /src
COPY MyceliumBloom/ ./MyceliumBloom/

WORKDIR /src/MyceliumBloom


# Build Tailwind into wwwroot/css/app.css
RUN pnpm run css:build

# Publish the .NET app
RUN dotnet publish MyceliumBloom.csproj -c Release -o /app/publish /p:UseAppHost=false


# ---------- Runtime stage ----------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final

WORKDIR /app

COPY --from=build /app/publish .

EXPOSE 8080

ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "MyceliumBloom.dll"]
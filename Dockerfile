# ==========================================
# Stage 1: Build Angular Frontend SPA
# ==========================================
FROM node:20-alpine AS frontend-build
WORKDIR /src/ui

# Copy package descriptors and install dependencies
COPY ChessAnalyzer.UI/package*.json ./
RUN npm ci

# Copy Angular source and build production bundle
COPY ChessAnalyzer.UI/ ./
RUN npm run build -- --configuration production

# ==========================================
# Stage 2: Build & Publish .NET Web API
# ==========================================
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS backend-build
WORKDIR /src

# Copy solution, nuget config, and project files for caching restore layer
COPY ChessAnalyzer.slnx ./
COPY nuget.config ./
COPY ChessAnalyzer.Domain/ChessAnalyzer.Domain.csproj ChessAnalyzer.Domain/
COPY ChessAnalyzer.Application/ChessAnalyzer.Application.csproj ChessAnalyzer.Application/
COPY ChessAnalyzer.Infrastructure/ChessAnalyzer.Infrastructure.csproj ChessAnalyzer.Infrastructure/
COPY ChessAnalyzer.Api/ChessAnalyzer.Api.csproj ChessAnalyzer.Api/
COPY ChessAnalyzer.UnitTests/ChessAnalyzer.UnitTests.csproj ChessAnalyzer.UnitTests/

RUN dotnet restore ChessAnalyzer.Api/ChessAnalyzer.Api.csproj

# Copy remaining source code
COPY ChessAnalyzer.Domain/ ChessAnalyzer.Domain/
COPY ChessAnalyzer.Application/ ChessAnalyzer.Application/
COPY ChessAnalyzer.Infrastructure/ ChessAnalyzer.Infrastructure/
COPY ChessAnalyzer.Api/ ChessAnalyzer.Api/

WORKDIR /src/ChessAnalyzer.Api
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

# ==========================================
# Stage 3: Final Production Runtime with Stockfish
# ==========================================
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Install native Linux Stockfish chess engine
RUN apt-get update && \
    apt-get install -y --no-install-recommends stockfish ca-certificates curl && \
    rm -rf /var/lib/apt/lists/*

# Verify Stockfish is present
RUN which stockfish || ls -l /usr/games/stockfish

# Copy published .NET application
COPY --from=backend-build /app/publish ./

# Copy compiled Angular SPA assets into wwwroot to serve together
COPY --from=frontend-build /src/ui/dist/chess-analyzer.ui/browser ./wwwroot

# Production Environment Settings
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:10000
ENV PORT=10000
ENV Stockfish__ExecutablePath=/usr/games/stockfish

EXPOSE 10000

ENTRYPOINT ["dotnet", "ChessAnalyzer.Api.dll"]

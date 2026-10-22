# ChessAnalyzer

Full-featured Chess Game Review & Analysis Application built with **Angular 19**, **ASP.NET Core 10**, and **Stockfish 17+**.

---

## Architecture Overview

* **Frontend:** Angular 19 SPA with 60 FPS hardware-accelerated piece animations, chess.com-style move classifications (Brilliant, Great, Best, Excellent, Good, Book, Inaccuracy, Mistake, Blunder), interactive board, and move timeline.
* **Backend:** ASP.NET Core 10 Web API using Clean Architecture (Domain, Application with MediatR, Infrastructure, Api).
* **Chess Engine:** Stockfish UCI engine integration with automatic cross-platform resolution (Windows local binary or Linux `/usr/games/stockfish`).

---

## Deploying to Render (1-Click or Manual)

This repository includes a multi-stage `Dockerfile` and `render.yaml` blueprint optimized for Render's free tier.

### Option A: Deploy with Render Blueprint (`render.yaml`)

1. Go to your [Render Dashboard](https://dashboard.render.com).
2. Click **New +** -> **Blueprint**.
3. Connect your GitHub repository `ChessAnalyzer`.
4. Render will read `render.yaml` and configure the web service automatically.
5. Click **Apply**.

### Option B: Deploy as a Web Service (Manual)

1. Go to your [Render Dashboard](https://dashboard.render.com).
2. Click **New +** -> **Web Service**.
3. Connect your GitHub repository `ChessAnalyzer`.
4. Fill in the following settings:
   * **Name:** `chessanalyzer` (or your preferred name)
   * **Environment:** `Docker`
   * **Branch:** `main`
   * **Plan:** `Free`
   * **Health Check Path:** `/`
5. Click **Create Web Service**.

Render will automatically:
- Compile the Angular frontend.
- Build and publish the ASP.NET Core 10 backend.
- Install native Stockfish on Ubuntu Linux.
- Serve both the SPA and the API on a single URL (`https://<your-service>.onrender.com`).

---

## Local Development

### Prerequisites
- .NET 10 SDK
- Node.js 20+ / npm
- Angular CLI (`npm i -g @angular/cli`)

### Run Backend API
```bash
dotnet run --project ChessAnalyzer.Api/ChessAnalyzer.Api.csproj
```
The API starts at `https://localhost:7155` and `http://localhost:5240`.

### Run Frontend UI
```bash
cd ChessAnalyzer.UI
npm install
npm start
```
The Angular app opens at `http://localhost:4200`.

### Run Unit Tests
```bash
# Backend unit tests (42 tests)
dotnet test

# Frontend unit tests (21 tests)
cd ChessAnalyzer.UI
npm test -- --watch=false --browsers=ChromeHeadless
```

# AudioBookarr

AudioBookarr is an audiobook-focused `*arr`-style manager. The MVP is shaped around familiar Sonarr/Radarr/Lidarr workflows: monitored library items, provider-driven metadata search, root folders, download clients, indexers, activity, system health, and Docker-first deployment.

## MVP Scope

- React web UI with a familiar left navigation and settings layout.
- .NET API host with JSON persistence under `/config`.
- Audible-first metadata search with Open Library fallback.
- Monitored audiobook library persistence.
- Docker path health validation for `/config`, `/audiobooks`, and `/downloads`.
- Integration storage for download clients and indexers.

Goodreads is intentionally deferred because the public API is no longer generally available.

## Local Development

Run the backend:

```bash
dotnet run --project src/AudioBookarr.Api/AudioBookarr.Api.csproj --urls http://localhost:8787
```

Run the frontend:

```bash
cd frontend
npm install
npm run dev
```

Build the frontend into the API host:

```bash
cd frontend
npm run build
```

## Docker

```bash
docker compose up --build
```

The default web UI listens on `http://localhost:8787`.

Volume layout:

- `/config`: app configuration and JSON state.
- `/audiobooks`: final audiobook library folder.
- `/downloads`: download-client output folder.

Keep container paths consistent across AudioBookarr and download clients to avoid import failures.

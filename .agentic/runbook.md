# Runbook

## Run locally
API (port 5102, Development, seeded invented data, in-memory):
```bash
dotnet build src/Plaza.Api/Plaza.Api.csproj          # here: add --configfile /home/claude/tools/offline-nuget.config
ASPNETCORE_URLS=http://localhost:5102 ASPNETCORE_ENVIRONMENT=Development dotnet run --project src/Plaza.Api --no-build
curl -H 'X-Demo-User: u-pieter' localhost:5102/api/groups/lindehof/posts
```
Demo users: `u-naledi` (Lindehof moderator), `u-pieter`, `u-zanele` (both groups), `u-jonas` (unverified, read-only), `u-ayesha` (Kiepersol Park moderator, not a Lindehof member). The header is a **dev-only stand-in for sign-in**; the app refuses to start with it outside Development.

Web (Vite proxies `/api`, `/health`, `/hubs` to 5102; override with `PLAZA_API`):
```bash
cd web && npm ci && npm run dev      # http://localhost:5173, switch residents top right
```

## Tests
```bash
dotnet test                                           # CI (needs NuGet)
/home/claude/tools/run-xunit-offline.sh tests/Plaza.Tests src/Plaza.Api/Plaza.Api.csproj src/Modules/Feed/Plaza.Feed/Plaza.Feed.csproj   # offline here
cd web && npx vitest run && npm run build
```

## Config
| Key | Values | Notes |
|---|---|---|
| `Auth:Mode` | `Demo` (Development only) / `Oidc` | `Oidc` fails fast until the OIDC slice lands |
| `ASPNETCORE_URLS` | e.g. `http://localhost:5102` | |

## Deploy
Not deployed yet (slice 1 is Development-only because of demo auth). Target: one container, two instances behind a load balancer with a SignalR backplane.

## Monitor and alerts
`/health/live`, `/health/ready`. Warning log `Live notification {Type} for group {GroupId} failed` means a push failed (the post is still stored).

## Common incidents
- **Toasts stop arriving:** check the warning above and the browser's live dot; WebSocket must pass the proxy (`ws: true`).
- **All data gone after restart:** expected in slice 1 (in-memory).

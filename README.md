# Plaza

An original community social network for neighbourhood associations: verified residents post notices and safety alerts in their neighbourhood group, members read them newest-first, online members get a live notification, and moderators remove posts with an audit trail. Showcase project: the client (Buurt Forum) and every person, street and number are invented.

## See it in action

![Plaza walkthrough: the Lindehof notice board, a moderator's safety alert arriving as a live toast for another member, and a non-member being refused the group's feed](docs/media/demo.gif)

| Neighbourhood board | Live alert for another member | Non-member refused |
|---|---|---|
| ![Lindehof Residents notice board](docs/media/group.png) | ![Live toast and the new safety alert on top of the feed](docs/media/live-alert.png) | ![Not-a-member message instead of the feed](docs/media/not-a-member.png) |

Full-quality video: [docs/media/demo.mp4](docs/media/demo.mp4). Mobile: [docs/media/mobile.png](docs/media/mobile.png). Recorded from the running app with `demo/` (`cd demo && npm install && npm run record`); the recorded browser is Pieter's, and Naledi's alert is posted through the API as a separate client.

## Design at a glance
**Brief:** 120 fictional neighbourhood associations run on scattered chat groups and email lists, so notices get lost and nobody knows who lives in the area. Plaza gives each neighbourhood one group with verified members, so a safety alert reaches every member within 10 seconds.

**Architecture:** a **modular monolith** (Profiles, Groups, Feed, Events, Notifications) in one ASP.NET Core process with SignalR for live notifications. Five business areas need clear boundaries, but four engineers need one thing to deploy. The architecture advisor rates this "Recommended" for the launch traits and microservices "Poor fit" for a small team. The modules match the services it proposes at global scale, and architecture tests enforce the boundaries, so Notifications can be extracted first when live connections pass ~50k (ADR-0001). Full reasoning, capacity sums, sequence diagrams and failure modes: [`docs/DESIGN.md`](docs/DESIGN.md).

```mermaid
flowchart LR
  resident[Resident / moderator<br/>browser] -->|"REST /api/*<br/>WebSocket /hubs/notifications"| api[Plaza.Api]
  api --> profiles[Profiles] & groups[Groups] & feed[Feed] & events[Events] & notif[Notifications]
  feed -->|contracts| groups & profiles & notif
  api -.->|slice 2| db[(Postgres)]
```


## Run it in GitHub Codespaces

[![Open in GitHub Codespaces](https://github.com/codespaces/badge.svg)](https://codespaces.new/Legion91919/plaza?quickstart=1)

The dev container installs the SDKs, restores packages and runs every test once on first start. The same setup works locally with VS Code's Dev Containers extension.

| What | Command | Port |
|---|---|---|
| API (Plaza) | `ASPNETCORE_ENVIRONMENT=Development ASPNETCORE_URLS=http://0.0.0.0:5102 dotnet run --project src/Plaza.Api` | 5102 |
| Web app | `cd web && npm run dev` | 5173 |
| All tests (same as CI) | `scripts/test-all.sh` | - |

## What it does (slice 1)
- `GET /api/groups/{groupId}/posts`: the group feed, newest first (members only, otherwise 403).
- `POST /api/groups/{groupId}/posts`: verified members post a `notice` or `safetyAlert` (`Idempotency-Key` header supported).
- `/hubs/notifications`: SignalR hub; online members of the group get `notification` events.
- `POST /api/groups/{groupId}/posts/{postId}/removal`: moderators remove a post with a reason, written to the append-only `GET /api/groups/{groupId}/moderation-log`.
- Web app (`web/`): the neighbourhood group page with composer, feed, live toasts and a demo user switcher.

Sign-in is a **development-only** `X-Demo-User` header standing in for OIDC (planned slice).

## Run locally
```bash
ASPNETCORE_URLS=http://localhost:5102 ASPNETCORE_ENVIRONMENT=Development dotnet run --project src/Plaza.Api
cd web && npm ci && npm run dev
```
Tests and details: `.agentic/runbook.md`.

## How this project is organised
Project knowledge lives in the hidden `.agentic/` folder (start with `.agentic/README.md`). AI agents read `AGENTS.md` first.

Built with the Baton model from the Agentic Engineering Kit: frame → map → decide → sketch → slice loop → pass the baton.

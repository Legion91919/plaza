# Handover

What the client needs to own this without us.

- [ ] Source, infrastructure-as-code and pipelines in the client's own accounts
- [ ] Every secret rotated and owned by the client
- [ ] Architecture and decisions explained (`architecture.md`, `decisions/`)
- [ ] Runbook rehearsed by a client engineer
- [ ] Known issues and next steps listed below
- [ ] AI agent setup documented so their team can keep using it

## Known issues
- Data is in memory (lost on restart) until slice 2.
- Sign-in is a dev-only header; the app refuses to start outside Development until OIDC lands.
- Live pushes are best effort (no outbox yet); members who join a group while connected get its toasts after reconnecting.
- No OpenTelemetry or generated OpenAPI client yet (both need NuGet packages).

## Recommended next steps
See `next-step.md` and `backlog.md` (Next/Later).
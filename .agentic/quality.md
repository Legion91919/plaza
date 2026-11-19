# Quality

## Definition of done
- Acceptance criteria met and demonstrated.
- Unit tests for rules, integration tests for each adapter, one end-to-end test per key flow.
- CI green: build, tests, lint, dependency and secret scan, web (vitest + build).
- Module boundaries enforced by architecture tests.
- Docs in `.agentic/` and `docs/DESIGN.md` updated in the same change.

## Service levels
| SLI | SLO |
|---|---|
| API success rate (non-4xx) | 99.9% over 30 days |
| p95 API latency (feed read, post) | < 300 ms |
| Safety-alert delivery to online members (post accepted → toast) | p95 < 10 s (goal < 2 s) |
| Live connection success (negotiate + handshake) | 99% |
| Moderation log completeness (removals with a log entry) | 100% |

## Current proof (slice 1)
- .NET: 45 tests (domain, use case, acceptance over HTTP + WebSocket, architecture), run offline here.
- Web: 19 vitest tests, `npm run build`.

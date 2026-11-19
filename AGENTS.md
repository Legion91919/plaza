# AGENTS.md

Plaza: Neighbourhood associations run on scattered chat groups and email lists, so notices get lost, events are missed and nobody knows who actually lives in the area.

## Read first
Everything about this project lives in the hidden `.agentic/` folder.
Start with `.agentic/README.md`; it says which file answers which question.
Load only the files your task needs.

## Hard rules
- Work in thin vertical slices: one ticket from `.agentic/backlog.md` at a time.
- No change without a test that fails first. Run the full test suite before you finish.
- A decision that is hard to reverse gets an ADR in `.agentic/decisions/`.
- Never commit secrets, real client data or personal data. Use `.env.example`.
- Do not grow this file. New knowledge goes in the right `.agentic/` file; history goes in `.agentic/changelog.md`.
- When you learn something that would help on the next project, add it to `.agentic/lessons.md`.

## What is live
- Slice 1: post to a neighbourhood group, newest-first feed, 403 for non-members, SignalR live notifications, moderator removal with append-only log. API port 5102, web in `web/`.
- Dev-only sign-in via `X-Demo-User`; in-memory data. Design: `docs/DESIGN.md`.

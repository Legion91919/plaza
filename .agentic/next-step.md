# Next step

- **Doing:** nothing in progress; slice 1 is done.
- **Done so far:** Sketch leg (`ux/`), slice 1 API + web, 45 .NET tests + 19 vitest tests, design dossier (`docs/DESIGN.md`).
- **Next action:** start slice 2: add EF Core + Npgsql, implement `IPostStore`/`IModerationLog`/group and profile stores on Postgres (one schema per module, insert-only moderation table), add the transactional outbox for `ILiveNotifier`.
- **Blocked by:** NuGet restore (blocked in this workspace; fine in CI).

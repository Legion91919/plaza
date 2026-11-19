# Backlog

Vertical slices, smallest valuable first.

## Now
- [x] Sketch leg: tokens + three variants of the slice-1 screen (`sketch-ui`)
- [x] Slice 1: A verified resident posts a notice in their neighbourhood group and members see it in the group feed and get a live notification
  - Given a resident is a member of a group, when they post, then the post appears in the group feed newest-first
  - Given a non-member, then they can't read the group feed (403)
  - Given a new post, then online members receive a live notification
  - Given a moderator removes a post, then it disappears from the feed and the removal is logged with who and why
  - Test notes: one test per criterion; seeded invented data.

## Next
- [ ] Slice 2: persistence (Postgres via EF Core) behind the same repository interfaces
- [ ] CI green with all gates

## Later
- [ ] OIDC sign-in replacing the dev-only `X-Demo-User` header (`AuthSetup` only)
- [ ] Transactional outbox for live notifications; web push/SMS for offline members
- [ ] Cached per-group feed read model + `before=` cursor paging
- [ ] Address verification flow (Profiles); "new since your last visit" counter (from sketch variant C)
- [ ] Events module: real calendar behind `IEventCalendar`
- [ ] Rate limits on posting; OpenTelemetry once package restore is available

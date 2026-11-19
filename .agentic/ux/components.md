# Components (group page)

Each component designs four states: **loading**, **empty**, **error**, **success**.

| Component | Props | Loading | Empty | Error | Success |
|---|---|---|---|---|---|
| `TopBar` | `users: DemoUser[]`, `currentUserId`, `onSwitchUser(id)`, `liveStatus: 'connecting'\|'live'\|'offline'` | user list placeholder "Loading residents…" | "No demo residents" | "Can't reach Plaza" text, switcher disabled | logo, live dot, demo user switcher |
| `GroupHeader` | `group?: Group`, `state` | skeleton title | n/a (group always exists) | 403 → "You're not a member of this group"; 404 → "Group not found" | area, name, member count, your role |
| `Composer` | `groupName`, `canPost`, `reasonIfNot?`, `onSubmit({body, kind})`, `submitting`, `error?` | button shows "Posting…", disabled | empty textarea, Post disabled until 3+ chars | inline `role=alert` message, draft kept | clears and focuses the textarea |
| `FeedList` | `posts: Post[]`, `state`, `canModerate`, `onRemove(post)`, `onRetry()` | 3 skeleton cards, `aria-busy` | "No notices yet" box | error box with "Try again" | newest-first `PostCard`s |
| `PostCard` | `post: Post`, `canModerate`, `onRemove` | n/a | n/a | n/a | avatar initials, name, verified line, relative time, kind tag, body; alert gets red edge + label |
| `RemoveDialog` | `post`, `onConfirm(reason)`, `onCancel`, `submitting`, `error?` | "Removing…" | reason empty → confirm disabled | inline error (e.g. already removed) | closes; card leaves the feed |
| `Toast` | `notification: LiveNotification`, `onDismiss` | n/a | nothing rendered | n/a | `role=status` toast, auto-dismiss 6 s; red "Safety" tag for alerts |
| `EventsPanel` | `events: Event[]`, `state` | "Loading events…" | "No events yet (events arrive in a later slice)" | "Events unavailable" | list (none in slice 1) |

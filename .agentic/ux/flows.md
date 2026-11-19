# User flows

## Flow 1: post a notice and tell online neighbours (slice 1)
1. Resident opens the group page → `screens/group-page.html` · `GET /api/me`, `GET /api/groups/{groupId}`, `GET /api/groups/{groupId}/posts` (newest first).
2. The page connects to the live hub → `TopBar` live dot · SignalR `/hubs/notifications?demoUser={id}`; the server adds the connection to `group:{groupId}` for every group the user belongs to; `Subscriptions()` confirms.
3. Resident writes a notice or safety alert in `Composer` → `POST /api/groups/{groupId}/posts` with an `Idempotency-Key` header. 201 adds it to the top of `FeedList`; 403 `not_verified` shows "Verify your address before posting".
4. Online members of that group see a `Toast` → hub method `notification` with `type=post.created`; their `FeedList` refreshes.
5. A non-member opening the same URL gets 403 `not_a_member` → `GroupHeader` error state.

## Flow 2: moderator removes a post (slice 1)
1. Moderator taps "Remove…" on a `PostCard` → `RemoveDialog` asks for a reason (5–500 characters).
2. `POST /api/groups/{groupId}/posts/{postId}/removal` `{ reason }` → 200 with the moderation log entry; members' feeds drop it via `type=post.removed`.
3. The moderator can review `GET /api/groups/{groupId}/moderation-log` (append-only: who, when, why).

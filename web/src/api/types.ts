export type GroupRole = 'member' | 'moderator';
export type PostKind = 'notice' | 'safetyAlert';

export interface GroupMembership { groupId: string; name: string; area: string; role: GroupRole }
export interface Me { userId: string; displayName: string; isVerified: boolean; groups: GroupMembership[] }
export interface Group { groupId: string; name: string; area: string; memberCount: number; yourRole: GroupRole }

export interface Post {
  postId: string;
  groupId: string;
  authorId: string;
  authorName: string;
  authorVerified: boolean;
  kind: PostKind;
  body: string;
  postedAt: string;
}

export interface ModerationEntry {
  sequence: number;
  action: string;
  groupId: string;
  postId: string;
  postAuthorId: string;
  moderatorId: string;
  reason: string;
  at: string;
}

export interface EventSummary { eventId: string; groupId: string; title: string; startsAt: string; venue: string }

/** Pushed by the hub method `notification`. */
export interface LiveNotification {
  type: 'post.created' | 'post.removed';
  groupId: string;
  postId: string;
  authorId: string;
  authorName: string;
  kind: PostKind;
  preview: string;
  occurredAt: string;
}

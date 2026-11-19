import { useState, type FormEvent } from 'react';
import type { PostKind } from '../api/types';
import { MAX_BODY, validateBody } from '../lib/feed';

interface Props {
  groupName: string;
  canPost: boolean;
  reasonIfNot?: string;
  onSubmit: (body: string, kind: PostKind, idempotencyKey: string) => Promise<void>;
  newKey?: () => string;
}

/** Inline composer. One idempotency key per draft, so a retry after a network error can't double-post. */
export function Composer({ groupName, canPost, reasonIfNot, onSubmit, newKey = () => crypto.randomUUID() }: Props) {
  const [body, setBody] = useState('');
  const [kind, setKind] = useState<PostKind>('notice');
  const [key, setKey] = useState(newKey);
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const invalid = validateBody(body);

  async function submit(e: FormEvent) {
    e.preventDefault();
    if (invalid || submitting) return;
    setSubmitting(true);
    setError(null);
    try {
      await onSubmit(body.trim(), kind, key);
      setBody('');
      setKind('notice');
      setKey(newKey());
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Could not post. Your draft is safe.');
    } finally {
      setSubmitting(false);
    }
  }

  if (!canPost) {
    return <p className="composer muted" role="note">{reasonIfNot}</p>;
  }

  return (
    <form className="composer" aria-label="Post a notice" onSubmit={submit}>
      <label htmlFor="notice-body" className="small"><b>Post a notice</b></label>
      <textarea id="notice-body" value={body} maxLength={MAX_BODY} placeholder="What should your neighbours know?"
        onChange={(e) => setBody(e.target.value)} />
      <div className="row">
        <div className="seg" role="radiogroup" aria-label="Kind">
          <label><input type="radio" name="kind" checked={kind === 'notice'} onChange={() => setKind('notice')} />Notice</label>
          <label className="al"><input type="radio" name="kind" checked={kind === 'safetyAlert'} onChange={() => setKind('safetyAlert')} />Safety alert</label>
        </div>
        <span className="muted small">{body.length} / {MAX_BODY}</span>
        <button className="btn" type="submit" disabled={!!invalid || submitting}>
          {submitting ? 'Posting…' : `Post to ${groupName}`}
        </button>
      </div>
      {error && <p role="alert" className="form-error">{error}</p>}
    </form>
  );
}

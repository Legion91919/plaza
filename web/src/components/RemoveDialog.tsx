import { useState } from 'react';
import type { Post } from '../api/types';

interface Props { post: Post; onConfirm: (reason: string) => Promise<void>; onCancel: () => void }

export const MIN_REASON = 5;

export function RemoveDialog({ post, onConfirm, onCancel }: Props) {
  const [reason, setReason] = useState('');
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function confirm() {
    setSubmitting(true);
    setError(null);
    try {
      await onConfirm(reason.trim());
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Could not remove the post.');
      setSubmitting(false);
    }
  }

  return (
    <div className="backdrop">
      <div className="dialog" role="dialog" aria-modal="true" aria-labelledby="remove-title">
        <h2 id="remove-title">Remove this post?</h2>
        <p className="muted small">By {post.authorName}: “{post.body.slice(0, 120)}”</p>
        <label htmlFor="remove-reason" className="small"><b>Reason (kept in the moderation log)</b></label>
        <textarea id="remove-reason" autoFocus value={reason} onChange={(e) => setReason(e.target.value)} maxLength={500} />
        {error && <p role="alert" className="form-error">{error}</p>}
        <div className="row end">
          <button className="btn ghost" onClick={onCancel}>Cancel</button>
          <button className="btn danger" disabled={reason.trim().length < MIN_REASON || submitting} onClick={confirm}>
            {submitting ? 'Removing…' : 'Remove post'}
          </button>
        </div>
      </div>
    </div>
  );
}

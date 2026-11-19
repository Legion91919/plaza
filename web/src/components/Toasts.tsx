import { useEffect } from 'react';
import type { LiveNotification } from '../api/types';

export interface ToastItem { id: string; groupName: string; n: LiveNotification }

export function Toasts({ items, onDismiss }: { items: ToastItem[]; onDismiss: (id: string) => void }) {
  useEffect(() => {
    const timers = items.map((t) => setTimeout(() => onDismiss(t.id), 6000));
    return () => timers.forEach(clearTimeout);
  }, [items, onDismiss]);

  return (
    <div className="toasts" role="status" aria-live="polite">
      {items.map(({ id, groupName, n }) => (
        <div key={id} className="toast">
          <span className={`tag${n.kind === 'safetyAlert' ? ' alert' : ''}`}>{n.kind === 'safetyAlert' ? 'Safety' : 'Notice'}</span>
          <div><b>{n.authorName} in {groupName}</b>{n.preview}</div>
          <button className="toast-x" aria-label="Dismiss" onClick={() => onDismiss(id)}>×</button>
        </div>
      ))}
    </div>
  );
}

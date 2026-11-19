/** Plaza mark: four paving tiles of a town square, one in the accent colour. */
export function Logo() {
  return (
    <span className="logo">
      <svg width="28" height="28" viewBox="0 0 28 28" aria-hidden="true">
        <rect x="1" y="1" width="12" height="12" rx="3" fill="var(--color-accent)" />
        <rect x="15" y="1" width="12" height="12" rx="3" fill="var(--color-border)" />
        <rect x="1" y="15" width="12" height="12" rx="3" fill="var(--color-border)" />
        <rect x="15" y="15" width="12" height="12" rx="3" fill="var(--color-text)" />
      </svg>
      Plaza
    </span>
  );
}

# Decision: neighbourhood group page

Reviewed at 1440px and 390px (headless Chrome screenshots). Scores 1–5.

| Variant | Main job speed | Clarity | Accessibility | Tone | Total |
|---|---|---|---|---|---|
| A: dense list (`group-page-a.html`) | 5 (composer always visible, keyboard keys) | 3 (rows crowd on mobile; alert only a small tag) | 4 (table semantics, labels) | 2 (feels like an admin tool) | 14 |
| B: cards (`group-page-b.html`) | 3 (posting hides behind a floating button) | 5 (one post per card, alert has a red edge **and** a label) | 4 (FAB overlaps content on mobile) | 5 (warm, civic) | 17 |
| C: guided (`group-page-c.html`) | 2 (three steps to post a two-line notice) | 4 ("2 new since your last visit" is useful) | 4 (radio cards are large targets) | 4 | 14 |

## Pick
**B's cards with A's always-visible composer** and C's two-option kind choice as a segmented control.
Residents read far more than they post (ReadHeavy), so cards win on clarity; but posting must stay one tap away,
so the composer sits inline above the feed instead of behind a floating button. Safety alerts get a red left
edge plus a "Safety alert" text tag, so colour is never the only signal. C's "new since last visit" counter
goes to the backlog (needs read-markers, slice 3+).

Final: `screens/group-page.html` (state switcher bottom-left: loading, empty, error, success).

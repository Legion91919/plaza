import { readFileSync } from 'node:fs';
import { fileURLToPath } from 'node:url';

const here = (p: string) => fileURLToPath(new URL(p, import.meta.url));

describe('design tokens', () => {
  it('tokens.css matches .agentic/ux/design-tokens.json', () => {
    const tokens = JSON.parse(readFileSync(here('../../../.agentic/ux/design-tokens.json'), 'utf8'));
    const css = readFileSync(here('./tokens.css'), 'utf8');
    for (const [name, value] of Object.entries<string>(tokens.color)) {
      expect(css).toContain(`--color-${name}: ${value};`);
    }
    for (const [name, value] of Object.entries<string>(tokens.space)) {
      expect(css).toContain(`--space-${name}: ${value};`);
    }
  });

  it('components never hard-code colours', () => {
    const css = readFileSync(here('./app.css'), 'utf8');
    expect(css.match(/#[0-9a-f]{3,8}\b/gi)).toBeNull();
  });
});

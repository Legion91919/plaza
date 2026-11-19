import { initials, relativeTime } from './time';

describe('relativeTime', () => {
  const now = new Date('2026-11-19T12:00:00Z');
  it.each([
    ['2026-11-19T11:59:40Z', 'just now'],
    ['2026-11-19T11:20:00Z', '40 min ago'],
    ['2026-11-19T07:00:00Z', '5 h ago'],
    ['2026-11-18T09:00:00Z', 'yesterday'],
    ['2026-11-16T12:00:00Z', '3 days ago'],
  ])('%s -> %s', (iso, expected) => expect(relativeTime(iso, now)).toBe(expected));

  it('builds avatar initials from the first two words', () => {
    expect(initials('Naledi Venter')).toBe('NV');
    expect(initials('ayesha')).toBe('A');
  });
});

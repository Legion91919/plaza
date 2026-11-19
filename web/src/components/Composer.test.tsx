import { act, fireEvent, render, screen } from '@testing-library/react';
import { Composer } from './Composer';

describe('Composer', () => {
  it('keeps Post disabled until the notice is long enough, then submits body, kind and key', async () => {
    const onSubmit = vi.fn().mockResolvedValue(undefined);
    let n = 0;
    render(<Composer groupName="Lindehof" canPost onSubmit={onSubmit} newKey={() => `key-${++n}`} />);
    const button = screen.getByRole('button', { name: 'Post to Lindehof' });
    expect(button).toHaveProperty('disabled', true);

    fireEvent.change(screen.getByRole('textbox'), { target: { value: '  Streetlight out on Vink Street  ' } });
    fireEvent.click(screen.getByLabelText('Safety alert'));
    await act(async () => { fireEvent.click(button); });

    expect(onSubmit).toHaveBeenCalledWith('Streetlight out on Vink Street', 'safetyAlert', 'key-1');
    expect((screen.getByRole('textbox') as HTMLTextAreaElement).value).toBe('');
  });

  it('keeps the draft and the same idempotency key when posting fails, so a retry cannot double-post', async () => {
    const onSubmit = vi.fn().mockRejectedValueOnce(new Error('Network down')).mockResolvedValue(undefined);
    let n = 0;
    render(<Composer groupName="Lindehof" canPost onSubmit={onSubmit} newKey={() => `key-${++n}`} />);
    fireEvent.change(screen.getByRole('textbox'), { target: { value: 'Water off on Saturday' } });

    await act(async () => { fireEvent.click(screen.getByRole('button', { name: 'Post to Lindehof' })); });
    expect(screen.getByRole('alert').textContent).toBe('Network down');
    expect((screen.getByRole('textbox') as HTMLTextAreaElement).value).toBe('Water off on Saturday');

    await act(async () => { fireEvent.click(screen.getByRole('button', { name: 'Post to Lindehof' })); });
    expect(onSubmit.mock.calls.map((c) => c[2])).toEqual(['key-1', 'key-1']);
  });

  it('explains why an unverified resident cannot post', () => {
    render(<Composer groupName="Lindehof" canPost={false} reasonIfNot="Verify your address to post here." onSubmit={vi.fn()} />);
    expect(screen.getByRole('note').textContent).toBe('Verify your address to post here.');
    expect(screen.queryByRole('textbox')).toBeNull();
  });
});

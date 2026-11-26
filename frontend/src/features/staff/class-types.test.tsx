import {
  fireEvent,
  render,
  screen,
  waitFor,
  within,
} from '@testing-library/react';
import { expect, test, vi } from 'vitest';
import { ClassTypes } from './class-types';

// Synthetic class types.
const classTypes = [
  { id: '0199a3c0-0000-7000-8000-0000000000a1', name: 'Spinning' },
  { id: '0199a3c0-0000-7000-8000-0000000000a2', name: 'Yoga' },
];

function renderTypes(
  onCreate = vi.fn().mockResolvedValue('saved'),
  onRename = vi.fn().mockResolvedValue('saved'),
) {
  render(
    <ClassTypes
      classTypes={classTypes}
      onCreate={onCreate}
      onRename={onRename}
    />,
  );
  return { onCreate, onRename };
}

test('lists the class types in the order given', () => {
  renderTypes();
  const list = screen.getByRole('list', { name: 'Class types' });
  expect(
    within(list)
      .getAllByRole('listitem')
      .map((item) => item.textContent),
  ).toEqual(['SpinningRename', 'YogaRename']);
});

test('adds a class type with a trimmed name and clears the field', async () => {
  const { onCreate } = renderTypes();
  const field = screen.getByLabelText('New class type');

  fireEvent.change(field, { target: { value: '  Pilates ' } });
  fireEvent.click(screen.getByRole('button', { name: 'Add' }));

  await waitFor(() => expect(onCreate).toHaveBeenCalledWith('Pilates'));
  await waitFor(() => expect(field).toHaveValue(''));
});

test('asks for a name before it calls the API', () => {
  const { onCreate } = renderTypes();

  fireEvent.click(screen.getByRole('button', { name: 'Add' }));

  expect(screen.getByText('Enter a name.')).toBeInTheDocument();
  expect(screen.getByLabelText('New class type')).toHaveAttribute(
    'aria-invalid',
    'true',
  );
  expect(onCreate).not.toHaveBeenCalled();
});

test('shows a name the API refused', async () => {
  renderTypes(
    vi.fn().mockResolvedValue({
      errors: { '': ['A class type with that name already exists.'] },
    }),
  );

  fireEvent.change(screen.getByLabelText('New class type'), {
    target: { value: 'yoga' },
  });
  fireEvent.click(screen.getByRole('button', { name: 'Add' }));

  expect(
    await screen.findByText('A class type with that name already exists.'),
  ).toBeInTheDocument();
});

test('renames a class type in place', async () => {
  const { onRename } = renderTypes();

  fireEvent.click(screen.getByRole('button', { name: 'Rename Yoga' }));
  const field = screen.getByLabelText('New name for Yoga');
  expect(field).toHaveValue('Yoga');
  fireEvent.change(field, { target: { value: 'Yin yoga' } });
  fireEvent.click(screen.getByRole('button', { name: 'Save' }));

  await waitFor(() =>
    expect(onRename).toHaveBeenCalledWith(
      '0199a3c0-0000-7000-8000-0000000000a2',
      'Yin yoga',
    ),
  );
  await waitFor(() =>
    expect(
      screen.queryByLabelText('New name for Yoga'),
    ).not.toBeInTheDocument(),
  );
});

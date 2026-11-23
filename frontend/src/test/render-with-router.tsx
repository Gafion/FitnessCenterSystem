import {
  createMemoryHistory,
  createRootRoute,
  createRouter,
  RouterProvider,
} from '@tanstack/react-router';
import { render } from '@testing-library/react';
import type { JSX } from 'react';

/** Renders a page component inside a router, so its links work. */
export async function renderWithRouter(Page: () => JSX.Element) {
  const router = createRouter({
    routeTree: createRootRoute({ component: Page }),
    history: createMemoryHistory(),
  });
  const result = render(<RouterProvider router={router} />);
  await router.load();
  return result;
}

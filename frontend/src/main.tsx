import { createRouter, RouterProvider } from '@tanstack/react-router';
import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
import i18n from './i18n';
import './index.css';
import { PageError, PageLoading } from './components/shell/page';
import { routeTree } from './routeTree.gen';

const router = createRouter({
  routeTree,
  defaultPendingComponent: PageLoading,
  defaultErrorComponent: PageError,
});

// The API answers in the app's language (D-065), so a switch reloads what the page shows.
i18n.on('languageChanged', () => {
  void router.invalidate();
});

declare module '@tanstack/react-router' {
  interface Register {
    router: typeof router;
  }
}

const root = document.getElementById('root');
if (!root) {
  throw new Error('Missing #root element in index.html');
}

createRoot(root).render(
  <StrictMode>
    <RouterProvider router={router} />
  </StrictMode>,
);

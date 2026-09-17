import './styles.css';

import { ApiError } from '@armenu/api-client';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { RouterProvider } from '@tanstack/react-router';
import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';

import { createAppRouter } from './app/router.tsx';
import { I18nProvider } from './i18n/I18nProvider.tsx';
import { Toaster } from './ui/Toaster.tsx';

const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      // Answers about the request itself (not found, forbidden, invalid) do not change by retrying.
      retry: (failureCount, error) => failureCount < 2 && !(error instanceof ApiError && error.status < 500),
    },
  },
});

const router = createAppRouter(queryClient);

const root = document.getElementById('root');
if (root === null) {
  throw new Error('index.html has no #root element.');
}

createRoot(root).render(
  <StrictMode>
    <I18nProvider>
      <Toaster>
        <QueryClientProvider client={queryClient}>
          <RouterProvider router={router} />
        </QueryClientProvider>
      </Toaster>
    </I18nProvider>
  </StrictMode>,
);

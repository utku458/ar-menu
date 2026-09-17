import './styles.css';

import { startMenuPreload } from './features/menu/menu-preload.ts';

// This entry stays tiny on purpose: the menu request starts now, while the application downloads in parallel
// (its chunks are preloaded from index.html by the initial-load build plugin).
startMenuPreload(window.location);

import('./app/start.tsx').catch((error: unknown) => {
  console.error(error);
  document.body.textContent = 'The menu could not be opened. Please reload the page.';
});

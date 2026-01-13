import { RenderMode, ServerRoute } from '@angular/ssr';

export const serverRoutes: ServerRoute[] = [
  {
    path: '',
    renderMode: RenderMode.Client, // Root redirects to session-management, use client
  },
  {
    path: 'health',
    renderMode: RenderMode.Client, // Health page makes API calls, can't prerender
  },
  {
    path: 'session-management/**',
    renderMode: RenderMode.Client, // Session management makes API calls, can't prerender
  },
  {
    path: '**',
    renderMode: RenderMode.Client, // Default to client rendering for this admin app
  },
];

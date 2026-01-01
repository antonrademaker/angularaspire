import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: '',
    redirectTo: '/session-management/sessions',
    pathMatch: 'full'
  },
  {
    path: 'session-management',
    children: [
      {
        path: 'tracks',
        loadComponent: () => import('./session-management/track-manager.component').then(c => c.TrackManagerComponent),
        data: { prerender: false }
      },
      {
        path: 'sessions',
        loadComponent: () => import('./session-management/session-manager.component').then(c => c.SessionManagerComponent),
        data: { prerender: false }
      }
    ]
  }
];

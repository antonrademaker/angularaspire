import { Routes } from '@angular/router';

export const routes: Routes = [
  { path: '', redirectTo: '/events', pathMatch: 'full' },
  { 
    path: 'events', 
    loadComponent: () => import('./event-management/event-list.component').then(c => c.EventListComponent) 
  },
  {
    path: 'events/:eventId/sessions',
    loadComponent: () => import('./session-management/session-browser.component').then(c => c.SessionBrowserComponent)
  },
  {
    path: 'register/:eventId',
    loadComponent: () => import('./registration/registration-form.component').then(c => c.RegistrationFormComponent)
  },
  { path: '**', redirectTo: '/events' }
];

import { Routes } from '@angular/router';

export const routes: Routes = [
  { path: '', redirectTo: '/events', pathMatch: 'full' },
  { 
    path: 'events', 
    loadComponent: () => import('./event-management/event-list.component').then(c => c.EventListComponent) 
  },
  {
    path: 'register/:eventId',
    loadComponent: () => import('./registration/registration-form.component').then(c => c.RegistrationFormComponent)
  },
  { path: '**', redirectTo: '/events' }
];

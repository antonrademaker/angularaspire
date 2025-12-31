import { Routes } from '@angular/router';

export const routes: Routes = [
  { path: '', redirectTo: '/events', pathMatch: 'full' },
  { 
    path: 'events', 
    loadComponent: () => import('./event-management/event-list.component').then(c => c.EventListComponent) 
  },
  { path: '**', redirectTo: '/events' }
];

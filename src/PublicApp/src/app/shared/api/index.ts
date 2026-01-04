/**
 * API Client Module
 * 
 * This module exports the generated TypeScript API client for the Event Management API.
 * 
 * Usage:
 * 1. Import the API_BASE_URL token in your app.module.ts
 * 2. Provide the API base URL in the providers array
 * 3. Inject ApiClient into your services/components
 * 
 * Example:
 * ```typescript
 * import { API_BASE_URL, ApiClient } from './shared/api';
 * 
 * @NgModule({
 *   providers: [
 *     { provide: API_BASE_URL, useValue: environment.apiUrl }
 *   ]
 * })
 * export class AppModule { }
 * ```
 */

export * from './api-client.generated';

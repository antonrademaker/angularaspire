import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { EventOrganizer, AddOrganizerRequest } from '../models/organizer.model';

@Injectable({
  providedIn: 'root',
})
export class OrganizerService {
  private apiUrl = '/api/events';

  constructor(private http: HttpClient) {}

  getOrganizers(eventId: string): Observable<EventOrganizer[]> {
    return this.http.get<EventOrganizer[]>(`${this.apiUrl}/${eventId}/organizers`);
  }

  addOrganizer(eventId: string, request: AddOrganizerRequest): Observable<void> {
    return this.http.post<void>(`${this.apiUrl}/${eventId}/organizers`, request);
  }

  removeOrganizer(eventId: string, personId: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${eventId}/organizers/${personId}`);
  }
}

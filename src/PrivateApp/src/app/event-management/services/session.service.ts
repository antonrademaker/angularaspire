import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Session, CreateSessionRequest, UpdateSessionRequest, AssignSessionRequest } from '../models/session.model';

@Injectable({
  providedIn: 'root'
})
export class SessionService {
  private apiUrl = '/api/events';

  constructor(private http: HttpClient) { }

  getSessions(eventId: string): Observable<Session[]> {
    return this.http.get<Session[]>(`${this.apiUrl}/${eventId}/sessions`);
  }

  createSession(eventId: string, request: CreateSessionRequest): Observable<Session> {
    return this.http.post<Session>(`${this.apiUrl}/${eventId}/sessions`, request);
  }

  updateSession(eventId: string, sessionId: string, request: UpdateSessionRequest): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/${eventId}/sessions/${sessionId}`, request);
  }

  deleteSession(eventId: string, sessionId: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${eventId}/sessions/${sessionId}`);
  }

  assignSession(eventId: string, sessionId: string, request: AssignSessionRequest): Observable<void> {
    return this.http.post<void>(`${this.apiUrl}/${eventId}/sessions/${sessionId}/assign`, request);
  }

  unassignSession(eventId: string, sessionId: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${eventId}/sessions/${sessionId}/assign`);
  }

  swapSessions(eventId: string, firstSessionId: string, secondSessionId: string): Observable<void> {
    return this.http.post<void>(`${this.apiUrl}/${eventId}/sessions/swap`, { firstSessionId, secondSessionId });
  }
}

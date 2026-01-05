import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Event, EventSearchResult, EventSearchRequest } from '../models/event.model';

@Injectable({
  providedIn: 'root'
})
export class EventService {
  private apiUrl = '/api/events';

  constructor(private http: HttpClient) { }

  getEvents(request: EventSearchRequest): Observable<EventSearchResult> {
    let params = new HttpParams();

    if (request.searchTerm) params = params.set('searchTerm', request.searchTerm);
    if (request.startDate) params = params.set('startDate', request.startDate);
    if (request.endDate) params = params.set('endDate', request.endDate);
    if (request.status !== undefined) params = params.set('status', request.status.toString());
    if (request.page) params = params.set('page', request.page.toString());
    if (request.pageSize) params = params.set('pageSize', request.pageSize.toString());
    if (request.sortAscending !== undefined) params = params.set('sortAscending', request.sortAscending.toString());

    return this.http.get<EventSearchResult>(this.apiUrl, { params });
  }

  getEvent(id: string): Observable<Event> {
    return this.http.get<Event>(`${this.apiUrl}/${id}`);
  }

  createEvent(event: any): Observable<Event> {
    return this.http.post<Event>(this.apiUrl, event);
  }

  updateEvent(id: string, event: any): Observable<Event> {
    return this.http.put<Event>(`${this.apiUrl}/${id}`, event);
  }

  deleteEvent(id: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }
}

import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Track, CreateTrackRequest, UpdateTrackRequest } from '../models/track.model';

@Injectable({
  providedIn: 'root'
})
export class TrackService {
  private apiUrl = '/api/events';

  constructor(private http: HttpClient) { }

  getTracks(eventId: string): Observable<Track[]> {
    return this.http.get<Track[]>(`${this.apiUrl}/${eventId}/tracks`);
  }

  createTrack(eventId: string, request: CreateTrackRequest): Observable<Track> {
    return this.http.post<Track>(`${this.apiUrl}/${eventId}/tracks`, request);
  }

  updateTrack(eventId: string, trackId: string, request: UpdateTrackRequest): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/${eventId}/tracks/${trackId}`, request);
  }

  deleteTrack(eventId: string, trackId: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${eventId}/tracks/${trackId}`);
  }

  reorderTracks(eventId: string, trackIds: string[]): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/${eventId}/tracks/reorder`, trackIds);
  }
}

import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { TimeSlot, CreateTimeSlotRequest, UpdateTimeSlotRequest } from '../models/time-slot.model';

@Injectable({
  providedIn: 'root'
})
export class TimeSlotService {
  private apiUrl = '/api/events';

  constructor(private http: HttpClient) { }

  getTimeSlots(eventId: string): Observable<TimeSlot[]> {
    return this.http.get<TimeSlot[]>(`${this.apiUrl}/${eventId}/time-slots`);
  }

  createTimeSlot(eventId: string, request: CreateTimeSlotRequest): Observable<TimeSlot> {
    return this.http.post<TimeSlot>(`${this.apiUrl}/${eventId}/time-slots`, request);
  }

  updateTimeSlot(eventId: string, timeSlotId: string, request: UpdateTimeSlotRequest): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/${eventId}/time-slots/${timeSlotId}`, request);
  }

  deleteTimeSlot(eventId: string, timeSlotId: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${eventId}/time-slots/${timeSlotId}`);
  }
}

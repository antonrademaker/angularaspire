import { Injectable } from '@angular/core';
import { CdkDragDrop, moveItemInArray, transferArrayItem } from '@angular/cdk/drag-drop';
import { Session, SessionAssignment } from '../models/session.model';
import { SessionService } from './session.service';
import { MatSnackBar } from '@angular/material/snack-bar';

@Injectable({
  providedIn: 'root'
})
export class ScheduleDragDropService {

  constructor(
    private sessionService: SessionService,
    private snackBar: MatSnackBar
  ) { }

  handleDrop(event: CdkDragDrop<Session[] | any>, eventId: string, trackId?: string, timeSlotId?: string): void {
    if (event.previousContainer === event.container) {
      // Reordering within the same list (not applicable for schedule grid usually, but maybe for sidebar)
      moveItemInArray(event.container.data, event.previousIndex, event.currentIndex);
    } else {
      // Moving from sidebar to grid or grid to sidebar
      const session = event.item.data as Session;
      
      if (trackId && timeSlotId) {
        // Dropped onto a slot
        this.assignSession(eventId, session, trackId, timeSlotId, event);
      } else {
        // Dropped back to sidebar (unassign)
        this.unassignSession(eventId, session, event);
      }
    }
  }

  private assignSession(eventId: string, session: Session, trackId: string, timeSlotId: string, event: CdkDragDrop<Session[] | any>): void {
    this.sessionService.assignSession(eventId, session.id, {
      trackId: trackId,
      timeSlotId: timeSlotId
    }).subscribe({
      next: () => {
        transferArrayItem(
          event.previousContainer.data,
          event.container.data,
          event.previousIndex,
          event.currentIndex
        );
        this.snackBar.open('Session assigned successfully', 'Close', { duration: 3000 });
      },
      error: (err) => {
        console.error('Failed to assign session', err);
        this.snackBar.open('Failed to assign session', 'Close', { duration: 3000 });
      }
    });
  }

  private unassignSession(eventId: string, session: Session, event: CdkDragDrop<Session[] | any>): void {
    this.sessionService.unassignSession(eventId, session.id).subscribe({
      next: () => {
        transferArrayItem(
          event.previousContainer.data,
          event.container.data,
          event.previousIndex,
          event.currentIndex
        );
        this.snackBar.open('Session unassigned', 'Close', { duration: 3000 });
      },
      error: (err) => {
        console.error('Failed to unassign session', err);
        this.snackBar.open('Failed to unassign session', 'Close', { duration: 3000 });
      }
    });
  }
}

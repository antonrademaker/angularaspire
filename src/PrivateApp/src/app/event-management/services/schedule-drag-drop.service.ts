import { Injectable } from '@angular/core';
import { CdkDragDrop, moveItemInArray, transferArrayItem } from '@angular/cdk/drag-drop';
import { Session } from '../models/session.model';
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
      // Moving from sidebar to grid or grid to sidebar or grid to grid
      const session = event.item.data as Session;

      if (trackId && timeSlotId) {
        // Dropped onto a slot
        if (event.container.data.length > 0) {
          // Target slot is occupied - Swap
          const targetSession = event.container.data[0] as Session;
          this.swapSessions(eventId, session, targetSession, event);
        } else {
          // Target slot is empty - Assign
          this.assignSession(eventId, session, trackId, timeSlotId, event);
        }
      } else {
        // Dropped back to sidebar (unassign)
        this.unassignSession(eventId, session, event);
      }
    }
  }

  private swapSessions(eventId: string, session1: Session, session2: Session, event: CdkDragDrop<Session[] | any>): void {
    this.sessionService.swapSessions(eventId, session1.id, session2.id).subscribe({
      next: () => {
        // Manually swap items in the arrays
        const sourceContainer = event.previousContainer;
        const targetContainer = event.container;

        // Remove session1 from source
        // Note: transferArrayItem removes from source and adds to target, but we need to swap.

        // We can't easily use transferArrayItem for a swap because it's one-way.
        // So we manipulate the data arrays directly.

        // 1. Remove session1 from source
        sourceContainer.data.splice(event.previousIndex, 1);

        // 2. Remove session2 from target (it should be at index 0 or we find it)
        const session2Index = targetContainer.data.indexOf(session2);
        if (session2Index > -1) {
          targetContainer.data.splice(session2Index, 1);
        }

        // 3. Add session1 to target
        targetContainer.data.push(session1);

        // 4. Add session2 to source
        sourceContainer.data.push(session2);

        this.snackBar.open('Sessions swapped successfully', 'Close', { duration: 3000 });
      },
      error: (err) => {
        console.error('Failed to swap sessions', err);
        this.snackBar.open('Failed to swap sessions', 'Close', { duration: 3000 });
      }
    });
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

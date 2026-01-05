import { Component, Input, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TimeSlotService } from '../../services/time-slot.service';
import { TrackService } from '../../services/track.service';
import { SessionService } from '../../services/session.service';
import { ScheduleDragDropService } from '../../services/schedule-drag-drop.service';
import { TimeSlot } from '../../models/time-slot.model';
import { Track } from '../../models/track.model';
import { Session } from '../../models/session.model';
import { ActivatedRoute } from '@angular/router';
import { MatDialog } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { DragDropModule, CdkDragDrop } from '@angular/cdk/drag-drop';
import { forkJoin } from 'rxjs';
import { SessionCardComponent } from '../session-card/session-card.component';

@Component({
  selector: 'app-schedule-grid',
  standalone: true,
  imports: [CommonModule, MatButtonModule, MatIconModule, DragDropModule, SessionCardComponent],
  templateUrl: './schedule-grid.component.html',
  styleUrls: ['./schedule-grid.component.scss']
})
export class ScheduleGridComponent implements OnInit {
  @Input() eventId: string | null = null;
  timeSlots: TimeSlot[] = [];
  tracks: Track[] = [];
  sessions: Session[] = [];
  
  // Grid data structure: timeSlotId -> trackId -> Session[]
  gridData: { [timeSlotId: string]: { [trackId: string]: Session[] } } = {};

  constructor(
    private timeSlotService: TimeSlotService,
    private trackService: TrackService,
    private sessionService: SessionService,
    private dragDropService: ScheduleDragDropService,
    private route: ActivatedRoute,
    private dialog: MatDialog
  ) {}

  ngOnInit(): void {
    if (!this.eventId) {
      this.eventId = this.route.snapshot.paramMap.get('id');
    }
    if (this.eventId) {
      this.loadData();
    }
  }

  loadData(): void {
    if (!this.eventId) return;

    forkJoin({
      slots: this.timeSlotService.getTimeSlots(this.eventId),
      tracks: this.trackService.getTracks(this.eventId),
      sessions: this.sessionService.getSessions(this.eventId)
    }).subscribe(({ slots, tracks, sessions }) => {
      this.timeSlots = slots.sort((a, b) => new Date(a.startTime).getTime() - new Date(b.startTime).getTime());
      this.tracks = tracks;
      this.sessions = sessions;
      this.initGrid();
    });
  }

  initGrid(): void {
    this.gridData = {};
    this.timeSlots.forEach(slot => {
        this.gridData[slot.id] = {};
        this.tracks.forEach(track => {
            this.gridData[slot.id][track.id] = [];
        });
    });
    this.populateGrid();
  }

  populateGrid(): void {
    this.sessions.forEach(session => {
        if (session.assignments && session.assignments.length > 0) {
            const assignment = session.assignments[0];
            if (this.gridData[assignment.timeSlotId] && this.gridData[assignment.timeSlotId][assignment.trackId]) {
                this.gridData[assignment.timeSlotId][assignment.trackId].push(session);
            }
        }
    });
  }

  drop(event: CdkDragDrop<Session[]>, trackId: string, timeSlotId: string): void {
    if (this.eventId) {
        this.dragDropService.handleDrop(event, this.eventId, trackId, timeSlotId);
    }
  }

  addTimeSlot(): void {
    // TODO: Open dialog to add time slot
    console.log('Add time slot');
  }

  editTimeSlot(slot: TimeSlot): void {
    // TODO: Open dialog to edit time slot
    console.log('Edit time slot', slot);
  }

  deleteTimeSlot(slot: TimeSlot): void {
    if (confirm(`Are you sure you want to delete ${slot.name}?`)) {
      this.timeSlotService.deleteTimeSlot(this.eventId!, slot.id).subscribe(() => {
        this.loadData();
      });
    }
  }
}

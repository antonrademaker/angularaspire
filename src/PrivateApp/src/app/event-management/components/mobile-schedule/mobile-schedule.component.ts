import { Component, Input, OnInit, OnChanges } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TimeSlot } from '../../models/time-slot.model';
import { Track } from '../../models/track.model';
import { Session } from '../../models/session.model';
import { MatCardModule } from '@angular/material/card';
import { MatExpansionModule } from '@angular/material/expansion';
import { MatIconModule } from '@angular/material/icon';

@Component({
  selector: 'app-mobile-schedule',
  standalone: true,
  imports: [CommonModule, MatCardModule, MatExpansionModule, MatIconModule],
  templateUrl: './mobile-schedule.component.html',
  styleUrls: ['./mobile-schedule.component.scss'],
})
export class MobileScheduleComponent implements OnInit, OnChanges {
  @Input() timeSlots: TimeSlot[] = [];
  @Input() tracks: Track[] = [];
  @Input() sessions: Session[] = [];

  // Data structure: timeSlotId -> Session[]
  sessionsBySlot: Record<string, Session[]> = {};

  ngOnInit(): void {
    this.groupSessions();
  }

  ngOnChanges(): void {
    this.groupSessions();
  }

  groupSessions(): void {
    this.sessionsBySlot = {};
    this.timeSlots.forEach((slot) => {
      this.sessionsBySlot[slot.id] = [];
    });

    this.sessions.forEach((session) => {
      if (session.assignments && session.assignments.length > 0) {
        const assignment = session.assignments[0];
        if (this.sessionsBySlot[assignment.timeSlotId]) {
          this.sessionsBySlot[assignment.timeSlotId].push(session);
        }
      }
    });
  }

  getTrackName(trackId: string): string {
    const track = this.tracks.find((t) => t.id === trackId);
    return track ? track.name : 'Unknown Track';
  }
}

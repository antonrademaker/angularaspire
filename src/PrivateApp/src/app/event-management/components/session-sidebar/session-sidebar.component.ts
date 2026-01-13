import { Component, Input, OnInit } from '@angular/core';

import { SessionService } from '../../services/session.service';
import { Session, SessionStatus } from '../../models/session.model';
import { ActivatedRoute } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { DragDropModule } from '@angular/cdk/drag-drop';

@Component({
  selector: 'app-session-sidebar',
  standalone: true,
  imports: [MatCardModule, MatIconModule, MatButtonModule, DragDropModule],
  templateUrl: './session-sidebar.component.html',
  styleUrls: ['./session-sidebar.component.scss'],
})
export class SessionSidebarComponent implements OnInit {
  @Input() eventId: string | null = null;
  unassignedSessions: Session[] = [];

  constructor(
    private sessionService: SessionService,
    private route: ActivatedRoute,
  ) {}

  ngOnInit(): void {
    if (!this.eventId) {
      this.eventId = this.route.snapshot.paramMap.get('id');
    }
    if (this.eventId) {
      this.loadUnassignedSessions();
    }
  }

  loadUnassignedSessions(): void {
    if (this.eventId) {
      this.sessionService.getSessions(this.eventId).subscribe((sessions) => {
        this.unassignedSessions = sessions.filter((s) => s.status === SessionStatus.Draft);
      });
    }
  }
}

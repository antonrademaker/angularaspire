import { Component, Input } from '@angular/core';

import { Session } from '../../models/session.model';
import { MatIconModule } from '@angular/material/icon';
import { DragDropModule } from '@angular/cdk/drag-drop';

@Component({
  selector: 'app-session-card',
  standalone: true,
  imports: [MatIconModule, DragDropModule],
  templateUrl: './session-card.component.html',
  styleUrls: ['./session-card.component.scss'],
})
export class SessionCardComponent {
  @Input() session!: Session;
}

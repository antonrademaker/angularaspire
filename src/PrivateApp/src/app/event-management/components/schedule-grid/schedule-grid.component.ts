import { Component, Input, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TimeSlotService } from '../../services/time-slot.service';
import { TimeSlot, TimeSlotType } from '../../models/time-slot.model';
import { ActivatedRoute } from '@angular/router';
import { MatDialog } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';

@Component({
  selector: 'app-schedule-grid',
  standalone: true,
  imports: [CommonModule, MatButtonModule, MatIconModule],
  templateUrl: './schedule-grid.component.html',
  styleUrls: ['./schedule-grid.component.scss']
})
export class ScheduleGridComponent implements OnInit {
  @Input() eventId: string | null = null;
  timeSlots: TimeSlot[] = [];

  constructor(
    private timeSlotService: TimeSlotService,
    private route: ActivatedRoute,
    private dialog: MatDialog
  ) {}

  ngOnInit(): void {
    if (!this.eventId) {
      this.eventId = this.route.snapshot.paramMap.get('id');
    }
    if (this.eventId) {
      this.loadTimeSlots();
    }
  }

  loadTimeSlots(): void {
    if (this.eventId) {
      this.timeSlotService.getTimeSlots(this.eventId).subscribe(slots => {
        this.timeSlots = slots;
      });
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
        this.loadTimeSlots();
      });
    }
  }
}

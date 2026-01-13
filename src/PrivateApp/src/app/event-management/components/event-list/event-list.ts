import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { EventService } from '../../services/event.service';
import { Event, EventSearchResult, EventStatus } from '../../models/event.model';

@Component({
  selector: 'app-event-list',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './event-list.html',
  styleUrls: ['./event-list.scss'],
})
export class EventListComponent implements OnInit {
  events: Event[] = [];
  totalCount = 0;
  currentPage = 1;
  pageSize = 10;
  loading = false;
  EventStatus = EventStatus;

  constructor(private eventService: EventService) {}

  ngOnInit(): void {
    this.loadEvents();
  }

  loadEvents(): void {
    this.loading = true;
    this.eventService
      .getEvents({
        page: this.currentPage,
        pageSize: this.pageSize,
      })
      .subscribe({
        next: (result: EventSearchResult) => {
          this.events = result.events;
          this.totalCount = result.totalCount;
          this.loading = false;
        },
        error: (error) => {
          console.error('Error loading events', error);
          this.loading = false;
        },
      });
  }

  onPageChange(page: number): void {
    this.currentPage = page;
    this.loadEvents();
  }

  getStatusLabel(status: EventStatus): string {
    switch (status) {
      case EventStatus.Draft:
        return 'Draft';
      case EventStatus.Published:
        return 'Published';
      case EventStatus.Cancelled:
        return 'Cancelled';
      case EventStatus.Completed:
        return 'Completed';
      default:
        return 'Unknown';
    }
  }
}

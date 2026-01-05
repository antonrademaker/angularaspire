import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { EventService } from '../../services/event.service';
import { Event, EventStatus } from '../../models/event.model';
import { dateRangeValidator } from '../../../shared-ui/validators/date-range.validator';

@Component({
  selector: 'app-event-form',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterModule],
  templateUrl: './event-form.html',
  styleUrls: ['./event-form.scss']
})
export class EventFormComponent implements OnInit {
  eventForm: FormGroup;
  isEditMode = false;
  eventId: string | null = null;
  loading = false;
  submitted = false;
  error = '';

  constructor(
    private fb: FormBuilder,
    private eventService: EventService,
    private route: ActivatedRoute,
    private router: Router
  ) {
    this.eventForm = this.fb.group({
      title: ['', Validators.required],
      slug: ['', [Validators.required, Validators.pattern('^[a-z0-9-]+$')]],
      description: ['', Validators.required],
      detailedDescription: [''],
      startDate: ['', Validators.required],
      endDate: ['', Validators.required],
      status: [EventStatus.Draft],
      logoUrl: [''],
      primaryColor: ['']
    }, { validators: dateRangeValidator('startDate', 'endDate') });
  }

  ngOnInit(): void {
    this.eventId = this.route.snapshot.paramMap.get('id');
    if (this.eventId) {
      this.isEditMode = true;
      this.loadEvent(this.eventId);
    }
  }

  loadEvent(id: string): void {
    this.loading = true;
    this.eventService.getEvent(id).subscribe({
      next: (event: Event) => {
        this.eventForm.patchValue({
          title: event.title,
          slug: event.slug,
          description: event.description,
          detailedDescription: event.detailedDescription,
          startDate: event.startDate.split('T')[0], // Simple date handling
          endDate: event.endDate.split('T')[0],
          status: event.status,
          logoUrl: event.logoUrl,
          primaryColor: event.primaryColor
        });
        this.loading = false;
      },
      error: (err) => {
        this.error = 'Failed to load event';
        this.loading = false;
        console.error(err);
      }
    });
  }

  onSubmit(): void {
    this.submitted = true;
    this.error = '';

    if (this.eventForm.invalid) {
      return;
    }

    this.loading = true;
    const eventData = this.eventForm.value;

    // Ensure dates are ISO strings
    eventData.startDate = new Date(eventData.startDate).toISOString();
    eventData.endDate = new Date(eventData.endDate).toISOString();
    eventData.status = Number(eventData.status);

    const request = this.isEditMode
      ? this.eventService.updateEvent(this.eventId!, eventData)
      : this.eventService.createEvent(eventData);

    request.subscribe({
      next: () => {
        this.router.navigate(['/events']);
      },
      error: (err) => {
        this.error = err.error || 'An error occurred';
        this.loading = false;
        console.error(err);
      }
    });
  }
}


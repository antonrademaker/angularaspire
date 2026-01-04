import { 
  Component, 
  OnInit, 
  signal, 
  computed, 
  inject, 
  ChangeDetectionStrategy, 
  Input 
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { Router, ActivatedRoute, RouterModule } from '@angular/router';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { lastValueFrom } from 'rxjs';
import { environment } from '../../environments/environment';

// Social Event interfaces based on the backend DTOs
export interface SocialEvent {
  id: string;
  eventId: string;
  title: string;
  slug: string;
  description: string;
  type: SocialEventType;
  typeName: string;
  startTime: string;
  endTime: string;
  location: string;
  room?: string;
  maxCapacity?: number;
  currentRsvpCount: number;
  waitlistCount: number;
  rsvpRequired: boolean;
  rsvpDeadline?: string;
  guestsAllowed: boolean;
  maxGuestsPerAttendee: number;
  dressCode?: string;
  costPerPerson?: number;
  currency: string;
  dietaryInfo?: string;
  status: SocialEventStatus;
  statusName: string;
  isPublished: boolean;
  tags: string[];
  customFields: Record<string, any>;
}

export type SocialEventType = 
  | 'Networking' 
  | 'Breakfast' 
  | 'Lunch' 
  | 'Dinner' 
  | 'CocktailReception' 
  | 'CoffeeBreak' 
  | 'TeamBuilding' 
  | 'Party' 
  | 'Tour' 
  | 'Other';

export type SocialEventStatus = 
  | 'Draft' 
  | 'Published' 
  | 'RsvpClosed' 
  | 'InProgress' 
  | 'Completed' 
  | 'Cancelled';

export interface SocialEventRsvp {
  id: string;
  socialEventId: string;
  socialEventTitle: string;
  userId: string;
  userFullName: string;
  userEmail: string;
  status: RsvpStatus;
  statusName: string;
  guestCount: number;
  guestNames: string[];
  dietaryRequirements?: string;
  notes?: string;
  isWaitlisted: boolean;
  waitlistPosition?: number;
  registeredAt: string;
  confirmedAt?: string;
  checkedInAt?: string;
  isCheckedIn: boolean;
  amountPaid: number;
  paymentStatus: PaymentStatus;
  paymentStatusName: string;
}

export type RsvpStatus = 
  | 'Registered' 
  | 'Confirmed' 
  | 'Declined' 
  | 'NoShow' 
  | 'Cancelled';

export type PaymentStatus = 
  | 'NotRequired' 
  | 'Pending' 
  | 'Paid' 
  | 'Refunded' 
  | 'Failed';

export interface RsvpAvailability {
  socialEventId: string;
  isAvailable: boolean;
  hasCapacity: boolean;
  waitlistAvailable: boolean;
  spotsRemaining?: number;
  waitlistPosition?: number;
  maxCapacity?: number;
  currentRsvpCount: number;
  waitlistCount: number;
  rsvpRequired: boolean;
  rsvpDeadline?: string;
  isRsvpOpen: boolean;
  guestsAllowed: boolean;
  maxGuestsPerAttendee: number;
  message: string;
  userHasRsvp: boolean;
  userRsvpStatus?: string;
}

export interface RsvpResult {
  success: boolean;
  rsvp?: SocialEventRsvp;
  errorMessage?: string;
  errorCode?: string;
}

@Component({
  selector: 'app-social-events',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    RouterModule
  ],
  templateUrl: './social-events.component.html',
  styleUrls: ['./social-events.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class SocialEventsComponent implements OnInit {
  @Input() eventId?: string;
  
  private http = inject(HttpClient);
  private router = inject(Router);
  private route = inject(ActivatedRoute);
  private fb = inject(FormBuilder);

  // Reactive signals
  socialEvents = signal<SocialEvent[]>([]);
  myRsvps = signal<Map<string, SocialEventRsvp>>(new Map());
  isLoading = signal(false);
  error = signal<string | null>(null);
  selectedEventId = signal<string | null>(null);
  selectedType = signal<string | null>(null);
  searchText = signal('');
  showRsvpDialog = signal(false);
  selectedSocialEvent = signal<SocialEvent | null>(null);
  rsvpAvailability = signal<RsvpAvailability | null>(null);
  rsvpInProgress = signal(false);
  rsvpError = signal<string | null>(null);
  guestCount = signal(0);

  // Filter options
  socialEventTypes: SocialEventType[] = [
    'Networking',
    'Breakfast',
    'Lunch',
    'Dinner',
    'CocktailReception',
    'CoffeeBreak',
    'TeamBuilding',
    'Party',
    'Tour',
    'Other'
  ];

  // RSVP form
  rsvpForm = this.fb.group({
    guestCount: [0, [Validators.min(0), Validators.max(10)]],
    guestNames: [''],
    dietaryRequirements: [''],
    notes: ['']
  });

  // Search form
  searchForm = this.fb.group({
    searchText: [''],
    type: ['']
  });

  // Computed values
  filteredSocialEvents = computed(() => {
    const text = this.searchText().toLowerCase();
    const type = this.selectedType();

    return this.socialEvents().filter(event => {
      const matchesText = !text || 
        event.title.toLowerCase().includes(text) || 
        event.description.toLowerCase().includes(text) ||
        event.location?.toLowerCase().includes(text) ||
        event.tags?.some(tag => tag.toLowerCase().includes(text));
      
      const matchesType = !type || event.type === type;
      
      return matchesText && matchesType;
    });
  });

  socialEventsByDate = computed(() => {
    const events = this.filteredSocialEvents();
    const grouped = new Map<string, SocialEvent[]>();

    for (const event of events) {
      const date = new Date(event.startTime).toDateString();
      if (!grouped.has(date)) {
        grouped.set(date, []);
      }
      grouped.get(date)!.push(event);
    }

    // Sort events within each day by start time
    for (const [, dayEvents] of grouped) {
      dayEvents.sort((a, b) => 
        new Date(a.startTime).getTime() - new Date(b.startTime).getTime());
    }

    return grouped;
  });

  myRsvpEvents = computed(() => {
    return this.socialEvents().filter(e => this.myRsvps().has(e.id));
  });

  constructor() {
    // Subscribe to form changes
    this.searchForm.get('searchText')?.valueChanges.subscribe(value => {
      this.searchText.set(value || '');
    });

    this.searchForm.get('type')?.valueChanges.subscribe(value => {
      this.selectedType.set(value || null);
    });

    // Track guest count for reactive template updates
    this.rsvpForm.get('guestCount')?.valueChanges.subscribe(value => {
      this.guestCount.set(value ?? 0);
    });
  }

  async ngOnInit() {
    const eventId = this.eventId || this.route.snapshot.paramMap.get('eventId');
    if (eventId) {
      this.selectedEventId.set(eventId);
      await Promise.all([
        this.loadSocialEvents(),
        this.loadMyRsvps()
      ]);
    }
  }

  async loadSocialEvents() {
    const eventId = this.selectedEventId();
    if (!eventId) return;

    this.isLoading.set(true);
    this.error.set(null);

    try {
      const events = await lastValueFrom(
        this.http.get<SocialEvent[]>(
          `${environment.publicApiUrl}/api/events/${eventId}/social-events`
        )
      );
      this.socialEvents.set(events);
    } catch (err) {
      const error = err as HttpErrorResponse;
      this.error.set(error.error?.message || 'Failed to load social events');
    } finally {
      this.isLoading.set(false);
    }
  }

  async loadMyRsvps() {
    const eventId = this.selectedEventId();
    if (!eventId) return;

    try {
      const rsvps = await lastValueFrom(
        this.http.get<SocialEventRsvp[]>(
          `${environment.publicApiUrl}/api/events/${eventId}/social-events/my-rsvps`
        )
      );
      const map = new Map<string, SocialEventRsvp>();
      for (const rsvp of rsvps) {
        map.set(rsvp.socialEventId, rsvp);
      }
      this.myRsvps.set(map);
    } catch (err) {
      // User may not be logged in
      console.log('Could not load RSVPs:', err);
    }
  }

  async openRsvpDialog(socialEvent: SocialEvent) {
    this.selectedSocialEvent.set(socialEvent);
    this.rsvpError.set(null);
    this.showRsvpDialog.set(true);

    // Reset form
    this.rsvpForm.reset({
      guestCount: 0,
      guestNames: '',
      dietaryRequirements: '',
      notes: ''
    });

    // Update max guests validator based on event
    const maxGuests = socialEvent.guestsAllowed ? socialEvent.maxGuestsPerAttendee : 0;
    this.rsvpForm.get('guestCount')?.setValidators([
      Validators.min(0),
      Validators.max(maxGuests)
    ]);
    this.rsvpForm.get('guestCount')?.updateValueAndValidity();

    // Check availability
    await this.checkAvailability(socialEvent.id);
  }

  closeRsvpDialog() {
    this.showRsvpDialog.set(false);
    this.selectedSocialEvent.set(null);
    this.rsvpAvailability.set(null);
  }

  async checkAvailability(socialEventId: string) {
    const eventId = this.selectedEventId();
    if (!eventId) return;

    try {
      const availability = await lastValueFrom(
        this.http.get<RsvpAvailability>(
          `${environment.publicApiUrl}/api/events/${eventId}/social-events/${socialEventId}/availability`
        )
      );
      this.rsvpAvailability.set(availability);
    } catch (err) {
      console.error('Failed to check availability:', err);
    }
  }

  async submitRsvp() {
    const socialEvent = this.selectedSocialEvent();
    const eventId = this.selectedEventId();
    if (!socialEvent || !eventId || !this.rsvpForm.valid) return;

    this.rsvpInProgress.set(true);
    this.rsvpError.set(null);

    try {
      const formValue = this.rsvpForm.value;
      const guestNames = formValue.guestNames
        ?.split(',')
        .map(name => name.trim())
        .filter(name => name.length > 0) || [];

      const result = await lastValueFrom(
        this.http.post<RsvpResult>(
          `${environment.publicApiUrl}/api/events/${eventId}/social-events/${socialEvent.id}/rsvp`,
          {
            guestCount: formValue.guestCount || 0,
            guestNames,
            dietaryRequirements: formValue.dietaryRequirements || null,
            notes: formValue.notes || null
          }
        )
      );

      if (result.success && result.rsvp) {
        const rsvps = new Map(this.myRsvps());
        rsvps.set(socialEvent.id, result.rsvp);
        this.myRsvps.set(rsvps);

        // Update event count
        const events = this.socialEvents().map(e => {
          if (e.id === socialEvent.id) {
            return {
              ...e,
              currentRsvpCount: e.currentRsvpCount + (result.rsvp!.isWaitlisted ? 0 : 1),
              waitlistCount: e.waitlistCount + (result.rsvp!.isWaitlisted ? 1 : 0)
            };
          }
          return e;
        });
        this.socialEvents.set(events);

        this.closeRsvpDialog();
        this.showMessage(
          result.rsvp.isWaitlisted
            ? `You've been added to the waitlist (position ${result.rsvp.waitlistPosition})`
            : 'Successfully RSVP\'d!'
        );
      } else {
        this.rsvpError.set(result.errorMessage || 'Failed to RSVP');
      }
    } catch (err) {
      const error = err as HttpErrorResponse;
      this.rsvpError.set(error.error?.message || 'Failed to RSVP');
    } finally {
      this.rsvpInProgress.set(false);
    }
  }

  async cancelRsvp(socialEvent: SocialEvent) {
    const eventId = this.selectedEventId();
    const rsvp = this.myRsvps().get(socialEvent.id);
    if (!eventId || !rsvp) return;

    const confirmed = confirm(`Are you sure you want to cancel your RSVP for "${socialEvent.title}"?`);
    if (!confirmed) return;

    try {
      await lastValueFrom(
        this.http.delete(
          `${environment.publicApiUrl}/api/events/${eventId}/social-events/${socialEvent.id}/rsvp/${rsvp.id}`
        )
      );

      const rsvps = new Map(this.myRsvps());
      const wasWaitlisted = rsvps.get(socialEvent.id)?.isWaitlisted;
      rsvps.delete(socialEvent.id);
      this.myRsvps.set(rsvps);

      // Update event counts
      const events = this.socialEvents().map(e => {
        if (e.id === socialEvent.id) {
          return {
            ...e,
            currentRsvpCount: wasWaitlisted ? e.currentRsvpCount : Math.max(0, e.currentRsvpCount - 1),
            waitlistCount: wasWaitlisted ? Math.max(0, e.waitlistCount - 1) : e.waitlistCount
          };
        }
        return e;
      });
      this.socialEvents.set(events);

      this.showMessage('RSVP cancelled successfully');
    } catch (err) {
      const error = err as HttpErrorResponse;
      this.showMessage(error.error?.message || 'Failed to cancel RSVP', true);
    }
  }

  viewEventDetails(socialEvent: SocialEvent) {
    const eventId = this.selectedEventId();
    if (eventId) {
      this.router.navigate(['/events', eventId, 'social-events', socialEvent.id]);
    }
  }

  getRsvp(socialEventId: string): SocialEventRsvp | undefined {
    return this.myRsvps().get(socialEventId);
  }

  hasRsvp(socialEventId: string): boolean {
    const rsvp = this.myRsvps().get(socialEventId);
    return rsvp !== undefined && rsvp.status !== 'Cancelled';
  }

  isWaitlisted(socialEventId: string): boolean {
    const rsvp = this.myRsvps().get(socialEventId);
    return rsvp?.isWaitlisted || false;
  }

  getCapacityPercentage(event: SocialEvent): number {
    if (!event.maxCapacity || event.maxCapacity === 0) return 0;
    return Math.round((event.currentRsvpCount / event.maxCapacity) * 100);
  }

  getSpotsRemaining(event: SocialEvent): number | null {
    if (!event.maxCapacity) return null;
    return Math.max(0, event.maxCapacity - event.currentRsvpCount);
  }

  isFull(event: SocialEvent): boolean {
    if (!event.maxCapacity) return false;
    return event.currentRsvpCount >= event.maxCapacity;
  }

  isRsvpOpen(event: SocialEvent): boolean {
    if (event.status !== 'Published') return false;
    if (event.rsvpDeadline) {
      return new Date(event.rsvpDeadline) > new Date();
    }
    return true;
  }

  formatTime(dateStr: string): string {
    const date = new Date(dateStr);
    return date.toLocaleTimeString('en-US', { 
      hour: 'numeric', 
      minute: '2-digit' 
    });
  }

  formatDate(dateStr: string): string {
    const date = new Date(dateStr);
    return date.toLocaleDateString('en-US', { 
      weekday: 'long',
      month: 'long', 
      day: 'numeric' 
    });
  }

  formatDateTime(dateStr: string): string {
    const date = new Date(dateStr);
    return date.toLocaleDateString('en-US', { 
      weekday: 'short',
      month: 'short', 
      day: 'numeric',
      hour: 'numeric',
      minute: '2-digit'
    });
  }

  formatDuration(event: SocialEvent): string {
    const start = new Date(event.startTime);
    const end = new Date(event.endTime);
    const diffMs = end.getTime() - start.getTime();
    const diffMins = Math.round(diffMs / 60000);
    
    if (diffMins < 60) {
      return `${diffMins} min`;
    }
    const hours = Math.floor(diffMins / 60);
    const mins = diffMins % 60;
    return mins > 0 ? `${hours}h ${mins}m` : `${hours}h`;
  }

  formatCost(event: SocialEvent): string {
    if (!event.costPerPerson) return 'Free';
    return `${event.currency} ${event.costPerPerson.toFixed(2)}`;
  }

  getTypeIcon(type: SocialEventType): string {
    const icons: Record<SocialEventType, string> = {
      'Networking': '🤝',
      'Breakfast': '🥐',
      'Lunch': '🍽️',
      'Dinner': '🍷',
      'CocktailReception': '🍸',
      'CoffeeBreak': '☕',
      'TeamBuilding': '🎯',
      'Party': '🎉',
      'Tour': '🚶',
      'Other': '📅'
    };
    return icons[type] || '📅';
  }

  getStatusClass(status: SocialEventStatus): string {
    const classes: Record<SocialEventStatus, string> = {
      'Draft': 'status-draft',
      'Published': 'status-published',
      'RsvpClosed': 'status-closed',
      'InProgress': 'status-in-progress',
      'Completed': 'status-completed',
      'Cancelled': 'status-cancelled'
    };
    return classes[status] || '';
  }

  clearFilters() {
    this.searchForm.reset();
    this.searchText.set('');
    this.selectedType.set(null);
  }

  private showMessage(message: string, isError = false) {
    console.log(isError ? 'Error:' : 'Success:', message);
    alert(message);
  }
}

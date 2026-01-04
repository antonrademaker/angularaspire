import { Component, OnInit, signal, computed, inject, ChangeDetectionStrategy, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder } from '@angular/forms';
import { Router, ActivatedRoute, RouterModule } from '@angular/router';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { lastValueFrom } from 'rxjs';

// Session interfaces
export interface Session {
  id: string;
  eventId: string;
  trackId?: string;
  trackName?: string;
  title: string;
  description: string;
  abstract?: string;
  startTime: string;
  endTime: string;
  type: string;
  difficultyLevel: string;
  maxAttendees?: number;
  currentAttendees: number;
  requiresSubscription: boolean;
  room?: string;
  building?: string;
  isVirtual: boolean;
  language: string;
  tags?: string[];
  speakers?: SessionSpeaker[];
}

export interface SessionSpeaker {
  speakerId: string;
  name: string;
  title?: string;
  role: string;
}

export interface Track {
  id: string;
  name: string;
  description?: string;
  color: string;
}

export interface SessionAvailability {
  sessionId: string;
  isAvailable: boolean;
  hasCapacity: boolean;
  maxAttendees?: number;
  currentAttendees: number;
  spotsRemaining?: number;
  waitlistCount: number;
  message: string;
  userStatus?: string;
  userWaitlistPosition?: number;
}

export interface Subscription {
  id: string;
  sessionId: string;
  sessionTitle: string;
  startTime: string;
  endTime: string;
  trackName?: string;
  room?: string;
  status: string;
  isWaitlisted: boolean;
  waitlistPosition?: number;
  isCheckedIn: boolean;
}

@Component({
  selector: 'app-session-browser',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    RouterModule
  ],
  templateUrl: './session-browser.component.html',
  styleUrls: ['./session-browser.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class SessionBrowserComponent implements OnInit {
  @Input() eventId?: string;
  
  private http = inject(HttpClient);
  private router = inject(Router);
  private route = inject(ActivatedRoute);
  private fb = inject(FormBuilder);

  // Reactive signals
  sessions = signal<Session[]>([]);
  tracks = signal<Track[]>([]);
  mySubscriptions = signal<Map<string, Subscription>>(new Map());
  isLoading = signal(false);
  error = signal<string | null>(null);
  selectedEventId = signal<string | null>(null);
  selectedTrackId = signal<string | null>(null);
  selectedType = signal<string | null>(null);
  selectedDifficulty = signal<string | null>(null);
  searchText = signal('');
  showOnlyAvailable = signal(false);
  viewMode = signal<'list' | 'schedule'>('list');

  // Filter options
  sessionTypes = ['Talk', 'Workshop', 'Panel', 'Keynote', 'Lightning', 'Networking', 'Break'];
  difficultyLevels = ['Beginner', 'Intermediate', 'Advanced', 'Expert'];

  // Search form
  searchForm = this.fb.group({
    searchText: [''],
    trackId: [''],
    type: [''],
    difficulty: [''],
    showOnlyAvailable: [false]
  });

  // Computed values
  filteredSessions = computed(() => {
    const text = this.searchText().toLowerCase();
    const trackId = this.selectedTrackId();
    const type = this.selectedType();
    const difficulty = this.selectedDifficulty();
    const onlyAvailable = this.showOnlyAvailable();

    return this.sessions().filter(session => {
      const matchesText = !text || 
        session.title.toLowerCase().includes(text) || 
        session.description.toLowerCase().includes(text) ||
        session.tags?.some(tag => tag.toLowerCase().includes(text)) ||
        session.speakers?.some(s => s.name.toLowerCase().includes(text));
      
      const matchesTrack = !trackId || session.trackId === trackId;
      const matchesType = !type || session.type === type;
      const matchesDifficulty = !difficulty || session.difficultyLevel === difficulty;
      const matchesAvailability = !onlyAvailable || 
        !session.maxAttendees || 
        session.currentAttendees < session.maxAttendees;
      
      return matchesText && matchesTrack && matchesType && matchesDifficulty && matchesAvailability;
    });
  });

  sessionsByTime = computed(() => {
    const sessions = this.filteredSessions();
    const grouped = new Map<string, Session[]>();

    for (const session of sessions) {
      const date = new Date(session.startTime).toDateString();
      if (!grouped.has(date)) {
        grouped.set(date, []);
      }
      grouped.get(date)!.push(session);
    }

    // Sort sessions within each day by start time
    for (const [, daySessions] of grouped) {
      daySessions.sort((a, b) => 
        new Date(a.startTime).getTime() - new Date(b.startTime).getTime());
    }

    return grouped;
  });

  mySubscribedSessions = computed(() => {
    return this.sessions().filter(s => this.mySubscriptions().has(s.id));
  });

  constructor() {
    // Subscribe to form changes
    this.searchForm.get('searchText')?.valueChanges.subscribe(value => {
      this.searchText.set(value || '');
    });

    this.searchForm.get('trackId')?.valueChanges.subscribe(value => {
      this.selectedTrackId.set(value || null);
    });

    this.searchForm.get('type')?.valueChanges.subscribe(value => {
      this.selectedType.set(value || null);
    });

    this.searchForm.get('difficulty')?.valueChanges.subscribe(value => {
      this.selectedDifficulty.set(value || null);
    });

    this.searchForm.get('showOnlyAvailable')?.valueChanges.subscribe(value => {
      this.showOnlyAvailable.set(!!value);
    });
  }

  async ngOnInit() {
    const eventId = this.eventId || this.route.snapshot.paramMap.get('eventId');
    if (eventId) {
      this.selectedEventId.set(eventId);
      await Promise.all([
        this.loadSessions(),
        this.loadTracks(),
        this.loadMySubscriptions()
      ]);
    }
  }

  async loadSessions() {
    if (!this.selectedEventId()) return;

    this.isLoading.set(true);
    this.error.set(null);

    try {
      const sessions = await lastValueFrom(
        this.http.get<Session[]>(`/api/v1/sessions?eventId=${this.selectedEventId()}`)
      );
      this.sessions.set(sessions);
    } catch (err) {
      const error = err as HttpErrorResponse;
      this.error.set(error.error?.message || 'Failed to load sessions');
    } finally {
      this.isLoading.set(false);
    }
  }

  async loadTracks() {
    if (!this.selectedEventId()) return;

    try {
      const tracks = await lastValueFrom(
        this.http.get<Track[]>(`/api/v1/tracks?eventId=${this.selectedEventId()}`)
      );
      this.tracks.set(tracks);
    } catch (err) {
      console.error('Failed to load tracks:', err);
    }
  }

  async loadMySubscriptions() {
    try {
      const subscriptions = await lastValueFrom(
        this.http.get<Subscription[]>(`/api/v1/my-subscriptions?eventId=${this.selectedEventId()}`)
      );
      const map = new Map<string, Subscription>();
      for (const sub of subscriptions) {
        map.set(sub.sessionId, sub);
      }
      this.mySubscriptions.set(map);
    } catch (err) {
      // User may not be logged in
      console.log('Could not load subscriptions:', err);
    }
  }

  async subscribe(session: Session) {
    try {
      const result = await lastValueFrom(
        this.http.post<Subscription>(`/api/v1/sessions/${session.id}/subscribe`, {})
      );
      
      const subscriptions = new Map(this.mySubscriptions());
      subscriptions.set(session.id, result);
      this.mySubscriptions.set(subscriptions);

      // Update session counts
      const sessions = this.sessions().map(s => {
        if (s.id === session.id) {
          return { ...s, currentAttendees: s.currentAttendees + (result.isWaitlisted ? 0 : 1) };
        }
        return s;
      });
      this.sessions.set(sessions);

      this.showMessage(result.isWaitlisted 
        ? `You've been added to the waitlist (position ${result.waitlistPosition})`
        : 'Successfully subscribed to session!');
    } catch (err) {
      const error = err as HttpErrorResponse;
      this.showMessage(error.error?.message || 'Failed to subscribe', true);
    }
  }

  async unsubscribe(session: Session) {
    const confirmed = confirm(`Are you sure you want to unsubscribe from "${session.title}"?`);
    if (!confirmed) return;

    try {
      await lastValueFrom(
        this.http.post(`/api/v1/sessions/${session.id}/unsubscribe`, {})
      );
      
      const subscriptions = new Map(this.mySubscriptions());
      const wasConfirmed = subscriptions.get(session.id)?.status === 'Confirmed';
      subscriptions.delete(session.id);
      this.mySubscriptions.set(subscriptions);

      // Update session counts
      if (wasConfirmed) {
        const sessions = this.sessions().map(s => {
          if (s.id === session.id) {
            return { ...s, currentAttendees: Math.max(0, s.currentAttendees - 1) };
          }
          return s;
        });
        this.sessions.set(sessions);
      }

      this.showMessage('Successfully unsubscribed from session');
    } catch (err) {
      const error = err as HttpErrorResponse;
      this.showMessage(error.error?.message || 'Failed to unsubscribe', true);
    }
  }

  viewSessionDetails(session: Session) {
    this.router.navigate(['/events', this.selectedEventId(), 'sessions', session.id]);
  }

  getSubscription(sessionId: string): Subscription | undefined {
    return this.mySubscriptions().get(sessionId);
  }

  isSubscribed(sessionId: string): boolean {
    const sub = this.mySubscriptions().get(sessionId);
    return sub !== undefined && sub.status !== 'Cancelled';
  }

  isWaitlisted(sessionId: string): boolean {
    const sub = this.mySubscriptions().get(sessionId);
    return sub?.isWaitlisted || false;
  }

  getCapacityPercentage(session: Session): number {
    if (!session.maxAttendees || session.maxAttendees === 0) return 0;
    return Math.round((session.currentAttendees / session.maxAttendees) * 100);
  }

  getTrackColor(trackId?: string): string {
    const track = this.tracks().find(t => t.id === trackId);
    return track?.color || '#cccccc';
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

  formatDuration(session: Session): string {
    const start = new Date(session.startTime);
    const end = new Date(session.endTime);
    const diffMs = end.getTime() - start.getTime();
    const diffMins = Math.round(diffMs / 60000);
    
    if (diffMins < 60) {
      return `${diffMins} min`;
    }
    const hours = Math.floor(diffMins / 60);
    const mins = diffMins % 60;
    return mins > 0 ? `${hours}h ${mins}m` : `${hours}h`;
  }

  toggleViewMode() {
    this.viewMode.set(this.viewMode() === 'list' ? 'schedule' : 'list');
  }

  clearFilters() {
    this.searchForm.reset();
    this.searchText.set('');
    this.selectedTrackId.set(null);
    this.selectedType.set(null);
    this.selectedDifficulty.set(null);
    this.showOnlyAvailable.set(false);
  }

  private showMessage(message: string, isError = false) {
    console.log(isError ? 'Error:' : 'Success:', message);
    alert(message);
  }
}
import { Component, OnInit, signal, computed, inject, ChangeDetectionStrategy, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { Router, ActivatedRoute } from '@angular/router';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { MatTableModule, MatTableDataSource } from '@angular/material/table';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatSortModule, Sort } from '@angular/material/sort';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatDialogModule, MatDialog, MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { MatCardModule } from '@angular/material/card';
import { MatChipsModule } from '@angular/material/chips';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatTimepickerModule } from '@angular/material/timepicker';
import { MatNativeDateModule, provideNativeDateAdapter } from '@angular/material/core';
import { MatTabsModule } from '@angular/material/tabs';
import { MatMenuModule } from '@angular/material/menu';
import { MatBadgeModule } from '@angular/material/badge';
import { MatDividerModule } from '@angular/material/divider';
import { lastValueFrom } from 'rxjs';

// Session interfaces and types
export interface Session {
  id: string;
  eventId: string;
  trackId?: string;
  trackName?: string;
  title: string;
  description: string;
  abstract?: string;
  slug: string;
  startTime: string;
  endTime: string;
  durationMinutes: number;
  type: SessionType;
  difficultyLevel: SessionDifficulty;
  maxAttendees?: number;
  currentAttendees: number;
  requiresSubscription: boolean;
  room?: string;
  building?: string;
  isVirtual: boolean;
  virtualUrl?: string;
  recordingUrl?: string;
  materialsUrl?: string;
  status: SessionStatus;
  isPublished: boolean;
  allowQuestions: boolean;
  isRecorded: boolean;
  language: string;
  prerequisites?: string;
  learningOutcomes?: string;
  targetAudience?: string;
  tags?: string[];
  customFields?: Record<string, any>;
  createdAt: string;
  updatedAt?: string;
  speakers?: SessionSpeaker[];
}

export enum SessionType {
  Talk = 'Talk',
  Workshop = 'Workshop',
  Panel = 'Panel',
  Keynote = 'Keynote',
  Lightning = 'Lightning',
  BoF = 'BoF',
  Networking = 'Networking',
  Break = 'Break',
  Other = 'Other'
}

export enum SessionDifficulty {
  Beginner = 'Beginner',
  Intermediate = 'Intermediate',
  Advanced = 'Advanced',
  Expert = 'Expert'
}

export enum SessionStatus {
  Draft = 'Draft',
  Scheduled = 'Scheduled',
  InProgress = 'InProgress',
  Completed = 'Completed',
  Cancelled = 'Cancelled'
}

export interface SessionSpeaker {
  speakerId: string;
  name: string;
  title?: string;
  bio?: string;
  role: string;
  displayOrder: number;
}

export interface CreateSessionRequest {
  eventId: string;
  trackId?: string;
  title: string;
  description: string;
  abstract?: string;
  slug: string;
  startTime: string;
  endTime: string;
  type: SessionType;
  difficultyLevel: SessionDifficulty;
  maxAttendees?: number;
  requiresSubscription: boolean;
  room?: string;
  building?: string;
  isVirtual: boolean;
  virtualUrl?: string;
  materialsUrl?: string;
  allowQuestions: boolean;
  isRecorded: boolean;
  language: string;
  prerequisites?: string;
  learningOutcomes?: string;
  targetAudience?: string;
  tags?: string[];
  customFields?: Record<string, any>;
}

export interface UpdateSessionRequest {
  title?: string;
  description?: string;
  abstract?: string;
  slug?: string;
  startTime?: string;
  endTime?: string;
  trackId?: string;
  type?: SessionType;
  difficultyLevel?: SessionDifficulty;
  maxAttendees?: number;
  requiresSubscription?: boolean;
  room?: string;
  building?: string;
  isVirtual?: boolean;
  virtualUrl?: string;
  recordingUrl?: string;
  materialsUrl?: string;
  allowQuestions?: boolean;
  isRecorded?: boolean;
  language?: string;
  prerequisites?: string;
  learningOutcomes?: string;
  targetAudience?: string;
  tags?: string[];
  customFields?: Record<string, any>;
}

export interface SessionSearchRequest {
  eventId?: string;
  trackId?: string;
  searchText?: string;
  types?: SessionType[];
  difficultyLevels?: SessionDifficulty[];
  startDate?: string;
  endDate?: string;
  tags?: string[];
  hasCapacity?: boolean;
  includeUnpublished?: boolean;
  room?: string;
  isVirtual?: boolean;
  page: number;
  pageSize: number;
  sortBy?: string;
  sortDirection?: 'asc' | 'desc';
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

export interface Track {
  id: string;
  name: string;
  color: string;
}

export interface SessionConflict {
  conflictingSessionId: string;
  conflictingSessionTitle: string;
  conflictingStartTime: string;
  conflictingEndTime: string;
  conflictType: string;
  room?: string;
}

@Component({
  selector: 'app-session-manager',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatTableModule,
    MatPaginatorModule,
    MatSortModule,
    MatButtonModule,
    MatIconModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatCheckboxModule,
    MatDialogModule,
    MatCardModule,
    MatChipsModule,
    MatProgressSpinnerModule,
    MatToolbarModule,
    MatTooltipModule,
    MatDatepickerModule,
    MatNativeDateModule,
    MatTabsModule,
    MatMenuModule,
    MatBadgeModule,
    MatDividerModule
  ],
  templateUrl: './session-manager.component.html',
  styleUrls: ['./session-manager.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class SessionManagerComponent implements OnInit {
  @Input() eventId?: string;
  @Input() trackId?: string;
  
  private fb = inject(FormBuilder);
  private http = inject(HttpClient);
  private router = inject(Router);
  private route = inject(ActivatedRoute);
  private dialog = inject(MatDialog);

  // Signals for reactive state management
  sessions = signal<Session[]>([]);
  tracks = signal<Track[]>([]);
  isLoading = signal(false);
  error = signal<string | null>(null);
  selectedEventId = signal<string | null>(null);
  selectedTrackId = signal<string | null>(null);
  searchText = signal('');
  showUnpublished = signal(true);
  pageIndex = signal(0);
  pageSize = signal(25);
  sortBy = signal('startTime');
  sortDirection = signal<'asc' | 'desc'>('asc');
  totalCount = signal(0);
  selectedTabIndex = signal(0);

  // Filter options
  sessionTypes = Object.values(SessionType);
  difficultyLevels = Object.values(SessionDifficulty);
  sessionStatuses = Object.values(SessionStatus);

  // Computed values
  displayedColumns = ['title', 'track', 'time', 'type', 'capacity', 'status', 'actions'];
  filteredSessions = computed(() => {
    const text = this.searchText().toLowerCase();
    const trackId = this.selectedTrackId();
    const showUnpublished = this.showUnpublished();
    
    return this.sessions().filter(session => {
      const matchesText = !text || 
        session.title.toLowerCase().includes(text) || 
        session.description.toLowerCase().includes(text) ||
        session.tags?.some(tag => tag.toLowerCase().includes(text));
      const matchesTrack = !trackId || session.trackId === trackId;
      const matchesPublished = showUnpublished || session.isPublished;
      return matchesText && matchesTrack && matchesPublished;
    });
  });

  // Form for search/filter
  searchForm = this.fb.group({
    searchText: [''],
    eventId: [''],
    trackId: [''],
    showUnpublished: [true],
    sessionType: [''],
    difficulty: ['']
  });

  // Table data source
  dataSource = new MatTableDataSource<Session>([]);

  constructor() {
    // Subscribe to form changes for reactive filtering
    this.searchForm.get('searchText')?.valueChanges.subscribe(value => {
      this.searchText.set(value || '');
    });

    this.searchForm.get('showUnpublished')?.valueChanges.subscribe(value => {
      this.showUnpublished.set(!!value);
    });

    this.searchForm.get('trackId')?.valueChanges.subscribe(value => {
      this.selectedTrackId.set(value || null);
    });

    this.searchForm.get('eventId')?.valueChanges.subscribe(value => {
      if (value) {
        this.selectedEventId.set(value);
        this.loadTracks();
        this.loadSessions();
      }
    });
  }

  async ngOnInit() {
    // Load event ID from route parameters, query parameters, or input
    const eventId = this.eventId || 
      this.route.snapshot.paramMap.get('eventId') || 
      this.route.snapshot.queryParamMap.get('eventId');
    const trackId = this.trackId || this.route.snapshot.queryParamMap.get('trackId');
    
    if (eventId) {
      this.selectedEventId.set(eventId);
      this.searchForm.patchValue({ eventId });
      await this.loadTracks();
      
      if (trackId) {
        this.selectedTrackId.set(trackId);
        this.searchForm.patchValue({ trackId });
      }
      
      await this.loadSessions();
    }
  }

  async loadTracks() {
    if (!this.selectedEventId()) return;

    try {
      const tracks = await lastValueFrom(
        this.http.get<Track[]>(`/api/v1/admin/tracks?eventId=${this.selectedEventId()}`)
      );
      this.tracks.set(tracks);
    } catch (err) {
      console.error('Failed to load tracks:', err);
    }
  }

  async loadSessions() {
    if (!this.selectedEventId()) return;

    this.isLoading.set(true);
    this.error.set(null);

    try {
      const request: SessionSearchRequest = {
        eventId: this.selectedEventId()!,
        trackId: this.selectedTrackId() || undefined,
        searchText: this.searchText() || undefined,
        includeUnpublished: this.showUnpublished(),
        page: this.pageIndex() + 1,
        pageSize: this.pageSize(),
        sortBy: this.sortBy(),
        sortDirection: this.sortDirection()
      };

      const response = await lastValueFrom(
        this.http.post<PagedResult<Session>>('/api/v1/admin/sessions/search', request)
      );

      this.sessions.set(response.items);
      this.totalCount.set(response.totalCount);
      this.dataSource.data = response.items;
    } catch (err) {
      const error = err as HttpErrorResponse;
      this.error.set(error.error?.message || 'Failed to load sessions');
      this.showErrorMessage('Failed to load sessions');
    } finally {
      this.isLoading.set(false);
    }
  }

  async createSession() {
    const dialogRef = this.dialog.open(SessionDialogComponent, {
      width: '800px',
      maxHeight: '90vh',
      data: { 
        eventId: this.selectedEventId(), 
        tracks: this.tracks(),
        mode: 'create' 
      }
    });

    const result = await lastValueFrom(dialogRef.afterClosed());
    if (result) {
      await this.loadSessions();
      this.showSuccessMessage('Session created successfully');
    }
  }

  async editSession(session: Session) {
    const dialogRef = this.dialog.open(SessionDialogComponent, {
      width: '800px',
      maxHeight: '90vh',
      data: { 
        session, 
        tracks: this.tracks(),
        mode: 'edit' 
      }
    });

    const result = await lastValueFrom(dialogRef.afterClosed());
    if (result) {
      await this.loadSessions();
      this.showSuccessMessage('Session updated successfully');
    }
  }

  async duplicateSession(session: Session) {
    const request: CreateSessionRequest = {
      eventId: session.eventId,
      trackId: session.trackId,
      title: `${session.title} (Copy)`,
      description: session.description,
      abstract: session.abstract,
      slug: `${session.slug}-copy`,
      startTime: session.startTime,
      endTime: session.endTime,
      type: session.type,
      difficultyLevel: session.difficultyLevel,
      maxAttendees: session.maxAttendees,
      requiresSubscription: session.requiresSubscription,
      room: session.room,
      building: session.building,
      isVirtual: session.isVirtual,
      virtualUrl: session.virtualUrl,
      materialsUrl: session.materialsUrl,
      allowQuestions: session.allowQuestions,
      isRecorded: session.isRecorded,
      language: session.language,
      prerequisites: session.prerequisites,
      learningOutcomes: session.learningOutcomes,
      targetAudience: session.targetAudience,
      tags: session.tags ? [...session.tags] : undefined,
      customFields: session.customFields ? { ...session.customFields } : undefined
    };

    this.isLoading.set(true);

    try {
      await lastValueFrom(
        this.http.post<Session>('/api/v1/admin/sessions', request)
      );

      await this.loadSessions();
      this.showSuccessMessage('Session duplicated successfully');
    } catch (err) {
      const error = err as HttpErrorResponse;
      this.showErrorMessage(error.error?.message || 'Failed to duplicate session');
    } finally {
      this.isLoading.set(false);
    }
  }

  async deleteSession(session: Session) {
    const confirmed = confirm(`Are you sure you want to delete "${session.title}"? This action cannot be undone.`);
    if (!confirmed) return;

    this.isLoading.set(true);

    try {
      await lastValueFrom(
        this.http.delete(`/api/v1/admin/sessions/${session.id}`)
      );

      await this.loadSessions();
      this.showSuccessMessage('Session deleted successfully');
    } catch (err) {
      const error = err as HttpErrorResponse;
      this.showErrorMessage(error.error?.message || 'Failed to delete session');
    } finally {
      this.isLoading.set(false);
    }
  }

  async publishSession(session: Session) {
    this.isLoading.set(true);

    try {
      await lastValueFrom(
        this.http.post(`/api/v1/admin/sessions/${session.id}/publish`, {})
      );

      await this.loadSessions();
      this.showSuccessMessage('Session published successfully');
    } catch (err) {
      const error = err as HttpErrorResponse;
      this.showErrorMessage(error.error?.message || 'Failed to publish session');
    } finally {
      this.isLoading.set(false);
    }
  }

  async unpublishSession(session: Session) {
    this.isLoading.set(true);

    try {
      await lastValueFrom(
        this.http.post(`/api/v1/admin/sessions/${session.id}/unpublish`, {})
      );

      await this.loadSessions();
      this.showSuccessMessage('Session unpublished successfully');
    } catch (err) {
      const error = err as HttpErrorResponse;
      this.showErrorMessage(error.error?.message || 'Failed to unpublish session');
    } finally {
      this.isLoading.set(false);
    }
  }

  async startSession(session: Session) {
    this.isLoading.set(true);

    try {
      await lastValueFrom(
        this.http.post(`/api/v1/admin/sessions/${session.id}/start`, {})
      );

      await this.loadSessions();
      this.showSuccessMessage('Session started');
    } catch (err) {
      const error = err as HttpErrorResponse;
      this.showErrorMessage(error.error?.message || 'Failed to start session');
    } finally {
      this.isLoading.set(false);
    }
  }

  async completeSession(session: Session) {
    this.isLoading.set(true);

    try {
      await lastValueFrom(
        this.http.post(`/api/v1/admin/sessions/${session.id}/complete`, {})
      );

      await this.loadSessions();
      this.showSuccessMessage('Session completed');
    } catch (err) {
      const error = err as HttpErrorResponse;
      this.showErrorMessage(error.error?.message || 'Failed to complete session');
    } finally {
      this.isLoading.set(false);
    }
  }

  async cancelSession(session: Session) {
    const reason = prompt('Please provide a reason for cancellation:');
    if (reason === null) return;

    this.isLoading.set(true);

    try {
      await lastValueFrom(
        this.http.post(`/api/v1/admin/sessions/${session.id}/cancel`, { reason })
      );

      await this.loadSessions();
      this.showSuccessMessage('Session cancelled');
    } catch (err) {
      const error = err as HttpErrorResponse;
      this.showErrorMessage(error.error?.message || 'Failed to cancel session');
    } finally {
      this.isLoading.set(false);
    }
  }

  async checkConflicts(session: Session) {
    try {
      const conflicts = await lastValueFrom(
        this.http.post<SessionConflict[]>(`/api/v1/admin/sessions/${session.id}/conflicts`, {
          startTime: session.startTime,
          endTime: session.endTime,
          room: session.room
        })
      );

      if (conflicts.length === 0) {
        alert('No scheduling conflicts detected.');
      } else {
        const conflictList = conflicts.map(c => 
          `• ${c.conflictingSessionTitle} (${c.conflictType})`
        ).join('\n');
        alert(`Scheduling conflicts detected:\n\n${conflictList}`);
      }
    } catch (err) {
      console.error('Failed to check conflicts:', err);
    }
  }

  viewSubscriptions(session: Session) {
    this.router.navigate(['/session-management', 'subscriptions', session.id]);
  }

  onPageChange(event: PageEvent) {
    this.pageIndex.set(event.pageIndex);
    this.pageSize.set(event.pageSize);
    this.loadSessions();
  }

  onSort(sort: Sort) {
    this.sortBy.set(sort.active);
    this.sortDirection.set(sort.direction as 'asc' | 'desc');
    this.loadSessions();
  }

  onTabChange(index: number) {
    this.selectedTabIndex.set(index);
  }

  getTrackColor(trackId?: string): string {
    const track = this.tracks().find(t => t.id === trackId);
    return track?.color || '#cccccc';
  }

  getStatusColor(status: SessionStatus): string {
    switch (status) {
      case SessionStatus.Draft: return '#9e9e9e';
      case SessionStatus.Scheduled: return '#2196f3';
      case SessionStatus.InProgress: return '#ff9800';
      case SessionStatus.Completed: return '#4caf50';
      case SessionStatus.Cancelled: return '#f44336';
      default: return '#cccccc';
    }
  }

  getCapacityPercentage(session: Session): number {
    if (!session.maxAttendees || session.maxAttendees === 0) return 0;
    return Math.round((session.currentAttendees / session.maxAttendees) * 100);
  }

  formatDateTime(dateStr: string): string {
    const date = new Date(dateStr);
    return date.toLocaleString('en-US', {
      month: 'short',
      day: 'numeric',
      hour: 'numeric',
      minute: '2-digit'
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

  private showSuccessMessage(message: string) {
    console.log('Success:', message);
    // TODO: Implement proper notification system
  }

  private showErrorMessage(message: string) {
    console.error('Error:', message);
    // TODO: Implement proper notification system
  }
}

// Dialog Component for creating/editing sessions
@Component({
  selector: 'app-session-dialog',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatDialogModule,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatCheckboxModule,
    MatChipsModule,
    MatIconModule,
    MatDatepickerModule,
    MatNativeDateModule,
    MatTabsModule,
    MatTimepickerModule
  ],
  template: `
    <h2 mat-dialog-title>
      {{ data.mode === 'create' ? 'Create New Session' : 'Edit Session' }}
    </h2>
    
    <mat-dialog-content>
      <mat-tab-group>
        <mat-tab label="Basic Info">
          <div class="tab-content">
            <form [formGroup]="sessionForm" class="session-form">
              <div class="row">
                <mat-form-field appearance="outline" class="full-width">
                  <mat-label>Session Title</mat-label>
                  <input matInput formControlName="title" placeholder="Enter session title" required>
                  <mat-error *ngIf="sessionForm.get('title')?.hasError('required')">
                    Title is required
                  </mat-error>
                </mat-form-field>
              </div>

              <div class="row">
                <mat-form-field appearance="outline" class="half-width">
                  <mat-label>Track</mat-label>
                  <mat-select formControlName="trackId">
                    <mat-option [value]="null">No Track</mat-option>
                    <mat-option *ngFor="let track of data.tracks" [value]="track.id">
                      {{ track.name }}
                    </mat-option>
                  </mat-select>
                </mat-form-field>

                <mat-form-field appearance="outline" class="half-width">
                  <mat-label>Slug</mat-label>
                  <input matInput formControlName="slug" placeholder="session-slug" required>
                </mat-form-field>
              </div>

              <div class="row">
                <mat-form-field appearance="outline" class="full-width">
                  <mat-label>Description</mat-label>
                  <textarea matInput formControlName="description" 
                            placeholder="Enter session description" 
                            rows="3" required></textarea>
                </mat-form-field>
              </div>

              <div class="row">
                <mat-form-field appearance="outline" class="full-width">
                  <mat-label>Abstract</mat-label>
                  <textarea matInput formControlName="abstract" 
                            placeholder="Enter detailed abstract" 
                            rows="4"></textarea>
                </mat-form-field>
              </div>

              <div class="row">
                <mat-form-field appearance="outline" class="third-width">
                  <mat-label>Session Type</mat-label>
                  <mat-select formControlName="type" required>
                    <mat-option *ngFor="let type of sessionTypes" [value]="type">
                      {{ type }}
                    </mat-option>
                  </mat-select>
                </mat-form-field>

                <mat-form-field appearance="outline" class="third-width">
                  <mat-label>Difficulty Level</mat-label>
                  <mat-select formControlName="difficultyLevel" required>
                    <mat-option *ngFor="let level of difficultyLevels" [value]="level">
                      {{ level }}
                    </mat-option>
                  </mat-select>
                </mat-form-field>

                <mat-form-field appearance="outline" class="third-width">
                  <mat-label>Language</mat-label>
                  <input matInput formControlName="language" placeholder="en">
                </mat-form-field>
              </div>
            </form>
          </div>
        </mat-tab>

        <mat-tab label="Schedule">
          <div class="tab-content">
            <form [formGroup]="sessionForm" class="session-form">
              <div class="row">
                <mat-form-field appearance="outline" class="quarter-width">
                  <mat-label>Start Date</mat-label>
                  <input matInput [matDatepicker]="startDatePicker" formControlName="startDate" required>
                  <mat-datepicker-toggle matIconSuffix [for]="startDatePicker"></mat-datepicker-toggle>
                  <mat-datepicker #startDatePicker></mat-datepicker>
                </mat-form-field>

                <mat-form-field appearance="outline" class="quarter-width">
                  <mat-label>Start Time</mat-label>
                  <input matInput [matTimepicker]="startTimePicker" formControlName="startTimeValue" required>
                  <mat-timepicker-toggle matIconSuffix [for]="startTimePicker"></mat-timepicker-toggle>
                  <mat-timepicker #startTimePicker [interval]="'15m'"></mat-timepicker>
                </mat-form-field>

                <mat-form-field appearance="outline" class="quarter-width">
                  <mat-label>End Date</mat-label>
                  <input matInput [matDatepicker]="endDatePicker" formControlName="endDate" required>
                  <mat-datepicker-toggle matIconSuffix [for]="endDatePicker"></mat-datepicker-toggle>
                  <mat-datepicker #endDatePicker></mat-datepicker>
                </mat-form-field>

                <mat-form-field appearance="outline" class="quarter-width">
                  <mat-label>End Time</mat-label>
                  <input matInput [matTimepicker]="endTimePicker" formControlName="endTimeValue" required>
                  <mat-timepicker-toggle matIconSuffix [for]="endTimePicker"></mat-timepicker-toggle>
                  <mat-timepicker #endTimePicker [interval]="'15m'"></mat-timepicker>
                </mat-form-field>
              </div>

              <div class="row">
                <mat-form-field appearance="outline" class="half-width">
                  <mat-label>Room</mat-label>
                  <input matInput formControlName="room" placeholder="Room name or number">
                </mat-form-field>

                <mat-form-field appearance="outline" class="half-width">
                  <mat-label>Building</mat-label>
                  <input matInput formControlName="building" placeholder="Building name">
                </mat-form-field>
              </div>

              <div class="row checkboxes">
                <mat-checkbox formControlName="isVirtual">Virtual Session</mat-checkbox>
                <mat-checkbox formControlName="isRecorded">Will be Recorded</mat-checkbox>
                <mat-checkbox formControlName="allowQuestions">Allow Q&A</mat-checkbox>
              </div>

              <div class="row" *ngIf="sessionForm.get('isVirtual')?.value">
                <mat-form-field appearance="outline" class="full-width">
                  <mat-label>Virtual Meeting URL</mat-label>
                  <input matInput formControlName="virtualUrl" placeholder="https://...">
                </mat-form-field>
              </div>
            </form>
          </div>
        </mat-tab>

        <mat-tab label="Capacity">
          <div class="tab-content">
            <form [formGroup]="sessionForm" class="session-form">
              <div class="row">
                <mat-form-field appearance="outline" class="half-width">
                  <mat-label>Max Attendees</mat-label>
                  <input matInput type="number" formControlName="maxAttendees" min="0">
                  <mat-hint>Leave empty for unlimited</mat-hint>
                </mat-form-field>

                <div class="half-width checkbox-field">
                  <mat-checkbox formControlName="requiresSubscription">
                    Requires Registration/Subscription
                  </mat-checkbox>
                </div>
              </div>

              <div class="row">
                <mat-form-field appearance="outline" class="full-width">
                  <mat-label>Target Audience</mat-label>
                  <input matInput formControlName="targetAudience" 
                         placeholder="e.g., Developers, DevOps Engineers">
                </mat-form-field>
              </div>

              <div class="row">
                <mat-form-field appearance="outline" class="full-width">
                  <mat-label>Prerequisites</mat-label>
                  <textarea matInput formControlName="prerequisites" 
                            placeholder="List any prerequisites" 
                            rows="2"></textarea>
                </mat-form-field>
              </div>

              <div class="row">
                <mat-form-field appearance="outline" class="full-width">
                  <mat-label>Learning Outcomes</mat-label>
                  <textarea matInput formControlName="learningOutcomes" 
                            placeholder="What attendees will learn" 
                            rows="2"></textarea>
                </mat-form-field>
              </div>
            </form>
          </div>
        </mat-tab>

        <mat-tab label="Materials">
          <div class="tab-content">
            <form [formGroup]="sessionForm" class="session-form">
              <div class="row">
                <mat-form-field appearance="outline" class="full-width">
                  <mat-label>Materials URL</mat-label>
                  <input matInput formControlName="materialsUrl" placeholder="https://...">
                  <mat-hint>Link to slides, code samples, etc.</mat-hint>
                </mat-form-field>
              </div>

              <div class="row" *ngIf="data.mode === 'edit'">
                <mat-form-field appearance="outline" class="full-width">
                  <mat-label>Recording URL</mat-label>
                  <input matInput formControlName="recordingUrl" placeholder="https://...">
                  <mat-hint>Link to session recording (after event)</mat-hint>
                </mat-form-field>
              </div>

              <div class="row">
                <mat-form-field appearance="outline" class="full-width">
                  <mat-label>Tags (comma separated)</mat-label>
                  <input matInput formControlName="tagsInput" 
                         placeholder="angular, typescript, web">
                </mat-form-field>
              </div>
            </form>
          </div>
        </mat-tab>
      </mat-tab-group>
    </mat-dialog-content>

    <mat-dialog-actions align="end">
      <button mat-button (click)="cancel()">Cancel</button>
      <button mat-raised-button 
              color="primary" 
              [disabled]="sessionForm.invalid || isSubmitting()"
              (click)="save()">
        {{ data.mode === 'create' ? 'Create' : 'Update' }}
      </button>
    </mat-dialog-actions>
  `,
  styles: [`
    mat-dialog-content {
      min-width: 700px;
      max-height: 70vh;
    }
    .tab-content {
      padding: 16px 0;
    }
    .session-form {
      display: flex;
      flex-direction: column;
    }
    .row {
      display: flex;
      gap: 16px;
      margin-bottom: 16px;
    }
    .full-width { flex: 1; }
    .half-width { flex: 0.5; }
    .third-width { flex: 0.33; }
    .quarter-width { flex: 0.25; min-width: 150px; }
    .checkboxes {
      gap: 24px;
    }
    .checkbox-field {
      display: flex;
      align-items: center;
    }
  `]
})
export class SessionDialogComponent implements OnInit {
  private fb = inject(FormBuilder);
  private http = inject(HttpClient);
  private dialogRef = inject(MatDialogRef<SessionDialogComponent>);
  data = inject(MAT_DIALOG_DATA) as { 
    session?: Session; 
    eventId?: string;
    tracks: Track[];
    mode: 'create' | 'edit' 
  };

  isSubmitting = signal(false);
  sessionTypes = Object.values(SessionType);
  difficultyLevels = Object.values(SessionDifficulty);

  sessionForm = this.fb.group({
    title: ['', [Validators.required, Validators.maxLength(300)]],
    description: ['', [Validators.required]],
    abstract: [''],
    slug: ['', [Validators.required, Validators.pattern(/^[a-z0-9-]+$/)]],
    trackId: [null as string | null],
    type: [SessionType.Talk, Validators.required],
    difficultyLevel: [SessionDifficulty.Intermediate, Validators.required],
    language: ['en'],
    startDate: [null as Date | null, Validators.required],
    startTimeValue: [null as Date | null, Validators.required],
    endDate: [null as Date | null, Validators.required],
    endTimeValue: [null as Date | null, Validators.required],
    room: [''],
    building: [''],
    isVirtual: [false],
    virtualUrl: [''],
    isRecorded: [true],
    allowQuestions: [true],
    maxAttendees: [null as number | null],
    requiresSubscription: [true],
    targetAudience: [''],
    prerequisites: [''],
    learningOutcomes: [''],
    materialsUrl: [''],
    recordingUrl: [''],
    tagsInput: ['']
  });

  ngOnInit() {
    if (this.data.mode === 'edit' && this.data.session) {
      const session = this.data.session;
      const startDateTime = new Date(session.startTime);
      const endDateTime = new Date(session.endTime);
      
      // Create time-only Date objects for the timepicker
      // The timepicker uses Date objects but only cares about hours/minutes
      const createTimeDate = (date: Date): Date => {
        const timeDate = new Date();
        timeDate.setHours(date.getHours(), date.getMinutes(), 0, 0);
        return timeDate;
      };
      
      this.sessionForm.patchValue({
        title: session.title,
        description: session.description,
        abstract: session.abstract || '',
        slug: session.slug,
        trackId: session.trackId || null,
        type: session.type,
        difficultyLevel: session.difficultyLevel,
        language: session.language,
        startDate: startDateTime,
        startTimeValue: createTimeDate(startDateTime),
        endDate: endDateTime,
        endTimeValue: createTimeDate(endDateTime),
        room: session.room || '',
        building: session.building || '',
        isVirtual: session.isVirtual,
        virtualUrl: session.virtualUrl || '',
        isRecorded: session.isRecorded,
        allowQuestions: session.allowQuestions,
        maxAttendees: session.maxAttendees,
        requiresSubscription: session.requiresSubscription,
        targetAudience: session.targetAudience || '',
        prerequisites: session.prerequisites || '',
        learningOutcomes: session.learningOutcomes || '',
        materialsUrl: session.materialsUrl || '',
        recordingUrl: session.recordingUrl || '',
        tagsInput: session.tags?.join(', ') || ''
      });
    } else {
      // Set default dates for new sessions (tomorrow at 9:00 and 10:00)
      const tomorrow = new Date();
      tomorrow.setDate(tomorrow.getDate() + 1);
      tomorrow.setHours(9, 0, 0, 0);
      
      const defaultStartTime = new Date();
      defaultStartTime.setHours(9, 0, 0, 0);
      
      const defaultEndTime = new Date();
      defaultEndTime.setHours(10, 0, 0, 0);
      
      this.sessionForm.patchValue({
        startDate: tomorrow,
        startTimeValue: defaultStartTime,
        endDate: tomorrow,
        endTimeValue: defaultEndTime
      });
    }

    // Auto-generate slug from title
    if (this.data.mode === 'create') {
      this.sessionForm.get('title')?.valueChanges.subscribe(title => {
        if (title && !this.sessionForm.get('slug')?.dirty) {
          const slug = title.toLowerCase()
            .replace(/[^a-z0-9\s-]/g, '')
            .replace(/\s+/g, '-')
            .replace(/-+/g, '-')
            .trim();
          this.sessionForm.get('slug')?.setValue(slug);
        }
      });
    }
  }

  // Helper to combine date and time into a single Date object
  // timeValue can be a Date object (from mat-timepicker) or a string (HH:mm format)
  private combineDateAndTime(date: Date, timeValue: Date | string): Date {
    const result = new Date(date);
    
    if (timeValue instanceof Date) {
      // Mat-timepicker returns a Date object
      result.setHours(timeValue.getHours(), timeValue.getMinutes(), 0, 0);
    } else if (typeof timeValue === 'string' && timeValue.includes(':')) {
      // String format "HH:mm"
      const [hours, minutes] = timeValue.split(':').map(Number);
      result.setHours(hours, minutes, 0, 0);
    } else {
      // Fallback - try to parse as date
      const timeDate = new Date(timeValue);
      if (!isNaN(timeDate.getTime())) {
        result.setHours(timeDate.getHours(), timeDate.getMinutes(), 0, 0);
      }
    }
    
    return result;
  }

  async save() {
    if (this.sessionForm.invalid) return;

    this.isSubmitting.set(true);

    const formValue = this.sessionForm.value;
    const tags = formValue.tagsInput 
      ? formValue.tagsInput.split(',').map(t => t.trim().toLowerCase()).filter(t => t)
      : undefined;

    // Combine date and time values
    const startDateTime = this.combineDateAndTime(formValue.startDate!, formValue.startTimeValue!);
    const endDateTime = this.combineDateAndTime(formValue.endDate!, formValue.endTimeValue!);

    try {
      if (this.data.mode === 'create') {
        const request: CreateSessionRequest = {
          eventId: this.data.eventId!,
          trackId: formValue.trackId || undefined,
          title: formValue.title!,
          description: formValue.description!,
          abstract: formValue.abstract || undefined,
          slug: formValue.slug!,
          startTime: startDateTime.toISOString(),
          endTime: endDateTime.toISOString(),
          type: formValue.type!,
          difficultyLevel: formValue.difficultyLevel!,
          maxAttendees: formValue.maxAttendees || undefined,
          requiresSubscription: formValue.requiresSubscription!,
          room: formValue.room || undefined,
          building: formValue.building || undefined,
          isVirtual: formValue.isVirtual!,
          virtualUrl: formValue.virtualUrl || undefined,
          materialsUrl: formValue.materialsUrl || undefined,
          allowQuestions: formValue.allowQuestions!,
          isRecorded: formValue.isRecorded!,
          language: formValue.language || 'en',
          prerequisites: formValue.prerequisites || undefined,
          learningOutcomes: formValue.learningOutcomes || undefined,
          targetAudience: formValue.targetAudience || undefined,
          tags: tags
        };

        await lastValueFrom(
          this.http.post<Session>('/api/v1/admin/sessions', request)
        );
      } else {
        const request: UpdateSessionRequest = {
          trackId: formValue.trackId || undefined,
          title: formValue.title!,
          description: formValue.description!,
          abstract: formValue.abstract || undefined,
          slug: formValue.slug!,
          startTime: startDateTime.toISOString(),
          endTime: endDateTime.toISOString(),
          type: formValue.type!,
          difficultyLevel: formValue.difficultyLevel!,
          maxAttendees: formValue.maxAttendees || undefined,
          requiresSubscription: formValue.requiresSubscription!,
          room: formValue.room || undefined,
          building: formValue.building || undefined,
          isVirtual: formValue.isVirtual!,
          virtualUrl: formValue.virtualUrl || undefined,
          recordingUrl: formValue.recordingUrl || undefined,
          materialsUrl: formValue.materialsUrl || undefined,
          allowQuestions: formValue.allowQuestions!,
          isRecorded: formValue.isRecorded!,
          language: formValue.language || 'en',
          prerequisites: formValue.prerequisites || undefined,
          learningOutcomes: formValue.learningOutcomes || undefined,
          targetAudience: formValue.targetAudience || undefined,
          tags: tags
        };
        await lastValueFrom(
          this.http.put<Session>(`/api/v1/admin/sessions/${this.data.session!.id}`, request)
        );
      }

      this.dialogRef.close(true);
    } catch (err) {
      const error = err as HttpErrorResponse;
      alert(error.error?.message || 'Failed to save session');
    } finally {
      this.isSubmitting.set(false);
    }
  }

  cancel() {
    this.dialogRef.close(false);
  }
}
import { Component, OnInit, signal, computed, inject, NgZone, ChangeDetectionStrategy } from '@angular/core';
import { DatePipe } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { Router, ActivatedRoute } from '@angular/router';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { CdkDragDrop, DragDropModule, moveItemInArray } from '@angular/cdk/drag-drop';
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
import { MatExpansionModule } from '@angular/material/expansion';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatTooltipModule } from '@angular/material/tooltip';
import { lastValueFrom } from 'rxjs';

// Track interfaces and types
export interface Track {
  id: string;
  eventId: string;
  name: string;
  description: string;
  color: string;
  slug: string;
  displayOrder: number;
  isActive: boolean;
  maxSessions?: number;
  tags?: string[];
  metadata?: Record<string, any>;
  createdAt: string;
  updatedAt?: string;
  sessionCount: number;
  totalCapacity: number;
}

export interface CreateTrackRequest {
  eventId: string;
  name: string;
  description: string;
  color: string;
  slug: string;
  displayOrder: number;
  isActive: boolean;
  maxSessions?: number;
  tags?: string[];
  metadata?: Record<string, any>;
}

export interface UpdateTrackRequest {
  name?: string;
  description?: string;
  color?: string;
  slug?: string;
  isActive?: boolean;
  maxSessions?: number;
  tags?: string[];
  metadata?: Record<string, any>;
}

export interface TrackSearchRequest {
  eventId?: string;
  searchText?: string;
  isActive?: boolean;
  tags?: string[];
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

export interface TrackReorderRequest {
  trackOrders: TrackOrderItem[];
}

export interface TrackOrderItem {
  trackId: string;
  displayOrder: number;
}

export interface TrackStatistics {
  totalSessions: number;
  totalCapacity: number;
  confirmedSubscriptions: number;
  waitlistCount: number;
  averageAttendance: number;
  popularSessions: string[];
}

@Component({
  selector: 'app-track-manager',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    DragDropModule,
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
    MatExpansionModule,
    MatToolbarModule,
    MatTooltipModule,
    DatePipe
],
  templateUrl: './track-manager.component.html',
  styleUrls: ['./track-manager.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class TrackManagerComponent implements OnInit {
  private fb = inject(FormBuilder);
  private http = inject(HttpClient);
  private router = inject(Router);
  private route = inject(ActivatedRoute);
  private dialog = inject(MatDialog);
  private ngZone = inject(NgZone);

  // Signals for reactive state management
  tracks = signal<Track[]>([]);
  isLoading = signal(false);
  error = signal<string | null>(null);
  selectedEventId = signal<string | null>(null);
  searchText = signal('');
  showInactive = signal(false);
  pageIndex = signal(0);
  pageSize = signal(25);
  sortBy = signal('displayOrder');
  sortDirection = signal<'asc' | 'desc'>('asc');
  totalCount = signal(0);
  statistics = signal<TrackStatistics | null>(null);

  // Computed values
  displayedColumns = ['drag', 'name', 'description', 'sessionCount', 'status', 'createdAt', 'actions'];
  filteredTracks = computed(() => {
    const text = this.searchText().toLowerCase();
    const showInactive = this.showInactive();
    return this.tracks().filter(track => {
      const matchesText = !text || 
        track.name.toLowerCase().includes(text) || 
        track.description.toLowerCase().includes(text) ||
        track.tags?.some(tag => tag.toLowerCase().includes(text));
      const matchesStatus = showInactive || track.isActive;
      return matchesText && matchesStatus;
    });
  });

  // Form for search/filter
  searchForm = this.fb.group({
    searchText: [''],
    eventId: [''],
    showInactive: [false]
  });

  // Table data source
  dataSource = new MatTableDataSource<Track>([]);

  constructor() {
    // Subscribe to form changes for reactive filtering
    this.searchForm.get('searchText')?.valueChanges.subscribe(value => {
      this.searchText.set(value || '');
    });

    this.searchForm.get('showInactive')?.valueChanges.subscribe(value => {
      this.showInactive.set(!!value);
    });

    this.searchForm.get('eventId')?.valueChanges.subscribe(value => {
      if (value) {
        this.selectedEventId.set(value);
        this.loadTracks();
      }
    });

    // Update data source when tracks change
    this.ngZone.runOutsideAngular(() => {
      computed(() => {
        this.ngZone.run(() => {
          this.dataSource.data = this.filteredTracks();
        });
      });
    });
  }

  async ngOnInit() {
    // Load event ID from route parameters
    const eventId = this.route.snapshot.paramMap.get('eventId');
    if (eventId) {
      this.selectedEventId.set(eventId);
      this.searchForm.patchValue({ eventId });
      await this.loadTracks();
      await this.loadStatistics();
    }
  }

  async loadTracks() {
    if (!this.selectedEventId()) return;

    this.isLoading.set(true);
    this.error.set(null);

    try {
      const request: TrackSearchRequest = {
        eventId: this.selectedEventId()!,
        searchText: this.searchText() || undefined,
        isActive: this.showInactive() ? undefined : true,
        page: this.pageIndex() + 1,
        pageSize: this.pageSize(),
        sortBy: this.sortBy(),
        sortDirection: this.sortDirection()
      };

      const response = await lastValueFrom(
        this.http.post<PagedResult<Track>>('/api/v1/admin/tracks/search', request)
      );

      this.tracks.set(response.items);
      this.totalCount.set(response.totalCount);
    } catch (err) {
      const error = err as HttpErrorResponse;
      this.error.set(error.error?.message || 'Failed to load tracks');
      this.showErrorSnackbar('Failed to load tracks');
    } finally {
      this.isLoading.set(false);
    }
  }

  async loadStatistics() {
    if (!this.selectedEventId()) return;

    try {
      const stats = await lastValueFrom(
        this.http.get<TrackStatistics>(`/api/v1/admin/tracks/${this.selectedEventId()}/statistics`)
      );
      this.statistics.set(stats);
    } catch (err) {
      console.error('Failed to load track statistics:', err);
    }
  }

  async createTrack() {
    const dialogRef = this.dialog.open(TrackDialogComponent, {
      width: '600px',
      data: { eventId: this.selectedEventId(), mode: 'create' }
    });

    const result = await lastValueFrom(dialogRef.afterClosed());
    if (result) {
      await this.loadTracks();
      await this.loadStatistics();
      this.showSuccessSnackbar('Track created successfully');
    }
  }

  async editTrack(track: Track) {
    const dialogRef = this.dialog.open(TrackDialogComponent, {
      width: '600px',
      data: { track, mode: 'edit' }
    });

    const result = await lastValueFrom(dialogRef.afterClosed());
    if (result) {
      await this.loadTracks();
      await this.loadStatistics();
      this.showSuccessSnackbar('Track updated successfully');
    }
  }

  async deleteTrack(track: Track) {
    const confirmed = confirm(`Are you sure you want to delete the track "${track.name}"? This action cannot be undone.`);
    if (!confirmed) return;

    this.isLoading.set(true);

    try {
      await lastValueFrom(
        this.http.delete(`/api/v1/admin/tracks/${track.id}`)
      );

      await this.loadTracks();
      await this.loadStatistics();
      this.showSuccessSnackbar('Track deleted successfully');
    } catch (err) {
      const error = err as HttpErrorResponse;
      this.showErrorSnackbar(error.error?.message || 'Failed to delete track');
    } finally {
      this.isLoading.set(false);
    }
  }

  async toggleTrackStatus(track: Track) {
    this.isLoading.set(true);

    try {
      const endpoint = track.isActive ? 'deactivate' : 'activate';
      await lastValueFrom(
        this.http.post(`/api/v1/admin/tracks/${track.id}/${endpoint}`, {})
      );

      await this.loadTracks();
      await this.loadStatistics();
      const action = track.isActive ? 'deactivated' : 'activated';
      this.showSuccessSnackbar(`Track ${action} successfully`);
    } catch (err) {
      const error = err as HttpErrorResponse;
      this.showErrorSnackbar(error.error?.message || 'Failed to update track status');
    } finally {
      this.isLoading.set(false);
    }
  }

  async duplicateTrack(track: Track) {
    const request: CreateTrackRequest = {
      eventId: track.eventId,
      name: `${track.name} (Copy)`,
      description: track.description,
      color: track.color,
      slug: `${track.slug}-copy`,
      displayOrder: track.displayOrder + 1,
      isActive: false,
      maxSessions: track.maxSessions,
      tags: track.tags ? [...track.tags] : undefined,
      metadata: track.metadata ? { ...track.metadata } : undefined
    };

    this.isLoading.set(true);

    try {
      await lastValueFrom(
        this.http.post<Track>('/api/v1/admin/tracks', request)
      );

      await this.loadTracks();
      await this.loadStatistics();
      this.showSuccessSnackbar('Track duplicated successfully');
    } catch (err) {
      const error = err as HttpErrorResponse;
      this.showErrorSnackbar(error.error?.message || 'Failed to duplicate track');
    } finally {
      this.isLoading.set(false);
    }
  }

  async onDrop(event: CdkDragDrop<Track[]>) {
    if (event.previousIndex === event.currentIndex) return;

    const tracks = [...this.tracks()];
    moveItemInArray(tracks, event.previousIndex, event.currentIndex);

    // Update display orders
    const trackOrders: TrackOrderItem[] = tracks.map((track, index) => ({
      trackId: track.id,
      displayOrder: index + 1
    }));

    this.isLoading.set(true);

    try {
      const request: TrackReorderRequest = { trackOrders };
      await lastValueFrom(
        this.http.post('/api/v1/admin/tracks/reorder', request)
      );

      // Update local state
      tracks.forEach((track, index) => {
        track.displayOrder = index + 1;
      });
      this.tracks.set(tracks);

      this.showSuccessSnackbar('Track order updated successfully');
    } catch (err) {
      const error = err as HttpErrorResponse;
      this.showErrorSnackbar(error.error?.message || 'Failed to reorder tracks');
      // Reload to get correct order
      await this.loadTracks();
    } finally {
      this.isLoading.set(false);
    }
  }

  onPageChange(event: PageEvent) {
    this.pageIndex.set(event.pageIndex);
    this.pageSize.set(event.pageSize);
    this.loadTracks();
  }

  onSort(sort: Sort) {
    this.sortBy.set(sort.active);
    this.sortDirection.set(sort.direction as 'asc' | 'desc');
    this.loadTracks();
  }

  navigateToSessions(trackId: string) {
    this.router.navigate(['/session-management', 'sessions'], { 
      queryParams: { trackId } 
    });
  }

  private showSuccessSnackbar(message: string) {
    console.log('Success:', message);
    // TODO: Implement proper notification system
  }

  private showErrorSnackbar(message: string) {
    console.error('Error:', message);
    // TODO: Implement proper notification system
  }
}

// Dialog Component for creating/editing tracks
@Component({
  selector: 'app-track-dialog',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MatDialogModule,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatCheckboxModule,
    MatChipsModule,
    MatIconModule
],
  template: `
    <h2 mat-dialog-title>
      {{ data.mode === 'create' ? 'Create New Track' : 'Edit Track' }}
    </h2>
    
    <mat-dialog-content>
      <form [formGroup]="trackForm" class="track-form">
        <div class="row">
          <mat-form-field appearance="outline" class="full-width">
            <mat-label>Track Name</mat-label>
            <input matInput formControlName="name" placeholder="Enter track name" required>
            @if (trackForm.get('name')?.hasError('required')) {
              <mat-error>
                Track name is required
              </mat-error>
            }
            @if (trackForm.get('name')?.hasError('maxlength')) {
              <mat-error>
                Track name must be less than 200 characters
              </mat-error>
            }
          </mat-form-field>
        </div>
    
        <div class="row">
          <mat-form-field appearance="outline" class="full-width">
            <mat-label>Description</mat-label>
            <textarea matInput formControlName="description"
              placeholder="Enter track description"
            rows="3"></textarea>
          </mat-form-field>
        </div>
    
        <div class="row">
          <mat-form-field appearance="outline" class="half-width">
            <mat-label>Slug</mat-label>
            <input matInput formControlName="slug" placeholder="track-slug" required>
            <mat-hint>URL-friendly identifier</mat-hint>
            @if (trackForm.get('slug')?.hasError('required')) {
              <mat-error>
                Slug is required
              </mat-error>
            }
            @if (trackForm.get('slug')?.hasError('pattern')) {
              <mat-error>
                Slug must contain only letters, numbers, and hyphens
              </mat-error>
            }
          </mat-form-field>
    
          <mat-form-field appearance="outline" class="half-width">
            <mat-label>Color</mat-label>
            <input matInput type="color" formControlName="color" required>
          </mat-form-field>
        </div>
    
        <div class="row">
          <mat-form-field appearance="outline" class="half-width">
            <mat-label>Display Order</mat-label>
            <input matInput type="number" formControlName="displayOrder" min="1">
          </mat-form-field>
    
          <mat-form-field appearance="outline" class="half-width">
            <mat-label>Max Sessions</mat-label>
            <input matInput type="number" formControlName="maxSessions" min="1">
            <mat-hint>Leave empty for unlimited</mat-hint>
          </mat-form-field>
        </div>
    
        <div class="row">
          <mat-form-field appearance="outline" class="full-width">
            <mat-label>Tags</mat-label>
            <input matInput
              placeholder="Add tags..."
              (keydown.enter)="addTagFromInput($event.target)">
            </mat-form-field>
          </div>
    
          <div class="row">
            <mat-checkbox formControlName="isActive">Active</mat-checkbox>
          </div>
        </form>
      </mat-dialog-content>
    
      <mat-dialog-actions align="end">
        <button mat-button (click)="cancel()">Cancel</button>
        <button mat-raised-button
          color="primary"
          [disabled]="trackForm.invalid || isSubmitting()"
          (click)="save()">
          {{ data.mode === 'create' ? 'Create' : 'Update' }}
        </button>
      </mat-dialog-actions>
    `,
  styles: [`
    .track-form {
      min-width: 500px;
    }
    .row {
      display: flex;
      gap: 16px;
      margin-bottom: 16px;
    }
    .full-width {
      flex: 1;
    }
    .half-width {
      flex: 0.5;
    }
  `]
})
export class TrackDialogComponent implements OnInit {
  private fb = inject(FormBuilder);
  private http = inject(HttpClient);
  private dialogRef = inject(MatDialogRef<TrackDialogComponent>);
  data = inject(MAT_DIALOG_DATA) as { track?: Track; eventId?: string; mode: 'create' | 'edit' };

  isSubmitting = signal(false);
  currentTags: string[] = [];

  trackForm = this.fb.group({
    name: ['', [Validators.required, Validators.maxLength(200)]],
    description: [''],
    slug: ['', [Validators.required, Validators.pattern(/^[a-z0-9-]+$/)]],
    color: ['#3f51b5', Validators.required],
    displayOrder: [1, [Validators.required, Validators.min(1)]],
    maxSessions: [null as number | null],
    tags: [[] as string[]],
    isActive: [true]
  });

  ngOnInit() {
    if (this.data.mode === 'edit' && this.data.track) {
      const track = this.data.track;
      this.trackForm.patchValue({
        name: track.name,
        description: track.description,
        slug: track.slug,
        color: track.color,
        displayOrder: track.displayOrder,
        maxSessions: track.maxSessions,
        tags: track.tags || [],
        isActive: track.isActive
      });
      this.currentTags = track.tags || [];
    }

    // Auto-generate slug from name
    if (this.data.mode === 'create') {
      this.trackForm.get('name')?.valueChanges.subscribe(name => {
        if (name && !this.trackForm.get('slug')?.dirty) {
          const slug = name.toLowerCase()
            .replace(/[^a-z0-9\s-]/g, '')
            .replace(/\s+/g, '-')
            .replace(/-+/g, '-')
            .trim();
          this.trackForm.get('slug')?.setValue(slug);
        }
      });
    }
  }

  addTag(event: any) {
    const value = (event.value || '').trim().toLowerCase();
    if (value && !this.currentTags.includes(value)) {
      this.currentTags.push(value);
      this.trackForm.get('tags')?.setValue([...this.currentTags]);
    }
    event.chipInput?.clear();
  }

  addTagFromInput(target: any) {
    const input = target as HTMLInputElement;
    const value = input.value.trim().toLowerCase();
    if (value && !this.currentTags.includes(value)) {
      this.currentTags.push(value);
      this.trackForm.get('tags')?.setValue([...this.currentTags]);
      input.value = '';
    }
  }

  removeTag(tag: string) {
    const index = this.currentTags.indexOf(tag);
    if (index >= 0) {
      this.currentTags.splice(index, 1);
      this.trackForm.get('tags')?.setValue([...this.currentTags]);
    }
  }

  trackByTag(index: number, tag: string): string {
    return tag;
  }

  async save() {
    if (this.trackForm.invalid) return;

    this.isSubmitting.set(true);

    try {
      if (this.data.mode === 'create') {
        const request: CreateTrackRequest = {
          eventId: this.data.eventId!,
          ...this.trackForm.value as any
        };

        await lastValueFrom(
          this.http.post<Track>('/api/v1/admin/tracks', request)
        );
      } else {
        const request: UpdateTrackRequest = this.trackForm.value as any;
        await lastValueFrom(
          this.http.put<Track>(`/api/v1/admin/tracks/${this.data.track!.id}`, request)
        );
      }

      this.dialogRef.close(true);
    } catch (err) {
      const error = err as HttpErrorResponse;
      alert(error.error?.message || 'Failed to save track');
    } finally {
      this.isSubmitting.set(false);
    }
  }

  cancel() {
    this.dialogRef.close(false);
  }
}
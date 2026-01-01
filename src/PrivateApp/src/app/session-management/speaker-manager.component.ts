import { Component, OnInit, signal, computed, inject, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
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
import { MatMenuModule } from '@angular/material/menu';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatDividerModule } from '@angular/material/divider';
import { COMMA, ENTER } from '@angular/cdk/keycodes';
import { MatChipInputEvent } from '@angular/material/chips';
import { lastValueFrom } from 'rxjs';

// Speaker interfaces and types
export interface SpeakerProfile {
  id: string;
  userId: string;
  displayName: string;
  title?: string;
  company?: string;
  shortBio?: string;
  fullBio?: string;
  photoUrl?: string;
  contactEmail?: string;
  phoneNumber?: string;
  websiteUrl?: string;
  socialLinks?: Record<string, string>;
  expertiseAreas?: string[];
  preferredSessionTypes?: string;
  availabilityNotes?: string;
  isPublic: boolean;
  isActive: boolean;
  status: SpeakerStatus;
  createdAt: string;
  updatedAt: string;
  userEmail?: string;
  userFullName?: string;
}

export enum SpeakerStatus {
  Active = 'Active',
  Pending = 'Pending',
  Inactive = 'Inactive',
  Suspended = 'Suspended',
  Archived = 'Archived'
}

export enum SpeakerRole {
  Speaker = 'Speaker',
  CoSpeaker = 'CoSpeaker',
  Moderator = 'Moderator',
  Panelist = 'Panelist',
  Facilitator = 'Facilitator'
}

export interface CreateSpeakerProfileRequest {
  userId: string;
  displayName: string;
  title?: string;
  company?: string;
  shortBio?: string;
  fullBio?: string;
  photoUrl?: string;
  contactEmail?: string;
  phoneNumber?: string;
  websiteUrl?: string;
  socialLinks?: Record<string, string>;
  expertiseAreas?: string[];
  preferredSessionTypes?: string;
  availabilityNotes?: string;
  isPublic: boolean;
}

export interface UpdateSpeakerProfileRequest {
  displayName?: string;
  title?: string;
  company?: string;
  shortBio?: string;
  fullBio?: string;
  photoUrl?: string;
  contactEmail?: string;
  phoneNumber?: string;
  websiteUrl?: string;
  socialLinks?: Record<string, string>;
  expertiseAreas?: string[];
  preferredSessionTypes?: string;
  availabilityNotes?: string;
  isPublic?: boolean;
  isActive?: boolean;
}

export interface SpeakerSearchRequest {
  query?: string;
  company?: string;
  expertiseAreas?: string[];
  status?: SpeakerStatus;
  isActive?: boolean;
  isPublic?: boolean;
  page: number;
  pageSize: number;
  sortBy?: string;
  sortDescending: boolean;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  currentPage: number;
  pageSize: number;
  totalPages: number;
}

export interface SessionSpeakerDetail {
  id: string;
  sessionId: string;
  speakerId: string;
  role: SpeakerRole;
  displayOrder: number;
  speaker?: SpeakerProfile;
}

export interface AssignSpeakerRequest {
  speakerId: string;
  role: SpeakerRole;
  displayOrder: number;
}

@Component({
  selector: 'app-speaker-manager',
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
    MatMenuModule,
    MatSnackBarModule,
    MatDividerModule
  ],
  templateUrl: './speaker-manager.component.html',
  styleUrls: ['./speaker-manager.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class SpeakerManagerComponent implements OnInit {
  private fb = inject(FormBuilder);
  private http = inject(HttpClient);
  private router = inject(Router);
  private route = inject(ActivatedRoute);
  private dialog = inject(MatDialog);
  private snackBar = inject(MatSnackBar);

  // Signals for reactive state management
  speakers = signal<SpeakerProfile[]>([]);
  isLoading = signal(false);
  error = signal<string | null>(null);
  searchText = signal('');
  selectedStatus = signal<SpeakerStatus | null>(null);
  showInactive = signal(false);
  pageIndex = signal(0);
  pageSize = signal(25);
  sortBy = signal('displayName');
  sortDescending = signal(false);
  totalCount = signal(0);
  selectedEventId = signal<string | null>(null);

  // Computed values
  displayedColumns = ['photo', 'displayName', 'company', 'expertise', 'status', 'actions'];
  
  // Status options
  statusOptions = Object.values(SpeakerStatus);
  roleOptions = Object.values(SpeakerRole);

  // Form for search/filter
  searchForm = this.fb.group({
    searchText: [''],
    company: [''],
    status: [null as SpeakerStatus | null],
    showInactive: [false]
  });

  // Table data source
  dataSource = new MatTableDataSource<SpeakerProfile>([]);

  // Chip input configuration
  readonly separatorKeysCodes = [ENTER, COMMA] as const;

  constructor() {
    // Subscribe to form changes for reactive filtering
    this.searchForm.get('searchText')?.valueChanges.subscribe(value => {
      this.searchText.set(value || '');
    });

    this.searchForm.get('status')?.valueChanges.subscribe(value => {
      this.selectedStatus.set(value);
      this.loadSpeakers();
    });

    this.searchForm.get('showInactive')?.valueChanges.subscribe(value => {
      this.showInactive.set(!!value);
      this.loadSpeakers();
    });
  }

  async ngOnInit() {
    // Load event ID from route parameters if present
    const eventId = this.route.snapshot.paramMap.get('eventId');
    if (eventId) {
      this.selectedEventId.set(eventId);
    }
    await this.loadSpeakers();
  }

  async loadSpeakers() {
    this.isLoading.set(true);
    this.error.set(null);

    try {
      const request: SpeakerSearchRequest = {
        query: this.searchText() || undefined,
        company: this.searchForm.get('company')?.value || undefined,
        status: this.selectedStatus() || undefined,
        isActive: this.showInactive() ? undefined : true,
        page: this.pageIndex() + 1,
        pageSize: this.pageSize(),
        sortBy: this.sortBy(),
        sortDescending: this.sortDescending()
      };

      const response = await lastValueFrom(
        this.http.post<PagedResult<SpeakerProfile>>('/api/v1/admin/speakers/search', request)
      );

      this.speakers.set(response.items);
      this.dataSource.data = response.items;
      this.totalCount.set(response.totalCount);
    } catch (err) {
      const error = err as HttpErrorResponse;
      this.error.set(error.error?.message || 'Failed to load speakers');
      this.showErrorSnackbar('Failed to load speakers');
    } finally {
      this.isLoading.set(false);
    }
  }

  async onSearch() {
    this.pageIndex.set(0);
    await this.loadSpeakers();
  }

  onPageChange(event: PageEvent) {
    this.pageIndex.set(event.pageIndex);
    this.pageSize.set(event.pageSize);
    this.loadSpeakers();
  }

  onSortChange(sort: Sort) {
    this.sortBy.set(sort.active);
    this.sortDescending.set(sort.direction === 'desc');
    this.loadSpeakers();
  }

  openCreateDialog() {
    const dialogRef = this.dialog.open(SpeakerFormDialogComponent, {
      width: '700px',
      data: { mode: 'create' }
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result) {
        this.loadSpeakers();
        this.showSuccessSnackbar('Speaker profile created successfully');
      }
    });
  }

  openEditDialog(speaker: SpeakerProfile) {
    const dialogRef = this.dialog.open(SpeakerFormDialogComponent, {
      width: '700px',
      data: { mode: 'edit', speaker }
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result) {
        this.loadSpeakers();
        this.showSuccessSnackbar('Speaker profile updated successfully');
      }
    });
  }

  openViewDialog(speaker: SpeakerProfile) {
    this.dialog.open(SpeakerViewDialogComponent, {
      width: '600px',
      data: { speaker }
    });
  }

  async updateStatus(speaker: SpeakerProfile, status: SpeakerStatus) {
    try {
      await lastValueFrom(
        this.http.patch(`/api/v1/admin/speakers/${speaker.id}/status`, { status })
      );
      await this.loadSpeakers();
      this.showSuccessSnackbar(`Speaker status updated to ${status}`);
    } catch (err) {
      const error = err as HttpErrorResponse;
      this.showErrorSnackbar(error.error?.message || 'Failed to update status');
    }
  }

  async deleteSpeaker(speaker: SpeakerProfile) {
    const dialogRef = this.dialog.open(ConfirmDialogComponent, {
      width: '400px',
      data: {
        title: 'Delete Speaker',
        message: `Are you sure you want to delete ${speaker.displayName}'s profile? This action cannot be undone.`,
        confirmText: 'Delete',
        cancelText: 'Cancel'
      }
    });

    dialogRef.afterClosed().subscribe(async (confirmed) => {
      if (confirmed) {
        try {
          await lastValueFrom(
            this.http.delete(`/api/v1/admin/speakers/${speaker.id}`)
          );
          await this.loadSpeakers();
          this.showSuccessSnackbar('Speaker profile deleted successfully');
        } catch (err) {
          const error = err as HttpErrorResponse;
          this.showErrorSnackbar(error.error?.message || 'Failed to delete speaker');
        }
      }
    });
  }

  getStatusColor(status: SpeakerStatus): string {
    switch (status) {
      case SpeakerStatus.Active: return 'primary';
      case SpeakerStatus.Pending: return 'accent';
      case SpeakerStatus.Inactive: return 'warn';
      case SpeakerStatus.Suspended: return 'warn';
      case SpeakerStatus.Archived: return '';
      default: return '';
    }
  }

  getExpertiseDisplay(speaker: SpeakerProfile): string {
    if (!speaker.expertiseAreas || speaker.expertiseAreas.length === 0) {
      return '-';
    }
    if (speaker.expertiseAreas.length <= 2) {
      return speaker.expertiseAreas.join(', ');
    }
    return `${speaker.expertiseAreas.slice(0, 2).join(', ')} +${speaker.expertiseAreas.length - 2}`;
  }

  private showSuccessSnackbar(message: string) {
    this.snackBar.open(message, 'Close', {
      duration: 3000,
      horizontalPosition: 'end',
      verticalPosition: 'top',
      panelClass: ['snackbar-success']
    });
  }

  private showErrorSnackbar(message: string) {
    this.snackBar.open(message, 'Close', {
      duration: 5000,
      horizontalPosition: 'end',
      verticalPosition: 'top',
      panelClass: ['snackbar-error']
    });
  }
}

// Speaker Form Dialog Component
@Component({
  selector: 'app-speaker-form-dialog',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatCheckboxModule,
    MatButtonModule,
    MatIconModule,
    MatChipsModule,
    MatProgressSpinnerModule
  ],
  template: `
    <h2 mat-dialog-title>{{ data.mode === 'create' ? 'Create Speaker Profile' : 'Edit Speaker Profile' }}</h2>
    <mat-dialog-content>
      <form [formGroup]="speakerForm" class="speaker-form">
        <div class="form-row">
          <mat-form-field appearance="outline" class="full-width">
            <mat-label>Display Name</mat-label>
            <input matInput formControlName="displayName" placeholder="Speaker's display name">
            @if (speakerForm.get('displayName')?.hasError('required')) {
              <mat-error>Display name is required</mat-error>
            }
          </mat-form-field>
        </div>

        <div class="form-row two-column">
          <mat-form-field appearance="outline">
            <mat-label>Title</mat-label>
            <input matInput formControlName="title" placeholder="e.g., Senior Developer">
          </mat-form-field>

          <mat-form-field appearance="outline">
            <mat-label>Company</mat-label>
            <input matInput formControlName="company" placeholder="Company name">
          </mat-form-field>
        </div>

        <div class="form-row two-column">
          <mat-form-field appearance="outline">
            <mat-label>Contact Email</mat-label>
            <input matInput formControlName="contactEmail" type="email" placeholder="speaker@example.com">
            @if (speakerForm.get('contactEmail')?.hasError('email')) {
              <mat-error>Please enter a valid email</mat-error>
            }
          </mat-form-field>

          <mat-form-field appearance="outline">
            <mat-label>Phone Number</mat-label>
            <input matInput formControlName="phoneNumber" placeholder="+1 (555) 123-4567">
          </mat-form-field>
        </div>

        <div class="form-row">
          <mat-form-field appearance="outline" class="full-width">
            <mat-label>Photo URL</mat-label>
            <input matInput formControlName="photoUrl" placeholder="https://example.com/photo.jpg">
          </mat-form-field>
        </div>

        <div class="form-row">
          <mat-form-field appearance="outline" class="full-width">
            <mat-label>Website URL</mat-label>
            <input matInput formControlName="websiteUrl" placeholder="https://speaker-website.com">
          </mat-form-field>
        </div>

        <div class="form-row">
          <mat-form-field appearance="outline" class="full-width">
            <mat-label>Short Bio</mat-label>
            <textarea matInput formControlName="shortBio" rows="2" 
                      placeholder="Brief introduction (max 500 characters)"></textarea>
            <mat-hint align="end">{{ speakerForm.get('shortBio')?.value?.length || 0 }}/500</mat-hint>
          </mat-form-field>
        </div>

        <div class="form-row">
          <mat-form-field appearance="outline" class="full-width">
            <mat-label>Full Bio</mat-label>
            <textarea matInput formControlName="fullBio" rows="4" 
                      placeholder="Detailed biography (supports markdown)"></textarea>
          </mat-form-field>
        </div>

        <div class="form-row">
          <mat-form-field appearance="outline" class="full-width">
            <mat-label>Expertise Areas</mat-label>
            <mat-chip-grid #chipGrid>
              @for (area of expertiseAreas(); track area) {
                <mat-chip-row (removed)="removeExpertise(area)">
                  {{ area }}
                  <button matChipRemove>
                    <mat-icon>cancel</mat-icon>
                  </button>
                </mat-chip-row>
              }
            </mat-chip-grid>
            <input placeholder="Add expertise area..."
                   [matChipInputFor]="chipGrid"
                   [matChipInputSeparatorKeyCodes]="separatorKeysCodes"
                   (matChipInputTokenEnd)="addExpertise($event)">
          </mat-form-field>
        </div>

        <div class="form-row">
          <mat-form-field appearance="outline" class="full-width">
            <mat-label>Preferred Session Types</mat-label>
            <input matInput formControlName="preferredSessionTypes" 
                   placeholder="e.g., Workshop, Keynote, Panel">
          </mat-form-field>
        </div>

        <div class="form-row">
          <mat-form-field appearance="outline" class="full-width">
            <mat-label>Availability Notes</mat-label>
            <textarea matInput formControlName="availabilityNotes" rows="2" 
                      placeholder="Any scheduling constraints or preferences"></textarea>
          </mat-form-field>
        </div>

        <div class="form-row social-links">
          <h4>Social Links</h4>
          <div class="social-links-grid">
            <mat-form-field appearance="outline">
              <mat-label>Twitter</mat-label>
              <input matInput formControlName="twitter" placeholder="@handle">
              <mat-icon matPrefix>alternate_email</mat-icon>
            </mat-form-field>

            <mat-form-field appearance="outline">
              <mat-label>LinkedIn</mat-label>
              <input matInput formControlName="linkedin" placeholder="linkedin.com/in/...">
              <mat-icon matPrefix>link</mat-icon>
            </mat-form-field>

            <mat-form-field appearance="outline">
              <mat-label>GitHub</mat-label>
              <input matInput formControlName="github" placeholder="github.com/...">
              <mat-icon matPrefix>code</mat-icon>
            </mat-form-field>
          </div>
        </div>

        <div class="form-row">
          <mat-checkbox formControlName="isPublic">Public Profile</mat-checkbox>
        </div>
      </form>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button (click)="onCancel()">Cancel</button>
      <button mat-raised-button color="primary" 
              (click)="onSave()" 
              [disabled]="speakerForm.invalid || isSubmitting()">
        @if (isSubmitting()) {
          <mat-spinner diameter="20"></mat-spinner>
        } @else {
          {{ data.mode === 'create' ? 'Create' : 'Save' }}
        }
      </button>
    </mat-dialog-actions>
  `,
  styles: [`
    .speaker-form {
      display: flex;
      flex-direction: column;
      gap: 8px;
      min-width: 500px;
    }

    .form-row {
      display: flex;
      gap: 16px;
    }

    .form-row.two-column {
      > * {
        flex: 1;
      }
    }

    .full-width {
      width: 100%;
    }

    .social-links {
      flex-direction: column;

      h4 {
        margin: 8px 0;
        color: rgba(0, 0, 0, 0.6);
      }
    }

    .social-links-grid {
      display: grid;
      grid-template-columns: repeat(3, 1fr);
      gap: 16px;
    }

    mat-dialog-actions {
      padding: 16px 24px;
    }
  `]
})
export class SpeakerFormDialogComponent {
  private fb = inject(FormBuilder);
  private http = inject(HttpClient);
  private dialogRef = inject(MatDialogRef<SpeakerFormDialogComponent>);
  data = inject(MAT_DIALOG_DATA) as { mode: 'create' | 'edit'; speaker?: SpeakerProfile };

  isSubmitting = signal(false);
  expertiseAreas = signal<string[]>([]);
  readonly separatorKeysCodes = [ENTER, COMMA] as const;

  speakerForm = this.fb.group({
    userId: ['', this.data.mode === 'create' ? Validators.required : []],
    displayName: ['', Validators.required],
    title: [''],
    company: [''],
    shortBio: ['', Validators.maxLength(500)],
    fullBio: [''],
    photoUrl: [''],
    contactEmail: ['', Validators.email],
    phoneNumber: [''],
    websiteUrl: [''],
    preferredSessionTypes: [''],
    availabilityNotes: [''],
    isPublic: [true],
    twitter: [''],
    linkedin: [''],
    github: ['']
  });

  ngOnInit() {
    if (this.data.mode === 'edit' && this.data.speaker) {
      const speaker = this.data.speaker;
      this.speakerForm.patchValue({
        displayName: speaker.displayName,
        title: speaker.title || '',
        company: speaker.company || '',
        shortBio: speaker.shortBio || '',
        fullBio: speaker.fullBio || '',
        photoUrl: speaker.photoUrl || '',
        contactEmail: speaker.contactEmail || '',
        phoneNumber: speaker.phoneNumber || '',
        websiteUrl: speaker.websiteUrl || '',
        preferredSessionTypes: speaker.preferredSessionTypes || '',
        availabilityNotes: speaker.availabilityNotes || '',
        isPublic: speaker.isPublic,
        twitter: speaker.socialLinks?.['twitter'] || '',
        linkedin: speaker.socialLinks?.['linkedin'] || '',
        github: speaker.socialLinks?.['github'] || ''
      });
      this.expertiseAreas.set(speaker.expertiseAreas || []);
    }
  }

  addExpertise(event: MatChipInputEvent) {
    const value = (event.value || '').trim();
    if (value) {
      this.expertiseAreas.update(areas => [...areas, value]);
    }
    event.chipInput!.clear();
  }

  removeExpertise(area: string) {
    this.expertiseAreas.update(areas => areas.filter(a => a !== area));
  }

  async onSave() {
    if (this.speakerForm.invalid) return;

    this.isSubmitting.set(true);
    const formValue = this.speakerForm.value;

    const socialLinks: Record<string, string> = {};
    if (formValue.twitter) socialLinks['twitter'] = formValue.twitter;
    if (formValue.linkedin) socialLinks['linkedin'] = formValue.linkedin;
    if (formValue.github) socialLinks['github'] = formValue.github;

    try {
      if (this.data.mode === 'create') {
        const request: CreateSpeakerProfileRequest = {
          userId: formValue.userId!,
          displayName: formValue.displayName!,
          title: formValue.title || undefined,
          company: formValue.company || undefined,
          shortBio: formValue.shortBio || undefined,
          fullBio: formValue.fullBio || undefined,
          photoUrl: formValue.photoUrl || undefined,
          contactEmail: formValue.contactEmail || undefined,
          phoneNumber: formValue.phoneNumber || undefined,
          websiteUrl: formValue.websiteUrl || undefined,
          socialLinks: Object.keys(socialLinks).length > 0 ? socialLinks : undefined,
          expertiseAreas: this.expertiseAreas().length > 0 ? this.expertiseAreas() : undefined,
          preferredSessionTypes: formValue.preferredSessionTypes || undefined,
          availabilityNotes: formValue.availabilityNotes || undefined,
          isPublic: formValue.isPublic!
        };

        await lastValueFrom(
          this.http.post('/api/v1/admin/speakers', request)
        );
      } else {
        const request: UpdateSpeakerProfileRequest = {
          displayName: formValue.displayName || undefined,
          title: formValue.title || undefined,
          company: formValue.company || undefined,
          shortBio: formValue.shortBio || undefined,
          fullBio: formValue.fullBio || undefined,
          photoUrl: formValue.photoUrl || undefined,
          contactEmail: formValue.contactEmail || undefined,
          phoneNumber: formValue.phoneNumber || undefined,
          websiteUrl: formValue.websiteUrl || undefined,
          socialLinks: Object.keys(socialLinks).length > 0 ? socialLinks : undefined,
          expertiseAreas: this.expertiseAreas().length > 0 ? this.expertiseAreas() : undefined,
          preferredSessionTypes: formValue.preferredSessionTypes || undefined,
          availabilityNotes: formValue.availabilityNotes || undefined,
          isPublic: formValue.isPublic!
        };

        await lastValueFrom(
          this.http.put(`/api/v1/admin/speakers/${this.data.speaker!.id}`, request)
        );
      }

      this.dialogRef.close(true);
    } catch (err) {
      console.error('Error saving speaker:', err);
    } finally {
      this.isSubmitting.set(false);
    }
  }

  onCancel() {
    this.dialogRef.close(false);
  }
}

// Speaker View Dialog Component
@Component({
  selector: 'app-speaker-view-dialog',
  standalone: true,
  imports: [
    CommonModule,
    MatDialogModule,
    MatButtonModule,
    MatIconModule,
    MatChipsModule
  ],
  template: `
    <h2 mat-dialog-title>{{ data.speaker.displayName }}</h2>
    <mat-dialog-content>
      <div class="speaker-view">
        <div class="speaker-header">
          @if (data.speaker.photoUrl) {
            <img [src]="data.speaker.photoUrl" [alt]="data.speaker.displayName" class="speaker-photo">
          } @else {
            <div class="speaker-photo-placeholder">
              <mat-icon>person</mat-icon>
            </div>
          }
          <div class="speaker-info">
            <h3>{{ data.speaker.displayName }}</h3>
            @if (data.speaker.title || data.speaker.company) {
              <p class="speaker-title">
                {{ data.speaker.title }}
                @if (data.speaker.title && data.speaker.company) { at }
                {{ data.speaker.company }}
              </p>
            }
          </div>
        </div>

        @if (data.speaker.shortBio) {
          <div class="speaker-section">
            <h4>About</h4>
            <p>{{ data.speaker.shortBio }}</p>
          </div>
        }

        @if (data.speaker.expertiseAreas?.length) {
          <div class="speaker-section">
            <h4>Expertise</h4>
            <mat-chip-set>
              @for (area of data.speaker.expertiseAreas; track area) {
                <mat-chip>{{ area }}</mat-chip>
              }
            </mat-chip-set>
          </div>
        }

        @if (data.speaker.contactEmail || data.speaker.websiteUrl) {
          <div class="speaker-section">
            <h4>Contact</h4>
            @if (data.speaker.contactEmail) {
              <p><mat-icon>email</mat-icon> {{ data.speaker.contactEmail }}</p>
            }
            @if (data.speaker.websiteUrl) {
              <p><mat-icon>language</mat-icon> <a [href]="data.speaker.websiteUrl" target="_blank">{{ data.speaker.websiteUrl }}</a></p>
            }
          </div>
        }

        @if (data.speaker.socialLinks && (data.speaker.socialLinks['twitter'] || data.speaker.socialLinks['linkedin'] || data.speaker.socialLinks['github'])) {
          <div class="speaker-section social-links">
            <h4>Social Links</h4>
            <div class="social-icons">
              @if (data.speaker.socialLinks['twitter']) {
                <a [href]="'https://twitter.com/' + data.speaker.socialLinks['twitter']" target="_blank" matTooltip="Twitter">
                  <mat-icon>alternate_email</mat-icon>
                </a>
              }
              @if (data.speaker.socialLinks['linkedin']) {
                <a [href]="data.speaker.socialLinks['linkedin']" target="_blank" matTooltip="LinkedIn">
                  <mat-icon>link</mat-icon>
                </a>
              }
              @if (data.speaker.socialLinks['github']) {
                <a [href]="'https://github.com/' + data.speaker.socialLinks['github']" target="_blank" matTooltip="GitHub">
                  <mat-icon>code</mat-icon>
                </a>
              }
            </div>
          </div>
        }

        <div class="speaker-section metadata">
          <p><strong>Status:</strong> {{ data.speaker.status }}</p>
          <p><strong>Public:</strong> {{ data.speaker.isPublic ? 'Yes' : 'No' }}</p>
          <p><strong>Active:</strong> {{ data.speaker.isActive ? 'Yes' : 'No' }}</p>
        </div>
      </div>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button [mat-dialog-close]>Close</button>
    </mat-dialog-actions>
  `,
  styles: [`
    .speaker-view {
      min-width: 400px;
    }

    .speaker-header {
      display: flex;
      gap: 16px;
      margin-bottom: 24px;
    }

    .speaker-photo, .speaker-photo-placeholder {
      width: 100px;
      height: 100px;
      border-radius: 50%;
      object-fit: cover;
    }

    .speaker-photo-placeholder {
      background: #e0e0e0;
      display: flex;
      align-items: center;
      justify-content: center;

      mat-icon {
        font-size: 48px;
        width: 48px;
        height: 48px;
        color: #9e9e9e;
      }
    }

    .speaker-info {
      display: flex;
      flex-direction: column;
      justify-content: center;

      h3 {
        margin: 0 0 4px 0;
      }

      .speaker-title {
        margin: 0;
        color: rgba(0, 0, 0, 0.6);
      }
    }

    .speaker-section {
      margin-bottom: 16px;

      h4 {
        margin: 0 0 8px 0;
        color: rgba(0, 0, 0, 0.6);
        font-size: 14px;
        text-transform: uppercase;
      }

      p {
        display: flex;
        align-items: center;
        gap: 8px;
        margin: 4px 0;
      }
    }

    .social-links .social-icons {
      display: flex;
      gap: 16px;

      a {
        color: inherit;
        text-decoration: none;
      }
    }

    .metadata {
      background: #f5f5f5;
      padding: 12px;
      border-radius: 4px;

      p {
        margin: 4px 0;
      }
    }
  `]
})
export class SpeakerViewDialogComponent {
  data = inject(MAT_DIALOG_DATA) as { speaker: SpeakerProfile };
}

// Confirm Dialog Component
@Component({
  selector: 'app-confirm-dialog',
  standalone: true,
  imports: [
    CommonModule,
    MatDialogModule,
    MatButtonModule
  ],
  template: `
    <h2 mat-dialog-title>{{ data.title }}</h2>
    <mat-dialog-content>
      <p>{{ data.message }}</p>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button [mat-dialog-close]="false">{{ data.cancelText }}</button>
      <button mat-raised-button color="warn" [mat-dialog-close]="true">{{ data.confirmText }}</button>
    </mat-dialog-actions>
  `
})
export class ConfirmDialogComponent {
  data = inject(MAT_DIALOG_DATA) as {
    title: string;
    message: string;
    confirmText: string;
    cancelText: string;
  };
}

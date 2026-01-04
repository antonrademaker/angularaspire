import { Component, OnInit, OnDestroy, inject, signal } from '@angular/core';

import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { FormBuilder, FormGroup, FormArray, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, ActivatedRoute } from '@angular/router';
import { Subject, takeUntil, combineLatest, startWith } from 'rxjs';
import { environment } from '../../environments/environment';
import { RealTimeService } from '../shared/real-time.service';
import { NotificationService } from '../shared/notification.service';

// Registration interfaces matching the API
export interface RegisterRequest {
  eventId: string;
  priority: RegistrationPriority;
  registrationData?: Record<string, any>;
  notes?: string;
}

export interface RegistrationResponse {
  id: string;
  event: EventSummary;
  user: UserSummary;
  status: string;
  priority: string;
  queuePosition?: number;
  registeredAt: string;
  confirmedAt?: string;
  estimatedWaitTimeMinutes?: number;
  registrationData?: Record<string, any>;
  canBeCancelled: boolean;
  confirmationUrl?: string;
}

export interface EventSummary {
  id: string;
  slug: string;
  title: string;
  startDateTime: string;
  endDateTime: string;
  location: string;
  venue?: string;
  maxCapacity?: number;
  currentRegistrations: number;
}

export interface UserSummary {
  id: string;
  name: string;
  email: string;
}

export interface QueueStatusResponse {
  eventId: string;
  confirmedCount: number;
  queuedCount: number;
  maxCapacity?: number;
  availableSpots?: number;
  isAtCapacity: boolean;
  processingRate?: number;
  estimatedClearTimeMinutes?: number;
}

export enum RegistrationPriority {
  Low = 0,
  Normal = 1,
  High = 2,
  VIP = 3
}

@Component({
  selector: 'app-registration-form',
  standalone: true,
  imports: [ReactiveFormsModule],
  template: `
    <div class="registration-form-container">
      <header class="form-header">
        <h1>Register for Event</h1>
        
        <!-- Real-time connection indicator -->
        <div class="connection-status" [class.connected]="isConnectedToRealTime()" [class.disconnected]="!isConnectedToRealTime()">
          @if (isConnectedToRealTime()) {
            <span class="status-indicator connected"></span>
            <span class="status-text">Live updates active</span>
            @if (lastQueueUpdate()) {
              <span class="last-update">Updated {{ formatRelativeTime(lastQueueUpdate()!) }}</span>
            }
          } @else {
            <span class="status-indicator disconnected"></span>
            <span class="status-text">Live updates unavailable</span>
            <span class="fallback-info">Updates every 30 seconds</span>
          }
        </div>

        @if (eventDetails()) {
          <div class="event-summary">
            <h2>{{ eventDetails()!.title }}</h2>
            <div class="event-info">
              <span class="date">{{ formatDate(eventDetails()!.startDateTime) }}</span>
              <span class="location">{{ eventDetails()!.location }}</span>
              @if (eventDetails()!.venue) {
                <span class="venue">{{ eventDetails()!.venue }}</span>
              }
            </div>
            <div class="capacity-info">
              <span class="current">{{ eventDetails()!.currentRegistrations }}</span>
              @if (eventDetails()!.maxCapacity) {
                <span> / {{ eventDetails()!.maxCapacity }} attendees</span>
              }
            </div>
          </div>
        }
      </header>

      @if (queueStatus()) {
        <div class="queue-status-card" [class.at-capacity]="queueStatus()!.isAtCapacity">
          <h3>Event Status</h3>
          <div class="status-info">
            <div class="stat">
              <span class="label">Confirmed</span>
              <span class="value">{{ queueStatus()!.confirmedCount }}</span>
            </div>
            <div class="stat">
              <span class="label">In Queue</span>
              <span class="value">{{ queueStatus()!.queuedCount }}</span>
            </div>
            @if (queueStatus()!.availableSpots !== undefined) {
              <div class="stat">
                <span class="label">Available</span>
                <span class="value">{{ queueStatus()!.availableSpots }}</span>
              </div>
            }
          </div>
          @if (queueStatus()!.isAtCapacity) {
            <div class="capacity-warning">
              <p>Event is at capacity. New registrations will be queued.</p>
              @if (queueStatus()!.estimatedClearTimeMinutes) {
                <p class="wait-time">Estimated wait time: {{ queueStatus()!.estimatedClearTimeMinutes }} minutes</p>
              }
            </div>
          }
        </div>
      }

      <form [formGroup]="registrationForm" (ngSubmit)="onSubmit()" class="registration-form">
        <!-- Priority Selection -->
        <div class="form-group">
          <label for="priority">Registration Priority</label>
          <select id="priority" formControlName="priority" class="form-control">
            <option [value]="RegistrationPriority.Normal">Normal</option>
            <option [value]="RegistrationPriority.High">High Priority</option>
            <option [value]="RegistrationPriority.VIP">VIP</option>
          </select>
          <small class="form-text">Higher priority registrations are processed first when spots become available</small>
        </div>

        <!-- Notes -->
        <div class="form-group">
          <label for="notes">Special Notes (Optional)</label>
          <textarea 
            id="notes" 
            formControlName="notes" 
            class="form-control" 
            rows="3" 
            placeholder="Any special requirements or notes...">
          </textarea>
        </div>

        <!-- Custom Registration Data -->
        <div class="form-group">
          <label>Additional Information</label>
          <div class="custom-fields" formArrayName="customFields">
            @for (field of customFieldsArray.controls; track $index) {
              <div class="custom-field" [formGroupName]="$index">
                <input 
                  type="text" 
                  formControlName="key" 
                  placeholder="Field name (e.g., Dietary Requirements)"
                  class="form-control field-key">
                <input 
                  type="text" 
                  formControlName="value" 
                  placeholder="Value"
                  class="form-control field-value">
                <button 
                  type="button" 
                  (click)="removeCustomField($index)"
                  class="btn btn-remove"
                  [disabled]="customFieldsArray.length <= 1">
                  Remove
                </button>
              </div>
            }
          </div>
          <button 
            type="button" 
            (click)="addCustomField()"
            class="btn btn-add-field">
            Add Field
          </button>
        </div>

        <!-- Form Actions -->
        <div class="form-actions">
          <button 
            type="button" 
            (click)="goBack()"
            class="btn btn-secondary">
            Cancel
          </button>
          <button 
            type="submit" 
            class="btn btn-primary"
            [disabled]="registrationForm.invalid || isSubmitting()">
            @if (isSubmitting()) {
              <span class="spinner"></span>
              Submitting...
            } @else {
              Register for Event
            }
          </button>
        </div>
      </form>

      <!-- Error Display -->
      @if (error()) {
        <div class="error-card">
          <h3>Registration Error</h3>
          <p>{{ error() }}</p>
          <button (click)="clearError()" class="btn btn-secondary">Dismiss</button>
        </div>
      }

      <!-- Success Display -->
      @if (registrationResult()) {
        <div class="success-card">
          <h3>Registration Successful!</h3>
          <p>Your registration has been {{ registrationResult()!.status.toLowerCase() }}.</p>
          @if (registrationResult()!.queuePosition) {
            <p class="queue-info">
              You are currently #{{ registrationResult()!.queuePosition }} in the queue.
              @if (registrationResult()!.estimatedWaitTimeMinutes) {
                <br>Estimated wait time: {{ registrationResult()!.estimatedWaitTimeMinutes }} minutes.
              }
            </p>
          }
          @if (registrationResult()!.confirmationUrl) {
            <p>Please check your email for confirmation details.</p>
          }
          <div class="success-actions">
            <button (click)="viewRegistration()" class="btn btn-primary">View Registration</button>
            <button (click)="backToEvents()" class="btn btn-secondary">Back to Events</button>
          </div>
        </div>
      }
    </div>
  `,
  styles: [`
    .registration-form-container {
      max-width: 800px;
      margin: 0 auto;
      padding: 2rem;
    }

    .form-header {
      margin-bottom: 2rem;
    }

    .form-header h1 {
      color: #2c3e50;
      margin-bottom: 0.5rem;
    }

    .connection-status {
      display: flex;
      align-items: center;
      gap: 0.5rem;
      margin-bottom: 1rem;
      padding: 0.5rem;
      border-radius: 0.25rem;
      font-size: 0.875rem;
      transition: all 0.3s ease;
    }

    .connection-status.connected {
      background-color: #d4edda;
      color: #155724;
    }

    .connection-status.disconnected {
      background-color: #fff3cd;
      color: #856404;
    }

    .status-indicator {
      width: 8px;
      height: 8px;
      border-radius: 50%;
      display: inline-block;
    }

    .status-indicator.connected {
      background-color: #28a745;
      animation: pulse 2s infinite;
    }

    .status-indicator.disconnected {
      background-color: #ffc107;
    }

    .status-text {
      font-weight: 500;
    }

    .last-update,
    .fallback-info {
      font-size: 0.75rem;
      opacity: 0.8;
    }

    @keyframes pulse {
      0%, 100% { opacity: 1; }
      50% { opacity: 0.5; }
    }

    .event-summary {
      background: #f8f9fa;
      padding: 1.5rem;
      border-radius: 0.5rem;
      border-left: 4px solid #007bff;
    }

    .event-summary h2 {
      margin: 0 0 1rem 0;
      color: #495057;
    }

    .event-info {
      display: flex;
      gap: 1rem;
      margin-bottom: 0.5rem;
      flex-wrap: wrap;
    }

    .event-info span {
      background: white;
      padding: 0.25rem 0.5rem;
      border-radius: 0.25rem;
      font-size: 0.9rem;
    }

    .capacity-info {
      font-weight: 500;
      color: #495057;
    }

    .queue-status-card {
      background: #e3f2fd;
      border: 1px solid #2196f3;
      border-radius: 0.5rem;
      padding: 1.5rem;
      margin-bottom: 2rem;
    }

    .queue-status-card.at-capacity {
      background: #fff3cd;
      border-color: #ffc107;
    }

    .queue-status-card h3 {
      margin: 0 0 1rem 0;
      color: #1976d2;
    }

    .queue-status-card.at-capacity h3 {
      color: #856404;
    }

    .status-info {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(100px, 1fr));
      gap: 1rem;
      margin-bottom: 1rem;
    }

    .stat {
      text-align: center;
    }

    .stat .label {
      display: block;
      font-size: 0.9rem;
      color: #6c757d;
      margin-bottom: 0.25rem;
    }

    .stat .value {
      display: block;
      font-size: 1.5rem;
      font-weight: bold;
      color: #495057;
    }

    .capacity-warning {
      background: #f8d7da;
      border: 1px solid #f5c6cb;
      padding: 1rem;
      border-radius: 0.25rem;
      color: #721c24;
    }

    .capacity-warning p {
      margin: 0.5rem 0;
    }

    .wait-time {
      font-weight: 500;
    }

    .registration-form {
      background: white;
      padding: 2rem;
      border-radius: 0.5rem;
      border: 1px solid #dee2e6;
      margin-bottom: 2rem;
    }

    .form-group {
      margin-bottom: 1.5rem;
    }

    .form-group label {
      display: block;
      margin-bottom: 0.5rem;
      font-weight: 500;
      color: #495057;
    }

    .form-control {
      width: 100%;
      padding: 0.75rem;
      border: 1px solid #ced4da;
      border-radius: 0.25rem;
      font-size: 1rem;
      transition: border-color 0.15s ease-in-out, box-shadow 0.15s ease-in-out;
    }

    .form-control:focus {
      border-color: #007bff;
      outline: 0;
      box-shadow: 0 0 0 0.2rem rgba(0, 123, 255, 0.25);
    }

    .form-text {
      display: block;
      margin-top: 0.25rem;
      font-size: 0.875rem;
      color: #6c757d;
    }

    .custom-fields {
      margin-bottom: 1rem;
    }

    .custom-field {
      display: grid;
      grid-template-columns: 1fr 1fr auto;
      gap: 0.5rem;
      margin-bottom: 0.5rem;
      align-items: center;
    }

    .field-key,
    .field-value {
      margin: 0;
    }

    .btn {
      padding: 0.75rem 1.5rem;
      border: none;
      border-radius: 0.25rem;
      font-size: 1rem;
      cursor: pointer;
      text-decoration: none;
      display: inline-block;
      text-align: center;
      transition: all 0.15s ease-in-out;
    }

    .btn:disabled {
      opacity: 0.6;
      cursor: not-allowed;
    }

    .btn-primary {
      background-color: #007bff;
      color: white;
    }

    .btn-primary:hover:not(:disabled) {
      background-color: #0056b3;
    }

    .btn-secondary {
      background-color: #6c757d;
      color: white;
    }

    .btn-secondary:hover:not(:disabled) {
      background-color: #545b62;
    }

    .btn-remove {
      background-color: #dc3545;
      color: white;
      padding: 0.5rem 1rem;
    }

    .btn-remove:hover:not(:disabled) {
      background-color: #c82333;
    }

    .btn-add-field {
      background-color: #28a745;
      color: white;
    }

    .btn-add-field:hover {
      background-color: #218838;
    }

    .form-actions {
      display: flex;
      gap: 1rem;
      justify-content: flex-end;
      margin-top: 2rem;
    }

    .error-card,
    .success-card {
      padding: 1.5rem;
      border-radius: 0.5rem;
      margin-bottom: 2rem;
    }

    .error-card {
      background-color: #f8d7da;
      border: 1px solid #f5c6cb;
      color: #721c24;
    }

    .success-card {
      background-color: #d4edda;
      border: 1px solid #c3e6cb;
      color: #155724;
    }

    .error-card h3,
    .success-card h3 {
      margin: 0 0 1rem 0;
    }

    .queue-info {
      font-weight: 500;
      margin: 1rem 0;
    }

    .success-actions {
      display: flex;
      gap: 1rem;
      margin-top: 1rem;
    }

    .spinner {
      display: inline-block;
      width: 1rem;
      height: 1rem;
      border: 2px solid #ffffff;
      border-radius: 50%;
      border-top-color: transparent;
      animation: spin 1s ease-in-out infinite;
      margin-right: 0.5rem;
    }

    @keyframes spin {
      to { transform: rotate(360deg); }
    }

    @media (max-width: 768px) {
      .registration-form-container {
        padding: 1rem;
      }

      .custom-field {
        grid-template-columns: 1fr;
        gap: 0.5rem;
      }

      .form-actions {
        flex-direction: column;
      }

      .event-info {
        flex-direction: column;
        gap: 0.5rem;
      }
    }
  `]
})
export class RegistrationFormComponent implements OnInit, OnDestroy {
  // Destroy subject for cleanup
  private readonly destroy$ = new Subject<void>();
  
  // Injected dependencies
  private readonly http = inject(HttpClient);
  private readonly fb = inject(FormBuilder);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly realTimeService = inject(RealTimeService);
  private readonly notificationService = inject(NotificationService);

  // Signals
  eventId = signal<string>('');
  eventDetails = signal<EventSummary | null>(null);
  queueStatus = signal<QueueStatusResponse | null>(null);
  registrationResult = signal<RegistrationResponse | null>(null);
  isSubmitting = signal(false);
  error = signal<string | null>(null);
  isConnectedToRealTime = signal(false);
  lastQueueUpdate = signal<Date | null>(null);

  // Form
  registrationForm!: FormGroup;
  
  // Expose enum to template
  RegistrationPriority = RegistrationPriority;

  ngOnInit() {
    // Initialize form first
    this.initializeForm();
    
    // Get eventId from route parameter
    this.route.params.subscribe(params => {
      const eventId = params['eventId'];
      if (eventId) {
        this.eventId.set(eventId);
        this.loadEventDetails();
        this.loadQueueStatus();
        this.setupRealTimeUpdates();
      } else {
        this.error.set('Invalid event ID');
        this.router.navigate(['/events']);
      }
    });

    // Monitor real-time connection status
    this.realTimeService.isConnected$
      .pipe(takeUntil(this.destroy$))
      .subscribe(connected => {
        this.isConnectedToRealTime.set(connected);
        if (connected && !this.registrationResult()) {
          // Show connection restored notification
          this.notificationService.showSuccess(
            'Connected',
            'Real-time updates are now active.',
            false
          );
        }
      });
    
    // Periodic queue status refresh (fallback for when SignalR is not available)
    setInterval(() => {
      if (!this.registrationResult() && this.eventId() && !this.isConnectedToRealTime()) {
        this.loadQueueStatus();
      }
    }, 30000);
  }

  ngOnDestroy() {
    this.destroy$.next();
    this.destroy$.complete();
    
    // Clean up event subscriptions
    if (this.eventId()) {
      this.realTimeService.unsubscribeFromEvent(this.eventId()).catch(console.error);
    }
  }

  /**
   * Setup real-time updates for this event
   */
  private async setupRealTimeUpdates() {
    const eventId = this.eventId();
    if (!eventId) return;

    try {
      // Subscribe to event updates
      await this.realTimeService.subscribeToEvent(eventId);
      
      // Get event-specific observables
      const eventObservables = this.realTimeService.getEventObservables(eventId);
      
      // Handle queue status updates
      eventObservables.queueStatus$
        .pipe(takeUntil(this.destroy$))
        .subscribe(update => {
          if (update) {
            this.lastQueueUpdate.set(new Date());
            this.updateQueueStatusFromRealTime(update);
          }
        });

      // Handle registration status updates
      eventObservables.registrationStatus$
        .pipe(takeUntil(this.destroy$))
        .subscribe(update => {
          if (update && this.registrationResult()) {
            this.updateRegistrationStatus(update);
          }
        });

      // Handle capacity updates
      eventObservables.capacityStatus$
        .pipe(takeUntil(this.destroy$))
        .subscribe(update => {
          if (update) {
            this.updateEventCapacity(update);
          }
        });

    } catch (error) {
      console.error('Failed to setup real-time updates:', error);
      // Continue with polling fallback
    }
  }

  /**
   * Update queue status from SignalR
   */
  private updateQueueStatusFromRealTime(update: any) {
    const currentStatus = this.queueStatus();
    
    this.queueStatus.set({
      eventId: update.eventId,
      confirmedCount: update.confirmedCount ?? currentStatus?.confirmedCount ?? 0,
      queuedCount: update.queuedCount ?? currentStatus?.queuedCount ?? 0,
      maxCapacity: update.maxCapacity ?? currentStatus?.maxCapacity,
      availableSpots: update.availableSpots ?? currentStatus?.availableSpots,
      isAtCapacity: update.isAtCapacity ?? currentStatus?.isAtCapacity ?? false,
      processingRate: update.processingRate ?? currentStatus?.processingRate,
      estimatedClearTimeMinutes: update.estimatedClearTimeMinutes ?? currentStatus?.estimatedClearTimeMinutes
    });

    // Show notification for significant queue changes
    if (update.queuePosition !== undefined && this.registrationResult()?.queuePosition !== update.queuePosition) {
      if (update.queuePosition === 0) {
        this.notificationService.showSuccess(
          'Registration Confirmed!',
          'Your registration has been confirmed.',
          true
        );
      } else {
        const waitTime = update.estimatedWaitMinutes 
          ? ` (estimated wait: ${update.estimatedWaitMinutes} minutes)`
          : '';
        
        this.notificationService.showInfo(
          'Queue Update',
          `You are now position #${update.queuePosition} in the queue${waitTime}.`
        );
      }
    }
  }

  /**
   * Update registration status from SignalR
   */
  private updateRegistrationStatus(update: any) {
    const current = this.registrationResult();
    if (!current) return;

    const updated: RegistrationResponse = {
      ...current,
      status: update.status,
      queuePosition: update.queuePosition,
      confirmedAt: update.confirmedAt ?? current.confirmedAt,
      estimatedWaitTimeMinutes: update.estimatedWaitTimeMinutes
    };

    this.registrationResult.set(updated);
  }

  /**
   * Update event capacity from SignalR
   */
  private updateEventCapacity(update: any) {
    const currentEvent = this.eventDetails();
    const currentQueue = this.queueStatus();
    
    if (currentEvent) {
      this.eventDetails.set({
        ...currentEvent,
        currentRegistrations: update.currentRegistrations ?? currentEvent.currentRegistrations
      });
    }

    if (currentQueue) {
      this.queueStatus.set({
        ...currentQueue,
        availableSpots: update.availableSpots,
        isAtCapacity: update.isAtCapacity,
        confirmedCount: update.confirmedCount ?? currentQueue.confirmedCount
      });
    }
  }

  private initializeForm() {
    this.registrationForm = this.fb.group({
      priority: [RegistrationPriority.Normal, Validators.required],
      notes: [''],
      customFields: this.fb.array([
        this.createCustomFieldGroup()
      ])
    });
  }

  private createCustomFieldGroup(): FormGroup {
    return this.fb.group({
      key: [''],
      value: ['']
    });
  }

  get customFieldsArray(): FormArray {
    return this.registrationForm.get('customFields') as FormArray;
  }

  addCustomField() {
    this.customFieldsArray.push(this.createCustomFieldGroup());
  }

  removeCustomField(index: number) {
    if (this.customFieldsArray.length > 1) {
      this.customFieldsArray.removeAt(index);
    }
  }

  private async loadEventDetails() {
    try {
      // Note: This would typically come from a route parameter or service
      // For now, we'll need the event details to be passed or fetched
      const response = await fetch(`${environment.publicApiUrl}/api/events/${this.eventId()}`);
      if (response.ok) {
        const event = await response.json();
        this.eventDetails.set({
          id: event.id,
          slug: event.slug,
          title: event.title || event.name,
          startDateTime: event.startDateTime || event.startDate,
          endDateTime: event.endDateTime || event.endDate,
          location: event.location,
          venue: event.venue || event.address,
          maxCapacity: event.maxCapacity || event.maxAttendees,
          currentRegistrations: event.currentRegistrations || event.currentAttendees
        });
      }
    } catch (error) {
      console.error('Failed to load event details:', error);
      this.error.set('Failed to load event details. Please try again.');
    }
  }

  private async loadQueueStatus() {
    try {
      const response = await fetch(`${environment.publicApiUrl}/api/registration/events/${this.eventId()}/queue-status`);
      if (response.ok) {
        const status = await response.json();
        this.queueStatus.set(status);
      }
    } catch (error) {
      console.error('Failed to load queue status:', error);
      // Non-critical error, don't show to user
    }
  }

  async onSubmit() {
    if (this.registrationForm.invalid || this.isSubmitting()) return;

    this.isSubmitting.set(true);
    this.error.set(null);

    try {
      const formValue = this.registrationForm.value;
      
      // Build registration data from custom fields
      const registrationData: Record<string, any> = {};
      formValue.customFields
        .filter((field: any) => field.key && field.value)
        .forEach((field: any) => {
          registrationData[field.key] = field.value;
        });

      const request: RegisterRequest = {
        eventId: this.eventId(),
        priority: formValue.priority,
        registrationData: Object.keys(registrationData).length > 0 ? registrationData : undefined,
        notes: formValue.notes || undefined
      };

      const response = await this.http.post<RegistrationResponse>(
        `${environment.publicApiUrl}/api/registration`,
        request
      ).toPromise();

      if (response) {
        this.registrationResult.set(response);
        
        // If real-time is connected, request immediate queue status
        if (this.isConnectedToRealTime()) {
          try {
            const queueUpdate = await this.realTimeService.requestQueueStatus(this.eventId());
            if (queueUpdate) {
              this.updateQueueStatusFromRealTime(queueUpdate);
            }
          } catch (error) {
            console.error('Failed to request real-time queue status:', error);
          }
        }
        
        // Fallback: refresh queue status after successful registration
        setTimeout(() => this.loadQueueStatus(), 1000);
        
        // Show success notification
        this.notificationService.showSuccess(
          'Registration Submitted',
          response.status === 'Confirmed' 
            ? 'Your registration has been confirmed!'
            : 'Your registration has been queued. You will be notified when confirmed.',
          true
        );
      }
    } catch (error) {
      console.error('Registration failed:', error);
      if (error instanceof HttpErrorResponse) {
        this.error.set(error.error?.detail || error.message || 'Registration failed. Please try again.');
      } else {
        this.error.set('An unexpected error occurred. Please try again.');
      }
      
      this.notificationService.showError(
        'Registration Failed',
        'Unable to submit your registration. Please try again.'
      );
    } finally {
      this.isSubmitting.set(false);
    }
  }

  formatDate(dateString: string): string {
    const date = new Date(dateString);
    return date.toLocaleDateString('en-US', {
      weekday: 'long',
      year: 'numeric',
      month: 'long',
      day: 'numeric',
      hour: 'numeric',
      minute: '2-digit'
    });
  }

  formatRelativeTime(date: Date): string {
    const now = new Date();
    const diffMs = now.getTime() - date.getTime();
    const diffSeconds = Math.floor(diffMs / 1000);
    const diffMinutes = Math.floor(diffSeconds / 60);
    
    if (diffSeconds < 10) return 'just now';
    if (diffSeconds < 60) return `${diffSeconds}s ago`;
    if (diffMinutes < 60) return `${diffMinutes}m ago`;
    
    return date.toLocaleTimeString('en-US', {
      hour: 'numeric',
      minute: '2-digit'
    });
  }

  clearError() {
    this.error.set(null);
  }

  viewRegistration() {
    if (this.registrationResult()) {
      this.router.navigate(['/registrations', this.registrationResult()!.id]);
    }
  }

  backToEvents() {
    this.router.navigate(['/events']);
  }

  goBack() {
    window.history.back();
  }
}
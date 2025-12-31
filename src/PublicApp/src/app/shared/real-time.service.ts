import { Injectable, OnDestroy } from '@angular/core';
import { Router } from '@angular/router';
import { BehaviorSubject, combineLatest, filter, map, Observable, takeUntil, Subject } from 'rxjs';
import { HubConnectionState } from '@microsoft/signalr';
import { SignalRService, QueueStatusUpdate, RegistrationStatusUpdate, EventCapacityUpdate } from './signalr.service';
import { NotificationService } from './notification.service';

export interface UserAuthenticationState {
  isAuthenticated: boolean;
  userId?: string;
  token?: string;
}

@Injectable({
  providedIn: 'root'
})
export class RealTimeService implements OnDestroy {
  private readonly destroy$ = new Subject<void>();
  private readonly authState = new BehaviorSubject<UserAuthenticationState>({
    isAuthenticated: false
  });

  // Event-specific observables
  private readonly eventSubscriptions = new Map<string, {
    queueStatus$: Observable<QueueStatusUpdate>;
    registrationStatus$: Observable<RegistrationStatusUpdate>;
    capacityStatus$: Observable<EventCapacityUpdate>;
  }>();

  public readonly isConnected$: Observable<boolean>;

  constructor(
    private signalRService: SignalRService,
    private notificationService: NotificationService,
    private router: Router
  ) {
    this.isConnected$ = this.signalRService.connectionState$.pipe(
      map(state => state === HubConnectionState.Connected)
    );
    this.initializeService();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
    this.signalRService.dispose();
  }

  /**
   * Initialize the real-time service
   */
  private initializeService(): void {
    this.setupNotificationHandling();
    this.setupAuthenticationHandling();
    this.attemptAutoConnection();
  }

  /**
   * Setup notification handling from SignalR
   */
  private setupNotificationHandling(): void {
    this.signalRService.notifications$
      .pipe(takeUntil(this.destroy$))
      .subscribe(notification => {
        this.notificationService.addNotification(notification);
      });

    // Handle specific event types with custom UI behavior
    this.signalRService.registrationStatus$
      .pipe(takeUntil(this.destroy$))
      .subscribe(update => {
        if (update.status === 'Confirmed') {
          this.notificationService.showSuccess(
            'Registration Confirmed!',
            `Your registration has been confirmed.`,
            true
          );
        } else if (update.status === 'Cancelled') {
          this.notificationService.showWarning(
            'Registration Cancelled',
            update.message || 'Your registration has been cancelled.',
            true
          );
        }
      });

    this.signalRService.queueStatus$
      .pipe(takeUntil(this.destroy$))
      .subscribe(update => {
        const waitTime = update.estimatedWaitMinutes 
          ? ` (estimated wait: ${update.estimatedWaitMinutes} minutes)`
          : '';
        
        this.notificationService.showInfo(
          'Queue Position Update',
          `You are now position #${update.queuePosition} in the queue${waitTime}.`
        );
      });

    this.signalRService.eventCapacity$
      .pipe(takeUntil(this.destroy$))
      .subscribe(update => {
        if (update.isAtCapacity) {
          this.notificationService.showWarning(
            'Event at Capacity',
            'This event is now at full capacity. New registrations will be queued.'
          );
        } else if (update.availableSpots <= 5 && update.availableSpots > 0) {
          this.notificationService.showInfo(
            'Limited Spots Available',
            `Only ${update.availableSpots} spots remaining for this event.`
          );
        }
      });
  }

  /**
   * Setup authentication state handling
   */
  private setupAuthenticationHandling(): void {
    combineLatest([
      this.authState,
      this.isConnected$
    ]).pipe(
      takeUntil(this.destroy$),
      filter(([auth, connected]) => auth.isAuthenticated && connected)
    ).subscribe(([auth]) => {
      if (auth.userId) {
        this.signalRService.authenticate(auth.userId).catch(error => {
          console.error('Failed to authenticate with SignalR:', error);
        });
      }
    });
  }

  /**
   * Attempt auto connection on service initialization
   */
  private attemptAutoConnection(): void {
    // Check if we have stored authentication info
    const storedAuth = this.getStoredAuthInfo();
    if (storedAuth.isAuthenticated) {
      this.setAuthenticationState(storedAuth);
      this.connect().catch(error => {
        console.error('Auto-connection failed:', error);
      });
    }
  }

  /**
   * Set user authentication state
   */
  public setAuthenticationState(authState: UserAuthenticationState): void {
    this.authState.next(authState);
    
    if (authState.isAuthenticated) {
      this.storeAuthInfo(authState);
    } else {
      this.clearStoredAuthInfo();
    }
  }

  /**
   * Connect to SignalR
   */
  public async connect(): Promise<void> {
    try {
      await this.signalRService.ensureConnection();
    } catch (error) {
      this.notificationService.showError(
        'Connection Failed',
        'Failed to connect to real-time updates. Some features may not work properly.'
      );
      throw error;
    }
  }

  /**
   * Disconnect from SignalR
   */
  public async disconnect(): Promise<void> {
    await this.signalRService.disconnect();
    this.setAuthenticationState({ isAuthenticated: false });
  }

  /**
   * Subscribe to updates for a specific event
   */
  public async subscribeToEvent(eventId: string): Promise<void> {
    await this.signalRService.subscribeToEvent(eventId);
    
    // Create event-specific observables
    this.eventSubscriptions.set(eventId, {
      queueStatus$: this.signalRService.queueStatus$.pipe(
        filter(update => update.eventId === eventId)
      ),
      registrationStatus$: this.signalRService.registrationStatus$.pipe(
        filter(update => update.eventId === eventId)
      ),
      capacityStatus$: this.signalRService.eventCapacity$.pipe(
        filter(update => update.eventId === eventId)
      )
    });
  }

  /**
   * Unsubscribe from event updates
   */
  public async unsubscribeFromEvent(eventId: string): Promise<void> {
    await this.signalRService.unsubscribeFromEvent(eventId);
    this.eventSubscriptions.delete(eventId);
  }

  /**
   * Get observables for a specific event
   */
  public getEventObservables(eventId: string) {
    return this.eventSubscriptions.get(eventId) || {
      queueStatus$: new BehaviorSubject<QueueStatusUpdate | null>(null).asObservable(),
      registrationStatus$: new BehaviorSubject<RegistrationStatusUpdate | null>(null).asObservable(),
      capacityStatus$: new BehaviorSubject<EventCapacityUpdate | null>(null).asObservable()
    };
  }

  /**
   * Request current queue status for an event
   */
  public async requestQueueStatus(eventId: string): Promise<QueueStatusUpdate | null> {
    try {
      return await this.signalRService.requestQueueStatus(eventId);
    } catch (error) {
      console.error('Failed to request queue status:', error);
      return null;
    }
  }

  /**
   * Check if connected to real-time updates
   */
  public isConnected(): boolean {
    return this.signalRService.isConnected();
  }

  /**
   * Get current authentication state
   */
  public getAuthenticationState(): Observable<UserAuthenticationState> {
    return this.authState.asObservable();
  }

  /**
   * Handle user login
   */
  public async onUserLogin(userId: string, token: string): Promise<void> {
    this.setAuthenticationState({
      isAuthenticated: true,
      userId,
      token
    });

    if (!this.isConnected()) {
      await this.connect();
    } else {
      await this.signalRService.authenticate(userId);
    }
  }

  /**
   * Handle user logout
   */
  public async onUserLogout(): Promise<void> {
    this.setAuthenticationState({ isAuthenticated: false });
    await this.disconnect();
  }

  /**
   * Navigate to event page and subscribe to updates
   */
  public async navigateToEventWithUpdates(eventId: string): Promise<void> {
    await this.router.navigate(['/events', eventId]);
    
    // Auto-subscribe to event updates if connected
    if (this.isConnected()) {
      try {
        await this.subscribeToEvent(eventId);
      } catch (error) {
        console.error('Failed to subscribe to event updates:', error);
      }
    }
  }

  /**
   * Get stored authentication info from localStorage
   */
  private getStoredAuthInfo(): UserAuthenticationState {
    try {
      const stored = localStorage.getItem('auth_info');
      if (stored) {
        const auth = JSON.parse(stored);
        return {
          isAuthenticated: !!auth.token,
          userId: auth.userId,
          token: auth.token
        };
      }
    } catch (error) {
      console.error('Failed to parse stored auth info:', error);
    }
    
    return { isAuthenticated: false };
  }

  /**
   * Store authentication info
   */
  private storeAuthInfo(authState: UserAuthenticationState): void {
    try {
      localStorage.setItem('auth_info', JSON.stringify({
        userId: authState.userId,
        token: authState.token
      }));
    } catch (error) {
      console.error('Failed to store auth info:', error);
    }
  }

  /**
   * Clear stored authentication info
   */
  private clearStoredAuthInfo(): void {
    localStorage.removeItem('auth_info');
    localStorage.removeItem('auth_token');
    sessionStorage.removeItem('auth_token');
  }

  /**
   * Clean up event subscriptions for route changes
   */
  public cleanupEventSubscriptions(): void {
    const subscribedEvents = this.signalRService.getSubscribedEvents();
    subscribedEvents.forEach(eventId => {
      this.signalRService.unsubscribeFromEvent(eventId).catch(error => {
        console.error(`Failed to unsubscribe from event ${eventId}:`, error);
      });
    });
    this.eventSubscriptions.clear();
  }
}
import { Injectable, OnDestroy } from '@angular/core';
import { BehaviorSubject, Observable, Subject } from 'rxjs';
import * as signalR from '@microsoft/signalr';
import { environment } from '../../environments/environment';

export interface QueueStatusUpdate {
  eventId: string;
  queuePosition: number;
  estimatedWaitMinutes?: number;
}

export interface RegistrationStatusUpdate {
  registrationId: string;
  eventId: string;
  status: string;
  message: string;
  queuePosition?: number;
}

export interface EventCapacityUpdate {
  eventId: string;
  availableSpots: number;
  totalCapacity?: number;
  isAtCapacity: boolean;
}

export interface NotificationMessage {
  type: 'info' | 'success' | 'warning' | 'error';
  title: string;
  message: string;
  timestamp: Date;
  persistent?: boolean;
}

@Injectable({
  providedIn: 'root'
})
export class SignalRService implements OnDestroy {
  private hubConnection?: signalR.HubConnection;
  private readonly baseUrl = environment.publicApiUrl || 'https://localhost:7001';

  // Connection state
  private readonly connectionStateSubject = new BehaviorSubject<signalR.HubConnectionState>(
    signalR.HubConnectionState.Disconnected
  );
  public readonly connectionState$ = this.connectionStateSubject.asObservable();

  // Event streams
  private readonly queueStatusSubject = new Subject<QueueStatusUpdate>();
  private readonly registrationStatusSubject = new Subject<RegistrationStatusUpdate>();
  private readonly eventCapacitySubject = new Subject<EventCapacityUpdate>();
  private readonly notificationSubject = new Subject<NotificationMessage>();

  // Public observables
  public readonly queueStatus$ = this.queueStatusSubject.asObservable();
  public readonly registrationStatus$ = this.registrationStatusSubject.asObservable();
  public readonly eventCapacity$ = this.eventCapacitySubject.asObservable();
  public readonly notifications$ = this.notificationSubject.asObservable();

  // Current subscriptions
  private subscribedEvents = new Set<string>();
  private authenticatedUserId?: string;

  constructor() {
    this.initializeConnection();
  }

  ngOnDestroy(): void {
    this.disconnect();
  }

  /**
   * Initialize the SignalR connection
   */
  private initializeConnection(): void {
    this.hubConnection = new signalR.HubConnectionBuilder()
      .withUrl(`${this.baseUrl}/hubs/registration`, {
        accessTokenFactory: () => this.getAccessToken(),
        withCredentials: true
      })
      .withAutomaticReconnect([0, 2000, 10000, 30000]) // Reconnect delays in milliseconds
      .configureLogging(environment.production ? signalR.LogLevel.Warning : signalR.LogLevel.Information)
      .build();

    this.setupEventHandlers();
    this.setupConnectionStateHandling();
  }

  /**
   * Setup SignalR event handlers
   */
  private setupEventHandlers(): void {
    if (!this.hubConnection) return;

    // Queue status updates
    this.hubConnection.on('QueueStatusUpdate', (data: QueueStatusUpdate) => {
      this.queueStatusSubject.next({
        ...data,
        eventId: data.eventId.toString()
      });
    });

    // Registration status updates
    this.hubConnection.on('RegistrationStatusUpdate', (data: RegistrationStatusUpdate) => {
      this.registrationStatusSubject.next({
        ...data,
        registrationId: data.registrationId.toString(),
        eventId: data.eventId.toString()
      });
    });

    // Event capacity updates
    this.hubConnection.on('EventCapacityUpdate', (data: EventCapacityUpdate) => {
      this.eventCapacitySubject.next({
        ...data,
        eventId: data.eventId.toString()
      });
    });

    // General notifications
    this.hubConnection.on('NotificationReceived', (data: any) => {
      this.notificationSubject.next({
        type: data.type || 'info',
        title: data.title || 'Notification',
        message: data.message,
        timestamp: new Date(data.timestamp || Date.now()),
        persistent: data.persistent || false
      });
    });

    // Registration confirmed from queue
    this.hubConnection.on('RegistrationConfirmedFromQueue', (data: any) => {
      this.notificationSubject.next({
        type: 'success',
        title: 'Registration Confirmed!',
        message: `Your registration for "${data.eventTitle}" has been confirmed from the queue.`,
        timestamp: new Date(),
        persistent: true
      });
      
      this.registrationStatusSubject.next({
        registrationId: data.registrationId.toString(),
        eventId: data.eventId.toString(),
        status: 'Confirmed',
        message: 'Registration confirmed from queue'
      });
    });

    // Registration cancelled
    this.hubConnection.on('RegistrationCancelled', (data: any) => {
      this.notificationSubject.next({
        type: 'warning',
        title: 'Registration Cancelled',
        message: `Your registration for "${data.eventTitle}" has been cancelled. ${data.reason || ''}`,
        timestamp: new Date(),
        persistent: true
      });
    });
  }

  /**
   * Setup connection state handling
   */
  private setupConnectionStateHandling(): void {
    if (!this.hubConnection) return;

    this.hubConnection.onclose(() => {
      this.connectionStateSubject.next(signalR.HubConnectionState.Disconnected);
      this.authenticatedUserId = undefined;
    });

    this.hubConnection.onreconnecting(() => {
      this.connectionStateSubject.next(signalR.HubConnectionState.Reconnecting);
    });

    this.hubConnection.onreconnected(() => {
      this.connectionStateSubject.next(signalR.HubConnectionState.Connected);
      this.resubscribeToEvents();
    });
  }

  /**
   * Connect to the SignalR hub
   */
  public async connect(): Promise<void> {
    if (!this.hubConnection || this.hubConnection.state === signalR.HubConnectionState.Connected) {
      return;
    }

    try {
      this.connectionStateSubject.next(signalR.HubConnectionState.Connecting);
      await this.hubConnection.start();
      this.connectionStateSubject.next(signalR.HubConnectionState.Connected);
      console.log('SignalR Connected');
    } catch (error) {
      console.error('SignalR Connection Error:', error);
      this.connectionStateSubject.next(signalR.HubConnectionState.Disconnected);
      throw error;
    }
  }

  /**
   * Disconnect from the SignalR hub
   */
  public async disconnect(): Promise<void> {
    if (!this.hubConnection) return;

    try {
      await this.hubConnection.stop();
      this.subscribedEvents.clear();
      this.authenticatedUserId = undefined;
    } catch (error) {
      console.error('SignalR Disconnect Error:', error);
    }
  }

  /**
   * Subscribe to updates for a specific event
   */
  public async subscribeToEvent(eventId: string): Promise<void> {
    if (!this.hubConnection || this.hubConnection.state !== signalR.HubConnectionState.Connected) {
      throw new Error('SignalR connection not established');
    }

    if (this.subscribedEvents.has(eventId)) {
      return; // Already subscribed
    }

    try {
      await this.hubConnection.invoke('JoinEventGroup', eventId);
      this.subscribedEvents.add(eventId);
      console.log(`Subscribed to event updates: ${eventId}`);
    } catch (error) {
      console.error(`Failed to subscribe to event ${eventId}:`, error);
      throw error;
    }
  }

  /**
   * Unsubscribe from updates for a specific event
   */
  public async unsubscribeFromEvent(eventId: string): Promise<void> {
    if (!this.hubConnection || this.hubConnection.state !== signalR.HubConnectionState.Connected) {
      return;
    }

    if (!this.subscribedEvents.has(eventId)) {
      return; // Not subscribed
    }

    try {
      await this.hubConnection.invoke('LeaveEventGroup', eventId);
      this.subscribedEvents.delete(eventId);
      console.log(`Unsubscribed from event updates: ${eventId}`);
    } catch (error) {
      console.error(`Failed to unsubscribe from event ${eventId}:`, error);
    }
  }

  /**
   * Request current queue status for an event
   */
  public async requestQueueStatus(eventId: string): Promise<QueueStatusUpdate | null> {
    if (!this.hubConnection || this.hubConnection.state !== signalR.HubConnectionState.Connected) {
      throw new Error('SignalR connection not established');
    }

    try {
      const result = await this.hubConnection.invoke('RequestQueueStatus', eventId);
      return result ? {
        eventId: eventId,
        queuePosition: result.queuePosition,
        estimatedWaitMinutes: result.estimatedWaitMinutes
      } : null;
    } catch (error) {
      console.error(`Failed to request queue status for event ${eventId}:`, error);
      throw error;
    }
  }

  /**
   * Authenticate with the hub using current user credentials
   */
  public async authenticate(userId: string): Promise<void> {
    if (!this.hubConnection || this.hubConnection.state !== signalR.HubConnectionState.Connected) {
      throw new Error('SignalR connection not established');
    }

    if (this.authenticatedUserId === userId) {
      return; // Already authenticated with this user
    }

    try {
      await this.hubConnection.invoke('AuthenticateUser', userId);
      this.authenticatedUserId = userId;
      console.log(`Authenticated with user: ${userId}`);
    } catch (error) {
      console.error(`Failed to authenticate user ${userId}:`, error);
      throw error;
    }
  }

  /**
   * Send a notification to other users (if authorized)
   */
  public async sendNotification(eventId: string, message: string, type: 'info' | 'success' | 'warning' | 'error' = 'info'): Promise<void> {
    if (!this.hubConnection || this.hubConnection.state !== signalR.HubConnectionState.Connected) {
      throw new Error('SignalR connection not established');
    }

    try {
      await this.hubConnection.invoke('SendNotificationToEvent', eventId, {
        type,
        message,
        timestamp: new Date().toISOString()
      });
    } catch (error) {
      console.error('Failed to send notification:', error);
      throw error;
    }
  }

  /**
   * Get current connection state
   */
  public getConnectionState(): signalR.HubConnectionState {
    return this.hubConnection?.state || signalR.HubConnectionState.Disconnected;
  }

  /**
   * Check if connected to SignalR hub
   */
  public isConnected(): boolean {
    return this.hubConnection?.state === signalR.HubConnectionState.Connected;
  }

  /**
   * Get list of subscribed events
   */
  public getSubscribedEvents(): string[] {
    return Array.from(this.subscribedEvents);
  }

  /**
   * Resubscribe to all events after reconnection
   */
  private async resubscribeToEvents(): Promise<void> {
    const events = Array.from(this.subscribedEvents);
    this.subscribedEvents.clear();

    for (const eventId of events) {
      try {
        await this.subscribeToEvent(eventId);
      } catch (error) {
        console.error(`Failed to resubscribe to event ${eventId}:`, error);
      }
    }

    // Reauthenticate if we were previously authenticated
    if (this.authenticatedUserId) {
      try {
        await this.authenticate(this.authenticatedUserId);
      } catch (error) {
        console.error('Failed to reauthenticate:', error);
        this.authenticatedUserId = undefined;
      }
    }
  }

  /**
   * Get access token for SignalR authentication
   */
  private getAccessToken(): string {
    // Try to get token from localStorage, sessionStorage, or wherever you store it
    const token = localStorage.getItem('auth_token') || 
                 sessionStorage.getItem('auth_token');
    
    if (!token) {
      console.warn('No authentication token found for SignalR connection');
      return '';
    }

    return token;
  }

  /**
   * Handle connection errors with retry logic
   */
  public async ensureConnection(): Promise<void> {
    if (this.isConnected()) {
      return;
    }

    const maxRetries = 3;
    let retryCount = 0;

    while (retryCount < maxRetries && !this.isConnected()) {
      try {
        await this.connect();
        break;
      } catch (error) {
        retryCount++;
        console.error(`Connection attempt ${retryCount} failed:`, error);
        
        if (retryCount < maxRetries) {
          const delay = Math.pow(2, retryCount) * 1000; // Exponential backoff
          await new Promise(resolve => setTimeout(resolve, delay));
        } else {
          throw new Error('Failed to establish SignalR connection after multiple attempts');
        }
      }
    }
  }

  /**
   * Clean up resources and event subscriptions
   */
  public dispose(): void {
    this.queueStatusSubject.complete();
    this.registrationStatusSubject.complete();
    this.eventCapacitySubject.complete();
    this.notificationSubject.complete();
    this.connectionStateSubject.complete();
    this.disconnect();
  }
}
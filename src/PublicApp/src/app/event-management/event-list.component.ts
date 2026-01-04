import { Component, OnInit, OnDestroy, inject, signal } from '@angular/core';

import { HttpClient } from '@angular/common/http';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { Subject, takeUntil } from 'rxjs';
import { environment } from '../../environments/environment';
import { RealTimeService } from '../shared/real-time.service';

export interface Event {
  id: string;
  slug: string;
  name: string;
  description: string;
  startDateTime: string;
  endDateTime: string;
  location: string;
  venue: string;
  maxCapacity?: number;
  currentRegistrations: number;
  status: string;
  visibility: string;
  customFields: Record<string, any>;
  tags: string[];
  createdAt: string;
  updatedAt: string;
  creator: {
    id: string;
    name: string;
    email: string;
  };
}

export interface EventSearchResponse {
  events: Event[];
  totalCount: number;
  currentPage: number;
  totalPages: number;
}

@Component({
  selector: 'app-event-list',
  standalone: true,
  imports: [FormsModule],
  template: `
    <div class="event-list-container">
      <header class="search-header">
        <h1>Discover Events</h1>
    
        <!-- Search Controls -->
        <div class="search-controls">
          <div class="search-bar">
            <input
              type="text"
              [(ngModel)]="searchQuery"
              (input)="searchEvents()"
              placeholder="Search events by name, description, or location..."
              class="search-input">
              <button (click)="searchEvents()" class="search-button">Search</button>
            </div>
    
            <div class="filters">
              <select [(ngModel)]="selectedTag" (change)="searchEvents()" class="filter-select">
                <option value="">All Categories</option>
                @for (tag of availableTags; track tag) {
                  <option [value]="tag">{{ tag }}</option>
                }
              </select>
    
              <select [(ngModel)]="locationFilter" (change)="searchEvents()" class="filter-select">
                <option value="">All Locations</option>
                @for (location of availableLocations; track location) {
                  <option [value]="location">{{ location }}</option>
                }
              </select>
    
              <select [(ngModel)]="sortBy" (change)="searchEvents()" class="filter-select">
                <option value="startDateTime">Date (Earliest First)</option>
                <option value="-startDateTime">Date (Latest First)</option>
                <option value="name">Name (A-Z)</option>
                <option value="-name">Name (Z-A)</option>
                <option value="-createdAt">Newest First</option>
              </select>
            </div>
          </div>
        </header>
    
        <!-- Loading State -->
        @if (loading()) {
          <div class="loading-state">
            <div class="spinner"></div>
            <p>Loading events...</p>
          </div>
        }
    
        <!-- Error State -->
        @if (error()) {
          <div class="error-state">
            <p>{{ error() }}</p>
            <button (click)="searchEvents()" class="retry-button">Try Again</button>
          </div>
        }
    
        <!-- Events Grid -->
        @if (!loading() && !error()) {
          <div class="events-container">
            @if (events().length === 0) {
              <div class="no-events">
                <h2>No events found</h2>
                <p>Try adjusting your search criteria or check back later for new events.</p>
              </div>
            }
            <div class="events-grid">
              @for (event of events(); track event) {
                <div class="event-card" (click)="viewEventDetails(event)">
                  <div class="event-card-header">
                    <h3 class="event-title">{{ event.name }}</h3>
                    <div class="event-status" [class]="'status-' + event.status.toLowerCase()">
                      {{ event.status }}
                    </div>
                  </div>
                  <div class="event-datetime">
                    <i class="icon-calendar"></i>
                    <span>{{ formatDateTime(event.startDateTime) }}</span>
                  </div>
                  <div class="event-location">
                    <i class="icon-location"></i>
                    <span>{{ event.location }}{{ event.venue ? ' - ' + event.venue : '' }}</span>
                  </div>
                  <div class="event-description">
                    {{ truncateDescription(event.description) }}
                  </div>
                  @if (event.tags.length > 0) {
                    <div class="event-tags">
                      @for (tag of event.tags; track tag) {
                        <span class="tag">{{ tag }}</span>
                      }
                    </div>
                  }
                  <div class="event-footer">
                    @if (event.maxCapacity) {
                      <div class="event-capacity">
                        <i class="icon-users"></i>
                        <span>{{ event.currentRegistrations }} / {{ event.maxCapacity }}</span>
                        <div class="capacity-bar">
                          <div class="capacity-fill" [style.width.%]="getCapacityPercentage(event)"></div>
                        </div>
                      </div>
                    }
                    <div class="event-actions">
                      <button
                        class="register-button"
                        [disabled]="!canRegister(event)"
                        (click)="registerForEvent(event, $event)">
                        {{ getRegistrationButtonText(event) }}
                      </button>
                    </div>
                  </div>
                </div>
              }
            </div>
            <!-- Pagination -->
            @if (totalPages() > 1) {
              <div class="pagination">
                <button
                  (click)="goToPage(currentPage() - 1)"
                  [disabled]="currentPage() <= 1"
                  class="page-button">
                  Previous
                </button>
                <span class="page-info">
                  Page {{ currentPage() }} of {{ totalPages() }}
                </span>
                <button
                  (click)="goToPage(currentPage() + 1)"
                  [disabled]="currentPage() >= totalPages()"
                  class="page-button">
                  Next
                </button>
              </div>
            }
          </div>
        }
      </div>
    `,
  styles: [`
    .event-list-container {
      max-width: 1200px;
      margin: 0 auto;
      padding: 1rem;
    }

    .search-header {
      margin-bottom: 2rem;
    }

    .search-header h1 {
      color: #2c3e50;
      margin-bottom: 1.5rem;
    }

    .search-controls {
      display: flex;
      flex-direction: column;
      gap: 1rem;
    }

    .search-bar {
      display: flex;
      gap: 0.5rem;
    }

    .search-input {
      flex: 1;
      padding: 0.75rem;
      border: 2px solid #e2e8f0;
      border-radius: 0.5rem;
      font-size: 1rem;
    }

    .search-input:focus {
      outline: none;
      border-color: #3b82f6;
    }

    .search-button {
      padding: 0.75rem 1.5rem;
      background: #3b82f6;
      color: white;
      border: none;
      border-radius: 0.5rem;
      cursor: pointer;
      font-weight: 500;
    }

    .search-button:hover {
      background: #2563eb;
    }

    .filters {
      display: flex;
      gap: 1rem;
      flex-wrap: wrap;
    }

    .filter-select {
      padding: 0.5rem;
      border: 2px solid #e2e8f0;
      border-radius: 0.375rem;
      background: white;
      font-size: 0.875rem;
    }

    .filter-select:focus {
      outline: none;
      border-color: #3b82f6;
    }

    .loading-state {
      display: flex;
      flex-direction: column;
      align-items: center;
      justify-content: center;
      padding: 3rem;
    }

    .spinner {
      width: 3rem;
      height: 3rem;
      border: 3px solid #e2e8f0;
      border-top: 3px solid #3b82f6;
      border-radius: 50%;
      animation: spin 1s linear infinite;
      margin-bottom: 1rem;
    }

    @keyframes spin {
      0% { transform: rotate(0deg); }
      100% { transform: rotate(360deg); }
    }

    .error-state {
      text-align: center;
      padding: 3rem;
      color: #dc2626;
    }

    .retry-button {
      padding: 0.75rem 1.5rem;
      background: #dc2626;
      color: white;
      border: none;
      border-radius: 0.5rem;
      cursor: pointer;
      margin-top: 1rem;
    }

    .no-events {
      text-align: center;
      padding: 3rem;
      color: #64748b;
    }

    .events-grid {
      display: grid;
      grid-template-columns: repeat(auto-fill, minmax(350px, 1fr));
      gap: 1.5rem;
      margin-bottom: 2rem;
    }

    .event-card {
      background: white;
      border: 2px solid #e2e8f0;
      border-radius: 0.75rem;
      padding: 1.5rem;
      cursor: pointer;
      transition: all 0.2s;
    }

    .event-card:hover {
      border-color: #3b82f6;
      transform: translateY(-2px);
      box-shadow: 0 4px 12px rgba(0, 0, 0, 0.1);
    }

    .event-card-header {
      display: flex;
      justify-content: space-between;
      align-items: flex-start;
      margin-bottom: 1rem;
    }

    .event-title {
      font-size: 1.25rem;
      font-weight: 600;
      color: #1f2937;
      margin: 0;
      flex: 1;
      margin-right: 1rem;
    }

    .event-status {
      padding: 0.25rem 0.75rem;
      border-radius: 9999px;
      font-size: 0.75rem;
      font-weight: 500;
      text-transform: uppercase;
    }

    .status-active {
      background: #dcfce7;
      color: #16a34a;
    }

    .status-draft {
      background: #f3f4f6;
      color: #6b7280;
    }

    .status-cancelled {
      background: #fef2f2;
      color: #dc2626;
    }

    .event-datetime, .event-location {
      display: flex;
      align-items: center;
      gap: 0.5rem;
      margin-bottom: 0.75rem;
      font-size: 0.875rem;
      color: #64748b;
    }

    .event-description {
      color: #4b5563;
      line-height: 1.5;
      margin-bottom: 1rem;
    }

    .event-tags {
      display: flex;
      flex-wrap: wrap;
      gap: 0.5rem;
      margin-bottom: 1rem;
    }

    .tag {
      padding: 0.25rem 0.5rem;
      background: #f1f5f9;
      color: #475569;
      border-radius: 0.375rem;
      font-size: 0.75rem;
      font-weight: 500;
    }

    .event-footer {
      border-top: 1px solid #e2e8f0;
      padding-top: 1rem;
    }

    .event-capacity {
      margin-bottom: 1rem;
    }

    .event-capacity > div:first-child {
      display: flex;
      align-items: center;
      gap: 0.5rem;
      font-size: 0.875rem;
      color: #64748b;
      margin-bottom: 0.5rem;
    }

    .capacity-bar {
      height: 0.5rem;
      background: #f1f5f9;
      border-radius: 0.25rem;
      overflow: hidden;
    }

    .capacity-fill {
      height: 100%;
      background: #3b82f6;
      transition: width 0.3s;
    }

    .event-actions {
      display: flex;
      justify-content: flex-end;
    }

    .register-button {
      padding: 0.75rem 1.5rem;
      background: #10b981;
      color: white;
      border: none;
      border-radius: 0.5rem;
      cursor: pointer;
      font-weight: 500;
      transition: background 0.2s;
    }

    .register-button:hover:not(:disabled) {
      background: #059669;
    }

    .register-button:disabled {
      background: #9ca3af;
      cursor: not-allowed;
    }

    .pagination {
      display: flex;
      justify-content: center;
      align-items: center;
      gap: 1rem;
      padding: 2rem 0;
    }

    .page-button {
      padding: 0.75rem 1.5rem;
      background: #3b82f6;
      color: white;
      border: none;
      border-radius: 0.5rem;
      cursor: pointer;
    }

    .page-button:hover:not(:disabled) {
      background: #2563eb;
    }

    .page-button:disabled {
      background: #9ca3af;
      cursor: not-allowed;
    }

    .page-info {
      color: #64748b;
      font-weight: 500;
    }

    /* Icons (using simple text for now, replace with icon font/library) */
    .icon-calendar::before { content: "📅 "; }
    .icon-location::before { content: "📍 "; }
    .icon-users::before { content: "👥 "; }

    @media (max-width: 768px) {
      .search-controls {
        gap: 0.75rem;
      }

      .filters {
        flex-direction: column;
      }

      .filter-select {
        width: 100%;
      }

      .events-grid {
        grid-template-columns: 1fr;
      }

      .event-card-header {
        flex-direction: column;
        align-items: flex-start;
        gap: 0.5rem;
      }

      .event-title {
        margin-right: 0;
      }

      .pagination {
        flex-direction: column;
        gap: 0.5rem;
      }
    }
  `]
})
export class EventListComponent implements OnInit, OnDestroy {
  private readonly destroy$ = new Subject<void>();
  
  private http = inject(HttpClient);
  private router = inject(Router);
  private realTimeService = inject(RealTimeService);

  // Reactive signals
  events = signal<Event[]>([]);
  loading = signal(false);
  error = signal<string | null>(null);
  totalPages = signal(0);
  currentPage = signal(1);
  totalCount = signal(0);
  isConnectedToRealTime = signal(false);

  // Track subscribed events for cleanup
  private subscribedEventIds = new Set<string>();

  // Search and filter properties
  searchQuery = '';
  selectedTag = '';
  locationFilter = '';
  sortBy = 'startDateTime';
  pageSize = 12;

  // Available filter options
  availableTags: string[] = [];
  availableLocations: string[] = [];

  private readonly apiUrl = `${environment.publicApiUrl}/api/events`;

  ngOnInit() {
    this.loadInitialData();
    this.setupRealTimeConnection();
  }

  ngOnDestroy() {
    this.destroy$.next();
    this.destroy$.complete();
    this.cleanupEventSubscriptions();
  }

  private setupRealTimeConnection() {
    // Monitor real-time connection status
    this.realTimeService.isConnected$
      .pipe(takeUntil(this.destroy$))
      .subscribe(connected => {
        this.isConnectedToRealTime.set(connected);
      });
  }

  /**
   * Subscribe to real-time updates for visible events
   */
  private async subscribeToVisibleEvents() {
    const currentEvents = this.events();
    
    for (const event of currentEvents) {
      const eventId = event.id;
      if (!this.subscribedEventIds.has(eventId)) {
        try {
          await this.realTimeService.subscribeToEvent(eventId);
          this.subscribedEventIds.add(eventId);
          
          // Get event-specific observables
          const eventObservables = this.realTimeService.getEventObservables(eventId);
          
          // Handle capacity updates for this event
          eventObservables.capacityStatus$
            .pipe(takeUntil(this.destroy$))
            .subscribe(update => {
              if (update && update.eventId === eventId) {
                this.updateEventCapacity(eventId, update);
              }
            });
            
        } catch (error) {
          console.error(`Failed to subscribe to event ${eventId}:`, error);
        }
      }
    }
  }

  /**
   * Update event capacity from real-time updates
   */
  private updateEventCapacity(eventId: string, update: any) {
    const currentEvents = this.events();
    const eventIndex = currentEvents.findIndex(e => e.id === eventId);
    
    if (eventIndex !== -1) {
      const updatedEvents = [...currentEvents];
      updatedEvents[eventIndex] = {
        ...updatedEvents[eventIndex],
        currentRegistrations: update.currentRegistrations ?? updatedEvents[eventIndex].currentRegistrations
      };
      
      this.events.set(updatedEvents);
    }
  }

  /**
   * Clean up event subscriptions
   */
  private cleanupEventSubscriptions() {
    this.subscribedEventIds.forEach(eventId => {
      this.realTimeService.unsubscribeFromEvent(eventId).catch(console.error);
    });
    this.subscribedEventIds.clear();
  }

  private async loadInitialData() {
    await this.searchEvents();
    await this.loadFilterOptions();
  }

  async searchEvents() {
    this.loading.set(true);
    this.error.set(null);

    // Clean up current subscriptions before loading new events
    this.cleanupEventSubscriptions();

    try {
      const params = new URLSearchParams({
        page: this.currentPage().toString(),
        pageSize: this.pageSize.toString(),
        sortBy: this.sortBy
      });

      if (this.searchQuery.trim()) {
        params.append('query', this.searchQuery.trim());
      }

      if (this.selectedTag) {
        params.append('tag', this.selectedTag);
      }

      if (this.locationFilter) {
        params.append('location', this.locationFilter);
      }

      const response = await this.http.get<EventSearchResponse>(
        `${this.apiUrl}/search?${params.toString()}`
      ).toPromise();

      if (response) {
        this.events.set(response.events);
        this.totalPages.set(response.totalPages);
        this.totalCount.set(response.totalCount);
        
        // Subscribe to real-time updates for the new events
        if (this.isConnectedToRealTime()) {
          await this.subscribeToVisibleEvents();
        }
      }
    } catch (error) {
      console.error('Failed to search events:', error);
      this.error.set('Failed to load events. Please try again later.');
    } finally {
      this.loading.set(false);
    }
  }

  private async loadFilterOptions() {
    try {
      // Load available tags
      const tagsResponse = await this.http.get<string[]>(
        `${this.apiUrl}/tags`
      ).toPromise();
      
      if (tagsResponse) {
        this.availableTags = tagsResponse;
      }

      // Load available locations
      const locationsResponse = await this.http.get<string[]>(
        `${this.apiUrl}/locations`
      ).toPromise();
      
      if (locationsResponse) {
        this.availableLocations = locationsResponse;
      }
    } catch (error) {
      console.error('Failed to load filter options:', error);
    }
  }

  goToPage(page: number) {
    if (page >= 1 && page <= this.totalPages()) {
      this.currentPage.set(page);
      this.searchEvents();
    }
  }

  viewEventDetails(event: Event) {
    this.router.navigate(['/events', event.slug]);
  }

  async registerForEvent(event: Event, clickEvent: MouseEvent) {
    clickEvent.stopPropagation(); // Prevent card click

    if (!this.canRegister(event)) {
      return;
    }

    try {
      // Navigate to registration form
      this.router.navigate(['/register', event.id]);
    } catch (error) {
      console.error('Failed to navigate to registration:', error);
      alert('Failed to open registration form. Please try again.');
    }
  }

  canRegister(event: Event): boolean {
    return event.status === 'Active' && 
           (!event.maxCapacity || event.currentRegistrations < event.maxCapacity);
  }

  getRegistrationButtonText(event: Event): string {
    if (event.status !== 'Active') {
      return 'Unavailable';
    }

    if (event.maxCapacity && event.currentRegistrations >= event.maxCapacity) {
      return 'Full';
    }

    return 'Register';
  }

  getCapacityPercentage(event: Event): number {
    if (!event.maxCapacity) return 0;
    return (event.currentRegistrations / event.maxCapacity) * 100;
  }

  formatDateTime(dateTimeString: string): string {
    const date = new Date(dateTimeString);
    return date.toLocaleDateString('en-US', {
      weekday: 'short',
      year: 'numeric',
      month: 'short',
      day: 'numeric',
      hour: 'numeric',
      minute: '2-digit',
      hour12: true
    });
  }

  truncateDescription(description: string, maxLength: number = 150): string {
    if (description.length <= maxLength) {
      return description;
    }
    return description.substring(0, maxLength).trim() + '...';
  }
}
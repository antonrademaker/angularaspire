import { Component, OnInit, OnDestroy, signal, computed, PLATFORM_ID, inject } from '@angular/core';
import { CommonModule, isPlatformBrowser } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { interval, Subject } from 'rxjs';
import { takeUntil, startWith, switchMap, catchError } from 'rxjs/operators';
import { of } from 'rxjs';

interface HealthCheck {
  name: string;
  status: string;
  duration: number;
  description?: string;
  exception?: string;
  data?: Record<string, unknown>;
}

interface HealthReport {
  status: string;
  totalDuration: number;
  timestamp: string;
  checks: HealthCheck[];
}

@Component({
  selector: 'app-health',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="health-page">
      <header class="health-header">
        <h1>System Health</h1>
        <div class="refresh-info">
          Auto-refresh every 30 seconds
          <button class="refresh-btn" (click)="refresh()" [disabled]="loading()">
            {{ loading() ? 'Checking...' : 'Refresh Now' }}
          </button>
        </div>
      </header>

      @if (error()) {
        <div class="error-banner">
          <span class="error-icon">⚠️</span>
          <div class="error-content">
            <strong>Connection Error</strong>
            <p>{{ error() }}</p>
          </div>
        </div>
      }

      <div class="overall-status" [class]="overallStatusClass()">
        <div class="status-icon">
          @switch (healthReport()?.status) {
            @case ('Healthy') { ✅ }
            @case ('Degraded') { ⚠️ }
            @case ('Unhealthy') { ❌ }
            @default { ⏳ }
          }
        </div>
        <div class="status-info">
          <span class="status-label">Overall Status</span>
          <span class="status-value">{{ healthReport()?.status || 'Checking...' }}</span>
          @if (healthReport()?.totalDuration) {
            <span class="status-duration">Response time: {{ healthReport()?.totalDuration | number:'1.0-0' }}ms</span>
          }
        </div>
        @if (healthReport()?.timestamp) {
          <div class="last-check">
            Last checked: {{ healthReport()?.timestamp | date:'medium' }}
          </div>
        }
      </div>

      <section class="checks-section">
        <h2>Health Checks</h2>
        
        @if (loading() && !healthReport()) {
          <div class="loading">
            <div class="spinner"></div>
            <p>Running health checks...</p>
          </div>
        }

        @if (healthReport()?.checks?.length) {
          <div class="checks-grid">
            @for (check of healthReport()?.checks; track check.name) {
              <div class="check-card" [class]="getCheckStatusClass(check.status)">
                <div class="check-header">
                  <span class="check-icon">
                    @switch (check.status) {
                      @case ('Healthy') { ✅ }
                      @case ('Degraded') { ⚠️ }
                      @case ('Unhealthy') { ❌ }
                      @default { ❓ }
                    }
                  </span>
                  <span class="check-name">{{ formatCheckName(check.name) }}</span>
                  <span class="check-status">{{ check.status }}</span>
                </div>
                <div class="check-details">
                  <div class="check-duration">
                    <span class="label">Duration:</span>
                    <span class="value">{{ check.duration | number:'1.0-2' }}ms</span>
                  </div>
                  @if (check.description) {
                    <div class="check-description">
                      <span class="label">Details:</span>
                      <span class="value">{{ check.description }}</span>
                    </div>
                  }
                  @if (check.exception) {
                    <div class="check-exception">
                      <span class="label">Error:</span>
                      <span class="value">{{ check.exception }}</span>
                    </div>
                  }
                </div>
              </div>
            }
          </div>
        }

        @if (!loading() && !healthReport()?.checks?.length && !error()) {
          <div class="no-checks">
            <p>No health checks configured</p>
          </div>
        }
      </section>

      <section class="api-endpoints">
        <h2>API Endpoints</h2>
        <div class="endpoint-list">
          <div class="endpoint">
            <code>/health</code>
            <span class="endpoint-desc">Basic health check (returns Healthy/Unhealthy)</span>
          </div>
          <div class="endpoint">
            <code>/alive</code>
            <span class="endpoint-desc">Liveness probe for orchestrators</span>
          </div>
          <div class="endpoint">
            <code>/api/health/detailed</code>
            <span class="endpoint-desc">Detailed health report (JSON)</span>
          </div>
        </div>
      </section>
    </div>
  `,
  styles: [`
    .health-page {
      max-width: 1200px;
      margin: 0 auto;
      padding: 24px;
      font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
    }

    .health-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-bottom: 24px;
    }

    .health-header h1 {
      margin: 0;
      font-size: 28px;
      color: #1a1a2e;
    }

    .refresh-info {
      display: flex;
      align-items: center;
      gap: 12px;
      color: #666;
      font-size: 14px;
    }

    .refresh-btn {
      padding: 8px 16px;
      background: #3b82f6;
      color: white;
      border: none;
      border-radius: 6px;
      cursor: pointer;
      font-size: 14px;
      transition: background 0.2s;
    }

    .refresh-btn:hover:not(:disabled) {
      background: #2563eb;
    }

    .refresh-btn:disabled {
      background: #94a3b8;
      cursor: not-allowed;
    }

    .error-banner {
      display: flex;
      align-items: flex-start;
      gap: 12px;
      padding: 16px;
      background: #fef2f2;
      border: 1px solid #fecaca;
      border-radius: 8px;
      margin-bottom: 24px;
    }

    .error-icon {
      font-size: 24px;
    }

    .error-content strong {
      color: #dc2626;
      display: block;
      margin-bottom: 4px;
    }

    .error-content p {
      margin: 0;
      color: #7f1d1d;
    }

    .overall-status {
      display: flex;
      align-items: center;
      gap: 20px;
      padding: 24px;
      border-radius: 12px;
      margin-bottom: 32px;
      background: #f8fafc;
      border: 2px solid #e2e8f0;
    }

    .overall-status.healthy {
      background: #f0fdf4;
      border-color: #86efac;
    }

    .overall-status.degraded {
      background: #fffbeb;
      border-color: #fcd34d;
    }

    .overall-status.unhealthy {
      background: #fef2f2;
      border-color: #fca5a5;
    }

    .status-icon {
      font-size: 48px;
    }

    .status-info {
      flex: 1;
    }

    .status-label {
      display: block;
      font-size: 14px;
      color: #64748b;
      text-transform: uppercase;
      letter-spacing: 0.05em;
    }

    .status-value {
      display: block;
      font-size: 28px;
      font-weight: 600;
      color: #1e293b;
    }

    .status-duration {
      display: block;
      font-size: 14px;
      color: #64748b;
      margin-top: 4px;
    }

    .last-check {
      font-size: 13px;
      color: #64748b;
      text-align: right;
    }

    .checks-section h2,
    .api-endpoints h2 {
      font-size: 20px;
      color: #1e293b;
      margin-bottom: 16px;
    }

    .loading {
      display: flex;
      flex-direction: column;
      align-items: center;
      padding: 48px;
      color: #64748b;
    }

    .spinner {
      width: 40px;
      height: 40px;
      border: 3px solid #e2e8f0;
      border-top-color: #3b82f6;
      border-radius: 50%;
      animation: spin 1s linear infinite;
      margin-bottom: 16px;
    }

    @keyframes spin {
      to { transform: rotate(360deg); }
    }

    .checks-grid {
      display: grid;
      grid-template-columns: repeat(auto-fill, minmax(350px, 1fr));
      gap: 16px;
      margin-bottom: 32px;
    }

    .check-card {
      background: white;
      border: 1px solid #e2e8f0;
      border-radius: 8px;
      overflow: hidden;
    }

    .check-card.healthy {
      border-left: 4px solid #22c55e;
    }

    .check-card.degraded {
      border-left: 4px solid #f59e0b;
    }

    .check-card.unhealthy {
      border-left: 4px solid #ef4444;
    }

    .check-header {
      display: flex;
      align-items: center;
      gap: 8px;
      padding: 12px 16px;
      background: #f8fafc;
      border-bottom: 1px solid #e2e8f0;
    }

    .check-icon {
      font-size: 18px;
    }

    .check-name {
      flex: 1;
      font-weight: 600;
      color: #1e293b;
    }

    .check-status {
      font-size: 12px;
      padding: 2px 8px;
      border-radius: 4px;
      background: #e2e8f0;
      color: #475569;
    }

    .check-details {
      padding: 12px 16px;
    }

    .check-duration,
    .check-description,
    .check-exception {
      display: flex;
      gap: 8px;
      margin-bottom: 8px;
      font-size: 14px;
    }

    .check-details .label {
      color: #64748b;
      min-width: 70px;
    }

    .check-details .value {
      color: #1e293b;
    }

    .check-exception .value {
      color: #dc2626;
    }

    .no-checks {
      padding: 48px;
      text-align: center;
      color: #64748b;
      background: #f8fafc;
      border-radius: 8px;
    }

    .api-endpoints {
      margin-top: 32px;
    }

    .endpoint-list {
      background: #1e293b;
      border-radius: 8px;
      overflow: hidden;
    }

    .endpoint {
      display: flex;
      align-items: center;
      padding: 12px 16px;
      border-bottom: 1px solid #334155;
    }

    .endpoint:last-child {
      border-bottom: none;
    }

    .endpoint code {
      font-family: 'Fira Code', monospace;
      background: #334155;
      padding: 4px 12px;
      border-radius: 4px;
      color: #22d3ee;
      min-width: 200px;
    }

    .endpoint-desc {
      color: #94a3b8;
      margin-left: 16px;
      font-size: 14px;
    }
  `]
})
export class HealthComponent implements OnInit, OnDestroy {
  private destroy$ = new Subject<void>();
  private refreshTrigger$ = new Subject<void>();
  private platformId = inject(PLATFORM_ID);

  healthReport = signal<HealthReport | null>(null);
  loading = signal(false);
  error = signal<string | null>(null);

  overallStatusClass = computed(() => {
    const status = this.healthReport()?.status?.toLowerCase();
    return status || 'checking';
  });

  constructor(private http: HttpClient) {}

  ngOnInit(): void {
    // Only run in browser, not during SSR/prerender
    if (!isPlatformBrowser(this.platformId)) {
      return;
    }

    // Auto-refresh every 30 seconds
    interval(30000)
      .pipe(
        startWith(0),
        takeUntil(this.destroy$)
      )
      .subscribe(() => this.fetchHealth());

    // Also respond to manual refresh triggers
    this.refreshTrigger$
      .pipe(takeUntil(this.destroy$))
      .subscribe(() => this.fetchHealth());
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  refresh(): void {
    this.refreshTrigger$.next();
  }

  private fetchHealth(): void {
    this.loading.set(true);
    this.error.set(null);

    this.http.get<HealthReport>('/api/health/detailed')
      .pipe(
        catchError(err => {
          if (err.status === 0) {
            this.error.set('Cannot connect to API. Make sure the PrivateApi is running.');
          } else if (err.status === 503) {
            // Service unavailable but we got a response - parse it
            return of(err.error as HealthReport);
          } else {
            this.error.set(`API error: ${err.status} ${err.statusText}`);
          }
          return of(null);
        })
      )
      .subscribe(report => {
        this.loading.set(false);
        if (report) {
          this.healthReport.set(report);
        }
      });
  }

  formatCheckName(name: string): string {
    // Convert camelCase or PascalCase to Title Case with spaces
    return name
      .replace(/([a-z])([A-Z])/g, '$1 $2')
      .replace(/([A-Z])([A-Z][a-z])/g, '$1 $2')
      .replace(/_/g, ' ')
      .replace(/\b\w/g, c => c.toUpperCase());
  }

  getCheckStatusClass(status: string): string {
    return status?.toLowerCase() || 'unknown';
  }
}

/**
 * OpenTelemetry configuration for browser
 * This provides distributed tracing for the Angular browser application
 */
import { WebTracerProvider } from '@opentelemetry/sdk-trace-web';
import { BatchSpanProcessor } from '@opentelemetry/sdk-trace-base';
import { OTLPTraceExporter } from '@opentelemetry/exporter-trace-otlp-http';
import { ZoneContextManager } from '@opentelemetry/context-zone';
import { registerInstrumentations } from '@opentelemetry/instrumentation';
import { FetchInstrumentation } from '@opentelemetry/instrumentation-fetch';
import { DocumentLoadInstrumentation } from '@opentelemetry/instrumentation-document-load';
import { Resource } from '@opentelemetry/resources';
import { ATTR_SERVICE_NAME } from '@opentelemetry/semantic-conventions';

/**
 * Initialize browser telemetry
 * Call this function early in main.ts
 */
export function initBrowserTelemetry(): void {
  // For browser, we use HTTP endpoint (typically on port 4318)
  // Aspire dashboard exposes OTLP HTTP on a different port than gRPC
  const otlpEndpoint = (window as any).__OTEL_EXPORTER_OTLP_ENDPOINT || 'http://localhost:4318/v1/traces';

  const resource = new Resource({
    [ATTR_SERVICE_NAME]: 'publicapp-browser',
  });

  const provider = new WebTracerProvider({
    resource: resource,
    spanProcessors: [
      new BatchSpanProcessor(
        new OTLPTraceExporter({
          url: otlpEndpoint,
        })
      ),
    ],
  });

  provider.register({
    contextManager: new ZoneContextManager(),
  });

  registerInstrumentations({
    instrumentations: [
      new FetchInstrumentation({
        // Propagate trace context to backend APIs
        propagateTraceHeaderCorsUrls: [
          /localhost/i,
          /127\.0\.0\.1/i,
          /api\//i,
        ],
        clearTimingResources: true,
      }),
      new DocumentLoadInstrumentation(),
    ],
  });

  console.log('✅ OpenTelemetry (Browser) configured for Aspire dashboard:', otlpEndpoint);
}

/**
 * OpenTelemetry configuration for SSR server (Node.js)
 * This must be imported first in server.ts to initialize before any other imports
 */
import { NodeSDK } from '@opentelemetry/sdk-node';
import { getNodeAutoInstrumentations } from '@opentelemetry/auto-instrumentations-node';
import { OTLPTraceExporter } from '@opentelemetry/exporter-trace-otlp-grpc';
import { OTLPMetricExporter } from '@opentelemetry/exporter-metrics-otlp-grpc';
import { PeriodicExportingMetricReader } from '@opentelemetry/sdk-metrics';
import { Resource } from '@opentelemetry/resources';
import { ATTR_SERVICE_NAME } from '@opentelemetry/semantic-conventions';

// Configure the OTLP endpoint - Aspire sets this environment variable
const otlpEndpoint = process.env['OTEL_EXPORTER_OTLP_ENDPOINT'] || 'http://localhost:4317';

// Create resource with service name
const resource = new Resource({
  [ATTR_SERVICE_NAME]: 'privateapp-ssr',
});

// Create metric reader
const metricReader = new PeriodicExportingMetricReader({
  exporter: new OTLPMetricExporter({
    url: otlpEndpoint,
  }),
  exportIntervalMillis: 5000,
});

const sdk = new NodeSDK({
  resource: resource,
  traceExporter: new OTLPTraceExporter({
    url: otlpEndpoint,
  }),
  metricReader: metricReader as any, // Type assertion needed due to SDK version mismatch
  instrumentations: [
    getNodeAutoInstrumentations({
      // Disable file system instrumentation to reduce noise
      '@opentelemetry/instrumentation-fs': {
        enabled: false,
      },
    }),
  ],
});

sdk.start();

console.log('✅ OpenTelemetry (SSR) configured for Aspire dashboard:', otlpEndpoint);

// Graceful shutdown
process.on('SIGTERM', () => {
  sdk
    .shutdown()
    .then(() => console.log('OpenTelemetry tracing terminated'))
    .catch((error) => console.log('Error terminating OpenTelemetry tracing', error))
    .finally(() => process.exit(0));
});

export { sdk };

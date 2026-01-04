# Research: Event Management System Architecture

**Date**: 2025-12-31  
**Feature**: Event Management System  
**Phase**: 0 - Architecture and Technology Research  
**Updated**: Aligned with clarified specification decisions

## Clarified Architectural Decisions

### Authentication Strategy: OAuth 2.0 with JWT Tokens
**Decision**: Implement OAuth 2.0 with JWT tokens for user authentication  
**Rationale**: Industry standard security, supports social logins, integrates well with external APIs and SignalR  
**Implementation**: ASP.NET Core Identity with JWT Bearer tokens, Angular JWT interceptors, refresh token rotation

### Event Capacity Management: Queue-Based Processing
**Decision**: Queue-based registration processing with real-time SignalR notifications  
**Rationale**: Prevents overselling, provides fairness during high demand, maintains good UX with immediate feedback  
**Implementation**: Redis-backed queue system, SignalR hubs for real-time updates, email confirmation backup

### Data Storage: PostgreSQL with JSON Columns  
**Decision**: PostgreSQL as primary database with JSON columns for custom event fields  
**Rationale**: ACID compliance for critical data, NoSQL flexibility for evolving requirements, excellent .NET integration  
**Implementation**: Entity Framework Core, JsonDocument properties, indexed JSON queries for performance

### Real-time Communication: SignalR for Key Events
**Decision**: SignalR messaging for registration updates, event changes, and session notifications  
**Rationale**: Immediate user feedback enhances UX, reduces perceived latency, supports offline-first design  
**Implementation**: SignalR hubs with group management, connection state management, graceful degradation to email

### API Rate Limiting: Tiered by Integration Type
**Decision**: Tiered rate limits based on API key types for external integrations  
**Rationale**: Flexible for different partner needs while protecting system resources  
**Implementation**: ASP.NET Core rate limiting middleware, Redis-backed counters, different limits per partner tier

## Angular Development Servers with Aspire: Best Practices Research

### Executive Summary

This research covers best practices for setting up two Angular development servers as Aspire JavaScript applications with proxy-based service discovery. The approach maximizes developer experience (DX) with Hot Module Replacement (HMR) while maintaining production deployment flexibility.

### 1. Angular Dev Servers as Aspire JavaScript Apps

#### Configuration Approach

Aspire 13.0+ provides robust JavaScript hosting capabilities through the `Aspire.Hosting.JavaScript` package. Angular applications can be registered as JavaScript resources using the `AddJavaScriptApp` or `AddNodeApp` methods.

**AppHost Configuration (Program.cs):**

```csharp
using Aspire.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

// .NET API Services
var apiService = builder.AddProject<Projects.EventManagement_Api>("eventapi")
    .WithHttpsHealthCheck("/health");

var identityService = builder.AddProject<Projects.EventManagement_Identity>("identityapi")  
    .WithHttpsHealthCheck("/health");

// Angular Applications as JavaScript Resources
var adminApp = builder.AddJavaScriptApp("admin-app", "../EventManagement.Admin", "dev")
    .WithNpm()
    .WithHttpEndpoint(port: 4200, env: "PORT")
    .WithEnvironment("NODE_ENV", "development")
    .WithReference(apiService)
    .WithReference(identityService);

var clientApp = builder.AddJavaScriptApp("client-app", "../EventManagement.Client", "dev")
    .WithNpm()  
    .WithHttpEndpoint(port: 4201, env: "PORT")
    .WithEnvironment("NODE_ENV", "development")
    .WithReference(apiService)
    .WithReference(identityService);

builder.Build().Run();
```

#### Package.json Scripts Configuration

Both Angular apps need properly configured scripts for Aspire integration:

```json
{
  "scripts": {
    "dev": "ng serve --host 0.0.0.0 --port $PORT --proxy-config proxy.conf.js",
    "build": "ng build",
    "build:prod": "ng build --configuration production",
    "start": "ng serve"
  }
}
```

**Key Configuration Points:**
- Use `--host 0.0.0.0` to bind to all interfaces for container/external access
- Use `$PORT` environment variable for dynamic port allocation
- Reference `proxy.conf.js` for dynamic proxy configuration

### 2. Angular Proxy Configuration with Aspire Environment Variables

#### Dynamic Proxy Configuration (proxy.conf.js)

The proxy configuration must read Aspire-injected environment variables to avoid hardcoded URLs:

```javascript
// proxy.conf.js
const { env } = require('process');

// Helper function to extract service URL from Aspire environment variables
function getServiceUrl(serviceName, scheme = 'https') {
  // Aspire injects variables like: EVENTAPI_HTTPS=https://localhost:7001
  const envKey = `${serviceName.toUpperCase()}_${scheme.toUpperCase()}`;
  const url = env[envKey];
  
  if (!url) {
    console.warn(`Service URL not found for ${envKey}, falling back to localhost`);
    return scheme === 'https' ? 'https://localhost:7001' : 'http://localhost:5001';
  }
  
  return url;
}

// Configuration object
const PROXY_CONFIG = {
  "/api/events/*": {
    "target": getServiceUrl('eventapi'),
    "secure": false, // Set to false for development self-signed certs
    "changeOrigin": true,
    "logLevel": "debug",
    "onError": function (err, req, res) {
      console.log('Proxy error:', err);
    }
  },
  "/api/identity/*": {
    "target": getServiceUrl('identityapi'),
    "secure": false,
    "changeOrigin": true,
    "logLevel": "debug"
  },
  "/api/health": {
    "target": getServiceUrl('eventapi'),
    "secure": false,
    "changeOrigin": true
  }
};

module.exports = PROXY_CONFIG;
```

#### Environment Variable Patterns

Aspire injects environment variables following these patterns:

```
# Service Discovery Format
SERVICENAME_SCHEME=https://host:port
services__servicename__scheme__0=https://host:port

# Examples for our services:
EVENTAPI_HTTPS=https://localhost:7001
IDENTITYAPI_HTTPS=https://localhost:7002
services__eventapi__https__0=https://localhost:7001
services__identityapi__https__0=https://localhost:7002
```

### 3. Production vs Development Deployment

#### Development Configuration

```csharp
// AppHost/Program.cs - Development
#if DEBUG
// Development: Independent Angular dev servers with HMR
var adminApp = builder.AddJavaScriptApp("admin-app", "../EventManagement.Admin", "dev")
    .WithNpm()
    .WithHttpEndpoint(port: 4200, env: "PORT")
    .WithReference(apiService);
#else  
// Production: Static files served by API
var adminStaticFiles = builder.AddProject<Projects.EventManagement_Api>("eventapi")
    .WithEnvironment("SERVE_ANGULAR", "true")
    .WithEnvironment("ANGULAR_DIST_PATH", "../EventManagement.Admin/dist");
#endif
```

#### Production API Configuration

Configure the .NET API to serve Angular static files in production:

```csharp
// EventManagement.Api/Program.cs
var builder = WebApplication.CreateBuilder(args);

// ... other service registrations

var app = builder.Build();

// In production, serve Angular static files
if (app.Environment.IsProduction() || 
    builder.Configuration.GetValue<bool>("SERVE_ANGULAR"))
{
    var angularDistPath = builder.Configuration["ANGULAR_DIST_PATH"] ?? "wwwroot";
    
    app.UseDefaultFiles(new DefaultFilesOptions
    {
        FileProvider = new PhysicalFileProvider(
            Path.Combine(app.Environment.ContentRootPath, angularDistPath))
    });
    
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new PhysicalFileProvider(
            Path.Combine(app.Environment.ContentRootPath, angularDistPath))
    });
    
    // Fallback routing for SPA
    app.MapFallbackToFile("index.html", new StaticFileOptions
    {
        FileProvider = new PhysicalFileProvider(
            Path.Combine(app.Environment.ContentRootPath, angularDistPath))
    });
}

app.Run();
```

### 4. NSwag SDK Generation Best Practices

#### NSwag Configuration File (nswag.json)

```json
{
  "$schema": "http://json.schemastore.org/nswag",
  "runtime": "Net80",
  "defaultVariables": null,
  "documentGenerator": {
    "fromDocument": {
      "json": null,
      "url": "https://localhost:7001/swagger/v1/swagger.json",
      "output": null,
      "newLineBehavior": "Auto"
    }
  },
  "codeGenerators": {
    "openApiToTypeScriptClient": {
      "className": "{controller}Client",
      "moduleName": "",
      "namespace": "",
      "typeScriptVersion": 4.0,
      "template": "Angular",
      "promiseType": "Promise",
      "httpClass": "HttpClient",
      "injectionTokenType": "InjectionToken",
      "rxJsVersion": 7.0,
      "dateTimeType": "Date",
      "nullValue": "Null",
      "generateOptionalParameters": true,
      "generateJsonMethods": false,
      "enforceFlagEnums": false,
      "handleReferences": false,
      "generateConstructorInterface": true,
      "convertConstructorInterfaceData": false,
      "importRequiredTypes": true,
      "useGetBaseUrlMethod": false,
      "baseUrlTokenName": "API_BASE_URL",
      "queryNullValue": "",
      "useAbortSignal": false,
      "inlineNamedDictionaries": false,
      "inlineNamedAny": false,
      "includeHttpContext": false,
      "templateDirectory": null,
      "typeNameGeneratorType": null,
      "propertyNameGeneratorType": null,
      "enumNameGeneratorType": null,
      "serviceHost": null,
      "serviceSchemes": null,
      "output": "src/app/core/api/api-client.service.ts",
      "newLineBehavior": "Auto"
    }
  }
}
```

#### Build Script Integration

Create a script to regenerate API clients:

```json
{
  "scripts": {
    "generate-api": "npm run generate-api:events && npm run generate-api:identity",
    "generate-api:events": "nswag run nswag-events.json",
    "generate-api:identity": "nswag run nswag-identity.json",
    "dev": "npm run generate-api && ng serve --proxy-config proxy.conf.js",
    "build": "npm run generate-api && ng build"
  }
}
```

### 5. Aspire JavaScript Resource Configuration

#### Complete Resource Builder Configuration

```csharp
// AppHost configuration with all JavaScript options
var adminApp = builder.AddJavaScriptApp("admin-app", "../EventManagement.Admin", "dev")
    .WithNpm(installPackages: true, "install", "--silent")
    .WithBuildScript("build:dev", "--source-map")
    .WithRunScript("dev", "--hmr", "--live-reload", "--open=false")
    .WithHttpEndpoint(port: 4200, env: "PORT", name: "https")
    .WithEnvironment("NODE_ENV", "development")
    .WithEnvironment("NG_CLI_ANALYTICS", "false")
    .WithEnvironment("BROWSER", "none") 
    .WithEnvironment("FORCE_COLOR", "1") // Enable colored output
    .WithEnvironment("CI", "false") // Disable CI mode
    .WithReference(apiService, "eventapi")
    .WithReference(identityService, "identityapi") 
    .WaitFor(apiService)
    .WaitFor(identityService);
```

### Architecture Recommendations

#### 1. Separation of Concerns
- **Development**: Independent Angular dev servers with HMR
- **Production**: Static file serving from .NET API
- **Testing**: Configurable between both modes

#### 2. Environment-Specific Configuration
- Use environment variables for all service URLs
- Implement configuration services for runtime adaptation
- Maintain separate proxy configurations for different environments

#### 3. Build Pipeline Integration
- Integrate NSwag generation into build processes
- Use MSBuild targets for coordinated builds
- Implement proper dependency management

#### 4. Service Discovery Strategy
- Use Aspire's built-in service discovery patterns
- Implement fallback mechanisms for development
- Maintain type safety throughout the chain

#### 5. Developer Experience Optimization
- Preserve HMR capabilities in development
- Minimize configuration duplication
- Provide clear error messages and fallbacks
- Use Aspire dashboard for service monitoring

---

## Technology Stack Validation

### .NET 10 + Aspire 10
**Decision**: Use .NET 10 with Aspire orchestration for backend services
**Rationale**: 
- .NET 10 provides latest performance improvements and enhanced native AOT support
- Aspire 10 offers mature service discovery, configuration, and advanced observability features
- Strong integration with OpenTelemetry and distributed tracing
- Native gRPC support with excellent performance characteristics

**Alternatives considered**: 
- .NET Framework: Rejected due to lack of cloud-native features
- .NET 6.0: Rejected due to missing latest performance improvements

### Angular 21 + TypeScript 5.9
**Decision**: Angular 21 with standalone components and TypeScript 5.9 strict mode
**Rationale**:
- Angular 21 provides latest performance optimizations and cutting-edge developer experience
- TypeScript 5.9 offers improved type inference, better performance, and enhanced IntelliSense
- Most advanced standalone component architecture aligns perfectly with IDesign service boundaries
- Enhanced Vite support for fastest possible development builds
- Latest Material Design 3 (M3) implementation with newest component features

**Alternatives considered**:
- React: Rejected due to team expertise and existing component library
- Vue.js: Rejected due to less mature enterprise tooling
- Built-in SSR support for better SEO and initial load performance
- Strong typing ensures API contract consistency

**Alternatives considered**:
- React: Rejected due to team expertise and existing component library
- Vue.js: Rejected due to less mature enterprise tooling

### SQL Server + Entity Framework Core
**Decision**: SQL Server as primary database with EF Core for data access
**Rationale**:
- Strong ACID guarantees for event registration consistency
- Excellent performance for complex queries (event search, session scheduling)
- Native JSON support for flexible event metadata
- EF Core provides excellent migrations and query optimization

**Alternatives considered**:
- PostgreSQL: Considered but SQL Server preferred for team familiarity
- NoSQL (Cosmos DB): Rejected due to complex relational requirements

## IDesign Business Service Architecture

### Service Boundary Analysis
**EventManagement Service**:
- Complete event lifecycle: create, update, publish, archive, search
- Owns event metadata, templates, and configuration
- Volatile: Event requirements change frequently

**Registration Service**:
- Complete registration workflow: subscribe, confirm, cancel, waitlist
- Owns registration data and capacity management
- Volatile: Registration rules vary by event type

**SessionManagement Service**:
- Complete session lifecycle: schedule, assign speakers, track attendance
- Owns session data, tracks, and scheduling logic
- Volatile: Session formats evolve (virtual, hybrid, multi-track)

**UserManagement Service**:
- Complete user lifecycle: authentication, profiles, preferences
- Owns user data and role-based permissions
- Stable: User management patterns are well-established

**Notification Service**:
- Complete communication workflow: templates, delivery, tracking
- Owns notification templates and delivery mechanisms
- Stable: Communication patterns are well-established

### Cross-Service Communication Patterns
**Decision**: C# interfaces for gRPC service definitions with automatic .proto generation
**Rationale**:
- C# interfaces provide strong typing and IntelliSense support during development
- Automatic .proto generation ensures consistency between service contracts
- Code-first approach aligns with .NET development practices
- Generated TypeScript models from C# provide type safety across full stack

**Implementation**:
- Use protobuf-net.Grpc or similar tools for automatic .proto generation
- C# service interfaces decorated with [ServiceContract] attributes
- Build pipeline generates .proto files and TypeScript models automatically
- HTTP/REST remains for client-API communication with OpenAPI generation

## Code Generation and Type Safety

### Full-Stack Type Safety Strategy
**Decision**: C# as single source of truth for all data models with TypeScript 5.9 generation
**Implementation**:
- **C# Models**: Define all entities, DTOs, and contracts in C#
- **gRPC Generation**: Automatic .proto file generation from C# interfaces
- **TypeScript 5.9 Generation**: Automatic TypeScript interface generation with enhanced type inference
- **Build Integration**: MSBuild targets generate artifacts during compilation
- **Advanced Validation**: Shared validation attributes with TypeScript 5.9 enhanced error reporting

**Tools and Workflow**:
- **protobuf-net.Grpc**: C# interfaces to .proto generation
- **NSwag**: C# models to TypeScript 5.9 interface generation with improved performance
- **FluentValidation**: Shared validation rules with TypeScript compilation
- **Build Pipeline**: Automated generation with file change detection and TypeScript 5.9 optimizations
- **Type Safety**: End-to-end type safety from SQL Server to Angular UI with enhanced IntelliSense

## Performance and Scalability Research

### Caching Strategy
**Decision**: Multi-layer caching with Redis and in-memory caches
**Implementation**:
- Redis: Session data, event search results, user profiles
- In-memory: Service configuration, lookup data, frequently accessed events
- Distributed cache invalidation using Redis pub/sub

**Rationale**: Event data has varying access patterns - some data is frequently read (event lists) while other data is write-heavy (registrations)

### Database Optimization
**Decision**: Read replicas + optimized indexing strategy
**Implementation**:
- Write operations to primary SQL Server instance
- Read operations (search, event lists) to read replicas
- Specialized indexes for event search and session scheduling
- Partitioning for large events with many sessions

### API Performance Targets
**Confirmed Achievable**:
- <100ms p50, <200ms p95 for all API endpoints
- 1000+ concurrent requests through connection pooling and async patterns
- <3s initial Angular app load through lazy loading and code splitting

## Observability and Monitoring

### OpenTelemetry Integration
**Decision**: Full OpenTelemetry implementation across all services
**Implementation**:
- Distributed tracing: All gRPC calls, HTTP requests, database operations
- Metrics: Business metrics (registrations/second) + infrastructure metrics
- Logging: Structured JSON logging with correlation IDs
- Health checks: Liveness/readiness probes for Kubernetes deployment

**Tools Integration**:
- Prometheus for metrics collection
- Jaeger for distributed tracing
- Grafana for dashboards and alerting
- Application Insights for production monitoring

## Security Architecture

### Authentication and Authorization
**Decision**: JWT-based authentication with role-based authorization
**Implementation**:
- Public API: JWT tokens for authenticated users
- Private API: Additional role-based access control (admin, organizer)
- gRPC services: Service-to-service authentication with certificates
- Angular apps: JWT token management with automatic refresh

### Data Protection
**Decision**: Encryption at rest and in transit
**Implementation**:
- TLS 1.3 for all HTTP/gRPC communication
- SQL Server Transparent Data Encryption (TDE)
- Key management through Azure Key Vault
- GDPR compliance for user data handling

## Testing Strategy

### Angular Testing with Vitest
**Decision**: Use Vitest instead of Jest for Angular unit testing
**Rationale**:
- Native Vite integration provides faster test execution (2-5x speed improvement)
- Better TypeScript support with native ESM handling
- Compatible with Angular's testing utilities (@angular/testing)
- Improved watch mode and hot module replacement during testing
- Better IDE integration and debugging experience

**Implementation**:
- Vitest for unit tests with @angular/testing utilities
- @testing-library/angular for component testing best practices
- MSW (Mock Service Worker) for HTTP mocking
- Playwright for E2E testing with real browser automation

### Multi-Layer Testing Approach
**Unit Testing**:
- .NET: xUnit with Moq for mocking, 90%+ coverage target
- Angular: Vitest with @angular/testing for component testing

**Integration Testing**:
- gRPC service contracts using TestServer
- Database integration tests with real SQL Server instances
- API endpoint testing with WebApplicationFactory

**End-to-End Testing**:
- Playwright for complete user workflows
- Test data management with database seeding
- Cross-browser testing (Chrome, Firefox, Safari)

**Performance Testing**:
- NBomber for API load testing
- Angular performance budgets in build pipeline
- Database query optimization validation

## Risk Mitigation

### High-Risk Areas Identified
1. **Event Capacity Management**: Race conditions during high-demand registration
   - Mitigation: Optimistic concurrency control + queue-based processing
2. **Session Scheduling Conflicts**: Complex business rules for session timing
   - Mitigation: Comprehensive validation layer + conflict detection algorithms
3. **Data Migration**: Evolving event formats requiring schema changes
   - Mitigation: Feature flags + backward compatibility strategies

### Deployment Strategy
**Decision**: Blue-green deployment with feature flags
**Implementation**:
- Aspire orchestration enables easy environment switching
- Feature flags allow gradual rollout of new functionality
- Database migrations with backward compatibility
- Monitoring-driven rollback triggers

## Conclusion

All technical decisions align with constitutional requirements and support the volatility-focused design. The architecture provides strong foundations for:
- Business service boundaries that minimize change impact
- Performance targets that exceed constitutional requirements  
- Comprehensive testing across all layers
- Observability for production support
- Security appropriate for event management domain

**Ready for Phase 1**: Data model design and contract definition
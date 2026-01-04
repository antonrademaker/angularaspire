<!--
Sync Impact Report:
- Version change: 2.7.0 → 2.8.0 (MINOR: Enhanced SDK generation requirements)
- Enhanced principle: VIII. Code Generation and Type Safety - Comprehensive Angular SDK generation with NSwag integration
- New requirements: Complete TypeScript SDK including models, HTTP services, validation, testing utilities, and Observable patterns
- Technical implementation: NSwag SDK generation, Angular service factories, type-safe HTTP clients with dependency injection
- Templates requiring updates: ✅ Plan template updated / ✅ Plan.md constitution check completed
- Follow-up TODOs: None - comprehensive Angular SDK generation fully specified
-->

# AngularAspire Constitution

## Core Principles

### I. Service-First Architecture (NON-NEGOTIABLE)
Every feature starts as a well-defined service contract with clear API boundaries.
All services MUST be:
- Contract-first with C# interfaces as source of truth, generating .proto files via code generation
- Independently deployable and testable
- Versioned with backward compatibility guarantees
- Documented with comprehensive API specifications

**Projects affected**: Public .NET API, Private .NET API, Shared gRPC Services
**Rationale**: C# interfaces provide strong typing and IntelliSense support while automatically generating compatible .proto files, ensuring consistent contracts across all clients and enabling independent service evolution.

### II. API Contract Consistency
All API endpoints MUST maintain strict contract consistency across public and private APIs.
Requirements:
- Shared data models and DTOs across all .NET APIs
- gRPC service definitions maintained in shared libraries
- OpenAPI/Swagger documentation auto-generated and kept current
- Breaking changes require new API versions, never modify existing contracts

**Projects affected**: All APIs and Angular clients
**Rationale**: Prevents integration failures and ensures reliable client-server communication.

### III. Cross-Platform Code Quality Standards
All codebases MUST adhere to unified quality standards regardless of technology stack.
Standards include:
- **C# .NET APIs**: StyleCop analyzers, nullable reference types enabled, ConfigureAwait(false) for async
- **Angular Apps**: ESLint + Prettier, strict TypeScript mode, OnPush change detection
- **Shared Standards**: 90%+ test coverage, zero compiler warnings, documented public APIs
- **Performance**: Response times <200ms p95, bundle sizes <2MB initial load

**Rationale**: Consistent quality across all projects reduces maintenance burden and improves developer experience.

### IV. Comprehensive Testing Strategy
Every feature requires multi-layer testing across the entire solution stack.
Testing MUST cover:
- **Unit Tests**: All business logic in isolation (xUnit for .NET, Vitest for Angular)
- **Integration Tests**: gRPC service communication and API endpoint contracts
- **End-to-End Tests**: Complete user workflows across public and private Angular apps (Playwright)
- **Contract Tests**: API schema validation and backward compatibility verification
- **Modern Testing Tools**: @testing-library/angular for component testing, MSW for HTTP mocking

**Rationale**: Multi-layer testing catches issues at appropriate levels and prevents regressions across service boundaries.

### V. User Experience Consistency (NON-NEGOTIABLE)
Public and private Angular applications MUST maintain consistent UX patterns and performance.
Requirements:
- Shared Angular component library for common UI elements
- Identical authentication and authorization flows
- Consistent error handling and user feedback patterns
- Unified loading states and accessibility standards (WCAG 2.1 AA)
- Performance parity: both apps achieve Lighthouse scores ≥90

**Rationale**: Users switching between public and private interfaces should have predictable, seamless experiences.

### VI. Observability with OpenTelemetry (NON-NEGOTIABLE)
All services and applications MUST implement comprehensive observability using OpenTelemetry standards.
Requirements:
- **Distributed Tracing**: All API calls, gRPC communications, and database operations traced with correlation IDs
- **Metrics Collection**: Performance counters, business metrics, and resource utilization monitoring
- **Structured Logging**: Consistent log formats with contextual information across all services
- **Health Checks**: Liveness and readiness probes for all services with detailed status reporting
- **Dashboards**: Real-time visibility into system performance and business metrics
- **Alerting**: Proactive monitoring with configurable thresholds and escalation policies

**Projects affected**: All .NET APIs, Angular applications, and gRPC services
**Rationale**: Observability is essential for diagnosing issues, understanding system behavior, and maintaining service reliability in a distributed architecture.

### VII. IDesign Method - Design for Change (NON-NEGOTIABLE)
All system architecture MUST follow IDesign methodology principles to ensure adaptability and maintainability.
Requirements:
- **Business-Driven Service Boundaries**: Services defined by business capabilities, NOT technical functions or data entities
- **Volatile/Stable Separation**: Separate frequently changing business logic from stable infrastructure and utilities
- **Service-Oriented Design**: Each service encapsulates complete business capability with clear contracts
- **Avoid Functional Decomposition**: Do NOT organize code by technical layers (controllers, services, repositories) but by business domains
- **Change-Resilient Architecture**: Design assumes requirements will change; minimize ripple effects across service boundaries
- **Contract-First Development**: Define service interfaces before implementation; contracts drive the architecture
- **Business Service Identification**: Services represent complete business workflows, not technical operations

**Architecture Impact**:
- **Public API**: Organized by business capabilities (Event Management, User Management, Registration Services)
- **Private API**: Same business service boundaries, different access controls and client interfaces
- **gRPC Services**: Each service represents one business capability with complete workflow encapsulation
- **Angular Apps**: Feature modules aligned with backend service boundaries for consistency
- **Database Design**: Schema follows service boundaries with minimal cross-service data dependencies

**Projects affected**: All projects - fundamental architecture principle affecting service boundaries and code organization
**Rationale**: IDesign method ensures systems can adapt to changing business requirements with minimal architectural impact, reducing maintenance costs and enabling rapid feature development.

### VIII. Code Generation and Type Safety (NON-NEGOTIABLE)
All data models and API clients MUST be generated from a single source of truth to ensure type consistency and eliminate manual coding errors.
Requirements:
- **C# as Source of Truth**: All data models, DTOs, and API contracts defined as C# classes with appropriate attributes
- **Automatic .proto Generation**: gRPC .proto files generated from C# interfaces using protobuf-net.Grpc or similar tools
- **Angular SDK Generation**: Complete TypeScript SDK automatically generated including models, services, and HTTP clients
- **Build Integration**: Code generation integrated into CI/CD pipeline with automatic updates on model changes
- **Type Validation**: Generated models include runtime validation compatible across C# and TypeScript
- **Documentation Generation**: API documentation and SDK documentation generated from C# XML comments and attributes
- **Version Compatibility**: Generated artifacts maintain backward compatibility through versioned generation

**Angular SDK Generation Requirements**:
- **TypeScript Models**: All DTOs, entities, and enums generated as TypeScript interfaces with full type safety
- **HTTP Client Services**: Generated Angular services with typed HTTP methods for all API endpoints
- **Validation Services**: Client-side validation generated from C# FluentValidation rules
- **Error Handling**: Typed error responses and exception handling built into generated services
- **Authentication Integration**: Generated services include JWT token handling and refresh logic
- **Testing Utilities**: Generated mock services and test data builders for Angular unit tests
- **Observable Patterns**: Generated services use Angular patterns (RxJS Observables, dependency injection)
- **Configuration Integration**: Generated services respect Angular environment configuration

**Technical Implementation**:
- **NSwag SDK Generation**: Use NSwag.MSBuild to generate complete Angular SDK from OpenAPI specifications
- **Service Factory Pattern**: Generated Angular services follow factory pattern with dependency injection
- **Type-Safe HTTP Clients**: All API calls are strongly typed with request/response models
- **Automatic Retry Logic**: Generated services include configurable retry and error handling
- **Build Process Integration**: SDK generation happens during .NET build with automatic Angular project updates
- **Version Management**: Generated SDK includes versioning metadata for compatibility checking

**Projects affected**: All projects - Angular applications consume generated SDK instead of manual HTTP calls
**Rationale**: SDK generation eliminates manual API client coding, ensures type consistency, reduces integration errors, and provides enterprise-grade client libraries with minimal maintenance overhead.

## Code Quality Standards

All projects must maintain enterprise-grade code quality:

### .NET API Projects
- **Static Analysis**: Enable all compiler warnings, use StyleCop and SonarAnalyzer
- **Platform Version**: .NET 10 with latest C# language features and native AOT support
- **IDesign Architecture**: Organize by business services, NOT technical layers (avoid Controllers/Services/Repositories folders)
- **Business Service Structure**: Each service represents complete business capability (EventManagementService, UserRegistrationService)
- **Volatile/Stable Separation**: Business logic in volatile assemblies, infrastructure utilities in stable assemblies
- **Security**: Input validation, output encoding, dependency injection for testability
- **Logging**: Structured logging with correlation IDs for request tracing
- **Configuration**: Environment-specific settings with secrets management

### Angular Applications
- **Platform Version**: Angular 21 with TypeScript 5.9 strict mode and latest standalone components
- **Testing Framework**: Vitest with @testing-library/angular for component testing
- **Build Tool**: Vite for fastest development builds and optimized production bundles
- **IDesign Feature Organization**: Feature modules aligned with backend business service boundaries
- **Business Capability Modules**: Organize by business domains (event-management, user-registration) not technical functions
- **State Management**: NgRx for complex cross-service state, services for single-capability state
- **Performance**: Bundle analysis, tree shaking, OnPush change detection
- **Security**: Content Security Policy, sanitization of user inputs
- **Accessibility**: Screen reader support, keyboard navigation, focus management

### gRPC Services
- **Business Service Design**: Each gRPC service represents ONE complete business capability
- **Contract-First Development**: C# interfaces define business contracts with automatic .proto generation
- **Schema Management**: Centralized C# service interfaces with automated .proto generation organized by business domain
- **Type Safety**: Strong typing in C# with automatic TypeScript model generation for frontend clients
- **Service Boundaries**: Avoid cross-service data dependencies; each service owns its complete business workflow
- **Error Handling**: Structured error responses with proper gRPC status codes
- **Performance**: Connection pooling, streaming for large datasets
- **Documentation**: Service documentation generated from C# XML comments and attributes

### IDesign Service Organization
- **EventManagementService**: Complete event lifecycle (create, update, publish, archive)
- **RegistrationService**: Complete registration workflow (subscribe, confirm, cancel, waitlist)
- **SessionService**: Complete session management (schedule, assign speakers, track attendance)
- **UserService**: Complete user management (authentication, profiles, preferences)
- **NotificationService**: Complete communication workflow (templates, delivery, tracking)
- **NO Technical Services**: Avoid generic services like DataService, LoggingService, ValidationService

## Performance Requirements

All components must meet strict performance benchmarks:

### API Performance
- Response time: <75ms p50, <150ms p95 for all endpoints (improved with .NET 10 performance)
- Throughput: Handle 2000+ concurrent requests (enhanced with Aspire 13)
- Resource usage: <384MB memory per service instance (optimized with native AOT)
- Database queries: <50ms average execution time

### Angular Application Performance
- Initial load: <2 seconds on 3G network (improved with Angular 21 optimizations)
- Lighthouse Performance: ≥95 score (enhanced with latest framework)
- Bundle size: <1.5MB initial, <400KB per lazy-loaded module (better tree-shaking)
- Runtime performance: 60fps interactions, <100ms response to user input

### gRPC Communication
- Service-to-service latency: <50ms p95
- Connection establishment: <100ms
- Message serialization: <10ms for typical payloads
- Network resilience: Automatic retry with exponential backoff

## Testing Standards

Comprehensive testing across all solution components:

### Test Coverage Requirements
- **Minimum Coverage**: 90% line coverage for business logic
- **Integration Coverage**: All API contracts and gRPC services
- **E2E Coverage**: Critical user journeys in both Angular applications
- **Performance Testing**: Load testing for all public APIs

### Test Environment Standards
- **Isolation**: Tests run against containerized dependencies
- **Data Consistency**: Deterministic test data for repeatable results
- **Parallel Execution**: All test suites must support parallel execution
- **CI/CD Integration**: Automated testing gates for all deployments

## Development Workflow

Multi-project feature development requires coordinated workflow:

1. **Service Design**: Define C# interfaces for gRPC contracts and API specifications first
2. **Code Generation**: Generate .proto files and TypeScript models from C# definitions
3. **Contract Validation**: Generate client code and validate integration points
4. **Parallel Development**: API and UI development proceed simultaneously using generated types
5. **Integration Testing**: Verify end-to-end functionality across all components
6. **Staged Deployment**: Deploy services first, then client applications
7. **Monitoring**: Post-deployment verification of performance and functionality

## Governance

This constitution supersedes all other development practices and standards.

**Amendment Process**:
- Proposed changes require documentation with cross-project impact analysis
- Team review and approval required for all amendments
- Version increments follow semantic versioning (MAJOR.MINOR.PATCH)
- All dependent templates and multi-project documentation must be updated consistently

**Compliance Review**:
- All pull requests must verify constitutional compliance across affected projects
- Architecture decisions must be justified against service-first principles
- Cross-cutting changes require approval from both .NET and Angular team leads
- Use `.specify/` templates and workflows for consistent multi-project development practices

**Version**: 2.8.0 | **Ratified**: 2025-12-30 | **Last Amended**: 2025-12-30

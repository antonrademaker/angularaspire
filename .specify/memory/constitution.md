<!--
Sync Impact Report:
- Version change: 2.0.0 → 2.1.0
- New principle added: VI. Observability with OpenTelemetry
- Enhanced sections: None
- Templates requiring updates: ✅ All reviewed and aligned
- Follow-up TODOs: None
-->

# AngularAspire Constitution

## Core Principles

### I. Service-First Architecture (NON-NEGOTIABLE)
Every feature starts as a well-defined service contract with clear API boundaries.
All services MUST be:
- Contract-first with gRPC definitions as source of truth
- Independently deployable and testable
- Versioned with backward compatibility guarantees
- Documented with comprehensive API specifications

**Projects affected**: Public .NET API, Private .NET API, Shared gRPC Services
**Rationale**: Ensures consistent data contracts across all clients and enables independent service evolution.

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
- **Unit Tests**: All business logic in isolation (xUnit for .NET, Jest/tunit for Angular)
- **Integration Tests**: gRPC service communication and API endpoint contracts
- **End-to-End Tests**: Complete user workflows across public and private Angular apps (Playwright)
- **Contract Tests**: API schema validation and backward compatibility verification

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

## Code Quality Standards

All projects must maintain enterprise-grade code quality:

### .NET API Projects
- **Static Analysis**: Enable all compiler warnings, use StyleCop and SonarAnalyzer
- **Architecture**: Clean Architecture with clear separation of concerns
- **Security**: Input validation, output encoding, dependency injection for testability
- **Logging**: Structured logging with correlation IDs for request tracing
- **Configuration**: Environment-specific settings with secrets management

### Angular Applications
- **Architecture**: Feature modules with lazy loading, shared component libraries
- **State Management**: NgRx for complex state, services for simple state
- **Performance**: Bundle analysis, tree shaking, OnPush change detection
- **Security**: Content Security Policy, sanitization of user inputs
- **Accessibility**: Screen reader support, keyboard navigation, focus management

### gRPC Services
- **Schema Management**: Centralized .proto files with semantic versioning
- **Error Handling**: Structured error responses with proper gRPC status codes
- **Performance**: Connection pooling, streaming for large datasets
- **Documentation**: Service documentation generated from proto definitions

## Performance Requirements

All components must meet strict performance benchmarks:

### API Performance
- Response time: <100ms p50, <200ms p95 for all endpoints
- Throughput: Handle 1000+ concurrent requests
- Resource usage: <512MB memory per service instance
- Database queries: <50ms average execution time

### Angular Application Performance
- Initial load: <3 seconds on 3G network
- Lighthouse Performance: ≥90 score
- Bundle size: <2MB initial, <500KB per lazy-loaded module
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

1. **Service Design**: Define gRPC contracts and API specifications first
2. **Contract Validation**: Generate client code and validate integration points
3. **Parallel Development**: API and UI development proceed simultaneously
4. **Integration Testing**: Verify end-to-end functionality across all components
5. **Staged Deployment**: Deploy services first, then client applications
6. **Monitoring**: Post-deployment verification of performance and functionality

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

**Version**: 2.1.0 | **Ratified**: 2025-12-30 | **Last Amended**: 2025-12-30

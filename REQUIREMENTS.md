# Requirements & Architecture

## Project Overview

A production-ready REST API service for reversing words(Assuming that words are separated by spaces) in sentences. Each request/response pair is persisted to a database and can be queried through additional endpoints.

**Core Functionality:**
- User submits: `"abc def"` → Service returns: `"cba fed"`
- All request/response pairs are stored in MongoDB
- Search capabilities by word and full history retrieval

---

## Architecture

### High-Level Design

```
┌─────────────┐
│   Client    │
└──────┬──────┘
       │ HTTPS
       ▼
┌─────────────────────────────────────┐
│      ReverseController              │
│  (HTTP Layer - Routing & Validation)│
└──────────┬──────────────────────────┘
           │
           ▼
┌─────────────────────────────────────┐
│      ReverseService                 │
│  (Business Logic & Orchestration)   │
└──────────┬──────────────────────────┘
           │
           ▼
┌─────────────────────────────────────┐
│    ReverseRepository                │
│  (Data Access Layer - MongoDB)      │
└──────────┬──────────────────────────┘
           │
           ▼
┌─────────────────────────────────────┐
│         MongoDB                     │
│  (NoSQL Database - Document Store)  │
└─────────────────────────────────────┘
```

## Roadmap & Milestones

### ✅ P0 - MVP (COMPLETED)

**Goal:** Core functionality with production-ready basics

- [x] **Setup Instructions** - Machine-agnostic README with Docker setup
- [x] **Requirements Documentation** - This file with architecture details
- [x] **3 API Endpoints** - POST reverse, GET search, GET history
- [x] **MongoDB Integration** - NoSQL database with text indexing
- [x] **Input Validation** - Max length (1000 chars), required field validation
- [x] **Error Handling Middleware** - Global exception handling with proper HTTP status codes
- [x] **Swagger Documentation** - Interactive API documentation
- [x] **Docker Support** - Dockerfile with multi-stage builds
- [x] **Docker Compose** - MongoDB + API in one command (`docker-compose up`)

**Design Decisions:**
- ErrorHandlingMiddleware added in P0 (moved from P4) for production readiness
- Input validation with Data Annotations on DTOs
- Layered architecture for maintainability and testability

---

### ✅ P1 - Production Essentials (COMPLETED)

**Goal:** Security, performance, and caching

#### a) Authentication & Authorization
**Approach:** Pragmatic test accounts with JWT
- Create hardcoded test users (admin, user1, user2)
- Implement JWT token-based authentication
- Add `[Authorize]` attributes to endpoints
- **Current State:** All users have identical access (roles not enforced)
- **Future (P5+):** Implement role-based authorization with `[Authorize(Roles = "Admin")]`
- **Future:** Migrate to Auth0 / Azure AD / IdentityServer and leverage a secure key management system

**Rationale:** Avoiding over-engineering with full user management in early stages

#### b) Async POST API - ⚠️ DECISION REVERSED
**Original Plan:** Make POST async  
**Updated Decision:** Keep synchronous
- Word reversal is CPU-bound and fast (<1ms)
- Async adds complexity without performance benefit
- Already uses `async` for MongoDB I/O operations
- **Alternative:** Focus on horizontal scaling + rate limiting (P3)

#### c) Caching Strategy
**Implementation:**
- Create `ICache` interface for abstraction
- Start with `IMemoryCache` (ASP.NET Core built-in)
- Cache reversed results with sentence as key
- **Sliding Expiration:** 1-hour window that resets on each access
  - Frequently accessed sentences stay cached indefinitely
  - Rarely used entries auto-expire after 1 hour of inactivity
  - Optimal for deterministic operations like word reversal
- No cache invalidation needed since operation is idempotent (deterministic)
- **Future:** Swap to Redis for distributed caching

**Cache Expiration Decision:**
- **Chosen:** Sliding expiration over absolute expiration
- **Rationale:** Word reversal is deterministic—"hello" always becomes "olleh"
- Popular phrases remain cached, reducing database load
- Memory-efficient: unused entries expire automatically

**Benefits:**
- Reduces database queries for repeated sentences
- Extensible design for future cache providers (Redis, Memcached)
- Performance improvement for high-frequency requests
- Maximum cache hit ratio for popular inputs

#### d) Response Optimization
- **Pagination** on `/api/reverse/history` endpoint (critical for production)
  - Add `?page=1&pageSize=20` query parameters
  - Return metadata: `totalCount`, `currentPage`, `totalPages`
- **Response Compression** (Gzip/Brotli) for large responses

---

### ✅ P2 - Deployment Ready (COMPLETED)

**Goal:** Containerization and hosting

#### a) Docker Compose Enhancement
- [x] Multi-container setup (API + MongoDB)
- [x] Environment variable configuration for rate limiting
- [x] Volume persistence for MongoDB data
- [x] Health checks in docker-compose.yml
- [x] Smart startup scripts (start.ps1/start.sh) with rebuild detection

#### b) Unit Tests
- [x] Created `ReverseSentence.Tests` project
- [x] Test coverage for:
  - `ReverseService.ReverseWords()` - core logic
  - `AuthService` - JWT token generation
  - `InMemoryCache` - caching behavior
  - `RateLimiting` - Token Bucket, Fixed Window, Sliding Window
- [x] FluentAssertions for readable assertions

#### c) Swagger Configuration
- [x] Swagger enabled with JWT authentication
- [x] Bearer token support in UI
- [x] API endpoint documentation
- [x] Request/response examples

---

### ✅ P3 - Resilience & Rate Limiting (COMPLETED)

**Goal:** Protect API from abuse and overload

#### a) Rate Limiting ✅
**Implementation:**
- ✅ ASP.NET Core 8.0 built-in rate limiting middleware
- ✅ Fixed Window: 100 requests/minute per IP (API endpoints)
- ✅ Token Bucket: 10 requests/minute for `/login` endpoint
- ✅ Sliding Window: 1000 requests/hour global limit
- ✅ Custom 429 responses with `Retry-After` header
- ✅ IP-based partitioning for per-client limits
- ✅ Configurable via appsettings.json
- ✅ Environment variable overrides in docker-compose.yml

**Policies Implemented:**
1. **api-limit** (Token Bucket) ⚠️ UPDATED
   - 100 tokens per minute per IP
   - Applied to all `/api/reverse` endpoints
   - Allows burst traffic (uses saved tokens)
   - Queue limit: 5 requests
   - **Decision:** Switched from Fixed Window to Token Bucket for better burst handling

2. **auth-limit** (Token Bucket)
   - 10 tokens per minute
   - Applied to `/api/auth/login`
   - Prevents brute force attacks
   - Queue limit: 2 requests

3. **global-limit** (Sliding Window)
   - 100,000 requests per hour total (safety net)
   - 6 segments per window for smooth distribution
   - Queue limit: 10 requests
   - Note: Per-IP limits are primary; global is fallback protection

**Benefits:**
- ✅ Prevents DoS attacks and API abuse
- ✅ Protects against brute force on authentication
- ✅ Automatic rate limit enforcement at middleware level
- ✅ Graceful degradation with queue support
- ✅ Clear error messages with retry information
- ✅ Zero external dependencies (built into .NET 8)

**Future Enhancement (P5+):**
- Redis-backed distributed rate limiting for multi-instance deployments
- Per-user rate limits (based on JWT claims)
- Dynamic rate limit adjustment based on load

#### b) Load Testing ✅
**Implementation:**
- ✅ k6 integration via Docker Compose with profiles
- ✅ Rate limiting load test (0→50 VUs, gradual ramp over 3 min)
- ✅ Spike test (10→500 VUs, validates system stability)
- ✅ Automated test scripts (load-test.ps1/load-test.sh)
- ✅ Realistic thresholds validating system protection

**Key Findings:**
- Rate limiting load test: 95% rate limiting at 50 VUs is **expected and correct**
- Spike test: System handles 500 concurrent users (p99=34ms) without crashing
- Successful requests remain fast (p95 < 500ms) even under load
- Proves Token Bucket rate limiting protects MongoDB from overload

**Benefits:**
- ✅ Validates rate limiting policies work as designed
- ✅ Confirms system stability under extreme load
- ✅ Demonstrates graceful degradation (429 responses, not crashes)
- ✅ Industry-standard k6 tool with Docker integration

---

### 🔄 P4 - Observability & Testing (In Progress)

**Goal:** Production monitoring and quality assurance

#### a) Health Checks
- [x] Basic health endpoint: `/health` (returns status + timestamp)
- [ ] Liveness probe: `/health/live` (is API running?)
- [ ] Readiness probe: `/health/ready` (is MongoDB connected?)
- [ ] Integrate with Kubernetes/Docker health checks

#### b) Telemetry & Monitoring
**OpenTelemetry Integration:**
- Distributed tracing (track request flow)
- Metrics: request duration, error rates, cache hit ratio
- Export to Prometheus/Grafana or Application Insights

#### c) Audit Logging
**Structured Logging:**
- Log request/response payloads (sanitized)
- Include user identity (from JWT claims)
- Log levels: Information (success), Warning (validation), Error (exceptions)
- Use Serilog with JSON formatting
- Ship logs to centralized system (ELK stack / Azure Monitor)

#### d) Cloud Hosting
- Deploy to Azure App Service / AWS ECS / Google Cloud Run
- Configure environment-specific settings
- Set up CI/CD pipeline (GitHub Actions / Azure DevOps)

**Example Log Entry:**
```json
{
  "timestamp": "2025-11-26T10:30:00Z",
  "level": "Information",
  "userId": "user1",
  "action": "ReverseSentence",
  "originalSentence": "hello world",
  "reversedSentence": "olleh dlrow",
  "duration": "15ms"
}
```

---

## 🔜 Future Enhancements (P5+)

- **User Profile Service** - Full user management with registration/login and secret management
- **Horizontal Scaling** - Multiple API instances behind load balancer (Nginx/Azure Load Balancer)
- **Role-Based Authorization** - Enforce `[Authorize(Roles = "Admin")]` for sensitive endpoints
- **Distributed Caching** - Redis for cache synchronization across instances
- **Distributed Rate Limiting** - Redis-backed rate limiting for multi-instance deployments
- **Data Archival & Cold Storage** - Move old, unqueried pairings to cold storage (Azure Blob/S3)
  - Archive records older than 90 days with no access
  - Keep hot data in MongoDB for fast queries
  - Implement tiered storage strategy (hot/warm/cold)
- **Analytics Dashboard** - Most reversed words, usage statistics
- **Webhook Support** - Notify external systems on new reversals
- **GraphQL API** - Alternative to REST for flexible queries
- **Multi-language Support** - Handle non-ASCII characters properly

---

### Technology Stack

- **Framework:** ASP.NET Core 8.0 (Web API)
- **Language:** C# with top-level statements
- **Database:** MongoDB 3.5.1
- **API Documentation:** Swagger/OpenAPI
- **Containerization:** Docker (multi-stage builds)
- **Architecture Pattern:** Layered (Controller → Service → Repository)

### Design Patterns

1. **Dependency Injection**: All services registered in `Program.cs`
2. **Repository Pattern**: Abstraction over MongoDB operations
3. **DTO Pattern**: Separate request/response models from domain entities
4. **Middleware Pipeline**: Error handling, HTTPS redirection, authorization
5. **Interface Segregation**: `IReverseService`, `IReverseRepository` for testability

### Database Choice: MongoDB

**Why MongoDB?**
- ✅ No relational data - simple document storage
- ✅ High performance for read/write operations
- ✅ Easy horizontal scaling (sharding)
- ✅ Flexible schema for future enhancements
- ✅ Text indexing for word search functionality

**Alternative Considered:**
- PostgreSQL with JSONB: Would provide ACID guarantees but adds complexity for this use case

---

## API Endpoints

### 0. GET `/health`
Simple health check for monitoring and orchestration.

**Response:**
```json
{
  "status": "healthy",
  "timestamp": "2025-11-27T10:30:00Z"
}
```

**Use Cases:** Docker health checks, Kubernetes liveness probes, uptime monitoring

### 1. POST `/api/reverse`
Reverses all words in a sentence.

**Request:**
```json
{
  "sentence": "hello world"
}
```

**Response:**
```json
{
  "originalSentence": "hello world",
  "reversedSentence": "olleh dlrow",
  "timestamp": "2025-11-26T10:30:00Z"
}
```

### 2. GET `/api/reverse/search?word={word}`
Search for request/response pairs containing a specific word.

**Response:**
```json
[
  {
    "id": "507f1f77bcf86cd799439011",
    "originalSentence": "hello world",
    "reversedSentence": "olleh dlrow",
    "createdAt": "2025-11-26T10:30:00Z"
  }
]
```

### 3. GET `/api/reverse/history`
Retrieve all request/response pairs (sorted by newest first).

**Response:**
```json
[
  {
    "id": "507f1f77bcf86cd799439011",
    "originalSentence": "hello world",
    "reversedSentence": "olleh dlrow",
    "createdAt": "2025-11-26T10:30:00Z"
  }
]
```

---

## Design Principles

1. **SOLID Principles** - Single responsibility, dependency injection
2. **Separation of Concerns** - Controller/Service/Repository layers
3. **Fail Fast** - Validate input early in the pipeline
4. **Observability First** - Logging, metrics, health checks
5. **Testability** - Interfaces enable unit testing with mocks
6. **Security by Design** - Authentication, input validation, rate limiting

---

## Performance Targets

- **Response Time:** <50ms (p95) for single reversal
- **Throughput:** 1000 requests/second (with horizontal scaling)
- **Database:** <10ms query time for history/search endpoints
- **Cache Hit Ratio:** >70% for cached endpoints (P1)
- **Availability:** 99.9% uptime (with health checks and auto-restart)

---

## Security Considerations

1. **Input Validation** - Max 1000 chars, sanitize special characters
2. **HTTPS Only** - Enforce TLS 1.2+ in production
3. **Rate Limiting** - Prevent brute force and DoS attacks
4. **Authentication** - JWT with short expiration (15 min access token)
5. **CORS** - Whitelist allowed origins
6. **Secrets Management** - Use Azure Key Vault / AWS Secrets Manager for connection strings

---

*Last Updated: November 27, 2025*
# Reverse Sentence API

Production-ready REST API that reverses words in sentences with MongoDB persistence, JWT authentication, rate limiting, and caching. See [REQUIREMENTS.md](REQUIREMENTS.md) for detailed architecture and roadmap.

## Features

- **Word Reversal**: Reverse each word in a sentence individually
- **Persistent Storage**: MongoDB with text search indexing
- **Authentication**: JWT-based authentication
- **Rate Limiting**: Token Bucket (100 req/min API, 10 req/min auth)
- **Caching**: In-memory cache with sliding expiration
- **API Documentation**: Interactive Swagger UI
- **Load Testing**: Built-in k6 load tests

---

## Prerequisites

- **Docker Desktop** - [Download](https://www.docker.com/products/docker-desktop)

Verify installation:
```bash
docker --version
```

---

## Quick Start

### 1. Clone Repository
```bash
git clone https://github.com/shubhavee/ReverseSentence.git
cd ReverseSentence
```

### 2. Start Application
```powershell
# Windows
.\start.ps1

# Linux/Mac
chmod +x start.sh && ./start.sh
```

**What to expect:**

1. **Rebuild prompt:** `"Rebuild image? (y/N)"`
   - First time: Press **Enter** (auto-builds)
   - After code changes: Type **y**
   - No changes: Press **Enter** (uses existing image)

2. **Load test prompt:** `"Run load tests? (y/N)"`
   - Testing performance: Type **y**
   - Just running the app: Press **Enter**

3. Script will start containers and wait for health checks (~30 seconds)

### 3. Access Swagger UI

Open: **http://localhost:5001/swagger**

**Certificate Warning:** Click "Advanced" → "Proceed to localhost" (safe for local development)

### 4. Stop Application

```bash
# Stop containers (keeps data)
docker-compose down

# Stop and delete all data
docker-compose down -v
``

---

## Authentication

**⚠️ Required:** All API endpoints (except `/health` and `/api/auth/login`) require authentication.

### Test Credentials

| Username | Password |
|----------|----------|
| user1 | User123! |
| user2 | User123! |
| admin | Admin123! |

> **Note:** All users currently have identical access. Role-based authorization will be implemented in P5+.

### Quick Test via Swagger

1. **POST /api/auth/login** → Use credentials above → Copy `token` value from response.
2. Click **Authorize** button (top right) on Swagger UI → Paste token → Authorize
3. Now you can test other endpoints like **POST /api/reverse**

### Using cURL

```bash
# Login
TOKEN=$(curl -X POST "https://localhost:5001/api/auth/login" \
  -H "Content-Type: application/json" \
  -d '{"username": "admin", "password": "Admin123!"}' \
  -k | jq -r '.token')

# Reverse sentence
curl -X POST "https://localhost:5001/api/reverse" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"sentence": "hello world"}' \
  -k
```

---

---

## API Endpoints

| Method | Endpoint | Description | Auth |
|--------|----------|-------------|------|
| GET | `/health` | Health check | No |
| POST | `/api/auth/login` | Get JWT token | No |
| POST | `/api/reverse` | Reverse sentence words | Yes |
| GET | `/api/reverse/search?word={word}` | Search by word | Yes |
| GET | `/api/reverse/history?page=1&pageSize=20` | Get history (paginated) | Yes |

### POST /api/reverse

**Request:**
```json
{"sentence": "hello world"}
```

**Response:**
```json
{
  "originalSentence": "hello world",
  "reversedSentence": "olleh dlrow",
  "timestamp": "2025-11-27T10:30:00Z"
}
```

**Rate Limit:** 100 requests/minute per IP

### GET /api/reverse/history

**Query Parameters:**
- `page` (default: 1)
- `pageSize` (default: 20, max: 100)

**Response:**
```json
{
  "data": [{...}],
  "currentPage": 1,
  "totalPages": 3,
  "totalCount": 42
}
```

**Full API documentation:** http://localhost:5001/swagger

### GET /health

Simple health check endpoint for monitoring.

**Response:**
```json
{
  "status": "healthy",
  "timestamp": "2025-11-27T10:30:00Z"
}
```

**Use case:** Docker health checks, Kubernetes liveness probes, uptime monitoring

---

---

## Configuration

Default settings in `appsettings.json` :

```bash
# MongoDB
MongoDbSettings__ConnectionString=mongodb://localhost:27017
MongoDbSettings__DatabaseName=ReverseSentenceDB

# Rate Limiting
RateLimiting__Api__TokenLimit=100
RateLimiting__Api__ReplenishmentMinutes=1

# JWT
Jwt__ExpirationMinutes=60
```

---

---

## Troubleshooting

**MongoDB not connecting:**
```bash
docker ps  # Check if containers are running
docker compose logs mongodb  # View logs
```

**Port 5001 already in use:**
```bash
# Windows
netstat -ano | findstr :5001
# Linux/Mac
lsof -i :5001

#kill process on port 5001
```

**Certificate warning in browser:**
Click "Advanced" → "Proceed to localhost" (safe for local dev)

**Clear all data:**
```bash
docker-compose down -v  # Deletes volumes
```

---

---

## Load Testing

Built-in k6 load tests validate rate limiting and performance:

```bash
.\ load-test.ps1  # Windows
./load-test.sh    # Linux/Mac
```

**Available tests:**
1. Rate limiting load test (0→50 VUs, 3 min)
2. Spike test (10→500 VUs)

**Expected behavior:**
- High rate limiting (80-95%) during peak load is **correct**
- Proves system protection from overload
- Successful requests remain fast (p95 < 500ms)

See `LoadTests/README.md` for details.

---

---

## Architecture

```
Client → Controller → Service → Repository → MongoDB
```

**Design Patterns:**
- Layered architecture (Controller/Service/Repository)
- Dependency injection
- DTO pattern for API contracts
- Repository pattern for data access
- Middleware pipeline (auth, rate limiting, error handling)

**Project Structure:**
```
ReverseSentence/
├── ReverseSentence/           # Main API project
│   ├── Controllers/           # API endpoints
│   ├── Services/              # Business logic + caching
│   ├── Repositories/          # MongoDB data access
│   ├── Models/                # Domain entities
│   ├── DTOs/                  # Request/response contracts
│   ├── Middleware/            # Cross-cutting concerns
│   └── Extensions/            # Rate limiting configuration
├── ReverseSentence.Tests/     # Unit tests
│   ├── Services/              # Service layer tests
│   └── Unit/                  # Unit test suites
├── LoadTests/                 # k6 load tests
│   └── k6/                    # Test scripts
├── docker-compose.yml         # Container orchestration
├── start.ps1 / start.sh       # Application startup scripts
├── load-test.ps1 / load-test.sh  # Load testing scripts
├── README.md                  # This file - setup guide
└── REQUIREMENTS.md            # Architecture & roadmap
```

---

---

## Documentation

- **[REQUIREMENTS.md](REQUIREMENTS.md)** - Architecture, roadmap, design decisions
- **Swagger UI** - Interactive API docs at http://localhost:5001/swagger

---

## Tech Stack

- ASP.NET Core 8.0
- MongoDB 3.5.1
- JWT Authentication
- Token Bucket Rate Limiting
- In-Memory Caching
- k6 Load Testing
- Docker + Docker Compose

---

**Version:** 1.0.0  
**Status:** Production-ready for local testing

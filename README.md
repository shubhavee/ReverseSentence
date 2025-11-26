# Reverse Sentence API

A production-ready REST API service that reverses words in sentences and stores all requests/responses in MongoDB.

##  Features

- **Reverse Words**: Submit a sentence and get each word reversed individually
- **Search History**: Find request/response pairs containing specific words
- **Full History**: Retrieve complete audit trail of all transformations
- **MongoDB Storage**: Persistent, scalable NoSQL database
- **Error Handling**: Comprehensive error handling middleware
- **Input Validation**: Request validation (1-1000 characters)
- **Swagger Documentation**: Interactive API documentation

---

##  Prerequisites

| Component | Version | Required |
|-----------|---------|----------|
| .NET SDK | 8.0+ |  Yes |
| Docker Desktop | Latest |  Yes (for MongoDB) |
| IDE | VS 2022 / VS Code / Rider |  Optional |

**Install .NET 8:**
- Download: https://dotnet.microsoft.com/download/dotnet/8.0
- Verify: `dotnet --version` (should show 8.0.x)

**Install Docker Desktop:**
- Download: https://www.docker.com/products/docker-desktop
- Verify: `docker --version` and `docker info`

---

##  Quick Start (3 Steps)

### **Step 1: Clone/Download the Project**

```bash
# Clone from Git
git clone <insert-my-repo-url>
cd ReverseSentence

# Or navigate to the project directory
cd path/to/ReverseSentence
```

### **Step 2: Start MongoDB & API**

**Option A: Use the Start Script (Recommended)**

```bash
# Windows (PowerShell)
.\start.ps1

# Linux/Mac
chmod +x start.sh
./start.sh
```

**Option B: Manual Start**

```bash
# Start MongoDB
docker-compose up -d

# Verify MongoDB is running
docker ps

# Start the API
dotnet run --launch-profile https
```

### **Step 3: Open Swagger UI**

After you see `Application started`, open your browser:

```
https://localhost:7017/swagger
```

** Certificate Warning:** Click "Advanced"  "Proceed to localhost" (safe for local development)

---

##  Testing the API

### **Method 1: Swagger UI (Easiest)**

1. Open `https://localhost:7017/swagger` in your browser
2. Login to get a token:
   - Try **POST /api/auth/login** first
   - Use credentials: `user1` / `User123!`
   - Copy the token from the response
3. Click the **Authorize** button (lock icon at top right)
4. Paste the token in the **Authorization** dialog (just the token, not "Bearer")
5. Click **Authorize** then **Close**
6. Now try **POST /api/reverse**  **Try it out**
7. Enter request body:
   ```json
   {
     "sentence": "hello world"
   }
   ```
8. Click **Execute**
9. See response:
   ```json
   {
     "originalSentence": "hello world",
     "reversedSentence": "olleh dlrow",
     "timestamp": "2025-11-26T10:30:00Z"
   }
   ```

### **Method 2: cURL**

```bash
# Step 1: Login to get token
TOKEN=$(curl -X POST "https://localhost:7017/api/auth/login" \
  -H "Content-Type: application/json" \
  -d '{"username": "admin", "password": "Admin123!"}' \
  -k | jq -r '.token')

# Step 2: Use token in subsequent requests
# Reverse a sentence
curl -X POST "https://localhost:7017/api/reverse" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"sentence": "hello world"}' \
  -k

# Search by word (only shows your data)
curl -X GET "https://localhost:7017/api/reverse/search?word=hello" \
  -H "Authorization: Bearer $TOKEN" \
  -k

# Get your history
curl -X GET "https://localhost:7017/api/reverse/history" \
  -H "Authorization: Bearer $TOKEN" \
  -k
```

**Note:** `-k` flag skips SSL verification for local testing

---

##  API Reference

### **0. Login (Authentication)**

**Endpoint:** `POST /api/auth/login`  
**Description:** Authenticate and receive JWT token

**Request:**
```json
{
  "username": "admin",
  "password": "Admin123!"
}
```

**Response (200 OK):**
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "username": "admin",
  "expiresAt": "2025-11-26T11:30:00Z"
}
```

**Response (401 Unauthorized):**
```json
{
  "error": "Invalid username or password"
}
```

**Token Expiration:** 60 minutes (configurable)

---

### **1. Reverse Sentence**

**Endpoint:** `POST /api/reverse`  
**Description:** Reverses each word in the sentence and stores it in MongoDB  
 **Requires:** JWT token in `Authorization: Bearer {token}` header

**Request:**
```json
{
  "sentence": "abc def"
}
```

**Response (200 OK):**
```json
{
  "originalSentence": "abc def",
  "reversedSentence": "cba fed",
  "timestamp": "2025-11-26T10:30:00.000Z"
}
```

**Validation:**
- Min length: 1 character
- Max length: 1000 characters
- Required field

---

### **2. Search by Word**

**Endpoint:** `GET /api/reverse/search?word={word}`  
**Description:** Finds all your request/response pairs containing the specified word  
 **Requires:** JWT token  
 **Data Scope:** Returns only the authenticated user's data

**Example:** `/api/reverse/search?word=hello`

**Response (200 OK):**
```json
[
  {
    "id": "507f1f77bcf86cd799439011",
    "originalSentence": "hello world",
    "reversedSentence": "olleh dlrow",
    "createdAt": "2025-11-26T10:30:00.000Z"
  }
]
```

**Features:**
- Case-insensitive search
- Partial matching supported
- Sorted by most recent first
- User-isolated data

---

### **3. Get History (Paginated)**

**Endpoint:** `GET /api/reverse/history?page={page}&pageSize={pageSize}`  
**Description:** Retrieves your stored request/response pairs with pagination  
 **Requires:** JWT token  
 **Data Scope:** Returns only the authenticated user's data

**Query Parameters:**
- `page` (optional): Page number, starts at 1. Default: `1`
- `pageSize` (optional): Items per page. Default: `20`, Max: `100`

**Example:** `/api/reverse/history?page=1&pageSize=20`

**Response (200 OK):**
```json
{
  "data": [
    {
      "id": "507f1f77bcf86cd799439011",
      "originalSentence": "test one",
      "reversedSentence": "tset eno",
      "createdAt": "2025-11-26T10:35:00.000Z"
    },
    {
      "id": "507f1f77bcf86cd799439012",
      "originalSentence": "test two",
      "reversedSentence": "tset owt",
      "createdAt": "2025-11-26T10:30:00.000Z"
    }
  ],
  "currentPage": 1,
  "pageSize": 20,
  "totalCount": 42,
  "totalPages": 3,
  "hasNextPage": true,
  "hasPreviousPage": false
}
```

**Features:**
- Paginated results
- Sorted by newest first
- Rich pagination metadata
- User-isolated data

---

##  Configuration

### **MongoDB Settings**

Configuration is in `appsettings.json`:

```json
{
  "MongoDbSettings": {
    "ConnectionString": "mongodb://localhost:27017",
    "DatabaseName": "ReverseSentenceDB",
    "CollectionName": "ReverseRequests"
  }
}
```

### **Environment Variables (Optional)**

Override settings using environment variables:

```bash
# Windows (PowerShell)
$env:MongoDbSettings__ConnectionString="mongodb://your-host:27017"
$env:MongoDbSettings__DatabaseName="YourDatabaseName"

# Linux/Mac
export MongoDbSettings__ConnectionString="mongodb://your-host:27017"
export MongoDbSettings__DatabaseName="YourDatabaseName"
```

---

##  Troubleshooting

### **Problem: MongoDB Connection Failed**

**Symptoms:** `Unable to connect to MongoDB` error

**Solutions:**
```bash
# Check if MongoDB is running
docker ps

# If not running, start it
docker-compose up -d

# Check logs
docker logs reversesentence-mongodb

# Restart MongoDB
docker-compose down
docker-compose up -d
```

---

### **Problem: Port Already in Use**

**Symptoms:** `Address already in use` error

**Solutions:**
```bash
# Find what's using port 7017
netstat -ano | findstr :7017

# Use different port
dotnet run --urls="https://localhost:5001;http://localhost:5000"

# Then open: https://localhost:5001/swagger
```

---

### **Problem: Swagger UI Opens with HTTP**

**Symptoms:** Browser opens `http://localhost:5292/swagger` instead of HTTPS

**Solution:**
Manually navigate to the **HTTPS URL**:
```
https://localhost:7017/swagger
```

**Why:** HTTPS is required for Swagger to work properly in development mode.

---

### **Problem: Certificate Warning in Browser**

**Symptoms:** "Your connection is not private" warning

**Solution:**
1. Click **"Advanced"**
2. Click **"Proceed to localhost (unsafe)"**

**This is safe for local development!**

**Optional - Trust the certificate permanently:**
```bash
dotnet dev-certs https --trust
```

---

### **Problem: Data Persists After Stopping API**

**Symptoms:** Old data still appears after restarting

**Why:** This is **expected behavior**! MongoDB runs separately from the API.

**To clear data:**
```bash
# Option 1: Delete from MongoDB
docker exec -it reversesentence-mongodb mongosh ReverseSentenceDB \
  --eval "db.ReverseRequests.deleteMany({})"

# Option 2: Reset MongoDB completely
docker-compose down -v
docker-compose up -d
```

---

##  Stopping the Application

```bash
# Stop the API
# Press Ctrl+C in the terminal running the API

# Stop MongoDB (keeps data)
docker-compose down

# Stop MongoDB (deletes all data)
docker-compose down -v
```

---

##  Verify MongoDB Data Directly

```bash
# Connect to MongoDB
docker exec -it reversesentence-mongodb mongosh

# Switch to database
use ReverseSentenceDB

# View all documents
db.ReverseRequests.find().pretty()

# Count documents
db.ReverseRequests.countDocuments()

# Search for specific word
db.ReverseRequests.find({
  $or: [
    { originalSentence: /hello/i },
    { reversedSentence: /hello/i }
  ]
}).pretty()

# Exit
exit
```

---

##  Project Structure

```
ReverseSentence/
 Controllers/
    ReverseController.cs          # API endpoints (3 routes)
 Services/
    IReverseService.cs            # Service interface
    ReverseService.cs             # Business logic (word reversal)
 Repositories/
    IReverseRepository.cs         # Repository interface
    ReverseRepository.cs          # MongoDB data access
 Models/
    ReverseRequest.cs             # Domain entity
    MongoDbSettings.cs            # Configuration model
 DTOs/
    ReverseRequestDto.cs          # Input validation DTO
    ReverseResponseDto.cs         # Response DTO
    HistoryItemDto.cs             # History item DTO
 Middleware/
    ErrorHandlingMiddleware.cs    # Global error handling
 Program.cs                         # Application entry point & DI
 appsettings.json                   # Configuration
 docker-compose.yml                 # MongoDB container setup
 start.ps1                          # Windows quick start
 start.sh                           # Linux/Mac quick start
 README.md                          # This file
 REQUIREMENTS.md                    # Detailed specifications
```

---

##  Architecture Overview

This project follows principles:

```

   API Layer (Controllers)           
   - Thin controllers                
   - Input validation                
   - HTTP concerns                   

             

   Service Layer                      
   - Business logic                   
   - Word reversal algorithm          
   - Orchestration                    

             

   Repository Layer                   
   - Data access abstraction          
   - MongoDB operations               
   - Text search indexing             

             

   MongoDB                            
   - Document storage                 
   - Persistent volumes               

```

**Key Patterns:**
-  Repository Pattern - Data access abstraction
-  Service Layer - Business logic isolation
-  DTOs - API contract protection
-  Dependency Injection - Loose coupling
-  Middleware Pipeline - Cross-cutting concerns

---

##  Additional Documentation

For detailed architecture, roadmap, and phase planning, see:

 **[REQUIREMENTS.md](REQUIREMENTS.md)** - Complete specifications including:
- Detailed architecture
- P0, P1, P2, P3, P4 milestones
- Future enhancements
- Design decisions
- Testing strategy

---

##  Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

---

##  License

This project is for educational/demonstration purposes.

---

##  Support

- **Documentation Issues** Check [REQUIREMENTS.md](REQUIREMENTS.md)
- **Setup Problems** See Troubleshooting section above
- **Feature Requests** Open a GitHub issue
- **Questions** Create a discussion

---

**Status:**  P0 Complete - Production Ready for Local Testing

**Version:** 1.0.0

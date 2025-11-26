# Running ReverseSentence API with Docker

This guide shows how to run the complete ReverseSentence API application using Docker.

## Prerequisites

- **Docker Desktop** installed and running
  - Windows/Mac: https://www.docker.com/products/docker-desktop
  - Linux: Install Docker Engine and Docker Compose
- Verify installation: `docker --version` and `docker compose version`

## Quick Start

### 1. Start the Application

From the root directory of the project, run:

```bash
docker compose up -d
```

This will:
- Pull the MongoDB image
- Build the API Docker image
- Start both containers
- Create a network for them to communicate

### 2. Verify It's Running

```bash
# Check container status
docker compose ps

# View API logs
docker compose logs api

# View MongoDB logs
docker compose logs mongodb
```

### 3. Access the API

Open your browser to:
```
http://localhost:5000/swagger
```

### 4. Test the API

#### Login to get a token:
```bash
curl -X POST "http://localhost:5000/api/auth/login" \
  -H "Content-Type: application/json" \
  -d '{"username": "admin", "password": "Admin123!"}'
```

Copy the token from the response.

#### Use the API:
```bash
# Replace YOUR_TOKEN with the actual token
TOKEN="YOUR_TOKEN"

curl -X POST "http://localhost:5000/api/reverse" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"sentence": "hello world"}'
```

## Available Commands

### Start the application
```bash
docker compose up -d
```

### Stop the application
```bash
docker compose down
```

### Stop and remove all data
```bash
docker compose down -v
```

### View logs
```bash
# All services
docker compose logs -f

# Just the API
docker compose logs -f api

# Just MongoDB
docker compose logs -f mongodb
```

### Rebuild the API image
```bash
# After code changes
docker compose build api

# Or rebuild and restart
docker compose up -d --build
```

### Restart a service
```bash
docker compose restart api
```

## Test Users

The application comes with pre-configured test users:

| Username | Password | Role |
|----------|----------|------|
| admin | Admin123! | Admin |
| user1 | User123! | User |
| user2 | User123! | User |

## Ports

- **API**: http://localhost:5000
- **MongoDB**: localhost:27017 (accessible from host machine)

## Troubleshooting

### Port conflicts
If port 5000 or 27017 is already in use, edit `docker-compose.yml`:

```yaml
ports:
  - "5001:8080"  # Change 5000 to 5001 for API
  # or
  - "27018:27017"  # Change 27017 to 27018 for MongoDB
```

### Container won't start
```bash
# Check logs for errors
docker compose logs api

# Remove old containers and restart
docker compose down
docker compose up -d
```

### Database connection issues
```bash
# Verify MongoDB is healthy
docker compose ps

# Restart MongoDB
docker compose restart mongodb

# Check MongoDB logs
docker compose logs mongodb
```

### Rebuild after code changes
```bash
docker compose down
docker compose build --no-cache
docker compose up -d
```

## Data Persistence

MongoDB data is stored in a Docker volume named `mongodb_data`. This means:
- ✅ Data persists when you stop/start containers
- ✅ Data survives container restarts
- ❌ Data is deleted when you run `docker compose down -v`

## Sharing the Application

To share this application with others:

### Option 1: Share the source code
```bash
# Others can run:
git clone <your-repo>
cd ReverseSentence
docker compose up -d
```

### Option 2: Share a Docker image
```bash
# Build and save the image
docker compose build api
docker save reversesentence-api:latest | gzip > reversesentence-api.tar.gz

# Others can load it:
docker load < reversesentence-api.tar.gz
docker compose up -d
```

### Option 3: Push to Docker Hub
```bash
# Tag the image
docker tag reversesentence-api:latest yourusername/reversesentence-api:latest

# Push to Docker Hub
docker push yourusername/reversesentence-api:latest

# Update docker-compose.yml to use:
# image: yourusername/reversesentence-api:latest
# (instead of build: ...)
```

## Clean Up

Remove everything (containers, networks, volumes):
```bash
docker compose down -v
docker rmi reversesentence-api
```

## Production Considerations

For production deployment, consider:

1. **Use HTTPS**: Configure TLS certificates
2. **Secure MongoDB**: Add authentication
3. **Environment Variables**: Store secrets securely
4. **Resource Limits**: Add memory/CPU limits
5. **Health Checks**: Already configured in docker-compose.yml
6. **Logging**: Configure centralized logging
7. **Backups**: Implement MongoDB backup strategy

---

**Quick Reference**
```bash
# Start
docker compose up -d

# Stop
docker compose down

# View logs
docker compose logs -f

# Rebuild
docker compose up -d --build
```

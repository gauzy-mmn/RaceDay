# RaceDay
A full-stack web-based event management system designed specifically for the South African road running, walking and cycling community.
Event Organisers can create and manage events, define entry categories, capture results, while Participants can browse upcoming events, enter them, and track their own results and performance history

# Roles
Organiser : Create,edit and delete their own events, manage event categories, capture participants results and view all event enrollments
Participant : Create an account, browser events, enter an event by selecting a category, view their own enrolments and track their personal results

# CI/CD
A Github Actions workflow that checks on every push that /docs exist and contains the necessary files
<img width="1532" height="352" alt="image" src="https://github.com/user-attachments/assets/5acf1d50-7195-4ee3-8230-e73398067536" />

## PART 2 

# GitHub Actions build and tests
<img width="1228" height="57" alt="image" src="https://github.com/user-attachments/assets/675cc8cb-4d17-45c1-98db-a91ee9457732" />

# Local build and tests
<img width="652" height="83" alt="image" src="https://github.com/user-attachments/assets/b0e7f69a-2650-4725-a54e-21757f7cf22a" />


# Youtube Video explanation
Part 1: https://youtu.be/xH9m8uek9uQ
Part 2: https://youtu.be/YXeLLlfLeoU
# AI tool Disclosure
Used Claude AI to help with the planning and proof-reading of the API Endpoint Plan


## Running RaceDay with Docker

### Requirements
- Docker Desktop with Linux containers enabled
- Docker Compose

### Setup

1. Clone the repository.
2. Create a `.env` file in the solution root.
3. Add `MSSQL_SA_PASSWORD` with a strong SQL Server password.
4. Open a terminal in the solution root.

### Start the application

```powershell
docker compose config --quiet
docker compose up -d --build
docker compose ps
```

### Application URLs

- MVC website: http://localhost:8080
- API Swagger: http://localhost:7059/swagger

### Docker services

- `mvc`: ASP.NET Core MVC application
- `api`: ASP.NET Core Web API
- `sql`: SQL Server 2022
- `azurite`: Local Azure Blob Storage emulator

### Persistent storage

Docker named volumes preserve:
- SQL Server database records
- Uploaded images in Azurite
- API Data Protection keys
- MVC Data Protection keys

### Restart the application

```powershell
docker compose restart
```

### View logs

```powershell
docker compose logs --tail 50 api
docker compose logs --tail 50 mvc
```

### Stop the application

```powershell
docker compose down
```

Do not use `docker compose down -v` unless you intentionally
want to delete the application's stored Docker data.

### Notes

The Docker setup uses Azurite for local development.
Azurite is an emulator, not a deployed Azure Storage account.

The `.env` file contains secrets and must not be committed
to source control.


# KazanlakEvents

Community event platform for Kazanlak, Bulgaria and the "For the Youths" NGO.
Bilingual (Bulgarian + English), enterprise-grade, covering event discovery, ticketing, payments, volunteer management, a blog, donation campaigns, and sponsor showcases.

---

## Architecture

Clean Architecture — four independent layers with strict inward dependency flow:

```
┌─────────────────────────────────────────────────────────┐
│  KazanlakEvents.Web  (MVC Controllers · Razor Views)    │
│  ↓ depends on                                           │
│  KazanlakEvents.Infrastructure  (EF Core · Identity ·   │
│                                  Repositories · Email)   │
│  ↓ depends on                                           │
│  KazanlakEvents.Application  (Services · DTOs ·         │
│                                Validation · Mapping)     │
│  ↓ depends on                                           │
│  KazanlakEvents.Domain  (Entities · Enums · Interfaces) │
│  (zero external dependencies)                           │
└─────────────────────────────────────────────────────────┘
```

Key patterns: Repository + Unit of Work, Service layer, MediatR pipeline behaviours (logging/validation/performance), AutoMapper, FluentValidation, Serilog structured logging, Hangfire background jobs, Redis cache with graceful degradation.

---

## Tech Stack

| Layer | Technology |
|-------|-----------|
| Framework | ASP.NET Core 9 MVC |
| ORM | Entity Framework Core 9 + SQL Server |
| Identity | ASP.NET Core Identity (6-tier roles) |
| Background jobs | Hangfire + SQL Server storage |
| Cache | Redis (StackExchange.Redis) |
| Payments | Stripe |
| Email | SMTP via MailKit |
| Validation | FluentValidation |
| Mapping | AutoMapper |
| Logging | Serilog → console + rolling file |
| UI | Bootstrap 5.3 + Bootstrap Icons |
| Maps | Leaflet.js |
| Calendar | FullCalendar.js |
| Testing | xUnit + Moq + FluentAssertions |

---

## Getting Started

### Prerequisites
- .NET 9 SDK
- SQL Server (LocalDB or full instance)
- Redis (optional — app degrades gracefully without it)

### Run locally

```bash
# Apply migrations
cd src/KazanlakEvents.Web
dotnet ef database update --project ../KazanlakEvents.Infrastructure

# Run
dotnet run
```

App available at `https://localhost:5001`.
Swagger UI available at `https://localhost:5001/swagger` (Development only).

### Default credentials

| Role | Email | Password |
|------|-------|----------|
| SuperAdmin | `admin@kazanlakevents.bg` | `Admin@123456` |

### Run tests

```bash
dotnet test
```

---

## Docker Deployment

### Prerequisites
- Docker 24+ and Docker Compose v2

### Quick start

```bash
# 1. Copy env template and fill in secrets
cp .env.example .env
# Edit .env — set SA_PASSWORD, STRIPE_SECRET_KEY, JWT_SECRET

# 2. Build and start (web + SQL Server + Redis)
docker compose up -d

# 3. Apply EF Core migrations on first run
docker compose exec web dotnet KazanlakEvents.Web.dll migrate
```

App available at `http://localhost:8080`.

### Services

| Service | Port | Description |
|---------|------|-------------|
| `web` | 8080 | ASP.NET Core 9 MVC |
| `db` | 1433 | SQL Server 2022 |
| `redis` | 6379 | Redis 7 cache |

### Build image only

```bash
docker build -t kazanlakevents .
```

---

## Environment Variables

All runtime secrets are injected via environment variables (never committed).
Copy `.env.example` → `.env` and fill in values before running Docker Compose.

| Variable | Required | Description |
|----------|----------|-------------|
| `SA_PASSWORD` | Yes | SQL Server SA password — min 8 chars, uppercase + digit + symbol |
| `STRIPE_SECRET_KEY` | Yes | Stripe secret key (`sk_live_...` or `sk_test_...`) |
| `JWT_SECRET` | Yes | JWT signing key — min 32 characters |
| `ConnectionStrings__DefaultConnection` | Yes | Full SQL Server connection string (set automatically by Compose) |
| `Redis__ConnectionString` | No | Redis connection string (default: `localhost:6379,abortConnect=false`) |
| `Email__SmtpHost` | No | SMTP server hostname |
| `Email__SmtpPort` | No | SMTP port (default: 587) |
| `Email__Username` | No | SMTP username |
| `Email__Password` | No | SMTP password |

Production secrets use the `KE_` prefix when loaded from system environment variables:
`KE_ConnectionStrings__DefaultConnection`, `KE_Stripe__SecretKey`, etc.

---

## REST API

The public REST API is available at `/api/v1/` and documented via Swagger UI at `/swagger` (Development environment only).

### Authentication

All protected endpoints require a JWT Bearer token obtained from the auth endpoint:

```http
POST /api/v1/auth/login
Content-Type: application/json

{ "email": "user@example.com", "password": "Password123!" }
```

Response:
```json
{ "token": "<jwt>", "expiry": "2026-04-27T12:00:00Z" }
```

Use `Authorization: Bearer <token>` on subsequent requests.

### Endpoints

#### Events — `GET /api/v1/events`
List published events with optional filters:

| Param | Type | Description |
|-------|------|-------------|
| `page` | int | Page number (default: 1) |
| `pageSize` | int | Items per page (default: 20, max: 50) |
| `categoryId` | int | Filter by category |
| `isFree` | bool | Filter free events |
| `searchQuery` | string | Full-text search on title |

#### `GET /api/v1/events/{slug}`
Get full event details by slug.

#### `GET /api/v1/events/upcoming`
Get the next N published events (default: 5).

#### `GET /api/v1/events/nearby`
Get events near a coordinate: `?lat=42.6&lng=25.4&radiusKm=50`

#### `POST /api/v1/events` *(Organizer+)*
Create a new event. Requires JWT.

#### Tickets — `GET /api/v1/tickets/mine` *(Authenticated)*
List the authenticated user's tickets.

#### `POST /api/v1/tickets/purchase` *(Authenticated)*
Purchase tickets:
```json
{ "eventId": "...", "ticketTypeId": "...", "quantity": 2 }
```

#### `GET /api/v1/tickets/{ticketNumber}` *(Authenticated)*
Get ticket details by ticket number.

#### `POST /api/v1/tickets/{ticketNumber}/checkin` *(Organizer/Volunteer)*
Check in a ticket by QR code.

#### Webhooks — `POST /api/v1/webhooks/register` *(Organizer+)*
Register a webhook URL for event notifications.

---

## User Roles

| Role | Access |
|------|--------|
| SuperAdmin | Everything + Hangfire dashboard + system config |
| Admin | Event approval, user management, moderation, analytics |
| Organizer | Create/manage own events, analytics, ticket management |
| Moderator | Review reports, hide/warn content, manage comments |
| Volunteer | Sign up for shifts, log hours, QR check-in |
| Attendee | Browse, buy tickets, comment, rate, follow, favorite |

---

## Localization

- Bulgarian (default): `/{controller}/{action}` or `/bg/{controller}/{action}`
- English: `/en/{controller}/{action}`

Language can be set via the BG/EN toggle in the navbar, stored in a cookie valid for 1 year.

---

## CI/CD

GitHub Actions workflow (`.github/workflows/ci.yml`):

| Trigger | Job | What it does |
|---------|-----|-------------|
| Push to `main` or `develop` | `build-and-test` | Restores, builds Release, runs all tests against SQL Server container |
| PR to `main` | `build-and-test` | Same gate |
| Push to `develop` | `deploy-staging` | Publishes and deploys to staging (configure your provider) |

---

## Contributing

1. **Fork** the repository and create your branch from `develop`:
   ```bash
   git checkout -b feature/my-feature develop
   ```
2. **Follow** the Clean Architecture layers — no business logic in controllers, no DbContext access outside repositories.
3. **Write tests** — service logic goes in `Tests/Services/`, HTTP behaviour in `Tests/Controllers/`.
4. **Localize** all user-facing strings in both `SharedResource.bg.resx` and `SharedResource.en.resx`.
5. **Run** `dotnet build` and `dotnet test` before opening a PR. All 23 tests must pass.
6. **Open a Pull Request** against `develop` with a clear description of the change.

---

## License

Copyright © 2026 "For the Youths" NGO. All rights reserved.

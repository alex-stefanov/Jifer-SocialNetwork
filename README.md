# KazanlakEvents

KazanlakEvents is a bilingual community event platform for Kazanlak, Bulgaria and the "For the Youths" NGO. It supports event discovery, ticketing, payments, volunteer coordination, blog content, donations, sponsors, moderation, and administrative workflows.

## Features

- Public event discovery with categories, search, maps, and calendar views
- Ticket purchase and QR-code check-in flows
- Volunteer shifts and task management
- Blog, comments, ratings, reports, and moderation features
- Donation campaigns and sponsor showcase modules
- Organizer, attendee, volunteer, moderator, admin, and super-admin roles
- Bulgarian and English localization
- REST API with Swagger documentation in development
- Background jobs, caching, structured logging, and security middleware
- Automated tests for application services

## Architecture

The solution follows Clean Architecture with separate domain, application, infrastructure, web, and test projects.

```text
src/
├── KazanlakEvents.Domain/
├── KazanlakEvents.Application/
├── KazanlakEvents.Infrastructure/
└── KazanlakEvents.Web/
tests/
└── KazanlakEvents.Application.Tests/
```

## Tech Stack

- ASP.NET Core MVC
- Entity Framework Core
- SQL Server
- ASP.NET Core Identity
- MediatR
- AutoMapper
- FluentValidation
- Serilog
- Hangfire
- Redis
- Stripe
- MailKit
- Bootstrap
- xUnit, Moq, FluentAssertions

## Getting Started

### Prerequisites

- .NET SDK
- SQL Server
- Redis, optional

### Run Locally

```bash
git clone https://github.com/alex-stefanov/Jifer-SocialNetwork.git
cd Jifer-SocialNetwork
dotnet restore KazanlakEvents.sln
dotnet build KazanlakEvents.sln
dotnet run --project src/KazanlakEvents.Web
```

Configure connection strings and external service keys with user secrets or environment variables before using database, payment, or email features.

## Testing

```bash
dotnet test KazanlakEvents.sln
```

## License

No license file is currently included.

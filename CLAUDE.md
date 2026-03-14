# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**InstruaMe** is an ASP.NET Core REST API for a driving instruction marketplace, connecting instructors and students. Written in C# targeting .NET 10.

## Commands

```bash
dotnet restore                              # Restore NuGet packages
dotnet build                                # Build the project
dotnet run --project src/InstruaMe.csproj   # Run the API (http://localhost:5229)
dotnet ef migrations add <Name>             # Create EF migration
dotnet ef database update                   # Apply migrations to DB
```

The API serves at `http://localhost:5229` and `https://localhost:7102` in development.

## Architecture

Clean layered architecture with all code under `src/`:

- **Controllers/** — `InstruaMeController` (auth/register), `InstructorController`, `StudentController`, `ChatController`
- **Domain/** — Entities, commands (inputs), results (outputs), enums, and service contracts
- **Infrastructure/ORM/** — EF Core `InstruaMeDbContext`; Fluent API mappings in `Infrastructure/Mappings/`
- **Services/** — `JwtTokenService`, `PasswordHasherService`, `WebSocketManager`, `ChatWebSocketHandler`

### Entity Hierarchy

All entities inherit from `Domain/Entities/Base/EntityBase.cs`, which provides:
- `Guid Id` — auto-generated
- `bool Deleted` — soft-delete flag (all queries filter `!x.Deleted`)
- `DateTimeOffset? CreatedAt` — auto-set to UtcNow
- `DateTimeOffset? UpdatedAt` — manually set on update

Current entities: `Student`, `Instructor`, `Review`, `Conversation`, `ChatMessage`.

### Data Access

PostgreSQL via `Npgsql.EntityFrameworkCore.PostgreSQL`. Local dev DB: `Host=localhost;Port=5432;Database=instruame;Username=postgres;Password=masterkey` (in `appsettings.json`).

Fluent API mappings are defined per-entity in `Infrastructure/Mappings/` and registered in `InstruaMeDbContext`.

All queries must include the soft-delete filter: `.Where(x => !x.Deleted)`.

### Authentication

JWT Bearer (HMAC-SHA256). Claims stored in token: `Sub` (UserId), `Email`, `Role`. Protected endpoints use `[Authorize]`. Config in `appsettings.json` under `"Jwt"` key.

Password hashing: PBKDF2/HMAC-SHA256, 100k iterations, 16-byte salt, 32-byte hash, Base64-encoded.

### WebSocket (Real-Time Chat)

WebSocket endpoint: `/ws/chat/{conversationId:guid}?token=<jwt>`

JWT is validated from the query string before the connection is accepted. `WebSocketManager` maintains a thread-safe `ConcurrentDictionary<conversationId, ConcurrentDictionary<userId, WebSocket>>`. Messages are broadcast to all other participants in the conversation.

## API Endpoints

### InstruaMeController (`/v1/InstruaMe`)

| Method | Route | Auth | Description |
|--------|-------|------|-------------|
| POST | `/instructor` | No | Register instructor |
| POST | `/student` | No | Register student |
| POST | `/login` | No | Login (returns JWT + role) |
| GET | `/me` | Bearer | Returns UserId, Email, Role from token |

### InstructorController (`/v1/instructor`)

| Method | Route | Auth | Description |
|--------|-------|------|-------------|
| GET | `/` | No | List instructors (paginated, filterable) |
| GET | `/{id:guid}` | No | Get instructor profile with reviews |
| PUT | `/me` | Bearer (Instructor) | Update own profile |
| GET | `/me/dashboard` | Bearer (Instructor) | Dashboard with rating stats |
| POST | `/{id:guid}/reviews` | Bearer (Student) | Submit review (1–5 rating) |
| GET | `/{id:guid}/reviews` | No | List instructor reviews |

**GET `/` query params** (`ListInstructorsQuery`): `Name` (ILike), `City`, `State`, `CarModel`, `MaxPricePerHour`, `MinRating`, `Page` (default 1), `PageSize` (default 20).

### StudentController (`/v1/student`)

| Method | Route | Auth | Description |
|--------|-------|------|-------------|
| GET | `/me` | Bearer | Get own profile |
| GET | `/{id:guid}` | Bearer | Get student by ID |
| PUT | `/me` | Bearer (Student) | Update own profile |

### ChatController (`/v1/chat`)

| Method | Route | Auth | Description |
|--------|-------|------|-------------|
| GET | `/conversations` | Bearer | List own conversations |
| POST | `/conversations/{instructorId:guid}` | Bearer (Student) | Get or create conversation |
| GET | `/conversations/{id:guid}/messages` | Bearer | List messages in conversation |

## Domain Models

### Commands (Input DTOs)

- `RegisterInstructorCommand` — Name, Email, PhoneNumber, Document, State, City, Birthday, CarModel, Biography, Description, Photo?, PricePerHour, Password
- `RegisterStudentCommand` — Name, Email, Birthday, Photo, Password, ConfirmPassword
- `LoginCommand` — Email, Password
- `UpdateInstructorCommand` — all fields optional (partial update)
- `UpdateStudentCommand` — Name?, Birthday?, Photo?
- `ListInstructorsQuery` — filter/pagination params
- `SubmitReviewCommand` — Rating (1–5), Comment
- `SendChatMessageCommand` — Content

### Results (Output DTOs)

- `LoginResult` — Token, Role
- `InstructorCardResult` — Id, Name, Photo, City, State, CarModel, PricePerHour, AverageRating, TotalReviews
- `InstructorProfileResult` — full profile + IReadOnlyList\<ReviewResult\>
- `InstructorDashboardResult` — TotalStudentReviewers, AverageRating, RecentReviews (top 5)
- `StudentProfileResult` — Id, Name, Email, Birthday, Photo
- `ReviewResult` — Id, StudentId, StudentName, StudentPhoto, Rating, Comment, CreatedAt
- `ChatMessageResult` — Id, ConversationId, SenderId, SenderRole, Content, Read, CreatedAt
- `ConversationResult` — Id, InstructorId, InstructorName, StudentId, StudentName, CreatedAt
- `PagedResult<T>` — Items, Page, PageSize, TotalCount, TotalPages

## Key Business Rules

- **Reviews**: Unique per (InstructorId, StudentId) pair — duplicate returns 409 Conflict.
- **Conversations**: Unique per (InstructorId, StudentId) pair — POST returns existing or creates new.
- **Role enforcement**: `PUT /instructor/me` and `GET /instructor/me/dashboard` require `Instructor` role; `POST /{id}/reviews` and `POST /chat/conversations/{instructorId}` require `Student` role.
- **Email lookup**: Case-insensitive, trimmed (`.ToLower().Trim()`).

## Infrastructure

### DB Tables & Constraints

| Table | Notable Constraints |
|-------|---------------------|
| Instructor | Photo max 1000 chars, PricePerHour numeric(10,2) default 0 |
| Review | Comment max 2000 chars, unique index (InstructorId, StudentId) |
| Conversation | Unique index (InstructorId, StudentId) |
| ChatMessage | Content max 4000 chars, SenderRole max 20 chars |

### Services

- **`PasswordHasherService`** — implements `IPasswordHasherService`; `Hash(password)` → `(hash, salt)`; `Verify(password, hash, salt)` → bool
- **`JwtTokenService`** — `GenerateToken(userId, email, role)` → JWT string
- **`WebSocketManager`** — Singleton; manages active WebSocket connections per conversation
- **`ChatWebSocketHandler`** — Singleton; handles the full WebSocket lifecycle (connect → receive → broadcast → disconnect)

## Docker

Multi-stage build (`Dockerfile`):
1. Build: `mcr.microsoft.com/dotnet/sdk:10.0` — restores, publishes to `/out`
2. Runtime: `mcr.microsoft.com/dotnet/aspnet:10.0` — runs on `$PORT` (default 8080)

## Planned (Empty Directories)

- `Domain/Contracts/Repositories/` — repository interfaces
- `Infrastructure/Repositories/` — repository implementations

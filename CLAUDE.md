# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**InstruaMe** is an ASP.NET Core REST API for a driving instruction marketplace, connecting instructors and students. Written in C# targeting .NET 10.

## Commands

```bash
dotnet restore                          # Restore NuGet packages
dotnet build                            # Build the project
dotnet run --project src/InstruaMe.csproj  # Run the API (http://localhost:5229)
dotnet ef migrations add <Name>         # Create EF migration
dotnet ef database update               # Apply migrations to DB
```

The API serves at `http://localhost:5229` and `https://localhost:7102` in development.

## Architecture

Clean layered architecture with all code under `src/`:

- **Controllers/** — Single controller (`InstruaMeController`) handling all routes under `/v1/InstruaMe`
- **Domain/** — Entities, commands (inputs), results (outputs), enums, and service contracts
- **Infrastructure/** — EF Core `DbContext`, Fluent API mappings in `Mappings/`, planned repository implementations in `Repositories/`
- **Services/** — `JwtTokenService` (HS256 JWT generation/validation) and `PasswordHasherService` (PBKDF2/HMAC-SHA256, 100k iterations)

### Entity Hierarchy

All entities inherit from `Domain/Entities/Base/EntityBase.cs` which provides `Id` (Guid), `Deleted`, `CreatedAt`, `UpdatedAt`.

Current entities: `Student` and `Instructor` — both sealed, with `PasswordHash`/`PasswordSalt` fields and a `Role` from `EUserRole` enum.

### Data Access

PostgreSQL via `Npgsql.EntityFrameworkCore.PostgreSQL`. Local dev DB: `Host=localhost;Port=5432;Database=instruame;Username=postgres;Password=masterkey` (in `appsettings.json`).

Fluent API mappings are defined per-entity in `Infrastructure/Mappings/` and registered in `InstruaMeDbContext`.

### Authentication

JWT Bearer authentication. Claims stored in token: `UserId`, `Email`, `Role`. Protected endpoints use `[Authorize]`. Config in `appsettings.json` under `"Jwt"` key.

## API Endpoints

| Method | Route | Auth |
|--------|-------|------|
| POST | `/v1/InstruaMe/instructor` | No |
| POST | `/v1/InstruaMe/student` | No |
| POST | `/v1/InstruaMe/login` | No |
| GET | `/v1/InstruaMe/me` | Bearer |

## Planned (Empty Directories)

- `Domain/Contracts/Repositories/` — repository interfaces
- `Infrastructure/Repositories/` — repository implementations

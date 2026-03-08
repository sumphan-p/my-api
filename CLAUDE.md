# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

ASP.NET Core Web API targeting **.NET 10** — **Centralized Auth Server (Identity Provider)**. Uses controller-based routing with Swagger/OpenAPI for API documentation. Database access via **Dapper** with **SQL Server** (`Microsoft.Data.SqlClient`).

**Current state:** Early stage — database schema and connectivity are in place, but actual auth endpoints (login, register, token refresh) are not yet implemented.

## Build & Run Commands

```bash
dotnet build          # Build the project
dotnet run            # Run the API (http://localhost:5070, https://localhost:7113)
dotnet test           # Run tests (no test project yet)
```

Swagger UI available at `/swagger` in Development mode.

## Architecture

- **Program.cs** — Minimal hosting setup (controllers, OpenAPI, Swagger, HTTPS redirection, authorization). Registers `DbSettings` as singleton via DI.
- **DbSettings.cs** — Wraps SQL Server connection string for dependency injection.
- **Controllers/TestController.cs** — `GET /api/test/test-db` — verifies database connectivity.
- **appsettings.json** — Configuration with SQL Server connection string (`DefaultConnection` → `AuthDB`, Windows Auth, `TrustServerCertificate=true`).
- **database/schema.sql** — Creates `AuthDB` database, `Users` table (Id, Username, PasswordHash/BCrypt, CreatedAt), `RefreshTokens` table (Id, UserId FK, Token, ExpiresAt, IsRevoked, CreatedAt) with indexes. Includes seed admin user (`Admin@1234`).
- Root namespace: `AuthAPI`

## Key Dependencies

- **Dapper 2.1.72** — Micro-ORM for database queries
- **Microsoft.Data.SqlClient 6.1.4** — SQL Server connectivity
- **Swashbuckle.AspNetCore 10.1.4** — Swagger/OpenAPI documentation
- **Microsoft.AspNetCore.OpenApi 10.0.1** — Built-in OpenAPI support

## Patterns & Conventions

- Controllers inject `DbSettings` and create `SqlConnection` manually per query (no repository layer yet).
- BCrypt for password hashing. Refresh token rotation design for JWT-based auth.
- No Entity Framework — use Dapper with raw SQL throughout.

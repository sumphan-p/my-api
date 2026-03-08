# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

ASP.NET Core Web API targeting **.NET 10**. Uses controller-based routing with Swagger/OpenAPI for API documentation. Database access via **Dapper** with **SQL Server** (`Microsoft.Data.SqlClient`).

## Build & Run Commands

```bash
dotnet build          # Build the project
dotnet run            # Run the API (http://localhost:5070, https://localhost:7113)
dotnet test           # Run tests (no test project yet)
```

Swagger UI available at `/swagger` in Development mode.

## Architecture

- **Program.cs** — Minimal hosting setup (controllers, OpenAPI, Swagger, HTTPS redirection, authorization)
- **Controllers/** — API controller classes (currently empty)
- **appsettings.json** — Configuration including SQL Server connection string (`DefaultConnection` pointing to `AuthDb` database)
- Root namespace: `my_api`

## Key Dependencies

- **Dapper** — Micro-ORM for database queries
- **Microsoft.Data.SqlClient** — SQL Server connectivity
- **Swashbuckle.AspNetCore** — Swagger/OpenAPI documentation

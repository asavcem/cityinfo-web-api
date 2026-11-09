# CityInfo Web API

An ASP.NET Core 10 learning project for managing cities and their points of interest. It demonstrates database-backed REST endpoints, DTO mapping, repository-based data access, and composable search and filtering.

Built while following Kevin Dockx's [ASP.NET Core 10 Web API Essentials: Supporting Databases, Searching, Filtering, and Paging](https://www.pluralsight.com/courses/asp-dot-net-core-10-web-api-essentials-supporting-databases-searching-filtering-paging) on Pluralsight, continuing an existing CityInfo API from earlier lessons.

**Status:** Practical work closed after modules 1–3 and the search/filter implementation in module 4. Paging and a separate service layer were not implemented. Remaining course videos are planned as viewing only.

## What the project demonstrates

- EF Core with SQLite, entity relationships, three migrations, and seeded data.
- Repository interfaces and scoped dependency injection.
- Async queries, optional related-data loading with `Include`, and read-only queries with `AsNoTracking`.
- Entity-to-DTO mapping with AutoMapper.
- Creating, reading, replacing, patching, and deleting points of interest.
- Validation, HTTP status codes, and a `Location` header for created resources.
- Bulk description updates with `ExecuteUpdateAsync`; the repository also contains an `ExecuteDeleteAsync` example.
- Request cancellation propagated through the city-list query.
- Exact city-name filtering and substring search across city names and descriptions.
- JSON/XML responses and a development OpenAPI JSON document.

## Technology

.NET 10 · ASP.NET Core controllers · Entity Framework Core 10 · SQLite · AutoMapper · System.Text.Json JSON Patch

## Run locally

Prerequisite: the .NET 10 SDK. Commands below use PowerShell, starting from this repository's root.

```powershell
dotnet restore CityInfo.slnx
dotnet tool restore
$env:ASPNETCORE_ENVIRONMENT = 'Development'
Set-Location CityInfo.API
dotnet ef database update
dotnet run --launch-profile http
```

The local EF tool is pinned to the project's EF Core version. Run migration and startup commands from `CityInfo.API` so the relative SQLite path resolves to the same file. Migration creates `CityInfo.db` with three cities and six points of interest; no database file is distributed.

- API: `http://localhost:5124/api/cities`
- OpenAPI JSON: `http://localhost:5124/openapi/v1.json` (Development only).
- Request examples: [CityInfo.API.http](CityInfo.API/CityInfo.API.http).

For HTTPS, run `dotnet dev-certs https --trust`, use `dotnet run --launch-profile https`, and change the request file's base URL to `https://localhost:7150`.

The checked-in development connection string points only to a local SQLite file. For a different location, set `ConnectionStrings__CityInfoDBConnectionString` in the environment. Outside Development, provide that setting explicitly.

## Try the API

```powershell
Invoke-RestMethod 'http://localhost:5124/api/cities'
Invoke-RestMethod 'http://localhost:5124/api/cities?name=Paris'
Invoke-RestMethod 'http://localhost:5124/api/cities?searchQuery=tower'
Invoke-RestMethod 'http://localhost:5124/api/cities?name=Paris&searchQuery=tower'
Invoke-RestMethod 'http://localhost:5124/api/cities/3?includePointsOfInterest=true'
```

`name` applies an exact match after trimming. `searchQuery` is trimmed and matched against the name or a non-null description. Providing both combines them with AND. Results are ordered by city name. Case behavior follows the configured SQLite string operations.

| Method | Route | Purpose / success response |
| --- | --- | --- |
| GET | `/api/cities` | List, filter, or search cities; 200 |
| GET | `/api/cities/{cityId}` | Read a city, optionally including points of interest; 200 |
| GET | `/api/cities/{cityId}/pointsofinterest` | List a city's points of interest; 200 |
| GET | `/api/cities/{cityId}/pointsofinterest/{id}` | Read one point of interest; 200 |
| POST | `/api/cities/{cityId}/pointsofinterest` | Create; 201 with Location |
| PUT | `/api/cities/{cityId}/pointsofinterest/{id}` | Replace; 204 |
| PATCH | `/api/cities/{cityId}/pointsofinterest/{id}` | JSON Patch; 204 |
| DELETE | `/api/cities/{cityId}/pointsofinterest/{id}` | Delete; 204 |
| PUT | `/api/cities/{cityId}/pointsofinterest/bulk` | Update descriptions; 200 with `updatedCount` |

Create/replace bodies use `name` and optional `description`; bulk updates use `newDescription`. PATCH requests use `application/json-patch+json`, for example:

```json
[{ "op": "replace", "path": "/name", "value": "Updated attraction" }]
```

## Verify

From the repository root:

```powershell
dotnet build CityInfo.slnx
dotnet build CityInfo.slnx -c Release
./scripts/Verify-Api.ps1
```

The smoke script builds the API, creates a fresh temporary SQLite database using the migrations, starts its own local API process, checks search/filtering, reads, CRUD, validation, bulk updates, and OpenAPI, then stops that process. It does not use your working database. Logs and the disposable test database are kept in the printed temporary directory.

This is a smoke check, not a comprehensive automated test suite.

Verified on 2026-11-01: Debug and Release builds passed with no warnings or errors; all 24 HTTP smoke checks passed using a fresh migrated database. NuGet's direct/transitive vulnerability check reported no known vulnerable packages in the configured sources at that time.

## Structure

```text
CityInfo.API/
  Controllers/   HTTP endpoints
  DbContexts/    EF Core configuration and seed data
  Entities/      Database entities and relationships
  Migrations/    Reproducible schema and seed changes
  Models/        Request and response DTOs
  Profiles/      AutoMapper mappings
  Services/      Repository contract, implementation, and mail demos
scripts/         Repeatable API smoke verification
.config/         Local EF CLI tool manifest
```

The request flow is controller → repository → EF Core → SQLite, with AutoMapper between entities and DTOs. There is no separate business service layer.

## Scope and limitations

This is a course-based portfolio sample, not a production deployment. Authentication, authorization policies, paging, and a separate service layer are outside the implemented scope. Cancellation is demonstrated on the city-list path.

`LocalMailService` and `CloudMailService` both write to the console; neither sends real email. The inherited file controller is a separate demonstration: its download action looks for an optional local `sample-file.pdf` and returns 404 when absent. Uploads check the supplied content type and size; file contents are not inspected. Sample PDFs and uploads are excluded.

## Learning focus and attribution

The practical work focused on moving from in-memory data to persistent EF Core queries, understanding foreign keys and navigation properties, separating entities from API DTOs, and implementing repository-backed mutations. Troubleshooting included dependency injection, migration tooling, bulk-update expressions, cancellation, and keeping composed filters in the final query.

CityInfo and the lesson design originate from the linked Kevin Dockx / Pluralsight course. This repository records my course-following implementation and adaptations; AI assistance was used for selected explanations, troubleshooting, code edits, and documentation. Course videos, transcripts, and downloaded reference archives are not included. Third-party packages retain their own licenses.


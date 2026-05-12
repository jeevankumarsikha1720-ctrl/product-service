# ProductService — Full Documentation

A full-stack product catalog built with **ASP.NET Core 10** (Clean Architecture, CQRS, EF Core) and a **React 19 + Vite + Tailwind v4** frontend. Backed by **SQL Server**. Designed as a foundation for the larger E-Commerce Microservices on Azure portfolio project.

---

## Table of contents

1. [What this project is](#what-this-project-is)
2. [High-level architecture](#high-level-architecture)
3. [Tech stack](#tech-stack)
4. [Repository layout](#repository-layout)
5. [Backend deep dive](#backend-deep-dive)
6. [Frontend deep dive](#frontend-deep-dive)
7. [Running locally](#running-locally)
8. [API reference](#api-reference)
9. [Database schema](#database-schema)
10. [Common workflows](#common-workflows)
11. [Known gaps and next steps](#known-gaps-and-next-steps)
12. [Design decisions worth defending in interviews](#design-decisions-worth-defending-in-interviews)

---

## What this project is

A working product catalog with two faces:

- A **customer-facing storefront** (`/`) — a grid of products with prices, stock badges, images, and an in-memory cart counter.
- An **admin CRUD interface** (`AdminProductsPage`) — search, paginate, create, edit, and delete products through modal forms.

Both views talk to a single REST API served by ASP.NET Core. The backend uses **Clean Architecture** (Domain / Application / Infrastructure / Api layers) and **CQRS via MediatR** so every operation is a discrete command or query with its own handler.

The point is not the product catalog itself — it's demonstrating production patterns that scale to multi-service systems.

---

## High-level architecture

```
┌────────────────────────────────────────────────────────────────┐
│  Browser                                                       │
│  ┌──────────────┐  ┌─────────────────┐                         │
│  │  StorePage   │  │ AdminProducts   │   React 19 + Vite       │
│  │  (catalog)   │  │ Page (CRUD)     │   TanStack Query        │
│  │              │  │                 │   React Hook Form       │
│  └──────┬───────┘  └────────┬────────┘   Tailwind v4           │
│         └──────────┬─────────┘                                 │
│                    │ fetch /api/...                            │
└────────────────────┼───────────────────────────────────────────┘
                     │ (Vite dev proxy)
                     ▼
┌────────────────────────────────────────────────────────────────┐
│  ASP.NET Core 10  (https://localhost:7080)                     │
│  ┌─────────────────────────────────────────────────────────┐   │
│  │ Controllers (ProductsController)                        │   │
│  │   ↓ ISender.Send()                                      │   │
│  │ MediatR pipeline → ValidationBehavior → Handler         │   │
│  │   ↓                                                     │   │
│  │ Application: CreateProductCommand, GetProductQuery, ... │   │
│  │   ↓ IProductRepository                                  │   │
│  │ Infrastructure: ProductRepository → EF Core             │   │
│  │   ↓                                                     │   │
│  │ Domain: Product aggregate (business rules)              │   │
│  └─────────────────────────────────────────────────────────┘   │
│  ExceptionHandlingMiddleware → Problem+JSON                    │
│  Serilog → structured logs                                     │
└────────────────────┬───────────────────────────────────────────┘
                     ▼
┌────────────────────────────────────────────────────────────────┐
│  SQL Server (localhost)  database: ProductService              │
│    schema: products  table: products  __EFMigrationsHistory    │
└────────────────────────────────────────────────────────────────┘
```

**Dependency rule:** arrows only point inward. The Domain layer knows nothing about EF Core. The Application layer knows nothing about ASP.NET. The Api layer wires everything together at startup.

---

## Tech stack

### Backend
| Layer | Choice | Why |
|-------|--------|-----|
| Runtime | .NET 10 (LTS) | Current Microsoft long-term support runtime, supported through Nov 2028 |
| Web framework | ASP.NET Core 10 | Controllers + middleware pipeline |
| ORM | EF Core 10 | First-party, mature, async-first |
| Database | SQL Server | Microsoft-native, plays well with Azure SQL |
| Mediator | MediatR 12 | CQRS handlers, pipeline behaviors |
| Validation | FluentValidation 11 | Fluent rule chains, integrated via MediatR pipeline |
| Logging | Serilog | Structured logging, App Insights-ready |
| Tests | xUnit + NSubstitute + FluentAssertions | Standard .NET test stack |

### Frontend
| Concern | Choice | Why |
|---------|--------|-----|
| Build tool | Vite 6 | Instant HMR, fast builds, the modern default |
| UI library | React 19 | Industry standard |
| Language | TypeScript 5 (strict) | Catches API mismatches at compile time |
| Styling | Tailwind v4 | Utility-first, no config file (uses `@theme` in CSS) |
| Data fetching | TanStack Query 5 | Caching, invalidation, mutations — replaces Redux for most server-state |
| Forms | React Hook Form 7 | Uncontrolled inputs, minimal re-renders |

---

## Repository layout

```
ProductService/
├── ProductService.sln                       ← Visual Studio solution
├── Directory.Build.props                    ← Solution-wide compiler settings
├── DOCUMENTATION.md                         ← This file
├── README.md                                ← Quickstart
├── .config/dotnet-tools.json                ← Pins dotnet-ef CLI version
│
├── src/
│   ├── ProductService.Api/                  ← HTTP entry point
│   │   ├── Controllers/ProductsController.cs
│   │   ├── Middleware/ExceptionHandlingMiddleware.cs
│   │   ├── HealthChecks/DatabaseHealthCheck.cs
│   │   ├── Program.cs                       ← DI composition root
│   │   └── appsettings*.json                ← Connection strings, logging
│   │
│   ├── ProductService.Application/          ← Business orchestration
│   │   ├── DependencyInjection.cs           ← AddApplication()
│   │   ├── Common/
│   │   │   ├── Behaviors/ValidationBehavior.cs   ← MediatR pipeline
│   │   │   ├── Exceptions/NotFoundException.cs
│   │   │   └── Models/PagedResult.cs
│   │   ├── Interfaces/IProductRepository.cs
│   │   └── Products/
│   │       ├── Commands/{Create,Update,Delete}Product/
│   │       ├── Queries/{GetProduct,ListProducts}/
│   │       └── Dtos/ProductDto.cs
│   │
│   ├── ProductService.Domain/               ← Pure business model
│   │   ├── Common/BaseEntity.cs
│   │   ├── Entities/Product.cs              ← Aggregate root
│   │   └── Exceptions/DomainException.cs
│   │
│   └── ProductService.Infrastructure/       ← External concerns
│       ├── DependencyInjection.cs           ← AddInfrastructure()
│       ├── Persistence/
│       │   ├── ProductDbContext.cs
│       │   └── Configurations/ProductConfiguration.cs
│       ├── Repositories/ProductRepository.cs
│       └── Migrations/                      ← EF Core migrations
│
├── tests/
│   └── ProductService.Tests/
│       ├── Domain/ProductTests.cs           ← Aggregate invariant tests
│       └── Application/CreateProductHandlerTests.cs
│
└── frontend/
    ├── package.json
    ├── vite.config.ts                       ← Dev proxy → :7080
    ├── tsconfig.json
    ├── index.html
    └── src/
        ├── main.tsx                         ← Entry, QueryClientProvider
        ├── App.tsx                          ← Currently renders StorePage
        ├── index.css                        ← Tailwind import + theme
        ├── types.ts                         ← Shared TypeScript types
        ├── api.ts                           ← fetch wrapper + productsApi
        ├── shared/ui.tsx                    ← Button, Input, Modal, etc.
        ├── store/StorePage.tsx              ← Customer-facing catalog
        └── admin/
            ├── AdminProductsPage.tsx        ← Admin CRUD page
            └── ProductForm.tsx              ← Create/edit form (shared)
```

---

## Backend deep dive

### The Domain layer

`Product` is an aggregate root. State changes go through methods, not setters:

```csharp
var product = Product.Create(name, description, price, currency, stock);
product.UpdateDetails(newName, newDescription, newPrice, newCurrency);
product.AdjustStock(-3);   // throws DomainException if stock would go negative
product.Deactivate();
```

Why this matters: if you put `set` on every property, callers can put the entity in any state — including invalid ones. Methods enforce invariants in one place. `DomainException` signals "you tried to do something that breaks business rules" and gets translated to HTTP 400 by middleware.

`BaseEntity` provides `Id` (Guid), `CreatedAtUtc`, `UpdatedAtUtc`, and a `Touch()` method.

### The Application layer (CQRS)

Every operation is a discrete request handled by exactly one handler:

```
POST /api/products   →   CreateProductCommand   →   CreateProductHandler
GET  /api/products/x →   GetProductQuery        →   GetProductHandler
PUT  /api/products/x →   UpdateProductCommand   →   UpdateProductHandler
```

Three things happen automatically before any handler runs (via `ValidationBehavior` in the MediatR pipeline):

1. The request hits the controller, which calls `sender.Send(request)`.
2. The `ValidationBehavior<TRequest, TResponse>` pipeline runs every registered `IValidator<TRequest>`.
3. If any validator fails, a `ValidationException` is thrown — never reaching the handler.

This means **handlers can assume valid input**. No `if (model.Name == null)` boilerplate.

### The Infrastructure layer

`ProductDbContext` is the EF Core DbContext. The actual mapping rules (column types, max lengths, indexes) live in `ProductConfiguration` — separated from the entity because they're an infrastructure concern.

`ProductRepository` implements `IProductRepository` (which is defined in Application). The Application layer references the interface; the concrete class is injected at runtime. **This means handlers are testable with a mock repository** — no Postgres or SQL Server needed.

### The Api layer

`Program.cs` is the composition root. It:
- Wires up Serilog
- Calls `AddApplication()` and `AddInfrastructure()` (extension methods that hide the DI registration details)
- Adds controllers, health checks, and CORS for the Vite dev server (`http://localhost:5173`)
- Auto-applies migrations in Development (in production this should be a CI/CD step)
- Maps health endpoints: `/health/live` (always returns OK if process is up) and `/health/ready` (verifies SQL Server is reachable via `DatabaseHealthCheck`)

`ExceptionHandlingMiddleware` is the single place exceptions get translated to HTTP. The mapping:

| Exception | Status | Response |
|-----------|--------|----------|
| `ValidationException` (FluentValidation) | 400 | List of `{propertyName, errorMessage}` |
| `NotFoundException` (Application) | 404 | Title with entity name + key |
| `DomainException` (Domain) | 400 | Title with the business rule violated |
| anything else | 500 | "An unexpected error occurred" + traceId |

This means handlers throw clear exceptions. They never construct `IActionResult` objects.

---

## Frontend deep dive

### Entry point and providers

`main.tsx` mounts React, sets up a single `QueryClient` (shared across the app), and renders `App`. The query client has sensible defaults: 30-second `staleTime` so we don't refetch on every focus, `retry: 1` so transient errors don't blow up the UI.

`App.tsx` currently renders `StorePage` directly. There's no router yet — see [Known gaps](#known-gaps-and-next-steps).

### `api.ts` — the typed fetch wrapper

A single thin `request<T>()` helper handles:
- Setting `Content-Type: application/json`
- Parsing JSON responses
- Translating non-2xx responses into thrown `Error` objects (with the original ApiError attached as `.apiError`)
- Returning `undefined` for 204 No Content (delete operations)

`productsApi` exposes typed methods: `list`, `get`, `create`, `update`, `delete`. The frontend never deals with raw URLs anywhere else.

### `types.ts` — single source of truth

TypeScript interfaces that **mirror the C# DTOs** field-for-field. If you change `ProductDto.cs` on the backend, update `Product` in `types.ts` too. (In a larger project you'd auto-generate these from OpenAPI; for now they're hand-kept.)

### `StorePage` — customer view

- Calls `productsApi.list({ page: 1, pageSize: 20 })` once on mount via `useQuery`.
- Renders a hero section and a responsive grid (`sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4`).
- Each card uses `https://picsum.photos/seed/{productId}/600/400` for a stable placeholder image keyed to the product ID.
- Local `cart` state via `useState` — adds products to an in-memory list. **This resets on refresh.** A real implementation would either persist to localStorage or hit a `/api/cart` endpoint.

### `AdminProductsPage` — admin view

The heart of the CRUD demo. Maintains five pieces of local state:
- `search` — debounced into the query key
- `page` — pagination cursor
- `createOpen` / `editing` / `deleting` — modal visibility

Three mutations via `useMutation`:
- `createMut` → `POST /api/products` → invalidates the products query
- `updateMut` → `PUT /api/products/{id}` → invalidates
- `deleteMut` → `DELETE /api/products/{id}` → invalidates

`qc.invalidateQueries({ queryKey: ["products"] })` after each mutation triggers an automatic refetch — the UI stays in sync with the server without manual state juggling.

### `ProductForm` — shared create/edit form

Used by both the Create modal and the Edit modal in `AdminProductsPage`. The `initial?: Product` prop toggles between modes:
- No `initial` → Create mode → shows the `stockQuantity` field
- With `initial` → Edit mode → hides stock (because Update doesn't change stock; that's `AdjustStock` on the entity, a separate concept)

Validation rules in the form mirror the backend's `CreateProductValidator` — name max 200, description max 2000, price ≥ 0, etc. **Both sides validate** because client-side validation is UX, server-side is enforcement.

### `shared/ui.tsx` — design system primitives

Small set of styled components: `Button` (4 variants), `Input`, `Label`, `FieldError`, `Modal`, `Badge`, `EmptyState`, `Spinner`. Pure Tailwind classes — no shadcn/ui or component library dependency. Modifying the design = editing this one file.

---

## Running locally

### Prerequisites

| What | Check command | If missing |
|------|---------------|------------|
| .NET 10 SDK | `dotnet --version` | https://dotnet.microsoft.com/download/dotnet/10.0 |
| SQL Server | Open SSMS, connect to `localhost` | Install SQL Server Developer or Express |
| Node.js 20+ | `node --version` | https://nodejs.org/ |

### One-time setup

```cmd
:: Backend
cd C:\Users\jeeva\Projects\ProductService
dotnet tool restore
dotnet restore
dotnet build

:: Frontend
cd frontend
npm install
```

### Day-to-day: run both servers

**Terminal 1 — backend:**
```cmd
cd C:\Users\jeeva\Projects\ProductService
dotnet run --project src/ProductService.Api
```
Listens on `https://localhost:7080`. Migrations auto-apply on first run.

**Terminal 2 — frontend:**
```cmd
cd C:\Users\jeeva\Projects\ProductService\frontend
npm run dev
```
Listens on `http://localhost:5173`. The Vite proxy forwards `/api/*` to the ASP.NET server.

Open http://localhost:5173 in your browser. You'll see the storefront.

### Accept the dev certificate

First time you run, the frontend's fetch to `https://localhost:7080` may fail because Windows doesn't trust the ASP.NET dev cert. Fix:

```cmd
dotnet dev-certs https --trust
```

Restart both servers.

### Running tests

```cmd
cd C:\Users\jeeva\Projects\ProductService
dotnet test
```

You should see 7 tests passing across `ProductTests` and `CreateProductHandlerTests`.

---

## API reference

Base URL in dev: `https://localhost:7080/api`. All payloads are JSON.

### `GET /api/products`
List with pagination and optional search.

Query parameters:
- `page` (default 1, min 1)
- `pageSize` (default 20, max 100)
- `search` (optional, matches product name with `LIKE %term%`)

Response:
```json
{
  "items": [
    { "id": "...", "name": "...", "price": 9.99, ... }
  ],
  "page": 1,
  "pageSize": 20,
  "totalCount": 42,
  "totalPages": 3,
  "hasNext": true,
  "hasPrevious": false
}
```

### `GET /api/products/{id}`
Returns a single `ProductDto`. **404** if not found.

### `POST /api/products`
Create a product.

Request body:
```json
{
  "name": "Mechanical keyboard",
  "description": "Hot-swappable, RGB",
  "price": 129.99,
  "currency": "USD",
  "stockQuantity": 50
}
```
Response: **201 Created** with the new `ProductDto` and a `Location` header.

### `PUT /api/products/{id}`
Update product details. Body must include `id` matching the route.

Note: `stockQuantity` is **not** updated through this endpoint. Stock changes go through `Product.AdjustStock()` on the entity — a future endpoint will expose this.

### `DELETE /api/products/{id}`
Returns **204 No Content** on success, **404** if not found.

### `GET /health/live`
Liveness probe. Always returns **200** if the process is up.

### `GET /health/ready`
Readiness probe. Returns **200** if SQL Server is reachable; **503** if not.

### Validation error response (400)
```json
{
  "status": 400,
  "title": "Validation failed",
  "errors": [
    { "propertyName": "Price", "errorMessage": "'Price' must be greater than or equal to '0'." }
  ],
  "traceId": "00-..."
}
```

---

## Database schema

Database: `ProductService` (created automatically on first run)
Schema: `products`

### `products` table

| Column | Type | Notes |
|--------|------|-------|
| `Id` | `uniqueidentifier` (PK) | Client-generated GUID, not autoincrement |
| `Name` | `nvarchar(200)` | Required, indexed |
| `Description` | `nvarchar(2000)` | Nullable |
| `Price` | `decimal(18, 2)` | Required, ≥ 0 enforced in domain |
| `Currency` | `nvarchar(3)` | ISO 4217 code |
| `StockQuantity` | `int` | Required, ≥ 0 enforced in domain |
| `IsActive` | `bit` | Required, defaults to true on create |
| `CreatedAtUtc` | `datetime2` | Required, set at construction |
| `UpdatedAtUtc` | `datetime2` | Nullable, set on any change |

**Indexes:** non-unique on `Name`, non-unique on `IsActive`.

**Migration history table:** `products.__EFMigrationsHistory`.

---

## Common workflows

### Adding a new endpoint

Say you want `POST /api/products/{id}/adjust-stock` that takes `{ delta: -5 }`:

1. **Application:** create `AdjustStockCommand`, `AdjustStockValidator`, `AdjustStockHandler` under `Products/Commands/AdjustStock/`. Handler loads the product, calls `product.AdjustStock(delta)`, saves.
2. **Api:** add an action method to `ProductsController` that builds the command and sends it via `ISender`.
3. **Frontend:** add `productsApi.adjustStock(id, delta)` to `api.ts`.

No changes to Domain (the method already exists) or Infrastructure (the repository is generic enough).

### Adding a new entity (say, Categories)

1. **Domain:** `Category` entity with `Create()` factory and invariants.
2. **Application:** `ICategoryRepository`, command/query handlers under `Categories/`.
3. **Infrastructure:** `CategoryConfiguration` + `CategoryRepository`. Add `DbSet<Category>` to `ProductDbContext`.
4. **Migration:** `dotnet ef migrations add AddCategories --project src/ProductService.Infrastructure --startup-project src/ProductService.Api`.
5. **Api:** `CategoriesController`.
6. **Frontend:** `category` to `types.ts`, `categoriesApi` to `api.ts`, components under `src/admin/categories/`.

### Resetting the database

```cmd
dotnet ef database drop --project src/ProductService.Infrastructure --startup-project src/ProductService.Api
```
Then run the API — migrations will reapply.

---

## Known gaps and next steps

These are deliberate omissions you should be aware of:

1. **No routing in the frontend.** `App.tsx` renders `StorePage` and nothing else. The `AdminProductsPage` exists but isn't reachable. **Fix:** add `react-router-dom`, mount `StorePage` at `/` and `AdminProductsPage` at `/admin`.

2. **No authentication.** The admin page is exposed to anyone who knows the URL. **Fix path:** add JWT auth on the backend, protect admin routes on the frontend, add a login flow.

3. **Cart is in-memory.** `StorePage` keeps cart state in `useState` — it disappears on refresh. **Fix:** persist to `localStorage`, or move to a cart context, or hit a `/api/cart` endpoint.

4. **No stock decrement on cart add.** Clicking "Add" doesn't actually reserve stock. **Fix:** wire the Add button to a `POST /api/cart/items` endpoint that calls `product.AdjustStock(-1)`.

5. **Types are hand-kept in sync.** `types.ts` mirrors `ProductDto.cs` manually. **Fix:** generate the frontend types from OpenAPI using a tool like `openapi-typescript`.

6. **No integration tests.** Only unit tests exist. **Fix:** add a test that spins up the full API + a test SQL Server via Testcontainers.

7. **The Update endpoint doesn't change stock.** This is intentional — stock changes should go through a dedicated `AdjustStock` endpoint. But the admin UI doesn't expose stock editing for existing products. **Fix:** add an "Adjust stock" button and a separate modal.

---

## Design decisions worth defending in interviews

These are the talking points if a recruiter asks why you chose X:

> **"Why Clean Architecture instead of just controllers and DbContexts?"**
> Separation of concerns and testability. Handlers are pure C# that I can unit test without spinning up EF Core. The Domain layer enforces invariants so business rules are in exactly one place, not scattered across controllers.

> **"Why CQRS / MediatR if there's only one database?"**
> CQRS isn't about read/write databases — it's about treating each operation as its own discrete unit with its own validation and handler. It means I can change the implementation of "create product" without touching "list products," and the code is naturally feature-organized rather than layer-organized.

> **"Why React + Vite over Blazor?"**
> Two reasons. First, recruiters at the kinds of companies I'm targeting expect React. Second, Vite's dev loop is the fastest in the industry — sub-second hot module reload, which matters when you're iterating on UI.

> **"Why TanStack Query instead of Redux?"**
> Most state in this app is server state, not client state. TanStack Query is purpose-built for that: caching, automatic refetching, optimistic updates. Redux is for shared client state that doesn't fit the server-state model — which I don't have here.

> **"Why SQL Server instead of Postgres?"**
> Two reasons. Microsoft-native tooling — EF Core has the deepest support for SQL Server. And the deployment target is Azure SQL Database, which is fully managed SQL Server. Matching dev to prod reduces surprises.

> **"How does this scale to multiple services?"**
> The Clean Architecture layers stay; what changes is the boundary. Each future service (Orders, Notifications) gets its own database per the database-per-service pattern. Inter-service communication moves to async via Service Bus. The Outbox pattern handles transactional consistency between writing to your DB and publishing events.

---

## Versions in use

```
.NET                              10.0.x  (LTS)
Microsoft.EntityFrameworkCore     10.0.0
Microsoft.EntityFrameworkCore.SqlServer  10.0.0
MediatR                           12.4.1
FluentValidation                  11.9.2
Serilog.AspNetCore                8.0.3

React                             19.x
Vite                              6.x
TypeScript                        5.6+
Tailwind CSS                      4.x
@tanstack/react-query             5.59+
react-hook-form                   7.53+
```

Run `dotnet list package` in any project folder for exact resolved versions. Run `npm list --depth=0` inside `frontend/` for frontend versions.

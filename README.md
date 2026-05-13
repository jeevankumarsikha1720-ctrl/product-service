# ProductService

Modern e-commerce backend and storefront built with ASP.NET Core, Clean Architecture, CQRS, and React.

---

## Live Demo

| Surface | URL |
|---------|-----|
| Storefront + Admin UI | https://salmon-grass-0d1e97810.7.azurestaticapps.net |
| API root | https://productservice-api-jk2026.azurewebsites.net/api/products |
| Readiness probe | https://productservice-api-jk2026.azurewebsites.net/health/ready |

Hosted on Azure:

- **Backend** — ASP.NET Core 10 on Azure App Service (Linux)
- **Database** — Azure SQL Database (Serverless, auto-pause)
- **Frontend** — React 19 + Vite on Azure Static Web Apps
- **CI/CD** — GitHub Actions auto-deploys the API on every push to `main`

---

# Overview

ProductService is a full-stack e-commerce inventory platform designed with enterprise backend architecture principles.

The project includes:

* ASP.NET Core Web API
* Clean Architecture
* CQRS + MediatR
* Entity Framework Core
* SQL Server
* React + TypeScript + Vite frontend
* Tailwind CSS UI
* Inventory movement ledger
* Stock reservation workflow
* Idempotent checkout
* Chargeback-aware audit trail
* Storefront + Admin experience

The system models real inventory operations instead of simple CRUD stock tracking.

---

# Live Inventory Ledger

Every cart action and checkout writes an immutable audit row to `inventory.InventoryMovements`.

| Reason   | Quantity | OnHandDelta | ReservedDelta | ReferenceId    | Note               |
| -------- | -------- | ----------- | ------------- | -------------- | ------------------ |
| Received | 10       | +10         |  0            | NULL           | Initial stock      |
| Reserved |  2       |   0         | +2            | cart-7777-...  | Cart reservation   |
| Released |  1       |   0         | -1            | cart-7777-...  | Removed from cart  |
| Sold     |  1       |  -1         | -1            | order-7777-... | Order shipped      |

Replaying every row chronologically reproduces the current state of any product.

> Drop a screenshot of your SSMS ledger output at `docs/screenshots/ledger.png` and uncomment the line below.
>
> `<!-- ![Inventory ledger](docs/screenshots/ledger.png) -->`

---

# Architecture

The backend follows Clean Architecture principles.

```text
src/
 ├── ProductService.Api
 ├── ProductService.Application
 ├── ProductService.Domain
 └── ProductService.Infrastructure
```

### Layers

| Layer          | Responsibility                         |
| -------------- | -------------------------------------- |
| Domain         | Business rules, aggregates, invariants |
| Application    | CQRS handlers, validation, DTOs        |
| Infrastructure | EF Core, repositories, persistence     |
| Api            | HTTP endpoints                         |
| Frontend       | React storefront/admin UI              |

---

# Features

## Product Management

* Create products
* Update products
* Delete products
* Pagination
* Filtering
* Product status management

---

## Inventory System

### Inventory Aggregate

The inventory system is implemented as a true domain aggregate with enforced invariants.

### Supported Operations

* Receive stock
* Reserve stock
* Release stock
* Commit stock (ship/sell)
* Return from sale
* Chargeback handling (return + write-off)
* Movement history
* Low-stock detection
* Manual adjustments
* Physical recount support

### Inventory Invariants

The domain guarantees:

```text
OnHand >= 0
Reserved >= 0
Reserved <= OnHand
Available = OnHand - Reserved
```

---

# Inventory Workflow

```text
Receive → Reserve → Release → Commit
```

### Example

```text
Receive 10 units
Available = 10

Reserve 2
Available = 8
Reserved = 2

Release 1
Available = 9
Reserved = 1

Commit 1
OnHand = 9
Reserved = 0
```

---

# Chargeback Handling

Chargebacks are a real-world e-commerce scenario where a customer disputes a charge after the order has shipped. Inventory has to record what actually happened so accounting can reconcile.

| Scenario                                          | Inventory action                       | Effect             |
| ------------------------------------------------- | -------------------------------------- | ------------------ |
| Customer wins, item returned in good condition    | `ReturnFromChargeback(qty, orderId)`   | OnHand +qty        |
| Customer wins, item NOT returned (friendly fraud) | `WriteOffFromChargeback(qty, orderId)` | Audit row only — stock was already gone from Commit |
| Merchant wins the dispute                         | No action                              | Sale stands as-is  |

The architectural principle: **inventory records the truth, it does not decide what happens.** The Payments service tells Orders "chargeback received." Orders decides whether the goods came back. Inventory just writes the row.

Every movement row carries:

* Reason (enum)
* Quantity
* OnHand delta
* Reserved delta
* Reference ID (cart / order / supplier shipment)
* Note
* Timestamp

This creates a complete, queryable audit trail surviving years after the fact.

---

# Idempotent Checkout

The `/api/inventory/products/{id}/commit` endpoint is idempotent via the `Idempotency-Key` header.

* Frontend generates one UUID per checkout attempt
* Same key sent on retry → server replays the cached response instead of double-committing stock
* Cache TTL: 24 hours
* In-memory store today; production swap to Redis is a one-line DI change

This prevents the most common e-commerce failure mode: a customer's network drops during checkout, they hit the button again, and the same order ships twice.

---

# Frontend

The frontend is built with:

* React 19
* TypeScript (strict)
* Vite 6
* Tailwind CSS v4
* TanStack Query
* React Hook Form
* React Router v7

## Frontend Features

* Storefront UI with live Available stock
* Product cards
* Cart drawer with quantity steppers
* Per-product stock cap (cannot add more than Available)
* Persistent cart via localStorage
* Admin product CRUD
* Inventory-aware purchasing (Add reserves, Remove releases, Checkout commits)
* Idempotent checkout with retry safety

---

# Technology Stack

## Backend

* ASP.NET Core 10
* C#
* MediatR
* FluentValidation
* Entity Framework Core 10
* SQL Server
* Serilog

## Frontend

* React 19
* TypeScript 5
* Vite 6
* Tailwind CSS v4
* TanStack Query 5

---

# Running the Project

## Backend

```bash
dotnet restore
dotnet build
dotnet run --project src/ProductService.Api
```

Backend runs on:

```text
https://localhost:7080
```

Migrations and the inventory backfill seeder run automatically on startup in Development.

---

## Frontend

```bash
cd frontend
npm install
npm run dev
```

Frontend runs on:

```text
http://localhost:5173
```

The Vite dev server proxies `/api/*` to the backend, so the React code talks to a single origin.

---

# Database

## Apply Migrations

```bash
dotnet ef database update --project src/ProductService.Infrastructure --startup-project src/ProductService.Api
```

Three migrations ship with the repo:

* `InitialSqlServer` — Products table
* `AddInventory` — InventoryItems + InventoryMovements tables in `inventory` schema
* `AddInventoryIndexes` — composite + reason + reference indexes, unique constraint on `InventoryItems.ProductId`

---

# API Endpoints

## Products

```http
GET    /api/products
GET    /api/products/{id}
POST   /api/products
PUT    /api/products/{id}
DELETE /api/products/{id}
```

## Inventory

```http
GET  /api/inventory/products/{productId}
GET  /api/inventory/{inventoryItemId}/movements

POST /api/inventory/products/{productId}/receive
POST /api/inventory/products/{productId}/reserve
POST /api/inventory/products/{productId}/release
POST /api/inventory/products/{productId}/commit       (supports Idempotency-Key header)
```

## Health

```http
GET /health/live
GET /health/ready    (verifies SQL Server reachability)
```

---

# Development Notes

* Inventory movement history is append-only
* Domain entities encapsulate all business rules — no setters, only methods that enforce invariants
* Commands and queries are separated using CQRS via MediatR
* Validation is handled with FluentValidation in the MediatR pipeline
* SQL Server is the persistence store with EF Core 10
* Frontend consumes the API through React Query with automatic invalidation on mutations
* Available stock is computed at read time as `OnHand - Reserved`, sourced from the live `InventoryItem`

---

# Future Improvements

* Authentication & authorization
* JWT security
* Role-based admin access
* Standalone Order microservice with event-driven communication
* Payment integration
* Warehouse / multi-location support
* Background job to release expired cart reservations
* Event-driven messaging via Azure Service Bus + Outbox pattern
* Redis idempotency store
* Redis caching for product reads
* Dockerized deployment
* CI/CD pipelines

---

# Author

Jeevan Kumar Sikha

Built as a portfolio project focused on enterprise backend architecture and real inventory domain modeling.

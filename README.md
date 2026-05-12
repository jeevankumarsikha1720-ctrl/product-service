# ProductService

Modern e-commerce backend and storefront built with ASP.NET Core, Clean Architecture, CQRS, and React.

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
* Storefront + Admin experience

The system models real inventory operations instead of simple CRUD stock tracking.

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

# Inventory Movement Ledger

Every inventory mutation creates an immutable movement record.

Movement history includes:

* Reason
* Quantity
* OnHand delta
* Reserved delta
* Reference ID
* Notes
* Timestamp

This creates a complete audit trail for inventory operations.

---

# Frontend

The frontend is built with:

* React
* TypeScript
* Vite
* Tailwind CSS
* TanStack Query

## Frontend Features

* Storefront UI
* Product listing
* Product cards
* Shopping cart UI
* Inventory-aware purchasing
* Admin product management

---

# Technology Stack

## Backend

* ASP.NET Core 10
* C#
* MediatR
* FluentValidation
* Entity Framework Core
* SQL Server

## Frontend

* React
* TypeScript
* Vite
* Tailwind CSS
* TanStack Query

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

---

# Database

## Apply Migrations

```bash
dotnet ef database update --project src/ProductService.Infrastructure --startup-project src/ProductService.Api
```

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

---

## Inventory

```http
GET  /api/inventory/products/{productId}
GET  /api/inventory/{inventoryItemId}/movements

POST /api/inventory/products/{productId}/receive
POST /api/inventory/products/{productId}/reserve
POST /api/inventory/products/{productId}/release
POST /api/inventory/products/{productId}/commit
```

---

# Development Notes

* Inventory movement history is append-only
* Domain entities encapsulate all business rules
* Commands and queries are separated using CQRS
* Validation is handled with FluentValidation
* SQL Server is used for persistence
* Frontend consumes the API through React Query

---

# Future Improvements

* Authentication & authorization
* JWT security
* Role-based admin access
* Checkout workflow
* Payment integration
* Warehouse/location support
* Order service
* Event-driven messaging
* Redis caching
* Dockerized deployment
* CI/CD pipelines

---

# Author

Jeevan Kumar Sikha

Built as a portfolio project focused on enterprise backend architecture and real inventory domain modeling.

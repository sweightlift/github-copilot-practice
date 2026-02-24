# CustomerManager — Project Analysis

## Overview

| Item | Detail |
|------|--------|
| **Type** | ASP.NET Core Web API |
| **Framework** | .NET 8.0 |
| **Architecture** | Controller → Service (2-layer) |
| **Data Store** | In-memory static list (no real database) |
| **API Docs** | Swagger / Swashbuckle (dev only) |

A lightweight legacy-style REST API that manages customer data. Intended as a starting point for a refactoring / modernization exercise.

---

## Project Structure

```
CustomerManager/
├── Program.cs                      # App entry point & DI/middleware config
├── CustomerManager.csproj          # Project file (.NET 8, Swashbuckle)
├── appsettings.json                # Config (connection string, API key placeholder)
├── Controllers/
│   ├── CustomersController.cs      # CRUD-ish endpoints for customers
│   └── HealthController.cs         # Health-check endpoint
├── Models/
│   └── DomainModels.cs             # Customer, Order, HealthResponse
└── Services/
    └── CustomerService.cs          # Business logic + in-memory data
```

---

## Key Components

### 1. Program.cs — Application Bootstrap

- Registers **Swagger** (title: *"Legacy API"*, v1.0.0).
- Registers `ICustomerService` → `CustomerService` as **Scoped**.
- Enables HTTPS redirection and maps controllers.
- Swagger UI is only exposed in the **Development** environment.

### 2. Models (`DomainModels.cs`)

| Model | Fields | Notes |
|-------|--------|-------|
| `Customer` | `Id`, `Name`, `Email`, `CreatedAt` | Core entity |
| `Order` | `Id`, `CustomerId`, `Status`, `Amount`, `OrderDate` | **Defined but never used anywhere** |
| `HealthResponse` | `Status`, `Message`, `Timestamp` | DTO for health endpoint |

### 3. Services (`CustomerService.cs`)

- **Interface:** `ICustomerService` — `GetCustomer(int)`, `SearchCustomer(string)`, `GetAllCustomers()`
- **Implementation:** `CustomerService`
  - Uses a **static in-memory list** with 3 seed customers (John Doe, Jane Smith, Bob Wilson).
  - `GetCustomer` — lookup by ID (`FirstOrDefault`).
  - `SearchCustomer` — case-insensitive partial name match.
  - `GetAllCustomers` — returns entire list.

### 4. Controllers

#### `CustomersController` (`/api/customers`)

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/api/customers/search?name=` | GET | Search customer by name (partial, case-insensitive) |
| `/api/customers/{id}` | GET | Get customer by ID |

- Both endpoints include basic input validation and return `400`/`404` as appropriate.
- A `TODO` comment marks `GetCustomer` for conversion to an **Agent Tool** (Step 5).

#### `HealthController` (`/`)

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/health` | GET | Returns status, message, and UTC timestamp |

---

## Configuration (`appsettings.json`)

| Key | Value | Status |
|-----|-------|--------|
| `ConnectionStrings:LocalDb` | LocalDB / `LegacyDb` | **Not used** — data is in-memory |
| `ApiSettings:ApiVersion` | `1.0.0` | Present but not referenced in code |
| `GitHubModels:ApiKey` | *(empty)* | Placeholder — not used yet |

---

## Dependencies

| Package | Version | Purpose |
|---------|---------|---------|
| `Swashbuckle.AspNetCore` | 6.4.0 | Swagger / OpenAPI generation |

No other NuGet packages. No Entity Framework, no authentication, no logging framework beyond the built-in defaults.

---

## Observations & Potential Issues

### What's Good
- Clean separation of concerns (Controller ↔ Service via interface).
- Dependency injection is properly configured.
- Input validation is present on controller actions.
- Swagger documentation is set up.

### Issues / Gaps

| # | Category | Detail |
|---|----------|--------|
| 1 | **Unused Model** | `Order` class is defined but never referenced in any service or controller. |
| 2 | **No Persistence** | Data lives in a `static List<Customer>` — lost on restart, shared across scopes (thread-safety risk). |
| 3 | **Connection String Unused** | `LocalDb` connection string in config is never consumed. |
| 4 | **Missing Endpoints** | `GetAllCustomers()` exists in the service but has **no controller action** exposing it. No Create/Update/Delete endpoints. |
| 5 | **No Order Endpoints** | No service or controller for the `Order` model. |
| 6 | **Static Data + Scoped DI** | Service is registered as `Scoped`, but the data is `static` — effectively a singleton list with no concurrency protection. |
| 7 | **No Authentication/Authorization** | All endpoints are open. |
| 8 | **No Unit Tests** | No test project exists. |
| 9 | **No Logging** | Controllers and services don't inject or use `ILogger`. |
| 10 | **`CreatedAt` uses `DateTime.Now`** | Seed data uses local time, while `HealthController` uses `DateTime.UtcNow` — inconsistent. |

---

## API Quick Reference

```
GET  /health                         → HealthResponse
GET  /api/customers/search?name=X    → Customer | 400 | 404
GET  /api/customers/{id}             → Customer | 400 | 404
```

---

## Suggested Next Steps (Modernization Path)

1. **Add a real data store** — Wire up Entity Framework Core with the existing `LocalDb` connection string.
2. **Expose `GetAllCustomers`** — Add a `GET /api/customers` endpoint.
3. **Implement CRUD** — Add Create, Update, Delete for customers.
4. **Build out Orders** — Create `IOrderService` + `OrdersController` to use the `Order` model.
5. **Add logging** — Inject `ILogger<T>` into controllers and services.
6. **Add tests** — Create an xUnit/NUnit project with unit and integration tests.
7. **Agent Tool conversion** — Follow the TODO in `CustomersController` to convert `GetCustomer` into an Agent Tool (Step 5 of the exercise).
8. **Security** — Add authentication/authorization middleware.

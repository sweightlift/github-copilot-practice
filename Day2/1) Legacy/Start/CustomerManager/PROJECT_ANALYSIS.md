# CustomerManager — Project Analysis

## Overview

| Item | Detail |
|------|--------|
| **Type** | ASP.NET Core Web API |
| **Framework** | .NET 8.0 |
| **Architecture** | Minimal API (single-file endpoints in Program.cs) |
| **Data Store** | In-memory static list (no real database) |
| **API Docs** | Swagger / Swashbuckle (dev only) |

A lightweight legacy-style REST API that manages customer data. Intended as a starting point for a refactoring / modernization exercise.

---

## Project Structure

```
CustomerManager/
├── Program.cs                      # App entry point, DI config & all Minimal API endpoints
├── CustomerManager.csproj          # Project file (.NET 8, Swashbuckle)
├── appsettings.json                # Config (connection string, API key placeholder)
├── Models/
│   └── DomainModels.cs             # Customer, Order, HealthResponse
└── Services/
    └── CustomerService.cs          # Business logic + in-memory data
```

---

## Key Components

### 1. Program.cs — Application Bootstrap & Endpoints

- Registers **Swagger** (title: *"Legacy API"*, v1.0.0).
- Registers `ICustomerService` → `CustomerService` as **Scoped**.
- Enables HTTPS redirection.
- Swagger UI is only exposed in the **Development** environment.
- **All endpoints defined inline as Minimal API** using `MapGet`, `MapPost`, `MapPut`, `MapDelete`.
- Customer endpoints grouped under `app.MapGroup("/api/customers")`.

### 2. Models (`DomainModels.cs`)

| Model | Fields | Notes |
|-------|--------|-------|
| `Customer` | `Id`, `Name`, `Email`, `CreatedAt` | Core entity |
| `Order` | `Id`, `CustomerId`, `Status`, `Amount`, `OrderDate` | **Defined but never used anywhere** |
| `HealthResponse` | `Status`, `Message`, `Timestamp` | DTO for health endpoint |

### 3. Services (`CustomerService.cs`)

- **Interface:** `ICustomerService` — `GetCustomer(int)`, `SearchCustomer(string)`, `GetAllCustomers()`, `AddCustomer(Customer)`, `UpdateCustomer(int, Customer)`, `DeleteCustomer(int)`
- **Implementation:** `CustomerService`
  - Uses a **static in-memory list** with 3 seed customers (John Doe, Jane Smith, Bob Wilson).
  - `GetCustomer` — lookup by ID (`FirstOrDefault`).
  - `SearchCustomer` — case-insensitive partial name match.
  - `GetAllCustomers` — returns entire list.
  - `AddCustomer` — auto-assigns next ID, sets `CreatedAt` to UTC now, appends to list.
  - `UpdateCustomer` — updates Name and Email for existing customer by ID.
  - `DeleteCustomer` — removes customer by ID, returns success/failure.

### 4. Endpoints (Minimal API in Program.cs)

#### Customer Endpoints (`/api/customers`)

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/api/customers` | GET | Get all customers |
| `/api/customers/search?name=` | GET | Search customer by name (partial, case-insensitive) |
| `/api/customers/{id}` | GET | Get customer by ID |
| `/api/customers` | POST | Add a new customer (JSON body: `name`, `email`) |
| `/api/customers/{id}` | PUT | Update customer by ID (JSON body: `name`, `email`) |
| `/api/customers/{id}` | DELETE | Delete customer by ID |

- All endpoints include input validation and return `400`/`404` as appropriate.
- POST returns `201 Created` with a `Location` header.
- DELETE returns `204 No Content` on success.
- A `TODO` comment marks `GetCustomer` for conversion to an **Agent Tool** (Step 5).

#### Health Endpoint

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
| 4 | **~~Missing Endpoints~~** | ~~`GetAllCustomers()` exists in the service but has no controller action. No Create/Update/Delete.~~ **RESOLVED** — Full CRUD now implemented. |
| 5 | **No Order Endpoints** | No service or controller for the `Order` model. |
| 6 | **Static Data + Scoped DI** | Service is registered as `Scoped`, but the data is `static` — effectively a singleton list with no concurrency protection. |
| 7 | **No Authentication/Authorization** | All endpoints are open. |
| 8 | **No Unit Tests** | No test project exists. |
| 9 | **No Logging** | Controllers and services don't inject or use `ILogger`. |
| 10 | **`CreatedAt` uses `DateTime.Now`** | Seed data uses local time, while `HealthController` uses `DateTime.UtcNow` — inconsistent. |

---

## API Quick Reference

```
GET     /health                         → HealthResponse
GET     /api/customers                  → List<Customer>
GET     /api/customers/search?name=X    → Customer | 400 | 404
GET     /api/customers/{id}             → Customer | 400 | 404
POST    /api/customers                  → 201 Created + Customer | 400
PUT     /api/customers/{id}             → Customer | 400 | 404
DELETE  /api/customers/{id}             → 204 No Content | 400 | 404
```

---

## Suggested Next Steps (Modernization Path)

1. **Add a real data store** — Wire up Entity Framework Core with the existing `LocalDb` connection string.
2. ~~**Expose `GetAllCustomers`** — Add a `GET /api/customers` endpoint.~~ ✅ Done
3. ~~**Implement CRUD** — Add Create, Update, Delete for customers.~~ ✅ Done
4. **Build out Orders** — Create `IOrderService` + `OrdersController` to use the `Order` model.
5. **Add logging** — Inject `ILogger<T>` into controllers and services.
6. **Add tests** — Create an xUnit/NUnit project with unit and integration tests.
7. **Agent Tool conversion** — Follow the TODO in `CustomersController` to convert `GetCustomer` into an Agent Tool (Step 5 of the exercise).
8. **Security** — Add authentication/authorization middleware.

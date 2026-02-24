# CustomerManager — Project Analysis

## Overview

| Item | Detail |
|------|--------|
| **Type** | ASP.NET Core Web API |
| **Framework** | .NET 9.0 |
| **Architecture** | Minimal API (single-file endpoints in Program.cs) + AG-UI Agent |
| **Data Store** | In-memory static list (no real database) |
| **API Docs** | Swagger / Swashbuckle (dev only) |
| **AI Agent (Legacy)** | Azure.AI.Inference SDK + GitHub Models (`/api/chat`) |
| **AI Agent (AG-UI)** | Microsoft Agent Framework + CopilotKit (`/agent`) |
| **Frontend** | Next.js 15 + CopilotKit 1.51.4 (`customer-manager-web/`) |

A lightweight legacy-style REST API that manages customer data, enhanced with two AI-powered conversational agents: a legacy Azure.AI.Inference endpoint (`/api/chat`) and a modern AG-UI protocol endpoint (`/agent`) powered by the Microsoft Agent Framework with CopilotKit frontend. Intended as a starting point for a refactoring / modernization exercise.

---

## Project Structure

```
CustomerManager/
├── Program.cs                      # App entry point, DI config, Minimal API endpoints, AG-UI Agent + legacy chat
├── CustomerManager.csproj          # Project file (.NET 9, Swashbuckle, Azure.AI.Inference, AG-UI, OpenAI)
├── appsettings.json                # Config (connection string, API key, GitHub Models settings)
├── Models/
│   └── DomainModels.cs             # Customer, Order, HealthResponse, ChatMessage, ChatRequest, ChatResponse
├── Plugins/
│   └── CustomerPlugin.cs           # CustomerToolDefinitions (tool schemas) + CustomerToolDispatcher (executor)
├── Services/
│   └── CustomerService.cs          # Business logic + in-memory data
└── customer-manager-web/           # Next.js 15 + CopilotKit frontend
    ├── src/app/
    │   ├── api/copilotkit/route.ts  # CopilotKit Runtime → AG-UI bridge
    │   ├── layout.tsx              # Root layout with CopilotKit provider
    │   └── page.tsx                # CopilotSidebar chat UI + feature cards
    ├── package.json
    └── next.config.ts
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
- **Legacy AI Agent Chat endpoint** (`POST /api/chat`) — creates an Azure.AI.Inference `ChatCompletionsClient` connected to GitHub Models (`https://models.github.ai/inference`), with tool definitions for manual function calling.
- **AG-UI Agent endpoint** (`POST /agent`) — uses Microsoft Agent Framework `ChatClientAgent` with `AIFunctionFactory.Create()` tools, streamed via SSE. Mapped via `app.MapAGUI("/agent", agent)`.
- CORS enabled for `localhost:3000` and `localhost:3333` for the CopilotKit frontend.
- Chat endpoint includes **retry logic** (up to 3 attempts with exponential backoff) for transient network failures.
- Uses `SocketsHttpHandler` with custom SSL validation to handle corporate proxy/certificate issues.
- Tool-calling loop: checks `CompletionsFinishReason.ToolCalls` → dispatches via `CustomerToolDispatcher` → sends results back → repeats until final text response.

### 2. Models (`DomainModels.cs`)

| Model | Fields | Notes |
|-------|--------|-------|
| `Customer` | `Id`, `Name`, `Email`, `CreatedAt` | Core entity |
| `Order` | `Id`, `CustomerId`, `Status`, `Amount`, `OrderDate` | **Defined but never used anywhere** |
| `HealthResponse` | `Status`, `Message`, `Timestamp` | DTO for health endpoint |
| `ChatMessage` | `Role`, `Content` | Chat history entry (user/assistant) |
| `ChatRequest` | `Message`, `History` | Inbound chat request with optional history |
| `ChatResponse` | `Reply`, `Timestamp` | Agent's response with timestamp |

### 3. Plugins (`CustomerPlugin.cs`) — AI Agent Tools

Refactored into two static classes:

- **`CustomerToolDefinitions`** — Provides `ChatCompletionsToolDefinition` + `FunctionDefinition` schemas for each tool. These are passed to the `ChatCompletionsClient` so the LLM knows which functions are available.
- **`CustomerToolDispatcher`** — Static executor that maps tool-call names to `ICustomerService` methods and returns JSON results.

The tool-calling flow is manual: the chat endpoint checks `CompletionsFinishReason.ToolCalls`, dispatches each call via `CustomerToolDispatcher`, appends results as `ChatRequestToolMessage`, and loops until the LLM returns a final text response.

> **Note:** The AG-UI agent (`/agent`) uses a separate tool registration via `AIFunctionFactory.Create()` in `Program.cs` — the `CustomerPlugin.cs` definitions are only used by the legacy `/api/chat` endpoint.

| Tool Name | Description |
|-----------|-------------|
| `get_all_customers` | Returns the full customer list |
| `get_customer_by_id` | Lookup by integer ID |
| `search_customer` | Partial name search (case-insensitive) |
| `add_customer` | Create a new customer (name + email) |
| `update_customer` | Update name/email by ID |
| `delete_customer` | Remove a customer by ID |

### 4. Services (`CustomerService.cs`)

- **Interface:** `ICustomerService` — `GetCustomer(int)`, `SearchCustomer(string)`, `GetAllCustomers()`, `AddCustomer(Customer)`, `UpdateCustomer(int, Customer)`, `DeleteCustomer(int)`
- **Implementation:** `CustomerService`
  - Uses a **static in-memory list** with 3 seed customers (John Doe, Jane Smith, Bob Wilson).
  - `GetCustomer` — lookup by ID (`FirstOrDefault`).
  - `SearchCustomer` — case-insensitive partial name match.
  - `GetAllCustomers` — returns entire list.
  - `AddCustomer` — auto-assigns next ID, sets `CreatedAt` to UTC now, appends to list.
  - `UpdateCustomer` — updates Name and Email for existing customer by ID.
  - `DeleteCustomer` — removes customer by ID, returns success/failure.

### 5. Endpoints (Minimal API in Program.cs)

#### Customer Endpoints (`/api/customers`) — REST

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
- ~~A `TODO` comment marks `GetCustomer` for conversion to an **Agent Tool** (Step 5).~~ **RESOLVED** — All CRUD operations are now exposed as Agent Tools via `CustomerPlugin`.

#### AI Agent Chat Endpoint

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/api/chat` | POST | Natural language chat with AI agent (legacy — Azure.AI.Inference) |

#### AG-UI Agent Endpoint (Microsoft Agent Framework)

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/agent` | POST | AG-UI protocol endpoint — SSE streaming, CopilotKit-compatible |

- Uses `ChatClientAgent` from `Microsoft.Agents.AI` with `OpenAIClient` → `IChatClient`.
- 6 customer tools registered via `AIFunctionFactory.Create()` (same CRUD operations as legacy).
- Protocol: AG-UI (Server-Sent Events) — emits `RUN_STARTED`, `TOOL_CALL_START/ARGS/END`, `TOOL_CALL_RESULT`, `TEXT_MESSAGE_START/CONTENT/END`, `RUN_FINISHED`.
- Connected to CopilotKit via `HttpAgent` in the Next.js runtime route.

- Request body: `{ "message": "...", "history": [{ "role": "user|assistant", "content": "..." }] }`
- Uses **GitHub Models** (`openai/gpt-4o-mini` via `https://models.github.ai/inference`).
- Agent is powered by **Azure.AI.Inference SDK** with `ChatCompletionsClient` + `AzureKeyCredential`.
- Tool calling is manual: the LLM returns `CompletionsFinishReason.ToolCalls`, the endpoint dispatches via `CustomerToolDispatcher`, and loops until final text.
- Includes **retry with backoff** (3 attempts) for transient `HttpRequestException` / `HttpIOException` errors.
- Returns structured `502` JSON error (`{ error, detail, attempt }`) when all retries are exhausted.

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
| `GitHubModels:ApiKey` | *(user-provided)* | GitHub Personal Access Token with `models:read` permission |
| `GitHubModels:ModelId` | `openai/gpt-4o-mini` (default) | Optional — override the LLM model for the AI agent |

---

## Dependencies

| Package | Version | Purpose |
|---------|---------|---------|
| `Swashbuckle.AspNetCore` | 6.4.0 | Swagger / OpenAPI generation |
| `Azure.AI.Inference` | 1.0.0-beta.5 | Azure AI Inference SDK — `ChatCompletionsClient`, tool definitions, manual tool-calling loop (legacy `/api/chat`) |
| `Microsoft.Agents.AI.Hosting.AGUI.AspNetCore` | 1.0.0-preview.251110.1 | Microsoft Agent Framework — AG-UI protocol hosting (`MapAGUI`) |
| `Microsoft.Extensions.AI.OpenAI` | 9.10.2-preview.1.25552.1 | `IChatClient` abstraction + `AIFunctionFactory` for tool registration |
| `OpenAI` | 2.6.0 | OpenAI .NET client (transitive via `Microsoft.Extensions.AI.OpenAI`) |

### Frontend Dependencies (customer-manager-web)

| Package | Version | Purpose |
|---------|---------|--------|
| `next` | 15.x | Next.js React framework |
| `@copilotkit/react-core` | 1.51.4 | CopilotKit React provider |
| `@copilotkit/react-ui` | 1.51.4 | CopilotSidebar chat component |
| `@copilotkit/runtime` | 1.51.4 | CopilotKit Runtime (API route handler) |
| `@ag-ui/client` | 0.0.45 | AG-UI HTTP agent client |

No Entity Framework, no authentication, no logging framework beyond the built-in defaults.

---

## Frontend (`customer-manager-web/`)

A **Next.js 15** application providing a CopilotKit-powered chat sidebar that communicates with the .NET backend via the AG-UI protocol.

### Key Files

| File | Purpose |
|------|--------|
| `src/app/api/copilotkit/route.ts` | CopilotKit Runtime API route — creates `HttpAgent` pointing to `http://localhost:5000/agent`, bridges CopilotKit ↔ AG-UI |
| `src/app/layout.tsx` | Root layout with `<CopilotKit>` provider wrapping all pages |
| `src/app/page.tsx` | Main page with `<CopilotSidebar>` (default open), feature cards, and architecture info |

### How It Works
1. User types a message in the `CopilotSidebar` chat widget.
2. CopilotKit sends `POST /api/copilotkit` to the Next.js runtime route.
3. The runtime route forwards the request to the .NET AG-UI agent at `http://localhost:5000/agent`.
4. The agent processes the message, potentially calling customer tools, and streams results back via SSE.
5. CopilotKit renders the streamed response in real-time.

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
POST    /api/chat                       → ChatResponse { reply, timestamp } | 400
```

---

## Suggested Next Steps (Modernization Path)

1. **Add a real data store** — Wire up Entity Framework Core with the existing `LocalDb` connection string.
2. ~~**Expose `GetAllCustomers`** — Add a `GET /api/customers` endpoint.~~ ✅ Done
3. ~~**Implement CRUD** — Add Create, Update, Delete for customers.~~ ✅ Done
4. **Build out Orders** — Create `IOrderService` + `OrdersController` to use the `Order` model.
5. **Add logging** — Inject `ILogger<T>` into controllers and services.
6. **Add tests** — Create an xUnit/NUnit project with unit and integration tests.
7. ~~**Agent Tool conversion** — Follow the TODO in `CustomersController` to convert `GetCustomer` into an Agent Tool (Step 5 of the exercise).~~ ✅ Done — All CRUD exposed via `CustomerToolDefinitions` + `CustomerToolDispatcher` + `ChatCompletionsClient`.
8. **Security** — Add authentication/authorization middleware.
9. ~~**Streaming chat** — Add SSE/streaming support for the chat endpoint.~~ ✅ Done — AG-UI protocol (`/agent`) provides full SSE streaming via CopilotKit.
10. **Persistent chat history** — Store conversation history in a database.

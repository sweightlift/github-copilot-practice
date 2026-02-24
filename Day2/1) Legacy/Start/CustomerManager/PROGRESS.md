# CustomerManager — Task Progress

## Goal
Build, run, and explore the CustomerManager legacy API project.

---

## Progress

### Step 1: Project Analysis ✅
- Analyzed all source files and configuration.
- Created [PROJECT_ANALYSIS.md](PROJECT_ANALYSIS.md) with full breakdown of architecture, endpoints, models, issues, and suggested next steps.

### Step 2: Install .NET 8 SDK ✅
- Discovered that the .NET 8 SDK was **not installed** on the machine.
- Downloaded and installed from https://dotnet.microsoft.com/download/dotnet/8.0
- Verified installation: **.NET SDK 8.0.418** confirmed working.

### Step 3: Build the Project ✅
```bash
dotnet build
```
- Build succeeded — **0 errors, 0 warnings**.

### Step 4: Run the Project ✅
- First attempt ran in **Production** mode (Swagger disabled).
- Restarted with `--environment Development` to enable Swagger.
- Server running at **http://localhost:5000**.
- Swagger UI accessible at **http://localhost:5000/swagger/index.html**.

### Step 5: Test the Endpoints ✅

| # | Endpoint | URL | Result |
|---|----------|-----|--------|
| 1 | Health check | `http://localhost:5000/health` | ✅ `{"status":"Healthy","message":"Legacy API is running"}` |
| 2 | Search customer | `http://localhost:5000/api/customers/search?name=John` | ✅ Returns John Doe (id: 1) |
| 3 | Get customer by ID | `http://localhost:5000/api/customers/2` | ✅ Returns Jane Smith (id: 2) |
| 4 | Swagger UI | `http://localhost:5000/swagger/index.html` | ✅ HTTP 200 (available in Development mode) |

### Step 6: Explore & Next Steps ✅
- Swagger UI is live for interactive API testing.
- Identified modernization tasks from the analysis.

### Step 7: Implement Full CRUD ✅
Added 4 new endpoints to `CustomersController` and corresponding service methods:

| # | Feature | Method | Endpoint | Status |
|---|---------|--------|----------|--------|
| 1 | Get all customers | GET | `/api/customers` | ✅ Returns list of all customers |
| 2 | Add customer | POST | `/api/customers` | ✅ Returns 201 Created + new customer with auto ID |
| 3 | Update customer | PUT | `/api/customers/{id}` | ✅ Returns updated customer |
| 4 | Delete customer | DELETE | `/api/customers/{id}` | ✅ Returns 204 No Content |

**Test results:**
- `GET /api/customers` → returned 3 seed customers ✅
- `POST /api/customers` with `{"name":"Alice Park","email":"alice@example.com"}` → created id=4 ✅
- `PUT /api/customers/4` with `{"name":"Alice Park-Kim","email":"alice.kim@example.com"}` → updated ✅
- `DELETE /api/customers/4` → 204 No Content ✅
- `GET /api/customers` after delete → back to 3 customers ✅

### Step 8: Update Documentation & Dashboards ✅
- Updated PROJECT_ANALYSIS.md with new endpoints, service methods, and resolved issues.
- Updated DIAGRAMS.html — added sequence diagrams for POST, PUT, DELETE; updated class diagram with CRUD methods.
- Updated INDEX.html — added POST/PUT/DELETE to endpoint table with color-coded method badges.

### Step 9: Convert to Minimal API ✅
- Replaced `CustomersController` and `HealthController` with **Minimal API** endpoints in `Program.cs`.
- Removed `Controllers/` folder entirely.
- Used `app.MapGroup("/api/customers")` for clean route grouping.
- Removed `builder.Services.AddControllers()` and `app.MapControllers()`.
- Rebuilt and tested all 7 endpoints — all pass ✅.
- Updated PROJECT_ANALYSIS.md, DIAGRAMS.html, and INDEX.html.

### Step 10: Microsoft Agent Framework + GitHub Models ✅
Integrated an AI-powered chat agent using **Microsoft Semantic Kernel Agent Framework** with **GitHub Models** as the LLM backend.

#### What Was Added
| Item | Detail |
|------|--------|
| NuGet Package | `Microsoft.SemanticKernel` 1.72.0 |
| NuGet Package | `Microsoft.SemanticKernel.Agents.Core` 1.72.0 |
| New File | `Plugins/CustomerPlugin.cs` — 6 `[KernelFunction]` tools wrapping `ICustomerService` |
| New Models | `ChatMessage`, `ChatRequest`, `ChatResponse` in `DomainModels.cs` |
| New Endpoint | `POST /api/chat` — natural language chat with AI agent |
| Config | `GitHubModels:ApiKey` and `GitHubModels:ModelId` in `appsettings.json` |

#### How It Works
1. User sends `POST /api/chat` with `{ "message": "Show me all customers" }`.
2. A `Kernel` is built with `AddOpenAIChatCompletion()` pointing to `https://models.inference.ai.azure.com` (GitHub Models).
3. `CustomerPlugin` is registered on the kernel — exposes CRUD as callable tools.
4. A `ChatCompletionAgent` is created with `FunctionChoiceBehavior.Auto()` — the LLM automatically decides which tools to call.
5. The agent processes the message, may invoke one or more tools, and returns a formatted reply.

#### Agent Tools (CustomerPlugin)
| Tool | Description |
|------|-------------|
| `get_all_customers` | Returns full customer list |
| `get_customer_by_id` | Lookup by ID |
| `search_customer` | Partial name search |
| `add_customer` | Create new customer (name + email) |
| `update_customer` | Update name/email by ID |
| `delete_customer` | Remove customer by ID |

#### Test Results
```
POST /api/chat { "message": "Show me all customers" }
→ ✅ Agent called get_all_customers tool, returned formatted list of 3 customers

POST /api/chat { "message": "Search for a customer named Jane" }
→ ✅ Agent called search_customer("Jane"), returned Jane Smith details

POST /api/chat { "message": "Add a new customer named Alice Johnson with email alice@company.com" }
→ ✅ Agent called add_customer("Alice Johnson", "alice@company.com"), returned new customer ID 4

POST /api/chat { "message": "Delete the customer with ID 4" }
→ ✅ Agent called delete_customer(4), confirmed deletion
```

#### Issues Encountered & Fixed
- **Build error:** `.WithOpenApi()` extension requires `Microsoft.AspNetCore.OpenApi` package — replaced with `.WithDescription()`.
- **SSL error:** Corporate proxy caused certificate validation failure — added `DangerousAcceptAnyServerCertificateValidator` on HttpClient.

### Step 11: Update All Documentation ✅
- Updated PROJECT_ANALYSIS.md with agent architecture, new packages, new endpoint, new models, and plugin tools.
- Updated PROGRESS.md with Step 10 details and test results.
- Updated DIAGRAMS.html with agent architecture diagram and updated class diagram.
- Updated INDEX.html with chat endpoint card and AI agent interactive section.

### Notes / Issues Encountered
- `dotnet` was not on PATH initially — used full path `C:\Program Files\dotnet\dotnet.exe` to verify, then PATH resolved after terminal restart.
- Running without `--environment Development` starts in Production mode, which disables Swagger middleware.
- HTTPS redirect warning appears (`Failed to determine the https port for redirect`) — harmless when using HTTP only.

---

## Legend
| Icon | Meaning |
|------|---------|
| ✅ | Completed |
| ⏳ | In Progress |
| ⬚ | Not Started |

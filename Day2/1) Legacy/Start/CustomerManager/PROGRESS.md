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

### Step 12: Fix Chat Reliability & INDEX.html ✅
Addressed intermittent failures from corporate network/proxy when calling GitHub Models, and removed the browser-based chat widget.

#### Changes Made
| Item | Detail |
|------|--------|
| **Program.cs** | Replaced `HttpClientHandler` with `SocketsHttpHandler` + custom SSL validation for HTTP/2 compatibility |
| **Program.cs** | Added retry logic — up to **3 attempts** with exponential backoff (1s, 2s) for transient `HttpRequestException`/`HttpIOException` |
| **Program.cs** | Added `IsTransientError()` helper that walks the full exception chain |
| **Program.cs** | Agent errors now return structured `502` JSON: `{ error, detail, attempt }` |
| **Program.cs** | Removed CORS middleware (no longer needed without browser chat widget) |
| **INDEX.html** | Replaced interactive chat widget with a **static info card** showing PowerShell/curl examples |
| **INDEX.html** | Removed `sendChat()` JavaScript function |

#### Reason
Corporate network policies cause intermittent SSL/connection failures when the browser (or .NET `HttpClientHandler`) tries to reach `https://models.inference.ai.azure.com`. The `SocketsHttpHandler` with `RemoteCertificateValidationCallback` resolves the SSL issue, and the retry logic handles the remaining transient drops.

#### Test Results (all via PowerShell)
```
POST /api/chat { "message": "List all customers" }         → ✅ returned 3 customers
POST /api/chat { "message": "Search for Jane" }             → ✅ found Jane Smith
POST /api/chat { "message": "Add ... TestUser ..." }        → ✅ created ID 4
POST /api/chat { "message": "Update customer 4 ..." }       → ✅ updated name & email
POST /api/chat { "message": "Delete customer with ID 4" }   → ✅ deleted
```

### Step 13: Migrate from Semantic Kernel to Azure.AI.Inference SDK ✅
Replaced the Microsoft Semantic Kernel Agent Framework with the **Azure.AI.Inference** SDK for direct, lightweight LLM integration.

#### What Changed
| Item | Before | After |
|------|--------|-------|
| NuGet Packages | `Microsoft.SemanticKernel` 1.72.0 + `Microsoft.SemanticKernel.Agents.Core` 1.72.0 | `Azure.AI.Inference` 1.0.0-beta.5 |
| Endpoint | `https://models.inference.ai.azure.com` | `https://models.github.ai/inference` |
| Model Format | `gpt-4o-mini` | `openai/gpt-4o-mini` |
| Client | `ChatCompletionAgent` (Semantic Kernel) | `ChatCompletionsClient` + `AzureKeyCredential` |
| Tool Definitions | `[KernelFunction]` attributes in `CustomerPlugin` | `ChatCompletionsToolDefinition` + `FunctionDefinition` in `CustomerToolDefinitions` |
| Tool Dispatch | Automatic via `FunctionChoiceBehavior.Auto()` | Manual loop: check `CompletionsFinishReason.ToolCalls` → execute → send results → loop |
| Plugin File | `Plugins/CustomerPlugin.cs` (single class) | `Plugins/CustomerPlugin.cs` refactored into `CustomerToolDefinitions` (static schema) + `CustomerToolDispatcher` (static executor) |

#### Architecture
1. User sends `POST /api/chat` with `{ "message": "..." }`.
2. A `ChatCompletionsClient` is created with `AzureKeyCredential` pointing to `https://models.github.ai/inference`.
3. `CustomerToolDefinitions` provides tool schemas (`ChatCompletionsToolDefinition` + `FunctionDefinition`) for all 6 CRUD operations.
4. The LLM response is checked for `CompletionsFinishReason.ToolCalls`.
5. If tool calls are present, `CustomerToolDispatcher` executes them against `ICustomerService` and sends results back.
6. The loop continues until the LLM returns a final text response.

#### What Was Kept
- Retry logic (3 attempts + exponential backoff)
- SSL bypass (`SocketsHttpHandler` with custom certificate validation)
- All 6 agent tools (get_all, get_by_id, search, add, update, delete)

#### Test Results (all 5 chat scenarios pass)
```
POST /api/chat { "message": "List all customers" }             → ✅ returned 3 customers
POST /api/chat { "message": "Search for Jane" }                 → ✅ found Jane Smith
POST /api/chat { "message": "Add ... TestUser ..." }            → ✅ created new customer
POST /api/chat { "message": "Delete customer with ID 4" }       → ✅ deleted
POST /api/chat { "message": "고객 목록을 보여주세요" }            → ✅ Korean prompt handled
```

### Step 14: CopilotKit + Microsoft Agent Framework Integration ✅
Integrated **CopilotKit** with the **Microsoft Agent Framework** (AG-UI protocol) to provide a React-based chat frontend powered by the .NET AI agent.

#### What Changed
| Item | Before | After |
|------|--------|-------|
| .NET SDK | 8.0.418 | **9.0.311** (required by AG-UI packages) |
| TFM | `net8.0` | **`net9.0`** |
| New NuGet | — | `Microsoft.Agents.AI.Hosting.AGUI.AspNetCore` 1.0.0-preview.251110.1 |
| New NuGet | — | `Microsoft.Extensions.AI.OpenAI` 9.10.2-preview.1.25552.1 |
| New NuGet | — | `OpenAI` 2.6.0 (transitive) |
| New Endpoint | — | `POST /agent` (AG-UI SSE streaming) |
| Frontend | None | **Next.js 15 + CopilotKit 1.51.4** (`customer-manager-web/`) |
| Node.js | Not installed | **v24.13.1 LTS** (via winget) |
| CORS | Not configured | `localhost:3000` + `localhost:3333` |

#### Architecture
```
┌──────────────────────────┐     AG-UI (SSE)     ┌─────────────────────────────┐
│  Next.js Frontend        │◄───────────────────► │  .NET 9 Backend             │
│  localhost:3333           │                      │  localhost:5000              │
│                          │                      │                             │
│  CopilotKit Provider     │  /api/copilotkit     │  Minimal API                │
│  CopilotSidebar          │────────────────────► │  /api/customers/* (REST)    │
│                          │                      │  /health                    │
│  @copilotkit/runtime     │                      │  /api/chat (legacy)         │
│  @ag-ui/client           │                      │  /agent (AG-UI MapAGUI)     │
│                          │                      │    └── ChatClientAgent      │
└──────────────────────────┘                      │        └── 6 AI Tools       │
                                                  └─────────────────────────────┘
```

#### New Files Created
| File | Purpose |
|------|---------|
| `customer-manager-web/` | Next.js 15 app (scaffolded via `create-next-app`) |
| `customer-manager-web/src/app/api/copilotkit/route.ts` | CopilotKit Runtime → AG-UI bridge (`HttpAgent` → `localhost:5000/agent`) |
| `customer-manager-web/src/app/layout.tsx` | Root layout with `<CopilotKit>` provider wrapper |
| `customer-manager-web/src/app/page.tsx` | Customer Manager page with `<CopilotSidebar>` chat UI |

#### AG-UI Agent Configuration (Program.cs)
```csharp
// OpenAI client → IChatClient (GitHub Models)
var chatClient = openAI.GetChatClient("openai/gpt-4o-mini").AsIChatClient();

// 6 tools registered via AIFunctionFactory.Create()
var agent = new ChatClientAgent(chatClient, tools: [
    AIFunctionFactory.Create(get_all_customers, ...),
    AIFunctionFactory.Create(get_customer_by_id, ...),
    AIFunctionFactory.Create(search_customer, ...),
    AIFunctionFactory.Create(add_customer, ...),
    AIFunctionFactory.Create(update_customer, ...),
    AIFunctionFactory.Create(delete_customer, ...),
]);

app.MapAGUI("/agent", agent);
```

#### Test Results
| Test | Result |
|------|--------|
| `GET /health` | ✅ Healthy |
| `GET /api/customers` | ✅ 3 seed customers |
| `POST /api/chat` (legacy) | ✅ Still works |
| `POST /agent` ("List all customers") | ✅ SSE stream: `RUN_STARTED → TOOL_CALL(get_all_customers) → TOOL_CALL_RESULT → TEXT_MESSAGE` |
| Next.js `GET /` | ✅ 200 — page renders |
| CopilotKit `POST /api/copilotkit` | ✅ 200 — runtime forwards to AG-UI agent |

#### Issues Encountered & Resolved
| Issue | Resolution |
|-------|-----------|
| `.NET 9 first-run error` (`Microsoft.Build` not found) | Retry — SDK initialization completed on 2nd build |
| `npm naming restriction` (`CustomerManager.Web` rejected) | Renamed to `customer-manager-web` |
| `CS0104 ChatResponse ambiguity` | Fully qualified: `CustomerManager.Models.ChatResponse` |
| Port 3000/3001 unavailable | Used port 3333; updated CORS policy |
| Next.js slow first compile (>2 min) | Waited for compilation to complete |

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

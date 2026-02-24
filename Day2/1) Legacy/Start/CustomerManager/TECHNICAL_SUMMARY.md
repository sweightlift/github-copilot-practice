# CustomerManager — Technical Summary for LLM Reference

> **Purpose:** Comprehensive technical reference for LLMs and developers building similar projects.  
> **Date:** February 24, 2026  
> **Project Type:** Full-stack AI-powered CRUD application with natural language interface

---

## 1. High-Level Architecture

```
┌──────────────────────────┐     AG-UI (SSE)      ┌──────────────────────────────┐
│  Next.js 15 Frontend     │◄────────────────────► │  .NET 9 Backend              │
│  localhost:3333           │                       │  localhost:5000               │
│                          │                       │                              │
│  CopilotKit Provider     │  /api/copilotkit      │  Minimal API endpoints       │
│  CopilotSidebar UI       │──────────────────────►│  /api/customers/* (REST)     │
│  @copilotkit/runtime     │                       │  /health                     │
│  @ag-ui/client           │                       │  /api/chat (legacy AI)       │
│  Tailwind CSS            │                       │  /agent (AG-UI, SSE stream)  │
└──────────────────────────┘                       │    └─ ChatClientAgent        │
                                                   │       └─ 6 AIFunction tools  │
                                                   └──────────────┬───────────────┘
                                                                  │
                                                   ┌──────────────▼───────────────┐
                                                   │  GitHub Models (LLM)         │
                                                   │  openai/gpt-4o-mini          │
                                                   │  models.github.ai/inference  │
                                                   └─────────────────────────────┘
```

**Data flow:**
1. User types in `CopilotSidebar` → React sends `POST /api/copilotkit` to Next.js route
2. CopilotKit Runtime forwards to .NET AG-UI agent at `POST /agent` via `HttpAgent`
3. .NET `ChatClientAgent` sends prompt + tool schemas to GitHub Models LLM
4. LLM responds with tool calls (e.g., `get_all_customers`) → agent executes against `ICustomerService`
5. Tool results sent back to LLM → LLM generates final natural language response
6. Response streamed to frontend via AG-UI SSE protocol (`RUN_STARTED → TOOL_CALL → TEXT_MESSAGE → RUN_FINISHED`)

---

## 2. Technology Stack

### Backend (.NET)

| Technology | Version | Purpose |
|-----------|---------|---------|
| .NET SDK | 9.0.311 | Runtime & build toolchain |
| ASP.NET Core | 9.0 | Web framework (Minimal API) |
| `Azure.AI.Inference` | 1.0.0-beta.5 | Legacy AI chat (`/api/chat`) — `ChatCompletionsClient` with manual tool-calling loop |
| `Microsoft.Agents.AI.Hosting.AGUI.AspNetCore` | 1.0.0-preview.251110.1 | AG-UI protocol hosting — `AddAGUI()`, `MapAGUI()`, SSE streaming |
| `Microsoft.Extensions.AI.OpenAI` | 9.10.2-preview.1.25552.1 | `IChatClient` abstraction, `AIFunctionFactory.Create()` for tool registration |
| `OpenAI` | 2.6.0 | OpenAI .NET client (transitive dependency) |
| `Swashbuckle.AspNetCore` | 6.4.0 | Swagger/OpenAPI generation |

### Frontend (Node.js / React)

| Technology | Version | Purpose |
|-----------|---------|---------|
| Node.js | v24.13.1 LTS | JavaScript runtime |
| npm | 11.8.0 | Package manager |
| Next.js | 16.1.6 | React framework (App Router) |
| React | 19.2.3 | UI library |
| `@copilotkit/react-core` | 1.51.4 | CopilotKit React provider |
| `@copilotkit/react-ui` | 1.51.4 | `CopilotSidebar` chat component |
| `@copilotkit/runtime` | 1.51.4 | CopilotKit Runtime (Next.js API route handler) |
| `@ag-ui/client` | 0.0.45 | `HttpAgent` — bridges CopilotKit to AG-UI protocol |
| Tailwind CSS | 4.x | Utility-first CSS |
| TypeScript | 5.x | Type safety |

### LLM Provider

| Item | Value |
|------|-------|
| Provider | GitHub Models |
| Model | `openai/gpt-4o-mini` |
| Endpoint | `https://models.github.ai/inference` |
| Auth | GitHub Personal Access Token (PAT) with `models:read` |
| Protocol | OpenAI-compatible Chat Completions API |

---

## 3. Project Structure

```
CustomerManager/
├── Program.cs                          # 299 lines — DI, Minimal API, AI agents, CORS
├── CustomerManager.csproj              # .NET 9, 4 NuGet packages
├── appsettings.json                    # Config (GitHub Models key, endpoint, model)
├── Models/
│   └── DomainModels.cs                 # Customer, Order, HealthResponse, ChatMessage/Request/Response
├── Plugins/
│   └── CustomerPlugin.cs              # CustomerToolDefinitions + CustomerToolDispatcher (legacy /api/chat)
├── Services/
│   └── CustomerService.cs             # ICustomerService + in-memory CRUD (static list, 3 seed records)
├── customer-manager-web/               # Next.js frontend
│   ├── package.json                    # Dependencies (CopilotKit, AG-UI, Next.js, React)
│   ├── next.config.ts
│   ├── src/app/
│   │   ├── api/copilotkit/route.ts    # CopilotKit Runtime → AG-UI bridge
│   │   ├── layout.tsx                  # Root layout with <CopilotKit> provider
│   │   ├── page.tsx                    # CopilotSidebar + feature cards
│   │   └── globals.css
│   └── ...
├── PROJECT_ANALYSIS.md
├── PROGRESS.md
├── COPILOTKIT_PLAN.md
├── FEEDBACK.md
├── DIAGRAMS.html                       # Mermaid-powered architecture diagrams
└── INDEX.html                          # Project dashboard
```

---

## 4. API Endpoints

### REST (Minimal API)

| Method | Endpoint | Description |
|--------|----------|-------------|
| `GET` | `/health` | Health check — returns `{ status, message, timestamp }` |
| `GET` | `/api/customers` | Get all customers |
| `GET` | `/api/customers/search?name=X` | Search by partial name (case-insensitive) |
| `GET` | `/api/customers/{id}` | Get customer by ID |
| `POST` | `/api/customers` | Add new customer `{ name, email }` → 201 Created |
| `PUT` | `/api/customers/{id}` | Update customer `{ name, email }` |
| `DELETE` | `/api/customers/{id}` | Delete customer → 204 No Content |

### AI Endpoints

| Method | Endpoint | Protocol | Description |
|--------|----------|----------|-------------|
| `POST` | `/api/chat` | JSON request/response | Legacy AI agent (Azure.AI.Inference + manual tool loop) |
| `POST` | `/agent` | AG-UI (SSE stream) | Modern AI agent (Microsoft Agent Framework + CopilotKit) |

---

## 5. Key Implementation Patterns

### 5.1 Two AI Agent Approaches (Side by Side)

#### Legacy: Azure.AI.Inference (`/api/chat`)

```csharp
// Manual tool-calling loop
var client = new ChatCompletionsClient(endpoint, credential, options);
var tools = CustomerToolDefinitions.GetTools(); // ChatCompletionsToolDefinition list

for (int round = 0; round < maxToolRounds; round++)
{
    var response = await client.CompleteAsync(options);
    if (result.FinishReason == CompletionsFinishReason.ToolCalls)
    {
        messages.Add(new ChatRequestAssistantMessage(result));
        foreach (var toolCall in result.ToolCalls)
        {
            var toolResult = CustomerToolDispatcher.Execute(
                toolCall.Function.Name, toolCall.Function.Arguments, svc);
            messages.Add(new ChatRequestToolMessage(toolCallId: toolCall.Id, content: toolResult));
        }
        continue; // next round
    }
    return Results.Ok(new ChatResponse { Reply = result.Content });
}
```

**Key characteristics:**
- Manual tool-call detection (`CompletionsFinishReason.ToolCalls`)
- Manual dispatch via switch-based `CustomerToolDispatcher`
- Tool schemas defined as raw JSON in `ChatCompletionsToolDefinition`
- Retry logic with exponential backoff (3 attempts)
- Request/response (no streaming)

#### Modern: Microsoft Agent Framework (`/agent`)

```csharp
// Declarative tool registration + automatic AG-UI streaming
var chatClient = openAI.GetChatClient(modelId).AsIChatClient();

var agentTools = new List<AITool>
{
    AIFunctionFactory.Create(
        () => JsonSerializer.Serialize(svc.GetAllCustomers()),
        "get_all_customers", "Get the full list of all customers"),
    AIFunctionFactory.Create(
        ([Description("The customer ID")] int id) => JsonSerializer.Serialize(svc.GetCustomer(id)),
        "get_customer_by_id", "Get a single customer by their ID"),
    // ... 4 more tools
};

var agent = new ChatClientAgent(chatClient, name: "CustomerAgent",
    description: "...", tools: agentTools);

app.MapAGUI("/agent", agent); // One-liner endpoint mapping
```

**Key characteristics:**
- `AIFunctionFactory.Create()` — type-safe tool registration with `[Description]` attributes
- `ChatClientAgent` handles tool-calling loop automatically
- `MapAGUI()` provides SSE streaming out of the box
- AG-UI protocol events: `RUN_STARTED`, `TOOL_CALL_START/ARGS/END`, `TOOL_CALL_RESULT`, `TEXT_MESSAGE_START/CONTENT/END`, `RUN_FINISHED`

### 5.2 CopilotKit Integration Pattern

**Next.js API Route (`route.ts`):**
```typescript
import { CopilotRuntime, ExperimentalEmptyAdapter, copilotRuntimeNextJSAppRouterEndpoint } from "@copilotkit/runtime";
import { HttpAgent } from "@ag-ui/client";

const runtime = new CopilotRuntime({
  agents: {
    customer_agent: new HttpAgent({ url: "http://localhost:5000/agent" }),
  },
});

export const POST = async (req: NextRequest) => {
  const { handleRequest } = copilotRuntimeNextJSAppRouterEndpoint({
    runtime,
    serviceAdapter: new ExperimentalEmptyAdapter(),
    endpoint: "/api/copilotkit",
  });
  return handleRequest(req);
};
```

**React Layout (`layout.tsx`):**
```tsx
import { CopilotKit } from "@copilotkit/react-core";
import "@copilotkit/react-ui/styles.css";

<CopilotKit runtimeUrl="/api/copilotkit" agent="customer_agent">
  {children}
</CopilotKit>
```

**Chat UI (`page.tsx`):**
```tsx
import { CopilotSidebar } from "@copilotkit/react-ui";

<CopilotSidebar
  labels={{ title: "Customer Manager Agent", initial: "..." }}
  defaultOpen={true}
  clickOutsideToClose={false}
/>
```

### 5.3 CORS Configuration

```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
        policy.WithOrigins("http://localhost:3000", "http://localhost:3333")
              .AllowAnyHeader()
              .AllowAnyMethod());
});
app.UseCors("AllowFrontend");
```

### 5.4 SSL Bypass for Corporate Environments

```csharp
var handler = new SocketsHttpHandler
{
    SslOptions = new System.Net.Security.SslClientAuthenticationOptions
    {
        RemoteCertificateValidationCallback = (_, _, _, _) => true
    }
};
var httpClient = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(60) };
```

### 5.5 Retry Logic with Exponential Backoff

```csharp
const int maxRetries = 3;
for (int attempt = 1; attempt <= maxRetries; attempt++)
{
    try { /* ... LLM call ... */ }
    catch (Exception ex) when (attempt < maxRetries && IsTransientError(ex))
    {
        await Task.Delay(1000 * attempt); // 1s, 2s backoff
    }
}

static bool IsTransientError(Exception ex)
{
    for (var e = ex; e != null; e = e.InnerException)
    {
        if (e is HttpRequestException or HttpIOException or IOException) return true;
        if (e.Message.Contains("ended prematurely", StringComparison.OrdinalIgnoreCase)) return true;
    }
    return false;
}
```

### 5.6 Service Layer Pattern

```csharp
// Interface-based DI
public interface ICustomerService
{
    Customer? GetCustomer(int id);
    Customer? SearchCustomer(string name);
    List<Customer> GetAllCustomers();
    Customer AddCustomer(Customer customer);
    Customer? UpdateCustomer(int id, Customer customer);
    bool DeleteCustomer(int id);
}

// Registration
builder.Services.AddScoped<ICustomerService, CustomerService>();
```

---

## 6. AG-UI Protocol Details

The AG-UI (Agent-User Interaction) protocol is an SSE-based streaming standard for real-time communication between AI agents and frontends.

### SSE Event Sequence (typical tool-calling flow)

```
event: RUN_STARTED
data: {"type":"RUN_STARTED","threadId":"...","runId":"..."}

event: TOOL_CALL_START
data: {"type":"TOOL_CALL_START","toolCallId":"...","toolCallName":"get_all_customers"}

event: TOOL_CALL_ARGS
data: {"type":"TOOL_CALL_ARGS","toolCallId":"...","delta":"{}"}

event: TOOL_CALL_END
data: {"type":"TOOL_CALL_END","toolCallId":"..."}

event: TOOL_CALL_RESULT
data: {"type":"TOOL_CALL_RESULT","toolCallId":"...","result":"[{\"Id\":1,...},...]"}

event: TEXT_MESSAGE_START
data: {"type":"TEXT_MESSAGE_START","messageId":"..."}

event: TEXT_MESSAGE_CONTENT
data: {"type":"TEXT_MESSAGE_CONTENT","messageId":"...","delta":"Here are all..."}

event: TEXT_MESSAGE_END
data: {"type":"TEXT_MESSAGE_END","messageId":"..."}

event: RUN_FINISHED
data: {"type":"RUN_FINISHED","threadId":"...","runId":"..."}
```

### Required NuGet Packages for AG-UI

```xml
<PackageReference Include="Microsoft.Agents.AI.Hosting.AGUI.AspNetCore" Version="1.0.0-preview.251110.1" />
<PackageReference Include="Microsoft.Extensions.AI.OpenAI" Version="9.10.2-preview.1.25552.1" />
```

### Minimal Setup Code

```csharp
builder.Services.AddAGUI();

var openAIClient = new OpenAIClient(
    new ApiKeyCredential(token),
    new OpenAIClientOptions { Endpoint = new Uri("https://models.github.ai/inference") });

var chatClient = openAIClient.GetChatClient("openai/gpt-4o-mini").AsIChatClient();

var agent = new ChatClientAgent(chatClient, name: "MyAgent",
    description: "System prompt here...",
    tools: [ AIFunctionFactory.Create(...), ... ]);

app.MapAGUI("/agent", agent);
```

---

## 7. GitHub Models Setup

GitHub Models provides free-tier access to OpenAI models via a GitHub PAT.

| Item | Value |
|------|-------|
| Endpoint | `https://models.github.ai/inference` |
| Auth | `Authorization: Bearer <github_pat_...>` |
| Protocol | OpenAI Chat Completions API compatible |
| Available Models | `openai/gpt-4o-mini`, `openai/gpt-4o`, `meta/llama-...`, etc. |
| Token Format | GitHub PAT with `models:read` permission |

### Usage with Azure.AI.Inference

```csharp
var client = new ChatCompletionsClient(
    new Uri("https://models.github.ai/inference"),
    new AzureKeyCredential("github_pat_..."));
```

### Usage with OpenAI .NET Client

```csharp
var client = new OpenAIClient(
    new ApiKeyCredential("github_pat_..."),
    new OpenAIClientOptions { Endpoint = new Uri("https://models.github.ai/inference") });
```

---

## 8. Domain Model

```csharp
public class Customer
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public string? Email { get; set; }
    public DateTime CreatedAt { get; set; }
}
```

- **Storage:** Static in-memory `List<Customer>` (no database)
- **Seed data:** 3 customers (John Doe, Jane Smith, Bob Wilson)
- **ID generation:** `Max(Id) + 1`
- **DI lifetime:** Scoped service, static data (effectively singleton data)

---

## 9. Evolution Timeline

| Step | What Was Done | Key Tech Added |
|------|---------------|----------------|
| 1 | Project analysis | — |
| 2 | Install .NET 8 SDK | .NET 8.0.418 |
| 3-6 | Build, run, test, explore | Swagger UI |
| 7 | Full CRUD implementation | 4 new endpoints |
| 8 | Documentation & dashboards | Mermaid diagrams, HTML dashboard |
| 9 | Convert to Minimal API | Removed controllers, `MapGroup()` |
| 10 | Semantic Kernel AI agent | `Microsoft.SemanticKernel` 1.72.0 |
| 11 | Documentation update | — |
| 12 | Reliability fixes | Retry logic, `SocketsHttpHandler`, error handling |
| 13 | Migrate to Azure.AI.Inference | `Azure.AI.Inference` 1.0.0-beta.5, manual tool loop |
| 14 | CopilotKit + Microsoft Agent Framework | .NET 9, AG-UI, CopilotKit, Next.js frontend |

---

## 10. Techniques & Patterns Reference

| Pattern | Implementation | Where |
|---------|---------------|-------|
| **Minimal API** | `app.MapGet/Post/Put/Delete()` with `MapGroup()` | Program.cs |
| **Dependency Injection** | `AddScoped<ICustomerService, CustomerService>()` | Program.cs |
| **LLM Tool Calling (Manual)** | `CompletionsFinishReason.ToolCalls` → dispatch → loop | Program.cs `/api/chat` |
| **LLM Tool Calling (Auto)** | `AIFunctionFactory.Create()` + `ChatClientAgent` | Program.cs `/agent` |
| **AG-UI Protocol** | `AddAGUI()` + `MapAGUI()` — SSE streaming | Program.cs |
| **CopilotKit Bridge** | `HttpAgent` → `CopilotRuntime` → Next.js route | route.ts |
| **React Chat UI** | `<CopilotKit>` provider + `<CopilotSidebar>` | layout.tsx, page.tsx |
| **Retry with Backoff** | 3 attempts, `Task.Delay(1000 * attempt)` | Program.cs |
| **Transient Error Detection** | Walk `InnerException` chain for `HttpRequestException` | Program.cs |
| **SSL Bypass** | `SocketsHttpHandler` + `RemoteCertificateValidationCallback` | Program.cs |
| **CORS** | `AddCors()` + `UseCors()` with specific origins | Program.cs |
| **Swagger** | `AddSwaggerGen()` + dev-only `UseSwagger()` | Program.cs |
| **Config Pattern** | `IConfiguration` with fallback to env vars | Program.cs |

---

## 11. How to Run

### Prerequisites

- .NET 9.0+ SDK
- Node.js 20+ LTS
- GitHub PAT with `models:read` permission

### Backend

```bash
cd CustomerManager
# Set API key (choose one method):
dotnet user-secrets set "GitHubModels:ApiKey" "github_pat_..."
# OR set env var: $env:GITHUB_TOKEN = "github_pat_..."

dotnet build
dotnet run --environment Development
# → http://localhost:5000 (Swagger: /swagger)
```

### Frontend

```bash
cd CustomerManager/customer-manager-web
npm install
npx next dev --port 3333
# → http://localhost:3333
```

### Quick Test

```powershell
# Health check
Invoke-RestMethod http://localhost:5000/health

# REST API
Invoke-RestMethod http://localhost:5000/api/customers

# Legacy AI chat
$body = @{ message = "List all customers" } | ConvertTo-Json
Invoke-RestMethod -Uri "http://localhost:5000/api/chat" -Method Post -Body $body -ContentType "application/json"

# AG-UI agent (raw SSE test)
$body = '{"messages":[{"role":"user","content":"List all customers"}],"runId":"test-1","threadId":"thread-1"}'
Invoke-WebRequest -Uri http://localhost:5000/agent -Method POST -Body $body -ContentType "application/json"
```

---

## 12. Key Decisions & Trade-offs

| Decision | Choice Made | Alternative Considered |
|----------|-------------|----------------------|
| .NET version | **9.0** (required by AG-UI packages) | 8.0 (original) |
| API style | **Minimal API** (no controllers) | Controller-based |
| Data storage | **In-memory static list** | EF Core + SQL |
| AI SDK | **Both** Azure.AI.Inference + Microsoft.Extensions.AI | Single SDK |
| Tool registration | **AIFunctionFactory.Create()** with lambdas | `[KernelFunction]` attributes (Semantic Kernel) |
| Frontend framework | **Next.js 15 + App Router** | Blazor, React (CRA) |
| Chat UI component | **CopilotSidebar** (side panel) | CopilotChat (inline), CopilotPopup (floating) |
| Agent protocol | **AG-UI (SSE)** | WebSocket, REST polling |
| Legacy endpoint kept | **Yes** (`/api/chat` preserved) | Removed to simplify |
| Frontend folder | **Nested** (`customer-manager-web/`) | Sibling folder |

---

## 13. Known Limitations

1. **No persistence** — Data lost on restart (in-memory static list)
2. **No authentication** — All endpoints are open
3. **No automated tests** — Manual testing only (PowerShell/curl)
4. **Unused `Order` model** — Defined in `DomainModels.cs` but never used
5. **Thread safety** — Static list with scoped DI, no concurrency protection
6. **Preview packages** — AG-UI and Microsoft.Extensions.AI.OpenAI are pre-release
7. **SSL bypass** — `RemoteCertificateValidationCallback` always returns true (dev only)
8. **Hardcoded URLs** — Frontend assumes backend at `localhost:5000`, backend assumes frontend at `localhost:3000/3333`

---

*This document is intended as a technical reference for LLMs and developers building similar AI-powered full-stack applications with .NET + CopilotKit + Microsoft Agent Framework.*

# CopilotKit + Microsoft Agent Framework Integration Plan

> **Goal**: Replace the current custom `/api/chat` endpoint with a proper  
> **AG-UI protocol** backend served by the Microsoft Agent Framework,  
> and add a **React (Next.js) frontend** powered by CopilotKit.

---

## Current State (as-is) — BEFORE integration

| Layer | Technology | Notes |
|-------|-----------|-------|
| Backend API | .NET 8 Minimal API | `http://localhost:5000` |
| AI Chat | `Azure.AI.Inference` + manual tool-calling loop | `/api/chat` endpoint in Program.cs |
| Tools | `CustomerToolDefinitions` + `CustomerToolDispatcher` | 6 tools (CRUD + search + list) |
| Frontend | None (PowerShell / Swagger testing only) | — |

---

## Achieved State ✅

| Layer | Technology | Notes |
|-------|-----------|-------|
| Backend API | **.NET 9** Minimal API | `http://localhost:5000` |
| AI Agent (AG-UI) | Microsoft Agent Framework + AG-UI ASP.NET Core hosting | `http://localhost:5000/agent` (same process) |
| Frontend | Next.js 15 + CopilotKit 1.51.4 | `http://localhost:3333` (port 3000/3001 were unavailable) |
| Protocol | AG-UI (Agent-User Interaction) via SSE | Standardized streaming between agent ↔ CopilotKit |
| Legacy Chat | Azure.AI.Inference (kept for backward compat) | `/api/chat` still works |

### Architecture Diagram

```
┌─────────────────────────┐      AG-UI (SSE)       ┌─────────────────────────────┐
│   Next.js Frontend      │◄──────────────────────► │  .NET Backend               │
│   (localhost:3333)      │                         │  (localhost:5000)            │
│                         │                         │                             │
│  CopilotKit Provider    │   /api/copilotkit       │  Minimal API endpoints      │
│  CopilotSidebar         │──────────────────────►  │  /api/customers/*           │
│                         │                         │  /health                    │
│                         │                         │                             │
│  @copilotkit/react-ui   │                         │  AG-UI Agent endpoint       │
│  @copilotkit/runtime    │                         │  /agent (MapAGUI)           │
│  @ag-ui/client          │                         │    └── ChatClientAgent      │
└─────────────────────────┘                         │        └── Customer Tools   │
                                                    └─────────────────────────────┘
```

---

## Step-by-Step Plan

### Step 1 — Upgrade to .NET 9 (required by AG-UI packages)

The `Microsoft.Agents.AI.Hosting.AGUI.AspNetCore` package targets .NET 9+.

- [x] Update `CustomerManager.csproj` → `<TargetFramework>net9.0</TargetFramework>`
- [x] Install .NET 9 SDK (9.0.311 via winget)
- [x] `dotnet build` — verified, 0 errors, 0 warnings

### Step 2 — Add Microsoft Agent Framework NuGet packages

```bash
dotnet add package Microsoft.Agents.AI.Hosting.AGUI.AspNetCore --version 1.0.0-preview.251110.1
dotnet add package Microsoft.Extensions.AI.OpenAI --version 9.10.2-preview.1.25552.1
dotnet add package OpenAI --version 2.6.0
```

- [x] Added 3 packages (`Microsoft.Agents.AI.Hosting.AGUI.AspNetCore` 1.0.0-preview.251110.1, `Microsoft.Extensions.AI.OpenAI` 9.10.2-preview.1.25552.1, `OpenAI` 2.6.0 as transitive)
- [x] Kept `Azure.AI.Inference` for backward-compat `/api/chat` endpoint. New AG-UI agent uses `Microsoft.Extensions.AI` `AIFunction` format.
- [x] `dotnet build` — 0 errors, resolved `ChatResponse` ambiguity with full qualification

### Step 3 — Create the AG-UI Agent endpoint in Program.cs

Add AG-UI hosting alongside the existing Minimal API:

```csharp
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Hosting.AGUI.AspNetCore;
using Microsoft.Extensions.AI;
using OpenAI;

// In service registration:
builder.Services.AddAGUI();

// Create the chat client (GitHub Models)
var githubToken = builder.Configuration["GitHubModels:ApiKey"]!;
var openAI = new OpenAIClient(
    new System.ClientModel.ApiKeyCredential(githubToken),
    new OpenAIClientOptions
    {
        Endpoint = new Uri("https://models.github.ai/inference")
    });
var chatClient = openAI.GetChatClient("openai/gpt-4o-mini").AsIChatClient();

// Create the agent with tools
var agent = new ChatClientAgent(
    chatClient,
    name: "CustomerAgent",
    description: "You are a helpful customer management assistant..."
);

// Map the AG-UI endpoint
app.MapAGUI("/agent", agent);
```

- [x] Registered `AddAGUI()` service in `Program.cs`
- [x] Created `OpenAIClient` → `IChatClient` via `AsIChatClient()` (GitHub Models endpoint)
- [x] Created `ChatClientAgent` with system prompt + 6 tools
- [x] Mapped AG-UI endpoint at `/agent` via `app.MapAGUI("/agent", agent)`
- [x] **Decision:** Kept existing `/api/chat` endpoint for backward compat / Swagger testing

### Step 4 — Register customer tools with the agent

The AG-UI agent needs access to customer management tools. Two approaches:

**Option A**: Convert existing `CustomerToolDefinitions` to `Microsoft.Extensions.AI` `AIFunction` format  
**Option B**: Use `[Description]` attributed methods and register them as `AIFunction` instances

```csharp
// Example tool registration with Microsoft.Extensions.AI
var agent = new ChatClientAgent(
    chatClient,
    name: "CustomerAgent",
    description: "...",
    tools: [
        AIFunctionFactory.Create((int id, ICustomerService svc) => svc.GetCustomer(id),
            "get_customer_by_id", "Get a single customer by their ID"),
        AIFunctionFactory.Create((ICustomerService svc) => svc.GetAllCustomers(),
            "get_all_customers", "Get the full list of all customers"),
        // ... more tools
    ]
);
```

- [x] **Decision:** Option B — `AIFunctionFactory.Create()` with `[Description]` lambdas
- [x] Registered all 6 customer tools (get_all, get_by_id, search, add, update, delete)
- [x] Verified tool-calling via AG-UI SSE protocol — full flow confirmed

### Step 5 — Scaffold the Next.js frontend

```bash
npx create-next-app@latest my-copilot-app
cd my-copilot-app
npm install @copilotkit/react-ui @copilotkit/react-core @copilotkit/runtime @ag-ui/client
```

- [x] Created Next.js app in nested folder `customer-manager-web/` (npm rejected `CustomerManager.Web` due to capital letters)
- [x] Installed `@copilotkit/react-ui@1.51.4`, `@copilotkit/react-core@1.51.4`, `@copilotkit/runtime@1.51.4`, `@ag-ui/client@0.0.45`
- [x] Runs on `http://localhost:3333` (ports 3000/3001 were in use). Node.js v24.13.1 LTS installed via winget.

### Step 6 — Setup CopilotKit Runtime (Next.js API route)

Create `app/api/copilotkit/route.ts`:

```typescript
import {
  CopilotRuntime,
  ExperimentalEmptyAdapter,
  copilotRuntimeNextJSAppRouterEndpoint,
} from "@copilotkit/runtime";
import { HttpAgent } from "@ag-ui/client";
import { NextRequest } from "next/server";

const serviceAdapter = new ExperimentalEmptyAdapter();

const runtime = new CopilotRuntime({
  agents: {
    customer_agent: new HttpAgent({ url: "http://localhost:5000/agent" }),
  },
});

export const POST = async (req: NextRequest) => {
  const { handleRequest } = copilotRuntimeNextJSAppRouterEndpoint({
    runtime,
    serviceAdapter,
    endpoint: "/api/copilotkit",
  });
  return handleRequest(req);
};
```

- [x] Created `src/app/api/copilotkit/route.ts` with `ExperimentalEmptyAdapter` + `CopilotRuntime` + `HttpAgent`
- [x] Pointed `HttpAgent` to `http://localhost:5000/agent`

### Step 7 — Configure CopilotKit Provider + Chat UI

**`app/layout.tsx`:**
```tsx
import { CopilotKit } from "@copilotkit/react-core";
import "@copilotkit/react-ui/styles.css";

export default function RootLayout({ children }: { children: React.ReactNode }) {
  return (
    <html lang="en">
      <body>
        <CopilotKit runtimeUrl="/api/copilotkit" agent="customer_agent">
          {children}
        </CopilotKit>
      </body>
    </html>
  );
}
```

**`app/page.tsx`:**
```tsx
"use client";
import { CopilotSidebar } from "@copilotkit/react-ui";

export default function Page() {
  return (
    <main>
      <CopilotSidebar
        labels={{
          title: "Customer Manager Agent",
          initial: "Hi! I can help you manage customers. Try asking me to list, search, add, update, or delete customers.",
        }}
      />
      <h1>Customer Manager</h1>
    </main>
  );
}
```

- [x] Wrapped app with `<CopilotKit runtimeUrl="/api/copilotkit" agent="customer_agent">` in `layout.tsx`
- [x] Added `<CopilotSidebar>` with customer management instructions + feature cards in `page.tsx`
- [x] Chat UI renders at `http://localhost:3333`

### Step 8 — End-to-end testing

- [x] Started .NET backend: `dotnet run --environment Development` → `http://localhost:5000`
- [x] Started Next.js frontend: `npx next dev -p 3333` → `http://localhost:3333`
- [x] Opened `http://localhost:3333` in browser
- [x] AG-UI SSE stream tested directly:
  - "List all customers" → `RUN_STARTED → TOOL_CALL(get_all_customers) → TOOL_CALL_RESULT(3 customers) → TEXT_MESSAGE` ✅
  - REST endpoints (`/health`, `/api/customers`) confirmed still working ✅
  - CopilotKit `/api/copilotkit` route returning 200 ✅

---

## Risks & Decisions

| # | Item | Decision Needed |
|---|------|----------------|
| 1 | **.NET 9 upgrade** | AG-UI packages require .NET 9+. Our project is .NET 8. Need SDK install + TFM bump. |
| 2 | **Keep `/api/chat`?** | Keep for backward compat / Swagger testing, or remove to simplify? |
| 3 | **Tool format migration** | Keep `Azure.AI.Inference` tool definitions or migrate to `Microsoft.Extensions.AI` `AIFunction`? |
| 4 | **SSL handler** | Corporate proxy SSL bypass — need to verify if `OpenAIClient` + AG-UI respects custom `HttpClient`. |
| 5 | **Same-process vs separate** | Host AG-UI in the same .NET process (`:5000/agent`) or a separate service (`:8000`)? Same-process is simpler. |
| 6 | **Frontend folder location** | Sibling folder `CustomerManager.Web/` or nested inside `CustomerManager/`? |

---

## Prerequisites Checklist

- [x] GitHub Personal Access Token (already in `appsettings.json`)
- [x] .NET 9.0.311 SDK (installed via `winget install Microsoft.DotNet.SDK.9`)
- [x] Node.js v24.13.1 LTS (installed via `winget install OpenJS.NodeJS.LTS`)
- [x] npm 11.8.0 (bundled with Node.js)

---

## File Changes Summary

| File | Action | Description |
|------|--------|-------------|
| `CustomerManager.csproj` | **Modified** ✅ | TFM → net9.0, added 3 NuGet packages |
| `Program.cs` | **Modified** ✅ | Added `AddAGUI()`, CORS, `ChatClientAgent` with 6 `AIFunctionFactory.Create()` tools, `MapAGUI("/agent", agent)` |
| `Plugins/CustomerPlugin.cs` | **Kept as-is** ✅ | Still used by legacy `/api/chat` endpoint |
| `customer-manager-web/` | **New folder** ✅ | Next.js 15 app with CopilotKit (renamed from `CustomerManager.Web` due to npm naming rules) |
| `customer-manager-web/src/app/api/copilotkit/route.ts` | **New** ✅ | CopilotKit Runtime → AG-UI bridge via `HttpAgent` |
| `customer-manager-web/src/app/layout.tsx` | **Modified** ✅ | CopilotKit provider wrapper + styles |
| `customer-manager-web/src/app/page.tsx` | **Rewritten** ✅ | CopilotSidebar chat UI + feature cards |

---

*Created: 2026-02-24 | Based on: https://docs.copilotkit.ai/microsoft-agent-framework/quickstart?agent=bring-your-own*

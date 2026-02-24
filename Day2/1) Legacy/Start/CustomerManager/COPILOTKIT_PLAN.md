# CopilotKit + Microsoft Agent Framework Integration Plan

> **Goal**: Replace the current custom `/api/chat` endpoint with a proper  
> **AG-UI protocol** backend served by the Microsoft Agent Framework,  
> and add a **React (Next.js) frontend** powered by CopilotKit.

---

## Current State (as-is)

| Layer | Technology | Notes |
|-------|-----------|-------|
| Backend API | .NET 8 Minimal API | `http://localhost:5000` |
| AI Chat | `Azure.AI.Inference` + manual tool-calling loop | `/api/chat` endpoint in Program.cs |
| Tools | `CustomerToolDefinitions` + `CustomerToolDispatcher` | 6 tools (CRUD + search + list) |
| Frontend | None (PowerShell / Swagger testing only) | — |

---

## Target State (to-be)

| Layer | Technology | Notes |
|-------|-----------|-------|
| Backend API | .NET 8 Minimal API *(unchanged)* | `http://localhost:5000` |
| AI Agent (AG-UI) | Microsoft Agent Framework + AG-UI ASP.NET Core hosting | `http://localhost:5000/agent` (same process) or separate `:8000` |
| Frontend | Next.js + CopilotKit | `http://localhost:3000` |
| Protocol | AG-UI (Agent-User Interaction) | Standardized streaming between agent ↔ CopilotKit |

### Architecture Diagram

```
┌─────────────────────────┐      AG-UI (SSE)       ┌─────────────────────────────┐
│   Next.js Frontend      │◄──────────────────────► │  .NET Backend               │
│   (localhost:3000)      │                         │  (localhost:5000)            │
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

- [ ] Update `CustomerManager.csproj` → `<TargetFramework>net9.0</TargetFramework>`
- [ ] Install .NET 9 SDK if not already available
- [ ] `dotnet build` — verify no breaking changes

### Step 2 — Add Microsoft Agent Framework NuGet packages

```bash
dotnet add package Microsoft.Agents.AI.Hosting.AGUI.AspNetCore --version 1.0.0-preview.251110.1
dotnet add package Microsoft.Extensions.AI.OpenAI --version 9.10.2-preview.1.25552.1
dotnet add package OpenAI --version 2.6.0
```

- [ ] Add the 3 packages above
- [ ] Keep `Azure.AI.Inference` (still used for CustomerToolDefinitions)  
      **OR** migrate tools to `Microsoft.Extensions.AI` tool format — decide at implementation time
- [ ] `dotnet build` — resolve any version conflicts

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

- [ ] Register `AddAGUI()` service
- [ ] Create `OpenAIClient` → `IChatClient` via `AsIChatClient()`
- [ ] Create `ChatClientAgent` with system prompt
- [ ] Map AG-UI endpoint at `/agent`
- [ ] Decide: keep existing `/api/chat` endpoint for backward compat or remove

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

- [ ] Decide on tool registration approach (A or B)
- [ ] Register all 6 customer tools with the agent
- [ ] Test tool-calling works via AG-UI protocol

### Step 5 — Scaffold the Next.js frontend

```bash
npx create-next-app@latest my-copilot-app
cd my-copilot-app
npm install @copilotkit/react-ui @copilotkit/react-core @copilotkit/runtime @ag-ui/client
```

- [ ] Create Next.js app in a sibling folder (e.g., `CustomerManager.Web/`)
- [ ] Install CopilotKit + AG-UI client packages
- [ ] Verify `npm run dev` starts on `http://localhost:3000`

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

- [ ] Create the API route file
- [ ] Point `HttpAgent` URL to the .NET AG-UI endpoint

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

- [ ] Wrap app with `<CopilotKit>` provider
- [ ] Add `<CopilotSidebar>` component
- [ ] Verify chat UI renders

### Step 8 — End-to-end testing

- [ ] Start .NET backend: `dotnet run --environment Development`
- [ ] Start Next.js frontend: `npm run dev`
- [ ] Open `http://localhost:3000`
- [ ] Test via CopilotSidebar:
  - "List all customers"
  - "Search for Jane"
  - "Add customer Alice Park, alice@test.com"
  - "Update customer 1 name to Jonathan Doe"
  - "Delete customer 3"
  - "모든 고객 목록을 보여주세요" (Korean)

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
- [ ] .NET 9.0 SDK
- [ ] Node.js 20+
- [ ] npm / pnpm / yarn

---

## File Changes Summary

| File | Action | Description |
|------|--------|-------------|
| `CustomerManager.csproj` | **Modify** | Upgrade TFM to net9.0, add 3 new NuGet packages |
| `Program.cs` | **Modify** | Add `AddAGUI()`, create `ChatClientAgent`, `MapAGUI("/agent", agent)` |
| `Plugins/CustomerPlugin.cs` | **Modify or Keep** | Convert tools to `AIFunction` format, or keep as-is if `/api/chat` stays |
| `CustomerManager.Web/` | **New folder** | Next.js app with CopilotKit |
| `CustomerManager.Web/app/api/copilotkit/route.ts` | **New** | CopilotKit Runtime → AG-UI bridge |
| `CustomerManager.Web/app/layout.tsx` | **New** | CopilotKit provider wrapper |
| `CustomerManager.Web/app/page.tsx` | **New** | CopilotSidebar chat UI |

---

*Created: 2026-02-24 | Based on: https://docs.copilotkit.ai/microsoft-agent-framework/quickstart?agent=bring-your-own*

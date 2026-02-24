# Smart Expense Tracker — Implementation Plan

> **Created:** February 24, 2026
> **Status:** In Progress
> **Goal:** Build a full-stack expense tracker with statistics dashboard, AI agent, and unit tests.
> **Grade Target:** A (30/30)

---

## 1. Architecture

```
┌──────────────────────────────┐                    ┌──────────────────────────────────┐
│  Next.js 15 Frontend         │   AG-UI (SSE)      │  .NET 9 Backend                  │
│  http://localhost:3333       │◄──────────────────► │  http://localhost:5000            │
│                              │                     │                                  │
│  CopilotKit Provider         │  /api/copilotkit    │  Minimal API endpoints           │
│  CopilotSidebar UI           │───────────────────► │  /api/expenses/* (REST)           │
│  Chart.js (react-chartjs-2)  │                     │  /stats/monthly (GET)            │
│  Tailwind CSS                │                     │  /health (GET)                   │
│                              │                     │  /agent (AG-UI, SSE stream)      │
│  Components:                 │                     │    └─ ChatClientAgent            │
│    ExpenseForm               │                     │       └─ 6 AIFunction tools      │
│    ExpenseTable              │                     │                                  │
│    StatsCharts               │                     │  Services:                       │
│                              │                     │    IExpenseService               │
└──────────────────────────────┘                     │      ├─ CRUD operations          │
                                                     │      ├─ Monthly aggregation      │
                                                     │      ├─ Category totals          │
                                                     │      └─ Daily averages           │
                                                     └────────────────┬─────────────────┘
                                                                      │
                                                     ┌────────────────▼─────────────────┐
                                                     │  GitHub Models (LLM)             │
                                                     │  openai/gpt-4o-mini              │
                                                     │  models.github.ai/inference      │
                                                     └──────────────────────────────────┘
```

### Data Flow

1. User interacts with React UI → REST calls to .NET backend for CRUD/stats
2. User asks AI via CopilotSidebar → CopilotKit Runtime → AG-UI `/agent` endpoint
3. .NET `ChatClientAgent` sends prompt + tool schemas to GitHub Models
4. LLM calls tools (e.g., `get_monthly_stats`) → agent executes against `IExpenseService`
5. Results streamed back via AG-UI SSE protocol

---

## 2. Project Structure

```
VibeCodingProject/
├── ExpenseTracker/                      # .NET 9 backend
│   ├── Program.cs                       # DI, CORS, AG-UI setup (~100 lines)
│   ├── ExpenseTracker.csproj            # net9.0, NuGet packages
│   ├── appsettings.json                 # Placeholder config only (no secrets)
│   ├── Models/
│   │   └── ExpenseModels.cs             # Expense, MonthlyStats, CategoryBreakdown
│   ├── Services/
│   │   └── ExpenseService.cs            # IExpenseService + in-memory implementation
│   └── Endpoints/
│       └── ExpenseEndpoints.cs          # MapGroup extension methods
│
├── ExpenseTracker.Tests/                # xUnit test project
│   ├── ExpenseTracker.Tests.csproj
│   └── ExpenseServiceTests.cs           # Unit tests for core logic
│
├── expense-tracker-web/                 # Next.js 15 frontend
│   ├── package.json
│   ├── next.config.ts
│   ├── tsconfig.json
│   └── src/app/
│       ├── api/copilotkit/route.ts      # CopilotKit → AG-UI bridge
│       ├── layout.tsx                   # CopilotKit provider
│       ├── page.tsx                     # Main dashboard + CopilotSidebar
│       ├── globals.css
│       └── components/
│           ├── ExpenseForm.tsx           # Add expense form
│           ├── ExpenseTable.tsx          # Expense list with delete
│           └── StatsCharts.tsx           # Pie chart + bar chart
│
├── PLAN.md                              # This file
├── PROGRESS.md                          # Step-by-step progress log
├── README.md                            # Final deliverable documentation
├── expense-tracker-proposal.md          # Original proposal
└── Evaluation Guide.md                  # Grading rubric
```

---

## 3. Domain Model

```csharp
public class Expense
{
    public int Id { get; set; }
    public decimal Amount { get; set; }
    public string Category { get; set; } = string.Empty;   // Food, Transport, Entertainment, etc.
    public DateTime Date { get; set; }
    public string? Description { get; set; }
}

public class MonthlyStats
{
    public decimal Total { get; set; }
    public decimal DailyAverage { get; set; }
    public string HighestCategory { get; set; } = string.Empty;
    public int TransactionCount { get; set; }
}

public class CategoryBreakdown
{
    public string Category { get; set; } = string.Empty;
    public decimal Total { get; set; }
    public int Count { get; set; }
    public decimal Percentage { get; set; }
}
```

---

## 4. API Design

### REST Endpoints

| Method | Endpoint | Description | Request Body | Response |
|--------|----------|-------------|--------------|----------|
| GET | `/health` | Health check | — | `{ status, timestamp }` |
| GET | `/api/expenses` | List all expenses | — | `Expense[]` |
| GET | `/api/expenses/{id}` | Get single expense | — | `Expense` |
| POST | `/api/expenses` | Add expense | `{ amount, category, date, description? }` | `Expense` (201) |
| PUT | `/api/expenses/{id}` | Update expense | `{ amount, category, date, description? }` | `Expense` |
| DELETE | `/api/expenses/{id}` | Delete expense | — | 204 |
| GET | `/stats/monthly?month=2026-02` | Monthly statistics | — | `MonthlyStats` |
| GET | `/stats/categories?month=2026-02` | Category breakdown | — | `CategoryBreakdown[]` |

### AI Agent Endpoint

| Method | Endpoint | Protocol | Description |
|--------|----------|----------|-------------|
| POST | `/agent` | AG-UI (SSE) | AI agent for natural language expense queries |

### AI Agent Tools

| Tool Name | Description |
|-----------|-------------|
| `get_all_expenses` | List all recorded expenses |
| `add_expense` | Add a new expense (amount, category, date, description) |
| `delete_expense` | Delete an expense by ID |
| `get_monthly_stats` | Get total, daily average, highest category for a month |
| `get_category_breakdown` | Get spending breakdown by category for a month |
| `search_expenses` | Search expenses by category or date range |

---

## 5. Core Logic (핵심 로직)

These are the key business logic methods in `IExpenseService`:

```csharp
public interface IExpenseService
{
    // CRUD
    List<Expense> GetAllExpenses();
    Expense? GetExpense(int id);
    Expense AddExpense(Expense expense);
    Expense? UpdateExpense(int id, Expense expense);
    bool DeleteExpense(int id);

    // Core Logic — Statistics
    MonthlyStats GetMonthlyStats(int year, int month);
    List<CategoryBreakdown> GetCategoryBreakdown(int year, int month);
    List<Expense> SearchExpenses(string? category, DateTime? from, DateTime? to);
}
```

### Aggregation Logic Detail

- **`GetMonthlyStats`**: Filters by year/month → sums amounts → divides by days in month → groups by category to find highest
- **`GetCategoryBreakdown`**: Groups by category → calculates total, count, and percentage per category
- **`SearchExpenses`**: Filters by optional category (case-insensitive) and/or date range

---

## 6. Testing Strategy

### Unit Tests (xUnit)

| Test Case | What It Verifies |
|-----------|-----------------|
| `GetMonthlyStats_ReturnsCorrectTotal` | Sum of expenses for a specific month |
| `GetMonthlyStats_ReturnsCorrectDailyAverage` | Total ÷ days in month |
| `GetMonthlyStats_ReturnsHighestCategory` | Category with the largest total |
| `GetMonthlyStats_EmptyMonth_ReturnsZeros` | Handles months with no data |
| `GetCategoryBreakdown_CalculatesPercentages` | Each category's % of total |
| `GetCategoryBreakdown_EmptyMonth_ReturnsEmpty` | Handles no data |
| `AddExpense_AssignsId` | Auto-increments ID |
| `AddExpense_InvalidAmount_Throws` | Rejects negative/zero amounts |
| `DeleteExpense_NonExistent_ReturnsFalse` | Handles missing ID |
| `SearchExpenses_FiltersByCategory` | Case-insensitive category filter |
| `SearchExpenses_FiltersByDateRange` | From/to date filtering |

---

## 7. Implementation Steps

### Phase 1: Backend Foundation
- [ ] **Step 1:** Create this plan file (PLAN.md)
- [ ] **Step 2:** Verify prerequisites (.NET 9, Node.js, npm)
- [ ] **Step 3:** Scaffold .NET project — Models + IExpenseService interface
- [ ] **Step 4:** Implement ExpenseService (CRUD + aggregation logic + seed data)
- [ ] **Step 5:** Add Minimal API endpoints via ExpenseEndpoints.cs + Program.cs
- [ ] **Step 6:** Test all REST endpoints manually (PowerShell)

### Phase 2: Testing
- [ ] **Step 7:** Create xUnit test project
- [ ] **Step 8:** Write unit tests for all core logic methods
- [ ] **Step 9:** Run tests, ensure all pass

### Phase 3: AI Agent
- [ ] **Step 10:** Add AG-UI + GitHub Models NuGet packages
- [ ] **Step 11:** Register AI tools and wire up ChatClientAgent
- [ ] **Step 12:** Test `/agent` endpoint with raw SSE call

### Phase 4: Frontend
- [ ] **Step 13:** Scaffold Next.js app with CopilotKit + Tailwind
- [ ] **Step 14:** Build ExpenseForm + ExpenseTable components
- [ ] **Step 15:** Build StatsCharts component (Chart.js pie + bar)
- [ ] **Step 16:** Wire up CopilotSidebar to AG-UI agent
- [ ] **Step 17:** End-to-end testing (UI ↔ Backend ↔ AI)

### Phase 5: Documentation & Polish
- [ ] **Step 18:** Write README (project description, install, API docs, core logic, Copilot usage)
- [ ] **Step 19:** Take screenshots of execution
- [ ] **Step 20:** Final review and cleanup

---

## 8. Tech Stack (Pinned Versions)

### Backend
| Package | Version | Purpose |
|---------|---------|---------|
| .NET SDK | 9.0.x | Runtime |
| `Microsoft.Agents.AI.Hosting.AGUI.AspNetCore` | 1.0.0-preview.251110.1 | AG-UI hosting |
| `Microsoft.Extensions.AI.OpenAI` | 9.10.2-preview.1.25552.1 | IChatClient + AIFunctionFactory |
| `Swashbuckle.AspNetCore` | 6.4.0 | Swagger |
| `xunit` | latest | Testing |
| `xunit.runner.visualstudio` | latest | Test runner |
| `Microsoft.NET.Test.Sdk` | latest | Test infrastructure |

### Frontend
| Package | Version | Purpose |
|---------|---------|---------|
| Next.js | 15+ | React framework |
| `@copilotkit/react-core` | ~1.51 | CopilotKit provider |
| `@copilotkit/react-ui` | ~1.51 | CopilotSidebar |
| `@copilotkit/runtime` | ~1.51 | Runtime bridge |
| `@ag-ui/client` | ~0.0.45 | AG-UI HttpAgent |
| `chart.js` | latest | Charts |
| `react-chartjs-2` | latest | React Chart.js wrapper |
| Tailwind CSS | 4.x | Styling |

---

## 9. Risk Assessment

| Risk | Likelihood | Impact | Mitigation |
|------|-----------|--------|------------|
| Port 5000/3333 conflict | Medium | Low | Check `Get-NetTCPConnection` before starting |
| Preview NuGet packages break | Low | High | Pin exact versions from working CustomerManager |
| GitHub Models rate limiting | Low | Medium | Retry with exponential backoff |
| Chart.js SSR issues in Next.js | Medium | Low | Use `dynamic()` import with `ssr: false` |
| Scope creep | Medium | High | Strict non-goals list, checkpoint every 3-4 steps |
| CORS issues | Low | Low | Reuse proven CORS config from CustomerManager |

---

## 10. Non-Goals (Explicitly Out of Scope)

- ❌ User authentication / login
- ❌ Persistent database (SQLite, SQL Server, etc.)
- ❌ Multi-currency support
- ❌ CSV/PDF export
- ❌ Legacy `/api/chat` endpoint (AG-UI only)
- ❌ Mobile responsive design (desktop-first)
- ❌ Deployment / CI/CD

---

## 11. "Done" Criteria

The project is **complete** when:

1. ✅ Backend runs on `http://localhost:5000` with Swagger UI
2. ✅ All REST endpoints work (CRUD + statistics)
3. ✅ All xUnit tests pass (`dotnet test`)
4. ✅ AI agent responds to natural language expense queries via CopilotSidebar
5. ✅ Frontend on `http://localhost:3333` shows form, table, and charts
6. ✅ README contains: project description, install instructions, API docs, core logic explanation, Copilot usage
7. ✅ Screenshots captured

---

## 12. Copilot Usage Documentation (for README)

During development, GitHub Copilot was used for:

- Generating the project plan and architecture
- Scaffolding .NET Minimal API endpoints
- Implementing core aggregation logic (monthly stats, category breakdown)
- Writing xUnit test cases and assertions
- Creating React components (ExpenseForm, ExpenseTable, StatsCharts)
- Setting up CopilotKit + AG-UI integration
- Generating Chart.js configuration
- Writing documentation

All generated code was reviewed, tested, and refined manually.

---

*This plan follows the guidelines in `.github/copilot-instructions.md`.*

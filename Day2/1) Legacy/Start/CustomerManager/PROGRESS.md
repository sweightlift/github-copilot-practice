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

### Step 6: Explore & Next Steps ⏳ (In Progress)
- Swagger UI is live for interactive API testing.
- Identify modernization tasks from the analysis (see [PROJECT_ANALYSIS.md](PROJECT_ANALYSIS.md#suggested-next-steps-modernization-path)).

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

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

# Copilot Custom Instructions

> Rules and guidelines derived from project feedback to maintain strengths and improve weaknesses.  
> This file is automatically read by GitHub Copilot to guide future interactions.

---

## Planning & Documentation (Keep Doing)

- **Plan before executing:** For any multi-step task, create a markdown plan file first (like `COPILOTKIT_PLAN.md`) with checkboxes, architecture diagrams, and risk assessment before writing any code.
- **Update docs after every milestone:** After completing each significant step, update all relevant documentation files (PROGRESS.md, PROJECT_ANALYSIS.md, DIAGRAMS.html, INDEX.html). Documentation is a first-class deliverable, not an afterthought.
- **Incremental delivery:** Break work into small, testable steps. Each step must result in a buildable, runnable state. Never combine unrelated changes in a single step.
- **Test before moving on:** Every feature must be verified (manual or automated) before marking it complete. Include test commands and expected results in progress logs.

## Backward Compatibility (Keep Doing)

- **Preserve existing endpoints:** When adding new functionality (e.g., AG-UI `/agent`), keep existing endpoints (e.g., `/api/chat`) working unless explicitly asked to remove them.
- **Non-breaking changes by default:** New features should be additive. Mark deprecated features clearly in documentation rather than removing them.

## Security & Secrets (Improve)

- **Never store secrets in `appsettings.json`:** Use `dotnet user-secrets` for local development or environment variables for production. Keep `appsettings.json` committed with placeholder values only.
- **Use `appsettings.Development.json` (gitignored)** for environment-specific overrides that contain sensitive data.
- **Pattern for API keys:**
  ```json
  // appsettings.json (committed) — placeholder only
  { "GitHubModels": { "ApiKey": "" } }
  ```
  ```bash
  # Set via user-secrets or env var
  dotnet user-secrets set "GitHubModels:ApiKey" "ghp_xxx..."
  ```

## Environment Management (Improve)

- **Check prerequisites first:** Before starting work that requires new SDKs, runtimes, or tools, verify they are installed. If not, install them as a discrete tracked step.
- **Document required versions:** Maintain a prerequisites section in README or PROGRESS.md:
  - .NET SDK version (currently 9.0.311)
  - Node.js version (currently v24.13.1 LTS)
  - npm version (currently 11.8.0)
- **Handle port conflicts proactively:** Before starting a server, check if the port is available. Kill stale processes first:
  ```powershell
  # Check port before starting
  Get-NetTCPConnection -LocalPort 5000 -ErrorAction SilentlyContinue
  ```
- **Prefer `devcontainer.json`** for reproducible environments when working in teams.

## Error Handling (Improve)

- **Read errors before retrying:** When a command fails (`Exit Code: 1`), always read and analyze the error output before retrying. Don't retry blindly.
- **Diagnose → Fix → Retry:** Follow this sequence instead of repeated retries:
  1. Read the full error message
  2. Identify the root cause (port conflict? missing dependency? build error?)
  3. Apply a targeted fix
  4. Retry once
- **Common .NET failures to check first:**
  - Port already in use → `Stop-Process` or choose another port
  - Missing SDK → `dotnet --list-sdks`
  - Ambiguous types → Use fully qualified names

## Scope Management (Improve)

- **Define "done" criteria upfront:** Before starting a feature, write 2-3 sentences defining what "done" looks like. Resist expanding scope mid-implementation unless explicitly agreed.
- **Use branches for major features:** Create separate Git branches for significant changes (e.g., `feature/copilotkit-integration`). Merge only after testing.
- **Checkpoint at milestones:** After every 3-4 steps, pause to review: Are we still aligned with the original goal? Should we stop here?

## Delegation & Communication (Improve)

- **Provide constraints when delegating:** When asking for implementation, include:
  - 2-3 specific constraints (e.g., "same port", "minimal UI", "no new dependencies")
  - Non-goals (what you explicitly don't want)
  - Preferred approach if you have one
- **Example good delegation:**
  > "Add CopilotKit frontend. Constraints: use the same .NET process on port 5000 for the agent, keep the UI minimal (just a chat sidebar), don't remove the legacy /api/chat endpoint."

## Code Quality (Maintain)

- **Resolve compiler warnings:** Treat warnings as errors. Fix ambiguity issues (e.g., `CS0104`) immediately with fully qualified names.
- **Use consistent naming:** Follow .NET conventions (PascalCase for public members) and npm conventions (kebab-case for packages).
- **Keep `Program.cs` focused:** As the file grows (currently ~300 lines), consider extracting endpoint groups into extension methods or separate files.

---

## Quick Reference: Project Conventions

| Convention | Standard |
|-----------|----------|
| .NET Framework | net9.0 |
| API Style | Minimal API (no controllers) |
| AI Agent (new) | Microsoft Agent Framework + AG-UI |
| AI Agent (legacy) | Azure.AI.Inference + manual tool loop |
| Frontend | Next.js 15 + CopilotKit |
| Documentation | Markdown + HTML dashboards |
| Testing | Manual (PowerShell/curl) — consider adding xUnit |
| Secrets | `dotnet user-secrets` (NOT in appsettings.json) |
| Backend URL | `http://localhost:5000` |
| Frontend URL | `http://localhost:3333` |

---

*Created: February 24, 2026 — Based on full project collaboration feedback*

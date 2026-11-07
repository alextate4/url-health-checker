# Walking Skeleton — Rosnet URL Health Monitor

**Phase:** 1
**Generated:** 2026-11-07

## Capability Proven End-to-End

A user runs `dotnet run --project server` from a fresh clone, opens http://localhost:5080, and
sees each seeded URL's latest health (Healthy / Unhealthy / Pending, status code, response time,
last checked time, error reason) from real scheduled HTTP checks that are saved, one row per
check, in SQLite.

## Architectural Decisions

| Decision | Choice | Rationale |
|---|---|---|
| Framework (backend) | ASP.NET Core 10 Minimal APIs, one project `server/HealthMonitor.csproj` (`net10.0`, Web SDK) | Rosnet is a .NET shop; each endpoint is a few lines, close to Express (log.json D-001) |
| Background work | One `CheckScheduler : BackgroundService` with a `PeriodicTimer` "what is due now?" loop; tick from `Monitor:TickSeconds` (5) | Built into .NET; per-URL intervals fall out of one loop; interval changes apply next tick (D-006, D1-14) |
| HTTP checks | Named `HttpClient` `"checker"` from `IHttpClientFactory`; `GET` + `HttpCompletionOption.ResponseHeadersRead`; timeout from `Monitor:RequestTimeoutSeconds` (10); redirects followed | No socket exhaustion; body never downloaded; HEAD rejected because some servers answer 405 (D1-16, D1-17, D1-18) |
| Check logic seam | `UrlChecker` plain class, constructor takes an `HttpClient`, `CheckAsync(url, token)` returns a `CheckResult` record | Phase 2 tests pass an `HttpClient` built on a fake `HttpMessageHandler`; no interface (D1-21) |
| Data layer | SQLite file `server/healthmonitor.db` via `Microsoft.Data.Sqlite` + `Dapper`, hand-written SQL, schema script at startup (`CREATE TABLE IF NOT EXISTS`), no migrations | Zero setup for the panel; every query is visible SQL (D-004, D-005, D1-11) |
| Data model | `urls(id, url, interval_seconds, created_at)`; `check_results(id, url_id, checked_at, status_code NULL, response_time_ms, healthy 0/1, error NULL)` + index `(url_id, checked_at)`; results append-only; "last checked" derived from the newest result | History, not just latest state; one source of truth (D1-06, D1-07, D1-09). Reversibility: costly (no migrations) |
| Time | UTC, ISO 8601 round-trip text (`"o"`, ends in `Z`) | Readable in the DB and sorts correctly as text (D1-08) |
| API | `GET /api/urls` → `[{ id, url, intervalSeconds, lastCheck: { checkedAt, statusCode, responseTimeMs, healthy, error } \| null }]`, camelCase, ascending id | One endpoint for the skeleton; error reason is returned (D1-12) |
| Frontend | React 18 (pinned) + Vite 8 + `@vitejs/plugin-react` 6, plain JavaScript, plain CSS, native `fetch`; hand-written `web/package.json` (no Vite template extras) | Light dependencies the owner can defend (D-002, D-003) |
| Serving | One process: ASP.NET Core serves the built UI from `server/wwwroot` (`UseDefaultFiles` + `UseStaticFiles`) and the API on the same address; Vite dev server proxies `/api` to `http://localhost:5080` | One command, no CORS (D-007, D1-02, D1-04) |
| Fresh-clone build | MSBuild target `BuildWebUi` (`BeforeTargets="Build"`) in the server `.csproj` runs `npm install` when `web/node_modules` is missing and `npm run build` when `server/wwwroot/index.html` is missing; `server/wwwroot/` is gitignored | "One `dotnet run`" from a fresh clone without committing built files (D1-03, D1-05) |
| Config | `server/appsettings.json`: `ConnectionStrings:HealthMonitor` = `Data Source=healthmonitor.db`, `Monitor:TickSeconds` = 5, `Monitor:RequestTimeoutSeconds` = 10 | Operator config, not UI settings (D1-16b) |
| Auth | None | Single-user local demo; auth is out of scope (REQUIREMENTS Out of Scope) |
| Deployment target | Local only: `dotnet run --project server` on `http://localhost:5080` (5000 avoided — macOS AirPlay Receiver uses it) | The panel clones and runs it; containers are v2 (PLAT-02) |
| Directory layout | `HealthMonitor.slnx` at the root; `server/` (ASP.NET Core), `web/` (Vite + React), `tests/` (xUnit, created in Phase 2) | `dotnet build` / `dotnet test` work from the root (D1-01) |
| Tests | xUnit, core-only, Phase 2 | D-009; no tests in Phase 1 |

## Conventions Set by This Phase

- One C# class per file, file named after the class (`Database.cs`, `UrlChecker.cs`, `CheckScheduler.cs`); small records live next to the class that produces them (`CheckResult` in `UrlChecker.cs`, `UrlStatus` in `Database.cs`).
- Endpoints are mapped inline in `server/Program.cs` with `app.MapGet(...)` (Express-style).
- SQL tables and columns are `snake_case`; queries alias columns to the C# property names (`interval_seconds AS IntervalSeconds`). Every value goes through a Dapper `@Parameter`.
- JSON is camelCase (ASP.NET Core default). Times are UTC `"o"` strings end to end; the browser formats them for display.
- React components are PascalCase `.jsx` files in `web/src/`; styles in `web/src/App.css`; text is rendered only as JSX text (React escapes it).
- Comments explain why, not what. First-use backend concepts get a `docs/CONCEPTS.md` entry (STE-100). Every package and real decision gets a `log.json` entry.

## Stack Touched in Phase 1

- [x] Project scaffold — `HealthMonitor.slnx`, `server/` (`dotnet new web`), `web/` (hand-written Vite + React 18)
- [x] Routing — `GET /api/urls`, plus `/` serving `server/wwwroot/index.html`
- [x] Database — real reads (`GetUrlStatusesAsync`, seed count) AND real writes (seed inserts, `InsertCheckResultAsync`)
- [x] UI — React table fetched from the API with loading and error states
- [x] Deployment — documented local full-stack run command: `dotnet run --project server` (README "Run it")

## Out of Scope (Deferred to Later Slices)

- History window, uptime %, average response time, recent-checks strip, polling, core tests — Phase 2
- Add / remove / re-time URLs from the UI, concurrent checks with a cap, retention and a settings table, full README — Phase 2 (merged, D-015)
- Pause/resume, check now, detail view, sparkline — stretch `/gsd-quick` tasks
- Every entry in `log.json` → `edgeCases` (EC-001 to EC-018) — stretch `/gsd-quick` tasks, by priority
- Display names for URLs, per-URL timeout in the UI (CONTEXT Deferred Ideas)
- Auth, multi-tenancy, retry/backoff, alerting, Postgres, containers, TypeScript (REQUIREMENTS Out of Scope / v2)

## Subsequent Slice Plan

Each later phase adds one vertical slice on top of this skeleton without altering its
architectural decisions:

- Phase 2 (Features & README, D-015): Pick a window (1h / 24h / 7d) and see uptime %, average response time, and a recent-checks strip per URL, refreshed by polling; core xUnit tests (`tests/`); add, remove, and re-time URLs from the UI; concurrent checks with a cap; global retention with a timed delete; full README.
- Stretch (`/gsd-quick`, only if time remains): pause/resume, check now, URL detail table, response-time sparkline; then logged edge cases by priority, documenting the rest.

# Phase 1: Walking Skeleton - Context

**Gathered:** 2026-11-07
**Status:** Ready for planning

<domain>
## Phase Boundary

Seeded URLs are checked on a schedule, every result is saved as its own SQLite row, and a React 18
page lists each URL with its latest health, status code, and response time. One `dotnet run`
from a fresh clone serves both the API and the built UI.

Requirements: URL-04, CHK-01, CHK-02, CHK-03, CHK-04, HIST-01, HIST-02, DASH-01, DASH-02, DLVR-01.

Not in this phase: adding/removing/editing URLs (Phase 3), concurrency cap (Phase 3), settings and
retention (Phase 3), history window, uptime, strip, polling, tests (Phase 2).

</domain>

<decisions>
## Implementation Decisions

**How these were made:** The owner skipped the gray-area discussion ("Lets just build it").
Every decision below is Claude's recommended default, accepted without debate. Each one is
logged as such in `log.json` (D-014) so the panel can see it was AI-picked. The owner can
override any of them before `/gsd-plan-phase 1`.

Already locked before this phase (do not revisit): D-001 to D-013 in `log.json`. Summary:
Minimal APIs, SQLite + Dapper with a schema script at startup, UTC times, one
`BackgroundService` + `PeriodicTimer` "what is due" loop, `IHttpClientFactory`, one process
serves API + `wwwroot`, Vite proxies `/api` in dev, React 18 pinned, plain JS/CSS, healthy =
200–399 within the timeout.

### Repo layout & fresh-clone build
- **P1-01:** Three folders at the root: `server/` (ASP.NET Core project), `web/` (Vite + React),
  `tests/` (xUnit project, created in Phase 2 — not now). One solution file at the root so
  `dotnet build`/`dotnet test` work from there.
- **P1-02:** Vite builds straight into `server/wwwroot/` (`build.outDir`). `server/wwwroot/` is
  gitignored — built files are not committed.
- **P1-03:** `dotnet run` from a fresh clone builds the UI by itself: an MSBuild target in the
  server `.csproj` runs `npm install` when `web/node_modules` is missing and `npm run build` when
  `server/wwwroot/index.html` is missing. Node must be installed (stated in the README). Rejected:
  committing built files (noisy diffs, stale UI); a manual npm step (breaks "one command").
  — **Reversibility:** reversible
- **P1-04:** Day-to-day frontend work uses `npm run dev` in `web/` with the `/api` proxy to the
  backend; the MSBuild target only covers the fresh-clone case.
- **P1-05:** Extend `.gitignore` for .NET (`bin/`, `obj/`), the SQLite file (`*.db`), and
  `server/wwwroot/`. The current file is the Node template only.

### Data model (SQLite)
- **P1-06:** `urls` table: `id` INTEGER PK, `url` TEXT NOT NULL, `interval_seconds` INTEGER NOT
  NULL, `created_at` TEXT NOT NULL. No display name — the dashboard shows the URL itself.
- **P1-07:** `check_results` table: `id` INTEGER PK, `url_id` INTEGER NOT NULL (FK → urls.id),
  `checked_at` TEXT NOT NULL, `status_code` INTEGER NULL, `response_time_ms` INTEGER NOT NULL,
  `healthy` INTEGER NOT NULL (0/1), `error` TEXT NULL. Index on `(url_id, checked_at)`.
  — **Reversibility:** costly — no migrations (D-005); changing columns later means editing the
  schema script and deleting the local DB file.
- **P1-08:** Timestamps are stored as ISO 8601 UTC text (round-trip `"o"` format). Readable in the
  DB and they sort correctly as text.
- **P1-09:** "Last checked" is **not** stored on the `urls` row. It is read from the newest
  `check_results` row. One source of truth; the scheduler's "is it due?" uses the same value.
- **P1-10:** No `settings` table in Phase 1. It arrives with retention in Phase 3.
- **P1-11:** DB file path comes from `appsettings.json` (connection string), default
  `healthmonitor.db` in the server content root. Schema script uses `CREATE TABLE IF NOT EXISTS`.

### Phase 1 API
- **P1-12:** One endpoint: `GET /api/urls` → array of
  `{ id, url, intervalSeconds, lastCheck: { checkedAt, statusCode, responseTimeMs, healthy, error } | null }`.
  `lastCheck` is `null` before the first check. JSON is camelCase (ASP.NET default). The error
  reason is returned here (success criterion 4).

### Seeds & check behavior
- **P1-13:** Seeds are inserted only when the `urls` table is empty:
  - `https://example.com` — 30s (healthy)
  - `https://github.com` — 60s (healthy; different interval shows per-URL scheduling)
  - `https://github.com/this-page-should-not-exist-rosnet` — 30s (404 → unhealthy with a status code)
  - `https://unreachable.invalid` — 30s (DNS failure → unhealthy with an error reason; `.invalid` is
    reserved by RFC 2606, so it can never resolve)
  Rejected: httpstat.us / httpbin as failing seeds — third-party test services that are often down.
- **P1-14:** Scheduler ticks every **5 seconds** (default; read from `appsettings.json`, not a UI setting). A URL is due when it has no results yet, or when
  its newest `checked_at` + `interval_seconds` ≤ now.
- **P1-15:** Phase 1 checks due URLs **one after another** within a tick. Concurrent checks with a
  cap are CHK-05 in Phase 3.
- **P1-16:** Request timeout **10 seconds** (default; read from `appsettings.json`, not a UI setting), set on a named `HttpClient` from `IHttpClientFactory`.
- **P1-16b:** Tick and timeout are operator config, not user settings: they live in `appsettings.json`, not in the UI or the DB. The only UI-configurable values in v1 are the per-URL interval, the history window, and retention. Error text for timeouts uses the configured value.
- **P1-17:** Use `GET` with `HttpCompletionOption.ResponseHeadersRead` (stop after headers; body is
  not downloaded). Rejected: `HEAD` — some servers answer 405 to HEAD, which would show a false
  unhealthy.
- **P1-18:** Follow redirects (HttpClient default). The healthy rule applies to the final status.
  Redirect loops stay deferred (EC-007).
- **P1-19:** Response time = `Stopwatch` from send until headers arrive. Recorded for failures too
  (a timeout records ~10000 ms).
- **P1-20:** Error reasons: timeout → `"Timed out after 10s"`; `HttpRequestException` → its
  message (covers DNS failure and connection refused); 4xx/5xx → no error text, the status code
  says it. Any other exception during a check is also recorded as unhealthy with its message —
  a check must never crash the loop.
- **P1-21:** The check logic lives in one small class (e.g. `UrlChecker`) that takes an
  `HttpClient` and returns a result. This lets Phase 2 tests feed it a fake `HttpMessageHandler`.
  It is not an interface or a layer — just a class.

### Dashboard
- **P1-22:** One plain HTML `<table>`, one row per URL. Columns: Status, URL, Code, Response time,
  Last checked, Error.
- **P1-23:** Status cell = colored dot **plus a word**: green "Healthy", red "Unhealthy", gray
  "Pending" (no check yet). The word keeps it readable without color.
- **P1-24:** Response time shown as `123 ms`. Code shows `—` when there was no response. Last
  checked shown in the browser's local time (`toLocaleTimeString`).
- **P1-25:** Data is fetched once when the page loads. No polling (DASH-06, Phase 2) and no
  refresh button — reloading the page shows newer results (success criterion 3).
- **P1-26:** Simple "Loading…" text, and "Could not load URLs: <message>" if the fetch fails.

### Process
- **P1-27:** Set `log.json` → `timebox.buildStartedAt` when the first line of application code is
  written (the start of execution).
- **P1-28:** New concepts that come up and are not already in `docs/CONCEPTS.md` get an STE-100
  entry during execution. Likely: hosted static files / `wwwroot`, MSBuild targets, `appsettings.json`
  config, `HttpCompletionOption`, `Stopwatch`. Already covered: C#/.NET, Minimal APIs,
  BackgroundService, PeriodicTimer, async/await, IHttpClientFactory, DI, SQLite, Dapper.

### Claude's Discretion
- Project and namespace names (e.g. `HealthMonitor`), file split inside `server/` (keep files small
  and obviously named), React component split (likely `App.jsx` + a row component).
- Whether the "due" check is done in SQL or in C# — whichever is easier to read.
- Exact CSS. Plain, readable, no design work.

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Rules and brief
- `CLAUDE.md` — working rules: time cap, locked stack, ask before adding packages, decision
  logging, edge cases deferred, core-only tests, simple code, STE-100 concepts, commit rules.
- `docs/KICKOFF_PROMPT.md` — summary of the Rosnet brief, features, definition of done.

### Decisions and scope
- `log.json` — decisions D-001 to D-014, `edgeCases` (EC-001 to EC-011, deferred — do not build),
  `timebox`.
- `.planning/REQUIREMENTS.md` — requirement IDs for this phase.
- `.planning/ROADMAP.md` § Phase 1 — goal and success criteria.

### Owner learning notes
- `docs/CONCEPTS.md` — existing concept notes; add new ones in the same style.

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- None. Greenfield repo — only docs, `log.json`, and planning files exist.

### Established Patterns
- None in code yet. Patterns set by this phase (folder layout, file naming, SQL style) become the
  conventions for Phases 2–5.

### Integration Points
- `.gitignore` exists (Node template) and must be extended for .NET, `*.db`, `server/wwwroot/`.
- Toolchain checked: .NET SDK 10.0.403, Node v24.

</code_context>

<specifics>
## Specific Ideas

- The seed list must show both states on first run: at least one healthy URL, one unhealthy with a
  status code (404), and one unhealthy with no response and an error reason (DNS failure).
- Packages expected in this phase: `Microsoft.Data.Sqlite` and `Dapper` (already approved in
  D-004/D-005); `react@18`, `react-dom@18`, `vite`, `@vitejs/plugin-react` (D-002). Anything else
  needs the owner's approval first.

</specifics>

<deferred>
## Deferred Ideas

- Per-URL timeout set from the UI — common in real monitors, but not a requirement; README next step.
- Display names for URLs — not needed; the URL itself is the label. Possible v2 polish.
- Everything already in `log.json` → `edgeCases` stays deferred (notably EC-001 overlapping checks,
  EC-007 redirects, EC-010 write contention, EC-011 large bodies — partly covered by
  `ResponseHeadersRead`).

</deferred>

---

*Phase: 01-walking-skeleton*
*Context gathered: 2026-11-07*

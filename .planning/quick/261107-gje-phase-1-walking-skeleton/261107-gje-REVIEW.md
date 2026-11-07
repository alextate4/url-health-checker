---
phase: 01-walking-skeleton
reviewed: 2026-11-07
depth: quick
files_reviewed: 16
findings: {critical: 0, warning: 2, info: 1, total: 3}
status: issues_found
---

# Phase 1 code review (quick 261107-gje)

Reviewer: gsd-code-reviewer subagent (read-only, did not run the app). Orchestrator notes in
*Verification* lines were added after checking each finding against the tree.

## Warnings

### WR-01: SQLite file location depends on the launch directory
**Files:** `server/appsettings.json`, `server/Program.cs`
The connection string `Data Source=healthmonitor.db` is relative to the process working
directory, not the content root (D1-11 says content root).
**Fix:** build the path from `builder.Environment.ContentRootPath`.
*Verification:* partly refuted. `dotnet run --project server` from the repo root was run and no
root-level DB was created — `dotnet run` uses the project folder as working directory. Only
`dotnet <path>/HealthMonitor.dll` from another folder would hit this. Downgraded: treat as an
edge case unless fixed cheaply.

### WR-02: fresh-clone build uses `npm install`, not `npm ci`
**File:** `server/HealthMonitor.csproj:16`
`npm install` may bump `^` ranges and rewrite the committed `web/package-lock.json` on a
reviewer's first `dotnet run`, leaving a dirty tree and an untested build.
**Fix:** `npm ci`.
*Verification:* confirmed.

## Info

### IN-01: `.gitignore` is the generic Node template
**File:** `.gitignore` (163 lines)
Rules for Next, Nuxt, Gatsby, Firebase, etc.; broad `out`, `dist`, `.cache` patterns.
**Fix:** trim to what the project uses: `node_modules/`, `bin/`, `obj/`, `.vs/`, `*.db*`,
`server/wwwroot/`, `.env*`, `*.log`.
*Verification:* confirmed.

## Checked and found sound
Response time (Stopwatch to headers, recorded on failures); healthy = 200–399; timeout vs
shutdown cancellation; scheduler cannot crash from a failed check; all SQL parameterized;
latest-check query uses the `(url_id, checked_at)` index; HttpResponseMessage and
SqliteConnection disposed, HttpClient from the factory; schema created before `app.Run()`;
frontend has loading/error states and no `dangerouslySetInnerHTML`.

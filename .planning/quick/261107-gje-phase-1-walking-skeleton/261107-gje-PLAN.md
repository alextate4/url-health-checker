---
phase: 01-walking-skeleton
plan: 01
quick_id: 261107-gje
type: execute
wave: 1
depends_on: []
files_modified:
  - .gitignore
  - HealthMonitor.slnx
  - server/HealthMonitor.csproj
  - server/Program.cs
  - server/Database.cs
  - server/UrlChecker.cs
  - server/CheckScheduler.cs
  - server/appsettings.json
  - server/Properties/launchSettings.json
  - web/package.json
  - web/package-lock.json
  - web/vite.config.js
  - web/index.html
  - web/src/main.jsx
  - web/src/App.jsx
  - web/src/UrlRow.jsx
  - web/src/App.css
  - log.json
  - docs/CONCEPTS.md
autonomous: true
requirements: [URL-04, CHK-01, CHK-02, CHK-03, CHK-04, HIST-01, HIST-02, DASH-01, DASH-02, DLVR-01]

estimate:
  tokens: 95000
  raw_tokens: 95000
  tasks: 3
  confidence: low

must_haves:
  truths:
    - "From a clean state (no server/wwwroot, no web/node_modules), `dotnet run --project server` builds the UI and serves the dashboard and the API at http://localhost:5080 from one process (DLVR-01, D1-03)"
    - "On first run the dashboard lists 4 seeded URLs, each row showing a status dot plus word, status code, response time in ms, last checked time, and error (URL-04, DASH-01, DASH-02, D1-13, D1-22..D1-26)"
    - "example.com shows healthy with a 2xx/3xx code; the GitHub 404 seed shows unhealthy with statusCode 404; unreachable.invalid shows unhealthy with statusCode null and a non-empty error returned by GET /api/urls (CHK-02, CHK-03, CHK-04, D1-20)"
    - "Each URL is re-checked when its own interval has passed: after ~45s, example.com (30s) has 2 or more result rows while github.com (60s) has exactly 1 (CHK-01, D1-14)"
    - "After a stop and restart, urls still has exactly 4 rows (no re-seed) and check_results keeps its earlier rows and keeps growing (HIST-01, HIST-02, D1-07, D1-13)"
  artifacts:
    - path: server/Database.cs
      provides: "Schema script, seeds, GetUrlStatusesAsync, InsertCheckResultAsync (Dapper, hand-written SQL)"
      contains: "CREATE TABLE IF NOT EXISTS check_results"
    - path: server/UrlChecker.cs
      provides: "One HTTP check returns a CheckResult (status, ms, healthy, error)"
      contains: "ResponseHeadersRead"
    - path: server/CheckScheduler.cs
      provides: "BackgroundService + PeriodicTimer loop that checks due URLs one after another"
      contains: "PeriodicTimer"
    - path: server/HealthMonitor.csproj
      provides: "BuildWebUi MSBuild target for the fresh-clone UI build"
      contains: "BuildWebUi"
    - path: web/src/App.jsx
      provides: "Dashboard table fetched from /api/urls with loading and error states"
      contains: "/api/urls"
  key_links:
    - from: server/CheckScheduler.cs
      to: server/Database.cs
      via: "InsertCheckResultAsync after every check (append-only history)"
      pattern: "InsertCheckResultAsync"
    - from: server/HealthMonitor.csproj
      to: web/package.json
      via: "BuildWebUi Exec runs npm install / npm run build in ../web"
      pattern: "npm run build"
    - from: web/vite.config.js
      to: server/wwwroot
      via: "build.outDir"
      pattern: "server/wwwroot"
    - from: web/src/App.jsx
      to: server/Program.cs
      via: "fetch of /api/urls is answered by app.MapGet for /api/urls"
      pattern: "/api/urls"
---

<objective>
Build the Phase 1 walking skeleton: seeded URLs are checked on a schedule, every result is saved as
its own SQLite row, and a React 18 table shows each URL's latest health, all served by one
`dotnet run --project server` from a fresh clone.

The spec is `.planning/phases/01-walking-skeleton/01-CONTEXT.md` (locked decisions D1-01..D1-28,
including D1-16b) plus the architecture table in `.planning/phases/01-walking-skeleton/SKELETON.md`.
Do not revisit those decisions. This plan cites them by ID instead of restating them.

Order (tracer-first): Task 1 wires the thinnest full path through every layer: SQLite seeds, the
API, the React table, the Vite build into `server/wwwroot`, and one `dotnet run`. At that point
every row shows "Pending". Task 2 adds the checker and the scheduler, so rows turn
Healthy/Unhealthy and history builds up. Task 3 writes the decision log and the concept notes.
Task 1 comes first because the riskiest wiring is the fresh-clone build and the single-process
serving, not the check logic.

Output: a runnable app and a commit history that tells the build story.
</objective>

<execution_context>
@~/.claude/gsd-core/workflows/execute-plan.md
@~/.claude/gsd-core/templates/summary.md
</execution_context>

<context>
@CLAUDE.md
@.planning/STATE.md
@.planning/phases/01-walking-skeleton/01-CONTEXT.md
@.planning/phases/01-walking-skeleton/SKELETON.md
@log.json
@docs/CONCEPTS.md
</context>

<rules_for_this_run>
These apply to every task. Read them before Task 1.

- **Packages (hard rule):** the only packages you may add are NuGet `Microsoft.Data.Sqlite` and
  `Dapper` (approved in log.json D-004/D-005), and npm `react@18`, `react-dom@18`, `vite`, and
  `@vitejs/plugin-react` (approved in D-002). Their optional peer dependencies stay uninstalled.
  If any other package seems needed, STOP. Do not add it. Report it as a blocker in the SUMMARY so
  the owner can decide (CLAUDE.md section 2).
- **No tests** in this plan. Core xUnit tests are Phase 2 (log.json D-009). Do not create `tests/`.
- **Edge cases, log only (CLAUDE.md section 4):** when you meet one, do not handle it. Append it to
  `log.json` → `edgeCases` with the next free id (currently EC-012) and the same keys as the
  existing entries: `id`, `priority` (1–3), `title`, `description`, `status: "deferred"`, `plain`.
  Do not repeat EC-001..EC-011. Likely examples: Node or npm missing on the machine, port 5080
  already in use, `server/wwwroot` going stale after a `web/src` edit (the MSBuild target only
  builds when `index.html` is missing, per D1-03). A failed check being recorded as unhealthy is
  core behavior, not an edge case.
- **Editing log.json:** load it, change it, and write it back with Python `json.load`, then
  `json.dumps(data, indent=2, ensure_ascii=False)` plus a trailing newline. At planning time this
  rewrites the file byte for byte, so the diff shows only your additions. Do not hand-edit the
  JSON with sed.
- **Commits:** make small commits whose messages tell the story. Suggested messages are listed in
  each task. End every message with a blank line and then
  `Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>`. Never amend, squash, or force-push.
  Stage files by name. Never stage `server/wwwroot/`, `*.db`, `bin/`, `obj/`, or `node_modules/`.
- **Code style (CLAUDE.md section 6, SKELETON "Conventions"):** one class per file, named after
  the file. Small records sit next to the class that produces them. No interfaces and no extra
  layers. Comments explain why, not what. Every SQL value goes through a Dapper `@Parameter`.
- **Before running the app:** if something is already listening on 5080 from an earlier attempt,
  stop it with `lsof -ti tcp:5080 | xargs kill`.
</rules_for_this_run>

<tasks>

<task type="tracer">
  <name>Task 1: Tracer, seeded URLs travel from SQLite to the React table through one `dotnet run`</name>
  <files>log.json, .gitignore, HealthMonitor.slnx, server/HealthMonitor.csproj, server/Program.cs, server/Database.cs, server/appsettings.json, server/Properties/launchSettings.json, web/package.json, web/package-lock.json, web/vite.config.js, web/index.html, web/src/main.jsx, web/src/App.jsx, web/src/UrlRow.jsx, web/src/App.css</files>
  <action>
**Start the clock (D1-27).** Before you run the first scaffold command, set `log.json` →
`timebox.buildStartedAt` to the current UTC time in ISO 8601 form with a trailing `Z`. Get it from
`date -u +%Y-%m-%dT%H:%M:%SZ`. Commit this change with the scaffold commit below.

**Scaffold (D1-01, D1-05).** From the repo root:
- Run `dotnet new sln -n HealthMonitor --format slnx`.
- Run `dotnet new web -n HealthMonitor -o server`.
- Run `dotnet sln HealthMonitor.slnx add server/HealthMonitor.csproj`.
- Run `dotnet add server package Microsoft.Data.Sqlite` and `dotnet add server package Dapper`.

Leave `server/appsettings.Development.json` as the template made it. In
`server/Properties/launchSettings.json`, set the `http` profile's `applicationUrl` to
`http://localhost:5080` and its `launchBrowser` to false. Delete the `https` profile, so the panel
is never asked to trust a dev certificate. Port 5080 avoids macOS AirPlay on 5000 (SKELETON).

Extend `.gitignore`. Keep the existing Node section and add a .NET section with these patterns:
`bin/`, `obj/`, `*.db`, `*.db-journal`, `*.db-wal`, `*.db-shm`, `server/wwwroot/`, and
`.vs/`. Suggested commit: `build: scaffold ASP.NET Core server and HealthMonitor solution; start
the build clock`.

**Config (D1-11, D1-14, D1-16, D1-16b).** In `server/appsettings.json`, add
`ConnectionStrings:HealthMonitor` set to `Data Source=healthmonitor.db`. Add a `Monitor` section
with `TickSeconds: 5` and `RequestTimeoutSeconds: 10`. Task 2 reads the `Monitor` section. The
relative path resolves to `server/healthmonitor.db`, because `dotnet run --project server` uses
`server/` as its working directory (checked at planning time).

**Database (D1-06..D1-11, D1-13).** Create `server/Database.cs` with a class `Database` that takes
the connection string in its constructor. Every method opens a new `SqliteConnection` in a `using`
block.
- `InitializeAsync()` runs one schema script, a C# const string with `CREATE TABLE IF NOT EXISTS`
  for `urls` and `check_results` (columns exactly as D1-06/D1-07) and
  `CREATE INDEX IF NOT EXISTS` on `check_results(url_id, checked_at)`. After that, if `urls` has
  zero rows, it inserts the 4 seeds from D1-13 in this exact order: example.com 30s, github.com
  60s, the github 404 page 30s, unreachable.invalid 30s. Each seed gets
  `created_at = DateTime.UtcNow.ToString("o")` (D1-08).
- `GetUrlStatusesAsync()` runs one query: `urls` LEFT JOIN its newest `check_results` row (pick it
  with a correlated subquery, `ORDER BY checked_at DESC LIMIT 1`), ordered by `urls.id`. It returns
  `UrlStatus(long Id, string Url, long IntervalSeconds, LastCheck? LastCheck)` and
  `LastCheck(string CheckedAt, long? StatusCode, long ResponseTimeMs, bool Healthy, string? Error)`.
  These records sit in `Database.cs`. `LastCheck` is null when the URL has no results (D1-09,
  D1-12).
- Pitfall: SQLite returns INTEGER columns as `Int64`, and Dapper's constructor mapping for
  positional records fails on type mismatches. Let Dapper map into a private flat row class with
  `{ get; set; }` properties typed `long`, `long?`, `string`, and `string?`. Then build the
  records in C# (`Healthy = row.Healthy == 1`). SQL aliases columns to the property names
  (`interval_seconds AS IntervalSeconds`), per SKELETON Conventions.

Suggested commit: `feat(server): create SQLite schema and seed four URLs on startup`.

**API and serving (D1-12, D-007).** In `server/Program.cs`, register `Database` as a singleton,
built from `builder.Configuration.GetConnectionString("HealthMonitor")`. After `builder.Build()`,
await `InitializeAsync()` once. Then call `app.UseDefaultFiles()` and `app.UseStaticFiles()`,
map `app.MapGet("/api/urls", ...)` returning `GetUrlStatusesAsync()`, and call `app.Run()`. JSON
stays camelCase by ASP.NET default, so the response has `lastCheck.checkedAt` and so on. Do not
add a SPA fallback, CORS, or Swagger. Suggested commit: `feat(server): add GET /api/urls with each
URL's latest check`.

**Web app (D-002, D-003, D1-04, D1-22..D1-26).** Hand-write the files. Do not use `npm create vite`,
which brings React 19 and ESLint extras (SKELETON).
- `web/package.json`: `private: true`, `type: "module"`, scripts `dev: vite`, `build: vite build`,
  `preview: vite preview`.
- Run `npm --prefix web install react@18 react-dom@18`, then
  `npm --prefix web install -D vite@8 @vitejs/plugin-react@6`. Commit `web/package-lock.json`.
  After installing, check that `web/package.json` has react and react-dom on `^18` and nothing else
  in dependencies.
- `web/vite.config.js`: `plugins: [react()]`; `build.outDir: '../server/wwwroot'` with
  `emptyOutDir: true` (D1-02); `server.proxy: { '/api': 'http://localhost:5080' }` (D1-04).
- `web/index.html` has `<div id="root"></div>` and a module script for `/src/main.jsx`. The page
  title is "URL Health Monitor".
- `web/src/main.jsx` mounts `App` with `createRoot` from `react-dom/client` inside `StrictMode`.
- `web/src/App.jsx` fetches `/api/urls` once in a `useEffect` (D1-25: no polling, no refresh
  button). A non-OK response throws `HTTP <status>`. While loading it shows "Loading…". On failure
  it shows "Could not load URLs: <message>" (D1-26). Otherwise it renders one `<table>` with the
  headers Status, URL, Code, Response time, Last checked, Error (D1-22), one `UrlRow` per URL,
  keyed by `id`.
- `web/src/UrlRow.jsx`: the status cell is a colored dot span plus a word. Use "Pending" (gray)
  when `lastCheck` is null, "Healthy" (green), or "Unhealthy" (red) (D1-23). Code shows
  `statusCode`, or `—` when it is null or there is no check. Response time shows `123 ms`, or `—`
  with no check. Last checked is `new Date(checkedAt).toLocaleTimeString()`, or `—`. Error shows
  the error text or nothing (D1-24). Render all text as JSX text only.
- `web/src/App.css`: plain and readable. Style the table borders and padding and the dot colors.
  Do no design work beyond that.

Suggested commits: `build(web): scaffold React 18 + Vite by hand (no template, React pinned to
18)`, then `feat(web): show seeded URLs in a status table`.

**Fresh-clone build (D1-03).** In `server/HealthMonitor.csproj`, add a target `BuildWebUi` with
`BeforeTargets="Build"`. It runs an `Exec` of `npm install` with `WorkingDirectory` set to
`$(MSBuildProjectDirectory)/../web`, only when `web/node_modules` does not exist. Then it runs an
`Exec` of `npm run build` in the same directory, only when
`$(MSBuildProjectDirectory)/wwwroot/index.html` does not exist. Add a one-line XML comment
explaining why: one command from a fresh clone, without committing built files. At planning time
a probe confirmed that a `wwwroot` created by a `BeforeTargets="Build"` target is served on the
very first `dotnet run`. After any later `web/src` change, refresh the built UI with
`npm --prefix web run build`. Suggested commit: `build: Vite builds into server/wwwroot and dotnet
build runs npm on a fresh clone`.
  </action>
  <verify>
    <automated>dotnet build HealthMonitor.slnx && git check-ignore -q server/wwwroot/index.html && git check-ignore -q server/healthmonitor.db && git check-ignore -q server/bin && rm -rf server/wwwroot web/node_modules server/healthmonitor.db /tmp/hm-urls.json && (dotnet run --project server > /tmp/hm-t1.log 2>&1 &) ; for i in $(seq 1 90); do curl -sf http://localhost:5080/api/urls > /tmp/hm-urls.json && break; sleep 2; done; curl -sf http://localhost:5080/ | grep -q 'id="root"' && test -f server/wwwroot/index.html && test -d web/node_modules && python3 -c "import json; d=json.load(open('/tmp/hm-urls.json')); assert [u['url'] for u in d]==['https://example.com','https://github.com','https://github.com/this-page-should-not-exist-rosnet','https://unreachable.invalid'], d; assert all(set(u)=={'id','url','intervalSeconds','lastCheck'} for u in d), d; print('T1 OK')"; R=$?; lsof -ti tcp:5080 | xargs kill 2>/dev/null; exit $R</automated>
    <fails_when>The command exits non-zero with no "T1 OK". This happens if `dotnet build` fails; if a `git check-ignore` misses (wwwroot, the db, or bin would be committed); if `/` never answers with the `id="root"` page (BuildWebUi did not rebuild the deleted wwwroot, or the server is not on 5080); or if the python assert prints an AssertionError (seeds missing, duplicated, out of order, or the JSON keys are not exactly id/url/intervalSeconds/lastCheck). Read /tmp/hm-t1.log for the cause.</fails_when>
  </verify>
  <done>From a state with no `server/wwwroot` and no `web/node_modules`, one `dotnet run --project server` installs and builds the UI and serves it at http://localhost:5080. GET /api/urls returns the 4 seeds in id order with `lastCheck: null`, and the page shows 4 "Pending" rows. `timebox.buildStartedAt` is set. Several small commits exist, each with the Co-Authored-By trailer.</done>
</task>

<task type="auto">
  <name>Task 2: Scheduled checks, each due URL is checked one after another and every result is saved as its own row</name>
  <files>server/UrlChecker.cs, server/CheckScheduler.cs, server/Database.cs, server/Program.cs, log.json</files>
  <action>
**Checker (D1-16..D1-21).** Create `server/UrlChecker.cs`. It is a plain class (no interface,
D1-21). Its constructor takes an `HttpClient`. `CheckAsync(string url, CancellationToken token)`
returns `CheckResult(int? StatusCode, long ResponseTimeMs, bool Healthy, string? Error)`, a record
in the same file.
- Send a `GET` with `HttpCompletionOption.ResponseHeadersRead` (D1-17). Dispose the response
  without reading the body. Redirects follow by default (D1-18).
- Time it with `Stopwatch` from send until headers arrive. Record the time for failures too
  (D1-19).
- Healthy means the final status code is 200–399 (CHK-03). A 4xx/5xx is unhealthy, with the code
  and a null error (D1-20).
- Catch the timeout as `TaskCanceledException` only when `token.IsCancellationRequested` is false.
  Record `Timed out after {N}s`, with N taken from the client's `Timeout.TotalSeconds`, so the text
  follows the configured value (D1-16b, D1-20).
- `HttpRequestException` records its `Message` (DNS failure, connection refused).
- Any other exception records its message as unhealthy. When the token is cancelled
  (app shutdown), let the cancellation propagate instead of recording it. A check must never
  crash the loop (D1-20).

Suggested commit: `feat(server): add UrlChecker (GET, headers only, healthy = 200–399)`.

**Persistence (HIST-01, D1-07, D1-08).** Add `InsertCheckResultAsync(long urlId, CheckResult result)`
to `Database`. It does one parameterized INSERT into `check_results`, with
`checked_at = DateTime.UtcNow.ToString("o")` and `healthy` stored as 1/0. It always INSERTs and
never UPDATEs, so history is append-only.

**Scheduler (D-006, D1-14, D1-15, D1-16).** Create `server/CheckScheduler.cs`, a
`CheckScheduler : BackgroundService`. Its constructor takes `Database`, `IHttpClientFactory`,
`IConfiguration`, and `ILogger<CheckScheduler>`.
- `ExecuteAsync` runs one check pass right away, so the dashboard is not "Pending" for the first
  tick. Then it loops on `PeriodicTimer(TimeSpan.FromSeconds(Monitor:TickSeconds))` with
  `WaitForNextTickAsync(stoppingToken)`.
- Each pass calls `GetUrlStatusesAsync()` and does the "is it due?" check in C#. Pick C# because
  it reads more easily than SQL date math (Claude's discretion). A URL is due when `LastCheck` is
  null, or when `DateTimeOffset.Parse(CheckedAt) + IntervalSeconds <= UtcNow` (D1-09, D1-14).
- Due URLs are checked one after another (D1-15, no concurrency until CHK-05 in Phase 2). Each
  one creates `new UrlChecker(httpClientFactory.CreateClient("checker"))`, awaits `CheckAsync`,
  then awaits `InsertCheckResultAsync`.
- Wrap each pass in try/catch. Log any exception with `ILogger` and keep looping. Without this,
  an unhandled exception in a BackgroundService stops the whole host by default in .NET 8+.

In `server/Program.cs`, register the named client with
`builder.Services.AddHttpClient("checker", c => c.Timeout = TimeSpan.FromSeconds(Monitor:RequestTimeoutSeconds))`
(D1-16) and register `builder.Services.AddHostedService<CheckScheduler>()`. Do not change the
dashboard. Task 1 already renders `lastCheck` fields when they are present. Suggested commit:
`feat(server): add CheckScheduler loop that checks due URLs and saves every result`.

If something here turns out to be an edge case (for example, a URL that answers but takes longer
than the timeout on every run), log it per the rules above and keep going.
  </action>
  <verify>
    <automated>rm -f server/healthmonitor.db /tmp/hm-urls.json && dotnet build HealthMonitor.slnx && (dotnet run --project server > /tmp/hm-t2.log 2>&1 &) ; for i in $(seq 1 60); do curl -sf http://localhost:5080/api/urls >/dev/null && break; sleep 2; done; sleep 15; curl -sf http://localhost:5080/api/urls > /tmp/hm-urls.json && python3 -c "import json; d={u['url']:u['lastCheck'] for u in json.load(open('/tmp/hm-urls.json'))}; assert len(d)==4, d; e=d['https://example.com']; n=d['https://github.com/this-page-should-not-exist-rosnet']; x=d['https://unreachable.invalid']; assert e and e['healthy'] is True and 200<=e['statusCode']<400 and e['responseTimeMs']>0, e; assert n and n['healthy'] is False and n['statusCode']==404, n; assert x and x['healthy'] is False and x['statusCode'] is None and x['error'], x; assert d['https://github.com'] and d['https://github.com']['checkedAt'].endswith('Z'), d; print('T2 classify OK')" && sleep 30 && test "$(sqlite3 server/healthmonitor.db "select count(*) from check_results r join urls u on u.id=r.url_id where u.url='https://example.com'")" -ge 2 && test "$(sqlite3 server/healthmonitor.db "select count(*) from check_results r join urls u on u.id=r.url_id where u.url='https://github.com'")" -eq 1 && echo "T2 interval OK"; R=$?; lsof -ti tcp:5080 | xargs kill 2>/dev/null; exit $R</automated>
    <fails_when>No "T2 classify OK" plus an AssertionError: a seed is still null (the scheduler never ran or the insert failed), example.com is not healthy with a 2xx/3xx code, the 404 seed's statusCode is not 404, or unreachable.invalid has no error text or a non-null code. Or no "T2 interval OK": example.com has fewer than 2 rows after ~45s (re-check on interval is broken), or github.com has more than 1 (per-URL intervals are ignored). Read /tmp/hm-t2.log for the cause.</fails_when>
    <automated>C1=$(sqlite3 server/healthmonitor.db "select count(*) from check_results") && (dotnet run --project server > /tmp/hm-t2b.log 2>&1 &) ; for i in $(seq 1 60); do curl -sf http://localhost:5080/api/urls >/dev/null && break; sleep 2; done; sleep 35; C2=$(sqlite3 server/healthmonitor.db "select count(*) from check_results"); U=$(sqlite3 server/healthmonitor.db "select count(*) from urls"); lsof -ti tcp:5080 | xargs kill 2>/dev/null; echo "before=$C1 after=$C2 urls=$U"; test "$C1" -gt 0 && test "$U" -eq 4 && test "$C2" -gt "$C1" && echo "T2 restart OK"</automated>
    <fails_when>No "T2 restart OK". If urls is not 4, the seeds were inserted again on restart. If after is less than or equal to before, history was reset or the scheduler did not resume. If before is 0 (or sqlite3 errors with "no such table"), the first run saved nothing or the DB file is not at server/healthmonitor.db.</fails_when>
  </verify>
  <done>Running the app checks every seed right away and then on its own interval. GET /api/urls shows example.com and github.com Healthy, the 404 seed Unhealthy with 404, and unreachable.invalid Unhealthy with an error reason. Each check is its own row in check_results, and a restart keeps the URLs and all earlier rows. Two commits (checker, scheduler) are in place, each with the trailer.</done>
</task>

<task type="auto">
  <name>Task 3: Record the Phase 1 decisions, AI notes, and new backend concepts for the owner</name>
  <files>log.json, docs/CONCEPTS.md</files>
  <action>
**log.json decisions** (CLAUDE.md section 3). Append these using the same keys as D-015: `id`,
`date`, `area`, `title`, `decision`, `alternatives` (each with `option` and `whyNot`),
`rationale`, `tradeoffs`, `plain` (STE-100), `ai` (`suggestedBy`, `accepted`, `changed`,
`rejected`), and `status`.
- **D-016, dependencies:** the exact package set and versions installed. Read them from
  `server/HealthMonitor.csproj` and `web/package.json`. Note that they were approved under
  D-002/D-004/D-005, and that `web/package.json` was hand-written instead of using the Vite
  template. Alternative: `npm create vite`, rejected because it defaults to React 19 and adds
  ESLint and other extras. Say that plugin-react's optional peers were left uninstalled.
- **D-017, local address:** http://localhost:5080, an http-only launch profile, and the browser
  does not open on launch. Alternatives: port 5000 (taken by macOS AirPlay Receiver) and keeping
  the https profile (makes the panel trust a dev certificate).
- Add another D-0NN only if you made a real choice the plan did not dictate. Do not log
  plan-dictated details again.
- `ai` blocks must be honest: Claude proposed these during planning, and the owner accepted the
  phase defaults in D-014.

**log.json aiNotes.** Append one entry, dated with today's date, for each notable AI moment that
actually happened during this run. Examples: generated code that failed to build or run, a plan
instruction that was wrong, a pitfall hit such as the Dapper/Int64 mapping. Name what was
rewritten. If nothing went wrong, append one note saying Claude generated all Phase 1 code from
the locked plan, and name the files the owner should read first to own it: `server/UrlChecker.cs`
and `server/CheckScheduler.cs`. Do not invent problems.

**log.json edgeCases.** Make sure every edge case met in Tasks 1–2 is logged (EC-012 onward, no
duplicates). Do not fix any of them.

**docs/CONCEPTS.md (D1-28, CLAUDE.md section 6).** Append entries in the existing style: `## Name`
heading, then **What it is**, **JavaScript comparison** (where one exists), and **Why we use it**.
Use STE-100 at a 5th-grade level: short sentences, one idea per sentence, active voice, common
words. Do not repeat the existing entries (C# and .NET, Minimal APIs, BackgroundService,
PeriodicTimer, async/await, IHttpClientFactory, Dependency injection, SQLite, Dapper). Use these
exact headings so the verify gate matches:
- `## The .csproj and the solution file` (compare with package.json and an npm workspace)
- `## appsettings.json` (compare with a config.json or a .env file read at startup)
- `## wwwroot and static files` (compare with `express.static`)
- `## MSBuild targets` (compare with an npm `prebuild` script)
- `## C# records` (compare with a plain object that has a fixed shape)
- `## CancellationToken` (compare with `AbortController` / `AbortSignal`)
- `## Stopwatch` (compare with `performance.now()`)
- `## HttpCompletionOption.ResponseHeadersRead` (compare with reading `response.status` from `fetch` without calling `.text()`)

Suggested commits: `docs: log Phase 1 package, port, and AI decisions`, then `docs(concepts):
explain the new backend ideas in plain English`. Each ends with the trailer.
  </action>
  <verify>
    <automated>python3 -c "import json; d=json.load(open('log.json')); t=d['timebox']['buildStartedAt']; assert t and t.endswith('Z'), t; ids=[x['id'] for x in d['decisions']]; assert len(ids)==len(set(ids)) and 'D-016' in ids and 'D-017' in ids, ids; new=[x for x in d['decisions'] if x['id']>='D-016']; assert all(x.get('alternatives') and x.get('plain') and x.get('ai') for x in new), new; ec=[e['id'] for e in d['edgeCases']]; assert len(ec)==len(set(ec)), ec; assert len(d['aiNotes'])>=5, d['aiNotes']; print('log OK')" && for h in '.csproj and the solution file' 'appsettings.json' 'wwwroot and static files' 'MSBuild targets' 'C# records' 'CancellationToken' 'Stopwatch' 'HttpCompletionOption.ResponseHeadersRead'; do grep -qF "## $h" docs/CONCEPTS.md || { echo "missing concept heading: $h"; exit 1; }; done && echo "concepts OK"</automated>
    <fails_when>Python prints an AssertionError: buildStartedAt is still null (it is null at HEAD), D-016 or D-017 is missing, a decision id or edge-case id appears twice, a new decision lacks alternatives, plain, or ai, or aiNotes still has only the 4 entries from HEAD. Or the loop prints "missing concept heading: X". None of these headings exist in docs/CONCEPTS.md at HEAD.</fails_when>
  </verify>
  <done>log.json is valid JSON with buildStartedAt set, D-016 and D-017 appended, every edge case met logged as deferred, and honest aiNotes. docs/CONCEPTS.md has the 8 new STE-100 entries with JavaScript comparisons and no duplicates. Both changes are committed with the trailer.</done>
</task>

</tasks>

<verification>
Run in order from the repo root after all tasks:
1. `dotnet build HealthMonitor.slnx` succeeds.
2. Task 1's automated block passes. It is the fresh-clone check: wwwroot and node_modules are
   removed, then one `dotnet run` rebuilds and serves everything.
3. Both of Task 2's automated blocks pass: classification, per-URL interval, and restart
   persistence.
4. Task 3's automated block passes.
5. `git status --porcelain` shows no `server/wwwroot`, `*.db`, `bin/`, `obj/`, or `node_modules`
   entries, and `git log --oneline` shows the story commits, each with the Co-Authored-By trailer
   (`git log -12 --format=%B | grep -c "Co-Authored-By: Claude Opus 5.5"` is at least the number
   of commits made).

Human check (end of phase, `human_verify_mode: end-of-phase`): run `dotnet run --project server`,
open http://localhost:5080, and confirm that the 4 rows show the dot plus word, the code, the
`ms`, the local time, and the error text. Then reload after 30s and confirm that the example.com
"Last checked" time moved.
</verification>

<success_criteria>
- ROADMAP Phase 1 success criteria 1–5 are all met, as shown by the Task 1 and Task 2 automated
  blocks.
- Requirements URL-04, CHK-01..CHK-04, HIST-01, HIST-02, DASH-01, DASH-02, and DLVR-01 are
  delivered.
- Only the approved packages were added. No tests, no edge-case handling, and nothing from the
  CONTEXT deferred list.
- log.json and docs/CONCEPTS.md are updated. Commit history tells the build story.
</success_criteria>

<output>
Create `.planning/quick/261107-gje-phase-1-walking-skeleton/261107-gje-SUMMARY.md` with
`status: complete` in its frontmatter. List the commits, any edge cases logged (by EC id), any AI
notes added, and any blocker (for example, a package that seemed needed but was not added).
</output>

# Kickoff Prompt

The prompt used to start the build with Claude Code. It is kept in the repo so the panel can see
exactly how the AI was directed. Paste it into a fresh session (or pass it to
`/gsd-new-project --auto @docs/KICKOFF_PROMPT.md`).

---

You are pairing with me on a timed take-home for a Senior Full-Stack Engineer role at Rosnet.
Read `CLAUDE.md` first and follow it exactly. `log.json` is our decision log and
`docs/CONCEPTS.md` holds simple explanations of backend concepts.

## The challenge

Build a small **URL / Service Health Monitor** that handles three things end to end:

1. **Checks.** A backend checks each URL on a schedule and captures, at minimum, the HTTP status
   code and the response time.
2. **History.** Results are persisted so we can see history, not just the latest state.
3. **UI.** A web UI where someone can see at a glance which URLs are healthy, which are not, and
   how things have looked over time.

The reviewers care about scoping, tradeoffs, and reasoning — not lines of code or coverage.
They would rather see a small thing done well than a sprawling thing half-done.

## Constraints

- **3-hour hard cap** on build time. If we approach it, stop and write what we would do next.
- Primary functionality first. Edge cases are logged in `log.json` → `edgeCases`, not built,
  until the core works end to end.
- Tests cover core requirements only. No edge-case tests.
- Ask before adding any package.

## Stack (decided — see `log.json` D-001 to D-007)

- **Backend:** C# / ASP.NET Core 10 Minimal APIs. One `BackgroundService` with a
  `PeriodicTimer` finds URLs that are due and checks them in parallel (capped concurrency,
  per-request timeout, `IHttpClientFactory`).
- **Data:** SQLite via `Microsoft.Data.Sqlite` + Dapper, hand-written SQL, schema script run at
  startup. Store all times in UTC.
- **Frontend:** React 18 (pinned) + Vite, plain JavaScript, plain CSS, native `fetch` with
  polling. No chart library — any over-time view uses plain HTML/CSS or inline SVG.
- **Run:** ASP.NET Core serves the built UI from `wwwroot`; Vite proxies `/api` in development.
- **Tests:** xUnit. Checker logic tested with a fake `HttpMessageHandler`; one API-level test
  with `WebApplicationFactory` against a temporary SQLite database.

## Features in scope

- Add and remove URLs from the UI. A few seed URLs on first run.
- **Per-URL check interval**, set when adding a URL and editable in the UI.
- Each check saves: URL id, timestamp (UTC), HTTP status code (nullable), response time in ms,
  healthy flag, and an error message when there was no response.
- **Healthy** = a response with status 200–399 within the timeout. Anything else — 4xx/5xx,
  timeout, DNS failure, connection refused — is **unhealthy** and is recorded, never thrown away.
- Dashboard: one row per URL with current status (clear healthy/unhealthy indicator), last
  status code, last response time, uptime % and average response time over the selected window,
  and a strip of recent checks colored by result.
- **History time window** selector in the UI (e.g. 1h / 24h / 7d).
- **Retention period** (global setting, editable in the UI). Results older than it are deleted
  on a timer.

## Out of scope (documented in the README instead)

Alerting, response-body assertions, retry/backoff, auth, multi-tenancy, containerization,
a design system, TypeScript, Postgres.

## How we work

1. Before code: confirm the data model and API endpoints, and log them in `log.json`.
2. Build a thin vertical slice first: one URL checked → saved → shown in the UI. Then widen.
3. Commit small and often with messages that tell the story.
4. Every decision → `log.json` `decisions`. Every AI suggestion I reject or rewrite → `aiNotes`.
5. Every new backend concept → `docs/CONCEPTS.md` in STE-100 at a 5th-grade level.
6. Every edge case noticed → `log.json` `edgeCases` with a priority. Do not fix it yet.
7. When the core is done: fix edge cases by priority while time remains.

## Definition of done

- `dotnet run` from a fresh clone starts the app; the dashboard shows seeded URLs being checked.
- Adding a URL with an interval makes it appear and get checked on that interval.
- An unreachable URL shows as unhealthy with a reason.
- Changing the history window changes the uptime/average numbers and the check strip.
- Old results are removed after the retention period.
- Core tests pass.
- README covers: what I built, how to run it, what I cut, what I'd do next, the testing approach
  (including what I didn't test and why — taken from `edgeCases`), and how I used AI.

# Rosnet URL Health Monitor

## What This Is

A small URL / service health monitor built as a timed take-home for the Rosnet Senior Full-Stack
Engineer role. A C# backend checks a user-managed list of URLs on per-URL schedules, stores every
result in SQLite, and a React 18 dashboard shows at a glance which URLs are healthy, which are
not, and how they have looked over a selectable time window. The audience is the Rosnet panel,
who will clone it, run it, read the code and commit history, and question every decision.

## Core Value

A user can open the dashboard and immediately see which URLs are healthy or unhealthy now and
how each has behaved over time — backed by real scheduled checks and persisted history.

## Requirements

### Validated

(None yet — ship to validate)

### Active

- [ ] Backend checks each URL on its own interval and records HTTP status code and response time
- [ ] Failed checks (4xx/5xx, timeout, DNS failure, connection refused) are recorded as unhealthy with a reason
- [ ] Every check result is persisted in SQLite (history, not just latest state)
- [ ] User can add and remove URLs from the UI, with a per-URL check interval; a few URLs are seeded on first run
- [ ] User can edit a URL's check interval; the change applies without restart
- [ ] Dashboard shows one row per URL: current healthy/unhealthy indicator, last status code, last response time
- [ ] Dashboard shows uptime % and average response time over the selected window, plus a strip of recent checks colored by result
- [ ] User can choose the history time window (e.g. 1h / 24h / 7d)
- [ ] User can set a global retention period; results older than it are deleted automatically
- [ ] `dotnet run` from a fresh clone serves both API and UI
- [ ] Core-requirement tests (xUnit) pass: check classification, persistence, history read-back
- [ ] README: what was built, how to run, what was cut, next steps, testing approach (incl. what wasn't tested and why), how AI was used

### Out of Scope

- Alerting / notifications — bonus territory per the brief; doesn't fit the 3-hour cap
- Response-body assertions — status + timing satisfy the core requirement
- Retry and backoff — a failed check is a valid data point; retries would hide flakiness
- Auth and multi-tenancy — single-user local demo
- Containerization — SQLite + `dotnet run` already gives zero-setup runs
- Design system / polished UI — the brief values a working solution with a clear README over polish
- Chart libraries — over-time view is plain HTML/CSS or inline SVG to keep dependencies light
- TypeScript — time and ownership; listed as a next step (D-003)
- Postgres — reviewers would need extra setup; documented as the production path (D-004)
- Edge cases in `log.json` → `edgeCases` — deferred until core works; fixed by priority only if time remains

## Context

- Brief: Rosnet "Senior Full-Stack Engineer — Take-Home Technical Challenge" (URL Health Monitor). Summarized in `docs/KICKOFF_PROMPT.md`.
- Panel: 90 minutes — ~5 min intro, 25–30 min solution walkthrough and demo (live run), remainder Q&A. They pull the repo 24–48h ahead and review code and commit history (no squash / force-push).
- They will dig into: scoping, backend design (storage, data model, scheduling, concurrency, error handling), frontend design (components, state, data fetching, UX), testing tradeoffs, AI collaboration (what was accepted / thrown out), line-level ownership, next steps.
- Owner is primarily a frontend JavaScript developer. Backend concepts are explained in `docs/CONCEPTS.md` (STE-100, 5th-grade level).
- Every decision is logged in `log.json` with alternatives, tradeoffs, plain explanation, and AI involvement. `CLAUDE.md` holds the working rules.
- Greenfield repo. macOS dev machine; .NET SDK must be installed (`brew install --cask dotnet-sdk`).

## Constraints

- **Timeline**: 3-hour hard cap on build time — the brief asks to stop and write down next steps rather than push past it
- **Tech stack**: C# / ASP.NET Core 10 Minimal APIs, SQLite + Dapper, React 18 + Vite, plain JavaScript and CSS — Rosnet is a .NET shop; owner wants light dependencies
- **Dependencies**: ask before adding any package — efficiency and explainability
- **Testing**: core requirements only, no edge-case tests — brief rewards reasoning over coverage
- **Ownership**: code must be simple enough for the owner to explain any line

## Key Decisions

Full detail, alternatives, and AI involvement live in `log.json`.

| Decision | Rationale | Outcome |
|----------|-----------|---------|
| C# / ASP.NET Core 10 Minimal APIs (D-001) | Rosnet stack; Express-like brevity | — Pending |
| React 18 + Vite, no frameworks (D-002) | Lightest path to JSX; pin React 18 | — Pending |
| Plain JavaScript, not TypeScript (D-003) | Speed and ownership within cap | — Pending |
| SQLite (D-004) | Zero setup for reviewers | — Pending |
| Dapper over EF Core (D-005) | Visible, explainable SQL | — Pending |
| BackgroundService + PeriodicTimer "what's due" loop (D-006) | No job library; per-URL intervals fall out naturally | — Pending |
| One process serves API + UI (D-007) | One command to demo, no CORS | — Pending |
| Per-URL intervals, history window, retention in scope (D-008) | Owner priority; cheap given D-006 | — Pending |
| Core-only tests, edge cases deferred (D-009) | Brief asks what was skipped and why | — Pending |
| Fresh session per GSD step (D-011) | Clean context; state on disk | — Pending |
| Healthy = status 200–399 within timeout | Simple, explainable rule | — Pending |

## Evolution

This document evolves at phase transitions and milestone boundaries.

**After each phase transition** (via `/gsd-transition`):
1. Requirements invalidated? → Move to Out of Scope with reason
2. Requirements validated? → Move to Validated with phase reference
3. New requirements emerged? → Add to Active
4. Decisions to log? → Add to Key Decisions
5. "What This Is" still accurate? → Update if drifted

**After each milestone** (via `/gsd-complete-milestone`):
1. Full review of all sections
2. Core Value check — still the right priority?
3. Audit Out of Scope — reasons still valid?
4. Update Context with current state

---
*Last updated: 2026-11-07 after initialization*

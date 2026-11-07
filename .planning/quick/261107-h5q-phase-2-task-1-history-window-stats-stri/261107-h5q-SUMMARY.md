---
phase: 02-features-readme
plan: 01
quick_id: 261107-h5q
subsystem: history-window
tags: [aspnet-minimal-api, sqlite, dapper, react, polling]
requires: [Phase 1 walking skeleton]
provides: [window-filtered dashboard API, per-URL history endpoint, window picker, recent-checks strip, 5s polling]
affects: [next Phase 2 task: core xUnit tests call Database.GetWindowSummaryAsync / GetChecksSinceAsync directly]
tech-stack:
  added: []
  patterns: [cutoff passed as ISO "o" UTC text, flat Dapper row classes, per-URL small queries]
key-files:
  created: []
  modified: [server/Database.cs, server/Program.cs, web/src/App.jsx, web/src/UrlRow.jsx, web/src/App.css, log.json, docs/CONCEPTS.md]
decisions: [D-018 window API shape, D-019 average counts only checks that got an HTTP response]
requirements-completed: [HIST-03, DASH-03, DASH-04, DASH-05, DASH-06]
status: complete
commits: 6
plan_head_before: 6eec7df8006b070d431fc2eacfb1b8163c5f98c6
plan_head_after: 9090ad2bb65503caa77ff24ed2ae4a828bb6f7e7
actuals:
  tokens: 14000
  tasks: 3
  commits: 6
completed: 2026-11-07
---

# Phase 2 Plan 01: History window, stats, and recent-checks strip Summary

A 1h/24h/7d window runs end to end. SQL aggregates give uptime % and an average that counts only checks with an HTTP response. A colored strip shows the 30 newest checks. The dashboard re-fetches every 5 seconds without a reload.

## Commits

| Hash | Message |
| ---- | ------- |
| fe8bd90 | feat(server): add ?window=1h\|24h\|7d with uptime % and average response time per URL |
| d8f10d5 | feat(web): window picker and uptime/avg columns, refreshed every 5s without reload |
| cf0d32a | feat(server): return the 30 newest checks per URL and add GET /api/urls/{id}/checks?window= |
| 705192e | feat(web): recent-checks strip colored by result, with a tooltip for each check |
| 3965420 | docs(log): D-018 window API shape, D-019 average counts responses only, new edge cases |
| 9090ad2 | docs(concepts): explain query strings, SQL aggregates, and text time comparison |

## Final JSON shapes

`GET /api/urls?window=1h|24h|7d` (no window means 1h, anything else is 400 `{ "error": "window must be 1h, 24h, or 7d" }`):

```json
[{
  "id": 1, "url": "https://example.com", "intervalSeconds": 30,
  "lastCheck": { "checkedAt": "...Z", "statusCode": 200, "responseTimeMs": 87, "healthy": true, "error": null },
  "summary": {
    "checkCount": 12, "uptimePercent": 100.0, "avgResponseTimeMs": 90.0,
    "recent": [ { "checkedAt": "...", "statusCode": 200, "responseTimeMs": 87, "healthy": true, "error": null } ]
  }
}]
```

`recent` has at most 30 checks, oldest first, ending at `lastCheck`. `uptimePercent` and `avgResponseTimeMs` are `null` when there is nothing to count.

`GET /api/urls/{id}/checks?window=...` returns a JSON array of checks (same five keys as above), oldest first, in the window only. Same 400 and 1h default.

## Verification

- Task 1 automated block: `T1 OK` (window whitelist and 400, 7d > 24h > 1h counts with probe rows, DNS failure shows 0% and null average, 404 seed shows a real average).
- Task 2 automated block: `T2 OK` (recent capped at 30 and ordered, `/checks` filtered by window, 400 on bad window, counts agree with the dashboard).
- Task 3 automated block: `log OK`, `concepts OK`.
- Probe rows (`h5q-probe`, `h5q-cap`) deleted after each run; final count is 0. Port 5080 is free.
- The human check (picker, strip growth, tooltip in a browser) was not done by this executor.

## Edge cases logged (all deferred)

EC-016 unknown id on `/checks` returns `[]`; EC-017 `/checks` has no paging; EC-018 polling continues in a hidden tab; EC-019 a slow poll can overlap the next; EC-020 a fresh database makes the three windows look the same; EC-021 the summary query reads every row in the window on each poll.

## AI notes added

One entry: Claude generated this code from the plan, built and probed without a rewrite, followed the plan's pitfall warnings without testing the unsafe versions. Read `Database.GetWindowSummaryAsync` first.

## Deviations from Plan

None - plan executed exactly as written. The commits go on `main` (the project works on `main`, `branching_strategy: none`, per the orchestrator).

## Known Stubs

None.

## Blockers

None. No package was needed or added.

## Self-Check: PASSED

All six commits are ancestors of HEAD; the modified files exist; probe rows 0; `server/wwwroot` and `*.db` are gitignored and unstaged.

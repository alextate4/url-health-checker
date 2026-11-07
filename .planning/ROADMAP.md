# Roadmap: Rosnet URL Health Monitor

## Overview

The build has a 3-hour hard cap. Phase 1 is a walking skeleton: seeded URLs are checked on a
schedule, every result is saved in SQLite, and a React dashboard shows the latest health of each
URL, all served by one `dotnet run`. Phase 2 widens that slice until every core requirement is
met: the over-time view (window, uptime, average response time, recent-checks strip), the core
tests, URL management, concurrent checks, retention, and the README. **The submission is complete
at the end of Phase 2.** Stretch work (extras, then deferred edge cases) is not a phase: it runs as
`/gsd-quick` tasks only if time remains (D-012, D-015). If any stretch work is cut, the README's
"what was cut" and "next steps" sections must still be updated before stopping.

## Phases

**Phase Numbering:**
- Integer phases (1, 2, 3): Planned milestone work
- Decimal phases (2.1, 2.2): Urgent insertions (marked with INSERTED)

Decimal phases appear between their surrounding integers in numeric order.

- [x] **Phase 1: Walking Skeleton** - Seeded URLs checked on a schedule, saved in SQLite, shown in the React dashboard, all from one `dotnet run`
- [x] **Phase 2: Features & README** - Window picker, uptime %, avg response time, recent-checks strip, auto-refresh, core tests, add/remove/re-time URLs, concurrent checks, retention, README (core complete)

## Phase Details

### Phase 1: Walking Skeleton
**Goal:** As a user, I want to open the dashboard and see each URL's latest health from real scheduled checks, so that I know at a glance which URLs are up right now.
**Mode:** mvp
**Depends on**: Nothing (first phase)
**Requirements**: URL-04, CHK-01, CHK-02, CHK-03, CHK-04, HIST-01, HIST-02, DASH-01, DASH-02, DLVR-01
**Success Criteria** (what must be TRUE):
  1. From a fresh clone, one `dotnet run` serves both the API and the built React dashboard from a single local address, with no second server and no CORS setup.
  2. On first run the dashboard lists a few seeded URLs, including one that is expected to fail, and each row shows a healthy/unhealthy indicator, the last status code, and the last response time in ms.
  3. Each URL is checked again once its own interval has elapsed: reloading the dashboard after that interval shows a newer result.
  4. A URL that answers 200–399 within the timeout shows healthy; the failing seed (4xx/5xx, timeout, DNS failure, or connection refused) shows unhealthy, and when there was no response its error reason is saved and returned by the API.
  5. After the app is stopped and started again, the URLs and every earlier check result are still there, because each check is stored as its own SQLite row, not as an overwrite of the latest state.
**Plans**: TBD
**UI hint**: yes

### Phase 2: Features & README
**Goal:** As a user, I want to see how each URL has behaved over a chosen time window and to add, remove, and re-time URLs and set how long history is kept, so that I can tell how each URL behaves over time and control what is monitored without restarting the app.
**Mode:** mvp
**Depends on**: Phase 1
**Workflow**: `/gsd-quick` tasks, no discuss or plan-check cycle (D-015)
**Requirements**: HIST-03, DASH-03, DASH-04, DASH-05, DASH-06, DLVR-02, URL-01, URL-02, URL-03, CHK-05, CONF-01, CONF-02, DLVR-03
**Success Criteria** (what must be TRUE):
  1. Each dashboard row shows uptime % and average response time calculated over the selected window.
  2. Each row shows a strip of recent checks, with each check colored by its healthy or unhealthy result.
  3. Switching the window between 1h, 24h, and 7d updates the stats and the strip to match, and the API returns a URL's check results for only the requested window.
  4. New check results appear on the dashboard on a polling interval, without a page reload.
  5. `dotnet test` passes core xUnit tests that can each fail for a real reason: check classification (healthy, unhealthy status, unreachable), result persistence, and history read-back for a window.
  6. User can add a URL with a check interval and remove a URL from the UI; the dashboard shows each change, and a newly added URL gets its first check, without a page reload.
  7. User can change a URL's check interval from the UI, and the new interval applies on the next scheduler tick with no restart.
  8. Adding a slow or timing-out URL does not delay checks for the other URLs, because checks run concurrently up to a fixed cap.
  9. User can set the global retention period from the UI, and check results older than it are deleted automatically on a timer.
  10. The README covers what was built, how to run it, what was cut, next steps, the testing approach (including what was not tested and why, with the deferred edge cases), and how AI was used, so the submission is complete even if all stretch work is cut.
**Plans**: TBD
**UI hint**: yes

## Stretch (not a phase — `/gsd-quick` only if time remains)

Run only after Phase 2 is complete. One `/gsd-quick` task per item, committed when it works, in
this order. At the 3-hour cap, stop and list everything unbuilt in the README.

1. **Extras** (EXTRA-01 → EXTRA-04, cut from the bottom): pause/resume a URL; "check now"; URL
   detail view with a table of individual checks; inline SVG response-time sparkline.
2. **Deferred edge cases** (EDGE-01): fix `log.json` → `edgeCases` strictly in priority order and
   mark each `fixed`; every one still deferred goes in the README under "What I didn't handle or
   test, and why".

## Progress

**Execution Order:**
Phases execute in numeric order: 1 → 2. Both are core and must complete. Stretch items run as
`/gsd-quick` tasks afterwards and are cut from the bottom at the 3-hour cap.

| Phase | Plans Complete | Status | Completed |
|-------|----------------|--------|-----------|
| 1. Walking Skeleton | 1/1 (quick 261107-gje) | Complete | 2026-11-07 |
| 2. Features & README | 5 quick tasks | Complete | 2026-11-07 |

# Roadmap: Rosnet URL Health Monitor

## Overview

The build has a 3-hour hard cap. Phase 1 is a walking skeleton: seeded URLs are checked on a
schedule, every result is saved in SQLite, and a React dashboard shows the latest health of each
URL, all served by one `dotnet run`. Phases 2 and 3 widen that slice until every core requirement
is met: first the over-time view (window, uptime, average response time, recent-checks strip) and
the core tests, then URL management, concurrent checks, retention, and the README. **The
submission is complete at the end of Phase 3.** Phases 4 and 5 are the stretch tier (D-012):
extras in numbered order, then deferred edge cases, cut from the bottom when the cap is reached.
If any stretch work is cut, the README's "what was cut" and "next steps" sections must still be
updated before stopping.

## Phases

**Phase Numbering:**
- Integer phases (1, 2, 3): Planned milestone work
- Decimal phases (2.1, 2.2): Urgent insertions (marked with INSERTED)

Decimal phases appear between their surrounding integers in numeric order.

- [ ] **Phase 1: Walking Skeleton** - Seeded URLs checked on a schedule, saved in SQLite, shown in the React dashboard, all from one `dotnet run`
- [ ] **Phase 2: Health Over Time** - Window picker, uptime %, average response time, recent-checks strip, auto-refresh, core tests
- [ ] **Phase 3: URL Management, Retention & README** - Add/remove/re-time URLs, concurrent checks, retention, README (core complete)
- [ ] **Phase 4: Stretch Extras** - Pause/resume, check now, detail view, sparkline; built in that order, cut from the bottom at the cap
- [ ] **Phase 5: Deferred Edge Cases** - Fix logged edge cases by priority if time remains; document the rest

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

### Phase 2: Health Over Time
**Goal:** As a user, I want to pick a time window and see each URL's uptime, average response time, and recent checks, so that I can tell how it has behaved over time.
**Mode:** mvp
**Depends on**: Phase 1
**Requirements**: HIST-03, DASH-03, DASH-04, DASH-05, DASH-06, DLVR-02
**Success Criteria** (what must be TRUE):
  1. Each dashboard row shows uptime % and average response time calculated over the selected window.
  2. Each row shows a strip of recent checks, with each check colored by its healthy or unhealthy result.
  3. Switching the window between 1h, 24h, and 7d updates the stats and the strip to match, and the API returns a URL's check results for only the requested window.
  4. New check results appear on the dashboard on a polling interval, without a page reload.
  5. `dotnet test` passes core xUnit tests that can each fail for a real reason: check classification (healthy, unhealthy status, unreachable), result persistence, and history read-back for a window.
**Plans**: TBD
**UI hint**: yes

### Phase 3: URL Management, Retention & README
**Goal:** As a user, I want to add, remove, and re-time URLs and set how long history is kept, so that I control what is monitored without restarting the app.
**Mode:** mvp
**Depends on**: Phase 2
**Requirements**: URL-01, URL-02, URL-03, CHK-05, CONF-01, CONF-02, DLVR-03
**Success Criteria** (what must be TRUE):
  1. User can add a URL with a check interval and remove a URL from the UI; the dashboard shows each change, and a newly added URL gets its first check, without a page reload.
  2. User can change a URL's check interval from the UI, and the new interval applies on the next scheduler tick with no restart.
  3. Adding a slow or timing-out URL does not delay checks for the other URLs, because checks run concurrently up to a fixed cap.
  4. User can set the global retention period from the UI, and check results older than it are deleted automatically on a timer.
  5. The README covers what was built, how to run it, what was cut, next steps, the testing approach (including what was not tested and why, with the deferred edge cases), and how AI was used, so the submission is complete even if Phases 4 and 5 are cut.
**Plans**: TBD
**UI hint**: yes

### Phase 4: Stretch Extras
**Goal:** As a user, I want to pause, re-check, and drill into a single URL, so that I can investigate a problem URL in detail; extras are built in order EXTRA-01 to EXTRA-04 and cut from the bottom if the 3-hour cap is reached.
**Mode:** mvp
**Depends on**: Phase 3
**Requirements**: EXTRA-01, EXTRA-02, EXTRA-03, EXTRA-04
**Success Criteria** (what must be TRUE):
  1. User can pause checking a URL and later resume it; while it is paused no new results are recorded, and the URL and its history stay in place.
  2. User can trigger "check now" on a URL and see a fresh result on its row without waiting for its interval.
  3. User can open a detail view for a URL that shows a table of its individual checks: time, status, ms, and error.
  4. Each dashboard row shows an inline SVG sparkline of response time over the selected window.
**Plans**: TBD
**UI hint**: yes
**Cut rule**: Build one extra at a time in numbered order and commit each one when it works. At the 3-hour cap, stop. Mark unbuilt extras as Deferred here, list them in the README under "what was cut" and "next steps", and add built extras to the README's "what was built". The README update is required even if this whole phase is cut.

### Phase 5: Deferred Edge Cases
**Goal:** As a user, I want to have the highest-priority deferred edge cases handled and the rest documented, so that the monitor behaves sensibly and every known gap is explained; this runs only if time remains after Phase 4.
**Mode:** mvp
**Depends on**: Phase 4
**Requirements**: EDGE-01
**Success Criteria** (what must be TRUE):
  1. Edge cases from `log.json` → `edgeCases` are fixed strictly in priority order (priority 1 first), and each fixed one is marked `fixed` in `log.json`.
  2. Each fixed edge case can be shown in the running app; for example, a malformed URL or an interval below the minimum is refused with a clear message instead of being saved.
  3. Every edge case still deferred at the cap is listed in the README under "What I didn't handle or test, and why".
**Plans**: TBD
**Cut rule**: Starts only after Phase 4 is complete or cut. At the 3-hour cap, stop and make sure the README lists every edge case that is still deferred.

## Progress

**Execution Order:**
Phases execute in numeric order: 1 → 2 → 3 → 4 → 5. Phases 1–3 are core and must all complete.
Phases 4–5 are stretch and are cut from the bottom at the 3-hour cap.

| Phase | Plans Complete | Status | Completed |
|-------|----------------|--------|-----------|
| 1. Walking Skeleton | 0/TBD | Not started | - |
| 2. Health Over Time | 0/TBD | Not started | - |
| 3. URL Management, Retention & README | 0/TBD | Not started | - |
| 4. Stretch Extras | 0/TBD | Not started | - |
| 5. Deferred Edge Cases | 0/TBD | Not started | - |

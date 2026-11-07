# Requirements: Rosnet URL Health Monitor

**Defined:** 2026-11-07
**Core Value:** A user can open the dashboard and immediately see which URLs are healthy or unhealthy now and how each has behaved over time — backed by real scheduled checks and persisted history.

## v1 Requirements

Requirements for the take-home submission. Each maps to a roadmap phase. Build order matters:
core categories first; **EXTRA** and **EDGE** are built last and cut from the bottom if the
3-hour cap is reached.

### URL Management

- [ ] **URL-01**: User can add a URL with a check interval from the UI
- [ ] **URL-02**: User can remove a URL from the UI
- [ ] **URL-03**: User can change a URL's check interval; the new interval applies on the next scheduler tick without a restart
- [ ] **URL-04**: On first run the app seeds a few URLs, including one that is expected to fail, so the demo shows both states

### Checks

- [ ] **CHK-01**: Backend checks each URL when its interval has elapsed since its last check
- [ ] **CHK-02**: Each check records the HTTP status code and the response time in milliseconds
- [ ] **CHK-03**: A check is healthy when it gets a 200–399 status within the timeout
- [ ] **CHK-04**: A check with a 4xx/5xx status, or with no response (timeout, DNS failure, connection refused), is recorded as unhealthy; when there is no response, the error reason is saved
- [ ] **CHK-05**: Checks for different URLs run concurrently with a concurrency cap, so one slow URL does not delay the others

### History

- [ ] **HIST-01**: Every check result is saved in SQLite with URL id, UTC timestamp, status code (nullable), response time, healthy flag, and error message (nullable)
- [ ] **HIST-02**: URLs, settings, and history survive an app restart
- [ ] **HIST-03**: The API returns check results for a URL within a requested time window

### Dashboard

- [ ] **DASH-01**: Dashboard lists every URL with a clear healthy/unhealthy indicator based on its latest check
- [ ] **DASH-02**: Each row shows the last status code and last response time
- [ ] **DASH-03**: Each row shows uptime % and average response time over the selected window
- [ ] **DASH-04**: Each row shows a strip of recent checks colored by result
- [ ] **DASH-05**: User can choose the history window (1h / 24h / 7d); stats and strip update to match
- [ ] **DASH-06**: Dashboard refreshes automatically on a polling interval without a page reload

### Settings

- [ ] **CONF-01**: User can set the global retention period from the UI
- [ ] **CONF-02**: Check results older than the retention period are deleted automatically on a timer

### Delivery

- [ ] **DLVR-01**: `dotnet run` from a fresh clone serves both the API and the built UI
- [ ] **DLVR-02**: Core tests pass: check classification (healthy, unhealthy status, unreachable), result persistence, and history read-back for a window
- [ ] **DLVR-03**: README covers what was built, how to run it, what was cut, next steps, testing approach (including what was not tested and why), and how AI was used

### Extras (stretch — build in this order, cut from the bottom)

- [ ] **EXTRA-01**: User can pause and resume checking a URL without deleting it or its history
- [ ] **EXTRA-02**: User can trigger an immediate "check now" for a URL
- [ ] **EXTRA-03**: User can open a URL detail view showing a table of its individual checks (time, status, ms, error)
- [ ] **EXTRA-04**: Each dashboard row shows an inline SVG sparkline of response time over the selected window

### Edge Cases (stretch — only if time remains)

- [ ] **EDGE-01**: Deferred edge cases in `log.json` → `edgeCases` are fixed in priority order while time remains; anything left is documented in the README

## v2 Requirements

Deferred. Tracked but not in the current roadmap.

### Quality

- **QUAL-01**: Frontend migrated to TypeScript with an API client generated from the OpenAPI spec
- **QUAL-02**: Edge-case tests for the items in `log.json` → `edgeCases`

### Monitoring

- **MON-01**: "Degraded" state for responses slower than a threshold
- **MON-02**: Alerting when a URL changes from healthy to unhealthy
- **MON-03**: Response-body assertions (e.g. expected text or JSON field)

### Platform

- **PLAT-01**: Postgres storage with time-partitioned history for production scale
- **PLAT-02**: Container image and deployment configuration

## Out of Scope

Explicitly excluded. Documented to prevent scope creep.

| Feature | Reason |
|---------|--------|
| Retry and backoff | A failed check is a real data point; retries would hide flakiness |
| Auth and multi-tenancy | Single-user local demo |
| Design system / polished UI | Brief values a working solution and clear README over polish |
| Chart libraries | Over-time view uses plain HTML/CSS and inline SVG to keep dependencies light |
| Job-scheduling libraries (Hangfire, Quartz) | A BackgroundService loop covers per-URL intervals (D-006) |

## Traceability

Which phases cover which requirements. Updated during roadmap creation.

| Requirement | Phase | Status |
|-------------|-------|--------|
| URL-01 | Phase 3 | Pending |
| URL-02 | Phase 3 | Pending |
| URL-03 | Phase 3 | Pending |
| URL-04 | Phase 1 | Pending |
| CHK-01 | Phase 1 | Pending |
| CHK-02 | Phase 1 | Pending |
| CHK-03 | Phase 1 | Pending |
| CHK-04 | Phase 1 | Pending |
| CHK-05 | Phase 3 | Pending |
| HIST-01 | Phase 1 | Pending |
| HIST-02 | Phase 1 | Pending |
| HIST-03 | Phase 2 | Pending |
| DASH-01 | Phase 1 | Pending |
| DASH-02 | Phase 1 | Pending |
| DASH-03 | Phase 2 | Pending |
| DASH-04 | Phase 2 | Pending |
| DASH-05 | Phase 2 | Pending |
| DASH-06 | Phase 2 | Pending |
| CONF-01 | Phase 3 | Pending |
| CONF-02 | Phase 3 | Pending |
| DLVR-01 | Phase 1 | Pending |
| DLVR-02 | Phase 2 | Pending |
| DLVR-03 | Phase 3 | Pending |
| EXTRA-01 | Phase 4 | Pending |
| EXTRA-02 | Phase 4 | Pending |
| EXTRA-03 | Phase 4 | Pending |
| EXTRA-04 | Phase 4 | Pending |
| EDGE-01 | Phase 5 | Pending |

**Coverage:**
- v1 requirements: 28 total
- Mapped to phases: 28
- Unmapped: 0 ✓

---
*Requirements defined: 2026-11-07*
*Last updated: 2026-11-07 after roadmap creation*

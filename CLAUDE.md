# Rosnet Case Study — URL Health Monitor

Take-home for the Rosnet Senior Full-Stack Engineer role. The brief is summarized in
`docs/KICKOFF_PROMPT.md`. The panel reads the code **and the commit history**, then asks us to
defend every line, every decision, and how AI was used.

## 1. Time cap

- Hard cap: **3 hours of build time**. The clock is tracked in `log.json` → `timebox`.
- Always pick the smaller version of a feature. If something will not fit, stop and write it in
  the README "Next steps" section instead of building it.
- Primary functionality first. Polish and edge cases come last, only if time is left.

## 2. Locked stack and dependencies

- Backend: C# / ASP.NET Core 10 (LTS) Minimal APIs, `BackgroundService` + `PeriodicTimer`,
  `IHttpClientFactory`, SQLite via `Microsoft.Data.Sqlite` + Dapper.
- Frontend: React **18** (pin it — the Vite template defaults to 19) + Vite, **plain JavaScript**
  (no TypeScript), plain CSS, native `fetch`. No Next.js, Tailwind, UI kits, chart libraries,
  or data-fetching libraries.
- ASP.NET Core serves the built frontend from `wwwroot`, so one `dotnet run` starts everything.
- **Ask before adding any NuGet or npm package.** Every new package gets a `log.json` decision.

## 3. Decision logging — `log.json`

Every real decision (architecture, data model, library, scope cut, test choice) gets appended to
`log.json` → `decisions` with: the choice, alternatives and why not, tradeoffs, a `plain`
explanation (see section 6), and an `ai` block saying who suggested it and what was accepted,
changed, or rejected. Notable AI moments that are not decisions (bad suggestion, bug in generated
code, something rewritten by hand) go in `log.json` → `aiNotes`. Be honest — the panel will ask
"what did you throw out?"

## 4. Edge cases — log, do not build

- Do **not** handle edge cases during the main build. When one comes up, add it to
  `log.json` → `edgeCases` with a priority and keep going.
- At the end, if time remains, fix them in priority order and mark them `fixed`.
- Unfixed ones go in the README under "What I didn't handle or test, and why".
- A failed check (timeout, DNS failure, connection refused, non-2xx) being **recorded as
  unhealthy is core behavior**, not an edge case.

## 5. Tests — core requirements only

- Unit/integration tests cover the core requirements only: a URL gets checked and the result
  (status, response time, healthy/unhealthy) is correct; results are saved; history can be read
  back. No edge-case tests. No shallow tests written to pad a number.
- Every test must be able to fail for a real reason. Say what each test proves.

## 6. Code we can own, explained simply

- No clever abstractions, no interfaces or layers "for later". Small files, obvious names.
  Comments explain **why**, not what.
- The owner is a frontend JavaScript developer. When a backend/C#/database concept is used for
  the first time, add an entry to `docs/CONCEPTS.md` written in **STE-100 Simplified Technical
  English at a 5th-grade reading level**: short sentences, one idea per sentence, active voice,
  common words, and a JavaScript comparison where one exists.

## 7. Sessions & context

- Work moves in GSD phases: discuss → plan → execute → verify. Start each step in a **fresh
  session** (`/clear`), not a `/compact` chain. `/gsd-next` picks the right next step.
- At session start, read `.planning/STATE.md` and the latest entries in `log.json` first.
- Before a session ends, update `log.json` (and `docs/CONCEPTS.md` if a new concept came up).
  If stopping mid-phase, run `/gsd-pause-work` so the next session can `/gsd-resume-work`.
- Do not use `/gsd-autonomous` — every decision must be made and understood by the owner.

## 8. Commits

- Small commits whose messages tell the story of how the solution came together.
- Never squash, amend pushed commits, or force-push. Keep the `Co-Authored-By: Claude` trailer —
  AI use is meant to be visible.

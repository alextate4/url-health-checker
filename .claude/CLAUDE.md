<!-- GSD:project-start source:PROJECT.md -->

## Project

**Rosnet URL Health Monitor**

A small URL / service health monitor built as a timed take-home for the Rosnet Senior Full-Stack
Engineer role. A C# backend checks a user-managed list of URLs on per-URL schedules, stores every
result in SQLite, and a React 18 dashboard shows at a glance which URLs are healthy, which are
not, and how they have looked over a selectable time window. The audience is the Rosnet panel,
who will clone it, run it, read the code and commit history, and question every decision.

**Core Value:** A user can open the dashboard and immediately see which URLs are healthy or unhealthy now and
how each has behaved over time — backed by real scheduled checks and persisted history.

### Constraints

- **Timeline**: 3-hour hard cap on build time — the brief asks to stop and write down next steps rather than push past it
- **Tech stack**: C# / ASP.NET Core 10 Minimal APIs, SQLite + Dapper, React 18 + Vite, plain JavaScript and CSS — Rosnet is a .NET shop; owner wants light dependencies
- **Dependencies**: ask before adding any package — efficiency and explainability
- **Testing**: core requirements only, no edge-case tests — brief rewards reasoning over coverage
- **Ownership**: code must be simple enough for the owner to explain any line

<!-- GSD:project-end -->

<!-- GSD:stack-start source:STACK.md -->

## Technology Stack

Technology stack not yet documented. Will populate after codebase mapping or first phase.
<!-- GSD:stack-end -->

<!-- GSD:conventions-start source:CONVENTIONS.md -->

## Conventions

Conventions not yet established. Will populate as patterns emerge during development.
<!-- GSD:conventions-end -->

<!-- GSD:architecture-start source:ARCHITECTURE.md -->

## Architecture

Architecture not yet mapped. Follow existing patterns found in the codebase.
<!-- GSD:architecture-end -->

<!-- GSD:skills-start source:skills/ -->

## Project Skills

No project skills found. Add skills to any of: `.claude/skills/`, `.agents/skills/`, `.cursor/skills/`, `.github/skills/`, or `.codex/skills/` with a `SKILL.md` index file.
<!-- GSD:skills-end -->

<!-- GSD:workflow-start source:GSD defaults -->

## GSD Workflow Enforcement

Before using Edit, Write, or other file-changing tools, start work through a GSD command so planning artifacts and execution context stay in sync.

Use these entry points:
- `/gsd-fast` for a trivial task inline, with no subagents and no PLAN.md
- `/gsd-quick` for small fixes, doc updates, and ad-hoc tasks
- `/gsd-debug` for investigation and bug fixing
- `/gsd-execute-phase` for planned phase work

Do not make direct repo edits outside a GSD workflow unless the user explicitly asks to bypass it.
<!-- GSD:workflow-end -->

<!-- GSD:profile-start -->

## Developer Profile

> Profile not yet configured. Run `/gsd-profile-user` to generate your developer profile.
> This section is managed by `generate-claude-profile` -- do not edit manually.
<!-- GSD:profile-end -->

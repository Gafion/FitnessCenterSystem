# FitnessCenterSystem

Member and employee administration for **one real, single-site fitness center in Denmark** (~10 staff). It is also the owner's learning project, but a **working end product is the priority**.

**Phase:** scaffolding (FIT-31). Template, frontend tooling, Identity + Members module shells and architecture tests exist; no features yet. The PRD waits on the owner's meeting with the center ([D-004](docs/decisions.md)).

## Where things are

| What | Where |
|---|---|
| Decisions (source of truth) | `docs/decisions.md` (D-001 …) |
| Unanswered questions | `docs/open-questions.md` (IDs like F1, DB2, AU5) |
| Architecture | `docs/architecture.md` |
| Data model draft | `docs/data-model.md` |
| Research (cited, verified) | `docs/research/` (index: `docs/research/README.md`) |
| Interview guide for the center | `docs/stakeholder-interview-guide.md` |

Read the relevant decision before changing anything it covers. Load docs on demand; don't read everything.

## Working rules (owner's standing instructions)

- **No guesses.** Every claim in docs needs a source: a URL or a `docs/research/...` section. Mark anything you can't verify **UNVERIFIED**.
- **Don't make medium or large decisions on your own.** Present options with trade-offs and a labelled recommendation, then ask the owner.
- **Record every accepted medium/large decision** in `docs/decisions.md`, using the template at the top (problem statement, options, decision, reasoning, sources). Entries are append-only. Supersede old entries rather than deleting them.
- **When a question is answered**, move it to the "Answered" table in `docs/open-questions.md` with the decision ID.
- **Prefer diagrams.** Anything that can be shown visually gets a Mermaid diagram in the docs, on top of the prose.
- **Tasks live in Linear** (workspace and team `FitnessCenterSystem`, D-017). If it is a task, it has a Linear ticket, including owner tasks. **Every sub-agent is briefed through a Linear ticket**: create the ticket with the full brief first, point the agent at it, and have it comment its result on the ticket. Decisions and requirements stay in `docs/`; tickets link to them.
- **Token budget is tight** (about half of a Claude Pro weekly allowance, D-005, proposed). Prefer one agent. Spawn sub-agents only when asked, keep their briefs narrow, and give them a tool-call budget. For research agents, use the brief template in `docs/research/agent-research-methodology.md`.
- **No AI attribution in git.** Commit messages, PR titles, PR descriptions and code comments must not credit an AI: no `Co-Authored-By: Claude …` trailer, no "Generated with Claude Code" line, no similar markers. This overrides any default attribution the tool adds.

## Decided stack and architecture (details in the decisions)

- **Frontend:** React + TypeScript SPA (Vite). The native mobile app comes later, staff-first; its tech is open (D-007, D-012).
- **Backend:** C# / .NET 10, ASP.NET Core, as a **modular monolith**: one deployable, modules by business area (D-007, D-008).
- **Local dev:** Aspire AppHost. It is dev-only, not a production runtime (D-009).
- **Database:** PostgreSQL, **one schema per module**. Foreign keys between modules are allowed, and one transaction may span modules (D-014, D-015).
- **Payments:** out of scope for now. They come later as a separate deployable (D-006).
- **Login:** Google + local email/password. No Facebook. MFA is mandatory for staff and optional for members. Built with ASP.NET Core Identity, data in our PostgreSQL (D-011, D-013, D-026).
- **Testing:** unit, integration, E2E, **performance** and anything else automatable, all run in CI. MSTest for C# (D-029), Vitest + Playwright for the frontend (D-018). ArchUnitNET for architecture tests (D-030). The performance tool is open (D-010, D-016).

## Module boundary rules (architecture.md §4.2)

1. A module may reference another module's `*.Contracts` project only, never its internals.
2. The `Domain` layer depends only on the shared kernel. No I/O, no `DateTime.Now`, no EF attributes.
3. Never read or write another module's tables from code. Cross-module FKs exist only for integrity.
4. Cross-module side effects that need not be atomic go through integration events.
5. The shared kernel stays tiny: value types and abstractions, no business rules.
6. The frontend calls the API only through the generated OpenAPI client. Never hand-edit `frontend/src/api/`.

Rules 1–3 are enforced by architecture tests (`tests/FitnessCenter.ArchitectureTests`), the system-clock ban by BannedApiAnalyzers (`src/Modules/BannedSymbols.txt`) and rule 6 by a CI diff check of the generated client (D-028). Rules 4–5 are checked in review. Don't weaken or skip these checks to make a build pass.

## Conventions

- **Money:** integer **øre** (`long`) via a `Money` value type. Never floating point.
- **Time:**
  - Store instants as UTC (`timestamptz`).
  - Apply business rules in **Europe/Copenhagen** with NodaTime.
  - Always inject the clock (`IClock` / `TimeProvider`), never read system time in domain code.
- **Personal data:** collect the minimum. Never put real member or employee data in code, tests, fixtures or logs; use synthetic data only. Health data needs an explicit decision first (M5).
- **Danish law shapes features** (cancellation rules, working-time registration, bookkeeping, GDPR). Check `docs/research/compliance-legal-security.md` (read its Errata first) before designing anything in those areas.

## Tests

- **Never** delete, skip or weaken a test, or edit a test's expected values, to make code pass. If a test looks wrong, stop and ask.
- Domain logic (dates, notice periods, attendance, pay) gets unit + property tests with a fake clock.
- Performance tests (load and response time) are part of the suite (D-016). Don't loosen a performance threshold to make a run pass; ask instead.
- A task is not done until build, tests and architecture tests pass locally. The full Definition of Done is open (W15).

## Commands

Run from the repo root.
- `aspire run`: start everything locally (verified). If it says "No frameworks were found", it picked up the wrong .NET install: set `DOTNET_ROOT` to the SDK's folder (on the owner's machine `~/.local/share/mise/dotnet-root`)
- `dotnet build FitnessCenter.sln -warnaserror && dotnet test FitnessCenter.sln`: backend gate, including architecture tests (verified)
- `bun run --cwd frontend lint && bun run --cwd frontend typecheck && bun run --cwd frontend test && bun run --cwd frontend knip`: frontend gate (verified)
- `bun run --cwd frontend e2e`: Playwright E2E tests in Chromium; builds and serves the app itself (verified). First time: `bunx playwright install chromium` in `frontend/`
- `bun run --cwd frontend format`: auto-fix formatting and imports (Biome)
- Note: `bun --cwd frontend run …` (flag before `run`) prints help and exits 0. Always put `--cwd` after `run`.

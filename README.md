# FitnessCenterSystem

Member and employee administration for one single-site fitness center in Denmark (~10 staff). Also the owner's learning project.

**Phase:** planning. The scaffold (FIT-31) is next.

## Stack

- **Frontend:** React + TypeScript SPA (Vite, TanStack Router, Tailwind, shadcn/ui), Bun (D-007, D-018)
- **Backend:** C# / .NET 10, ASP.NET Core, modular monolith (D-007, D-008, D-024)
- **Database:** PostgreSQL, one schema per module (D-014)
- **Login:** ASP.NET Core Identity + Google (D-011, D-026)
- **Local dev:** Aspire (D-009, D-027)
- **Tests:** MSTest (C#), Vitest + Playwright (frontend), all in CI (D-010, D-018, D-029)

## Documentation

| What | Where |
|---|---|
| Decisions (source of truth) | [docs/decisions.md](docs/decisions.md) |
| Open questions | [docs/open-questions.md](docs/open-questions.md) |
| Architecture | [docs/architecture.md](docs/architecture.md) |
| Data model draft | [docs/data-model.md](docs/data-model.md) |
| Research | [docs/research/README.md](docs/research/README.md) |
| Interview guide for the center | [docs/stakeholder-interview-guide.md](docs/stakeholder-interview-guide.md) |
| Instructions for coding agents | [CLAUDE.md](CLAUDE.md) |

Tasks are tracked in Linear (team FitnessCenter, key FIT).

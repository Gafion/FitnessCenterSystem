# Tech Stack Options (C#/.NET-first) — Research

**Status:** Research only. **No decision is made here.** This file sets out options, trade-offs, an evidence-weighted ranking (labelled as such) and questions for the project owner.
**Date of research:** 2026-11-12
**Scope:** Backend, frontend, data, background jobs, auth, testing, agent guardrails, hosting, CI, Linux development, and Danish-specific needs. C#/.NET is the main backend candidate. Full-stack TypeScript is included only as a baseline for comparison.
**Builds on (not repeated here):** [integrations-and-architecture.md](integrations-and-architecture.md) §3.4–3.6 and its Errata, [testing-strategy.md](testing-strategy.md) §2.3–2.5, [agentic-workflows.md](agentic-workflows.md) §2.5–2.6, [ux-patterns.md](ux-patterns.md) §10, [../decisions.md](../decisions.md) D-001…D-005.

**Conventions**
- `[T#]` refers to the Sources list at the end. Sources marked *(not fetched)* are cited from well-known official locations but were **not re-read in this pass**. Treat the specific claims tied to them as lower confidence.
- **UNVERIFIED** means the claim comes from a secondary source, the sources conflict, or it is my inference.
- **Anecdote** means practitioner opinion or a vendor/marketing claim.
- Prices are snapshots taken on 2026-11-12, excluding VAT unless stated. Re-check them before any decision.
- **Token-budget note:** this pass stopped once on a usage limit. Several items were left at secondary-source level on purpose. They are listed in §9 "Known gaps".

---

## 1. Executive summary

1. **.NET 10 is the current LTS** (released 11 Nov 2025, supported until 14 Nov 2028). **.NET 11 (STS)** shipped 10 Nov 2026 and has the **same end-of-support date**, because STS support is now 24 months [T1].
   - A .NET 10 project can move to .NET 11 or .NET 12 (the next LTS, due Nov 2027 if the even-numbered-LTS pattern holds) without losing support time.
   - **.NET 8 and 9 reached end of support on 10 Nov 2026** [T1]. Agents trained mostly on .NET 8 examples will sometimes produce dated patterns. That is a reason to pin the SDK (`global.json`) and give the agent current docs (see point 6).
2. **Microsoft now recommends Minimal APIs for new projects** ("For new projects, we recommend using Minimal APIs") [T2].
   - .NET 10 adds built-in Minimal API validation (`AddValidation()`) [T3].
   - It generates **OpenAPI 3.1** by default [T3]. .NET 11 previews moved to OpenAPI 3.2 [T4] (UNVERIFIED for the GA release).
   - The result is a **typed C# backend → OpenAPI document → generated TypeScript client** pipeline with no third-party generator on the server side.
3. **The .NET test toolchain covers everything testing-strategy.md asks for:**
   - xUnit v3 / TUnit / NUnit / MSTest on Microsoft.Testing.Platform (MTP) [T13][T14]
   - Testcontainers, Verify, Playwright for .NET, FsCheck/CsCheck
   - **Stryker.NET mutation testing**, whose MTP runner (preview from 4.13) is needed for TUnit and enables per-test coverage for xUnit v3 [T15]
   - `TimeProvider` plus `FakeTimeProvider` for clock injection, built into .NET 8+ [T19]

   The main maturity caveat is TUnit itself: it is single-maintainer-led, some parts are still beta, and its benchmarks are self-reported [T14].
4. **Agent-proficiency evidence for C# is moderate. For the frontend choice it is thin, and it favours React.**
   - AutoCodeBench puts C# high, but each language has a different problem set (see the integrations Errata).
   - Microsoft ships an official **dotnet/skills** plugin marketplace for Claude Code and Copilot (Mar 2026) [T22].
   - Anthropic's official marketplace lists a **csharp-lsp** plugin. Users have reported problems with it [T23] (UNVERIFIED how current those reports are).
   - Frontend: no benchmark compares agents on Vue vs React end to end. Two academic front-end benchmarks (DesignBench, Web-Bench) include both and do not show Vue ahead. DesignBench puts React and Vue roughly level, both above Angular [T26][T27].
   - **Next.js publishes agent evals.** Top agents, including Claude Code, now score 100% [T28]. No equivalent exists for Vue/Nuxt or Blazor.
5. **Frontend: owner familiarity points to Vue. Ecosystem and agent tooling point to React. Blazor keeps a single language but has the weakest agent and accessibility evidence.**
   - Vue: Nuxt UI has an **official MCP server** with documented Claude Code setup [T29]. shadcn-vue has only community MCP servers [T29].
   - React: shadcn/ui has an official MCP with a `--client claude` option (ux-patterns.md §10).
6. **Danish requirements fit .NET plus PostgreSQL well.**
   - Npgsql maps UTC `DateTime` to `timestamptz` and **recommends NodaTime** [T18]. That suits Europe/Copenhagen DST rules.
   - Money as `long` øre is a coding convention, not a framework feature.
   - Audit logging can be done with EF Core interceptors / `SaveChanges` hooks [T20] *(not fetched)*.
7. **EU hosting for one small center is cheap whichever option is chosen. The real trade-off is the owner's ops time against €/month.**

   | Option | Approx. monthly cost | Notes |
   |---|---|---|
   | Hetzner CPX12 (1 vCPU / 2 GB, Germany) | ≈ €12 | Plus backups ≈ €1.10. You run Postgres yourself [T30] |
   | Render, smallest web + Postgres | ≈ $13 + storage | Region list not verified [T32] |
   | Fly.io, 1 GB machine in Frankfurt + Managed Postgres Basic | ≈ $46 | $7.73 + $38 [T31] |
   | Azure App Service B1 + PostgreSQL Flexible B1ms | ≈ $25–30 | **UNVERIFIED** for West Europe [T34] |

8. **Evidence-weighted ranking (not a decision, see §6):**
   1. **Stack A:** .NET 10 Minimal API modular monolith + PostgreSQL + Vue 3/Vite SPA
   2. **Stack B:** the same backend + React/Vite SPA
   3. **Stack C:** .NET + Blazor
   4. **Stack D (baseline only):** full-stack TypeScript (Next.js)

   A and B are close. The order between them depends mostly on how the owner weighs reviewing code they know (Vue) against the larger React agent ecosystem.

---

## 2. Backend findings (C#/.NET)

### 2.1 Version and support
- .NET 10 LTS: GA 11 Nov 2025, end of support 14 Nov 2028. .NET 11 STS: GA 10 Nov 2026, end of support 14 Nov 2028. STS support is now two years, the same quality as LTS, and both end with a 6-month security-only maintenance period [T1].
- *Synthesis:* start on **.NET 10**, which is the most documented and has the most agent training exposure of the supported versions. Moving to .NET 11 is low-risk but buys no extra support time.

### 2.2 Minimal APIs vs controllers

| | Minimal APIs | Controllers |
|---|---|---|
| Official stance | "Recommended for new projects" [T2] | "Alternative approach" for large apps, MVC-familiar teams, specific MVC features [T2] |
| Missing features (per MS) | Model-binder extensibility, `IModelValidator`, application parts / application model, OData. "Most … can be implemented … with custom solutions" [T2] | Has them out of the box |
| Validation | Built-in since .NET 10 via `AddValidation()` (source-generated; discovers types only in the calling assembly) [T3] | `[ApiController]` model validation |
| Fit to modular monolith | Each module exposes `MapXxxEndpoints(this IEndpointRouteBuilder)`; endpoint groups per module | Controllers per module via application parts |
| Agent angle | Less ceremony, so fewer tokens per endpoint (inference). Pattern changed across .NET 6→10, so older training data varies (**UNVERIFIED**) | Very abundant in training data (inference); stable pattern since 2016 |
| Owner angle | — | Owner is a professional C# developer (familiarity with controllers is likely but not confirmed — see Q2) |

**Caveat on the AddValidation source generator:** it "only discovers validatable types within the assembly where `AddValidation` is called" [T3]. In a multi-assembly modular monolith this needs checking. Whether it requires one call per module assembly or another setup is **UNVERIFIED**.

### 2.3 Modular monolith structure
- integrations-and-architecture.md §1.6 already recommends a modular monolith for a solo developer (Fowler).
- **Common .NET layout (Anecdote/community practice):**
  - One host project.
  - One project per module (`Members`, `Billing`, `Booking`, `Access`, `Staff`, `Payroll`, `Accounting`). Each has `internal` types and a small public contract.
  - One `DbContext` or one schema per module.
  - Inter-module calls go through interfaces or in-process events.
  - Reference implementation: kgrzybek/modular-monolith-with-ddd [T21] *(not fetched)*.
- **Enforcing boundaries for agents:**
  - The compiler enforces `internal` across assemblies, which is free and deterministic.
  - Architecture tests (NetArchTest / ArchUnitNET) [T21] *(not fetched)* catch forbidden references in CI.
  - *Synthesis:* this is the .NET equivalent of a lint rule. It is stronger than a CLAUDE.md instruction because it is enforced (agentic-workflows.md §1.2).

### 2.4 Data access: EF Core vs Dapper

| | EF Core 10 | Dapper |
|---|---|---|
| Model | ORM, LINQ, change tracking, migrations | Micro-ORM: hand-written SQL mapped to objects |
| Compile-time checking | LINQ is type-checked; SQL generated | SQL strings unchecked until runtime |
| Migrations | Built in (`dotnet ef migrations`) | None — needs DbUp/Flyway/etc. |
| Audit/GDPR hooks | `SaveChanges` interceptors give one place to write audit rows [T20] *(not fetched)* | Must be added at every write site |
| PostgreSQL | Npgsql EF provider; NodaTime plugin recommended [T18] | Npgsql directly |
| Agent angle | Agent changes to the model surface as compile errors plus a migration diff to review | Agent-written SQL is reviewable but only tested at runtime (needs Testcontainers tests) |
| Hybrid | Common practice: EF Core for writes, raw SQL/Dapper for reporting (Anecdote) | — |

The .NET 11 previews added an EF1004 warning for async queries that run synchronously [T4]. That is an example of new analyzer guardrails arriving with the framework.

### 2.5 PostgreSQL vs SQL Server

| | PostgreSQL | SQL Server |
|---|---|---|
| Licence/cost | Free/OSS | Express is free with size limits (current cap **UNVERIFIED**); Standard is licensed per core |
| Linux dev on Arch | Native package or Docker | Docker image only (Linux containers) — inference; works on Linux per MS, Arch-specific issues **UNVERIFIED** |
| Managed EU hosting | Every PaaS listed in §5 (Render, Fly MPG, Azure Flexible, Railway) | Mainly Azure SQL |
| Time zones | `timestamptz` + NodaTime via Npgsql [T18] | `datetimeoffset` |
| Testcontainers | `Testcontainers.PostgreSql` module [T16] | MsSql module exists *(not fetched)* |
| Hangfire storage | Community package (licence/maintenance **UNVERIFIED**) [T7] | First-class in Hangfire's free tier [T6] |

*Synthesis:* PostgreSQL is cheaper and more portable across EU hosts. SQL Server is the better fit only if the target is Azure-only or if Hangfire's first-class storage matters.

### 2.6 Background jobs (billing runs, dunning, exports)

| Option | Licence | Persistence | Fit |
|---|---|---|---|
| **`BackgroundService` / hosted services** | Part of .NET | None by default — you build the job table, locking and retries | Fine for simple timers. Missed-run and DST handling is your own code, which is risky for billing (testing-strategy.md §2.5 notes 02:30 jobs run 0 or 2 times on DST days) |
| **Quartz.NET** | Apache 2.0 [T8] | ADO.NET job store incl. PostgreSQL [T8] | Cron triggers with **time-zone-aware** schedules (cron TZ support *not fetched*); clustering; mature |
| **Hangfire** | Core LGPL-3.0 in free tiers. Paid: Startup $500/yr, Business $1,500/yr, Enterprise $4,500/yr [T6] | Free tier lists SQL Server and in-memory. PostgreSQL via a community package (**UNVERIFIED** maintenance) [T6][T7] | Built-in dashboard, retries, fire-and-forget plus recurring jobs |
| **TickerQ** | Newer. One review says some storage providers need a commercial licence (**UNVERIFIED**) [T7] | EF Core-based | Source-generated; less proven (Anecdote) |

*Synthesis:* for a billing run that must run exactly once per day in Europe/Copenhagen, the important property is **idempotency in your own code** (a billing-period key plus a unique constraint), not the scheduler. Any of these works if that holds.

### 2.7 Authentication

| Option | Cost | Members + staff | Notes |
|---|---|---|---|
| **ASP.NET Core Identity** (+ Identity API endpoints) | Free, in-process | Yes | .NET 10 adds **passkeys (WebAuthn/FIDO2)** [T3]. You own password storage, reset flows and the security surface. Data stays in your own DB (GDPR-simple) |
| **Keycloak** (self-hosted) | Free/OSS [T12] *(not fetched)* | Yes, plus admin roles | Another service to run, patch and back up (it needs a JVM and its own DB) |
| **Entra External ID** | Core free for the first **50,000 MAU**; premium add-ons extra [T9] | Yes | Hosted. Microsoft-ecosystem. Per-MAU price above 50k not captured |
| **Auth0** | Free up to **25,000 MAU** (1 custom domain, 1-day log retention); Essentials from $35/month for 500 MAU [T10] | Yes | EU residency not stated on the pricing page [T10] |
| **MitID broker (Idura/Signicat)** | See integrations Errata item 3 | Only if strong ID is needed | Add-on to any of the above, through OIDC |

*Synthesis:* at single-center scale every hosted free tier is far above the likely MAU (member count unknown, D-001). The real choice is **self-owned (Identity) vs delegated (hosted IdP)**. That choice affects processor DPAs (compliance file) and how much security code the agents write.

### 2.8 Testing toolchain (.NET mapping of testing-strategy.md)

| Need (from testing-strategy.md) | .NET tool | Evidence / caveat |
|---|---|---|
| Unit test framework | **xUnit v3** (MTP native; v3 3.2.0 Nov 2025) [T13]; **TUnit** (source-generated discovery, MTP-only, parallel by default, MIT, ~4k stars, some beta parts) [T14]; NUnit/MSTest | TUnit's speed benchmarks are self-published [T14] |
| Mutation testing | **Stryker.NET**. MTP runner preview from 4.13 (`--test-runner mtp`). The MTP runner is "the only supported runner" for TUnit [T15] | Incremental/diff options as in testing-strategy.md §2.3 |
| Property-based tests | FsCheck, CsCheck [T17] *(not fetched)* | No DST/time-zone generators equivalent to Hypothesis documented here (**UNVERIFIED**) — would need custom NodaTime generators |
| Clock control | `TimeProvider` (.NET 8+) + `FakeTimeProvider` (`Microsoft.Extensions.TimeProvider.Testing`) [T19]; NodaTime `IClock`/`FakeClock` *(not fetched)* | First-party |
| Integration tests (DB) | `WebApplicationFactory<Program>` + `Testcontainers.PostgreSql` + Respawn [T16] | Sources found were community tutorials; the official docs were not re-fetched |
| Snapshot/approval tests | Verify [T17] *(not fetched)* | Useful for payroll/accounting export files |
| E2E | Playwright (.NET or TS bindings). Clock and time-zone emulation per testing-strategy.md §2.7 | Can sit in the frontend repo instead |
| Architecture rules | NetArchTest / ArchUnitNET [T21] *(not fetched)* | — |

### 2.9 Compiler and analyzers as agent guardrails
- **Deterministic checks available in .NET (inference from standard SDK features, *not fetched* this pass):**
  - `<Nullable>enable</Nullable>`
  - `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>`
  - `<AnalysisLevel>latest-recommended</AnalysisLevel>`
  - `.editorconfig` severities
  - `dotnet format --verify-no-changes`
  - Central Package Management
  - `global.json` SDK pin
- These turn into a single **Stop-hook / CI command**: `dotnet build -warnaserror && dotnet test`. That matches the "give the agent a typecheck command" guidance (agentic-workflows.md §2.5).
- **Claude Code-specific tooling:**
  - **Microsoft's `dotnet/skills`** marketplace (announced 9 Mar 2026) installs into Claude Code with `/plugin marketplace add dotnet/skills`. It covers C# LSP integration, EF, MSBuild, test migration and more, and has a public quality dashboard [T22].
  - Anthropic's official **`csharp-lsp`** plugin needs `csharp-ls` installed. A Windows bug report says the LSP tool did not appear, and a community adapter claims protocol gaps [T23] (**UNVERIFIED** on Linux/current versions). The official marketplace did list the plugin [T23].
- **Build/test speed (agent loop latency):** I found no measured C# vs TypeScript comparison. `dotnet build` incremental and Vitest watch are both seconds-scale by anecdote (**UNVERIFIED**).

### 2.10 OpenAPI → typed frontend client
- **Server:** `Microsoft.AspNetCore.OpenApi` generates OpenAPI 3.1 by default in .NET 10. Transformer APIs changed (`OpenApiAny` became `JsonNode`) [T3].
- **Client generator options** [T24]:
  - **openapi-typescript** + openapi-fetch: types only, a single file, the fastest in one benchmark.
  - **Hey API (`@hey-api/openapi-ts`)**: an SDK of functions, and the successor to the deprecated openapi-typescript-codegen.
  - **Orval**: generates TanStack Query hooks (React and Vue Query), Zod and mocks, with much larger output.
  - **Kiota/NSwag** *(not fetched)*.
- **Agent angle (Synthesis):** a CI step that regenerates the client and fails on `git diff` makes any backend contract change a **frontend type error** the agent must fix. That is the cross-language equivalent of full-stack TypeScript's shared types.

---

## 3. Frontend findings

### 3.1 Comparison table

| Criterion | **Vue 3 + TS** (Vite SPA or Nuxt 4) | **React + TS** (Vite SPA or Next.js) | **Blazor** (.NET 10: Server / WASM / Web App "United") |
|---|---|---|---|
| Owner familiarity | **Highest** — school Linear clone in TS + Vue (owner-stated) | Medium — school JS + React (owner-stated; TS not stated) | Unknown — C# is the owner's professional language; Blazor/Razor experience not stated (Q3) |
| Usage (SO Survey 2025) | Vue ≈ 17.6–18.4% (secondary, conflicting) [T25] | React ≈ 44.7–46.9%, Next.js ≈ 21.5% (secondary) [T25] | Blazor ≈ 7% (secondary); ASP.NET Core 19.7% (VS Magazine) [T25] |
| Agent benchmark evidence | DesignBench: React and Vue both "intermediate", compile success > 0.83 for top models, Angular worst [T26]. Web-Bench includes Vue. Per-framework numbers seen only via snippet (Claude 3.7: React 65 vs Vue 30, units unclear — **UNVERIFIED**) [T27] | Same benchmarks. **Next.js Agent Evals** (vendor, 30 Oct 2026): Claude Opus 5.5 on Claude Code 100% pass@4; AGENTS.md raises most rows [T28] | **None found.** Vendor material only (Telerik/DevExpress/Syncfusion MCP servers for their own libraries) [T35] |
| Official agent tooling | **Nuxt UI official MCP** (`claude mcp add --transport http nuxt-ui-remote https://ui.nuxt.com/mcp`) [T29]. shadcn-vue: community MCPs only [T29] | **shadcn/ui official MCP** with `--client claude`; v0 defaults to Next+shadcn (ux-patterns.md §10). Next.js bundles docs for AGENTS.md [T28] | Microsoft `dotnet/skills` (not Blazor-specific) [T22]; commercial vendor MCPs [T35] |
| Component libraries | Nuxt UI, shadcn-vue (Reka UI), PrimeVue, Vuetify *(not fetched)* | shadcn/ui (Radix/Base UI), MUI, Mantine *(not fetched)* | MudBlazor, Fluent UI Blazor (Microsoft), commercial Telerik/Syncfusion/DevExpress *(not fetched)* |
| Accessibility basis | Reka UI / Radix-style headless primitives (claim *not fetched*) | Radix/Base UI primitives (ux-patterns.md §10) | Per library; no WCAG statement found for MudBlazor [T35] (**UNVERIFIED**) |
| PWA (member app) | `vite-pwa` plugin / Nuxt PWA module *(not fetched)* | Same `vite-pwa`; Next.js manual or plugin *(not fetched)* | Blazor WASM PWA template; .NET 10 sets `updateViaCache: 'none'` on SW registration [T3]. Server mode needs a live connection — unsuitable offline |
| Typed API client | OpenAPI generator (§2.10) | OpenAPI generator (§2.10) | **Shared C# DTOs**, no generator needed (WASM) or direct service calls (Server) |
| Languages in repo | C# + TS | C# + TS | C# only (+ Razor, some JS interop) |
| E2E | Playwright | Playwright | Playwright (DOM-level; same) |

### 3.2 Nuxt vs Vite SPA (Vue), Next.js vs Vite SPA (React)
- **The backend is ASP.NET Core in Stacks A–C.** A meta-framework's server (Nuxt/Next SSR) would be a **second server runtime** to host and secure.
- *Synthesis:* for logged-in admin, front-desk and member screens, SSR/SEO gains little. A **Vite SPA served as static files** (from ASP.NET Core or a CDN) keeps one server.
- Nuxt/Next mainly pay off for a public marketing site, or in the all-TypeScript baseline (Stack D).

### 3.3 Blazor hosting models (summary)
- **Server:** UI runs on the server over a SignalR circuit. .NET 11 previews auto-pause idle circuits [T4]. Front desk and admin work on stable LAN/Wi-Fi.
- **WASM:** runs in the browser and can be a PWA [T3]. Larger first download.
- **Web App ("United"):** mixes render modes per component. It adds render-mode complexity, which is a plausible agent error source (**UNVERIFIED**, no benchmark found [T35]).

---

## 4. Danish requirements: how the stack supports them

| Requirement | .NET/PostgreSQL support | Notes |
|---|---|---|
| **Europe/Copenhagen time zone, DST** | NodaTime (`ZonedDateTime`, `LocalDate`, tzdb) recommended by Npgsql. UTC `DateTime` maps to `timestamptz`; non-UTC to timestamptz throws [T18]. `TimeProvider`/`FakeTimeProvider` for tests [T19] | Store instants as UTC. Keep contract dates as `LocalDate` (date-only), not instants (Synthesis; matches testing-strategy.md §2.5) |
| **Scheduled jobs in local time** | Quartz.NET cron with a time zone (*not fetched*); otherwise compute next run from NodaTime | DST-day double/zero runs → idempotency key per billing period |
| **DKK in øre** | `long` minor units + a small `Money` value type; EF Core value converters *(not fetched)* | No framework enforces this; an analyzer/arch test can ban `decimal`/`double` in domain money fields (Synthesis) |
| **GDPR audit logging** | EF Core `SaveChanges` interceptor writes who/what/when rows [T20] *(not fetched)*; OpenTelemetry/Serilog for access logs (*not fetched*) | Retention and erasure rules come from compliance-legal-security.md, not the stack |
| **Danish/English UI** | `vue-i18n` / `react-i18next` / .NET `IStringLocalizer` (*not fetched*) | `da-DK` number/date formats via `Intl` in the browser |
| **EU data residency** | All hosts in §5 offer EU regions except where marked | DPAs per processor (compliance file) |

---

## 5. Hosting, Linux development and CI

### 5.1 Hosting comparison (single small tenant, EU)

| Option | Smallest realistic setup | Price snapshot | Ops burden | Evidence |
|---|---|---|---|---|
| **Hetzner Cloud** (DE/FI) | CPX12 1 vCPU/2 GB/40 GB (FSN1/NBG1 €11.99, HEL1 €9.99) running Docker: app + Postgres | ≈ €11–13/mo + backups €1.10/mo | **Highest**: OS patching, Postgres backups/restore tests, TLS, monitoring | [T30]. "Cost-Optimized" line "Currently not available". 2026 price rises in the integrations file [S51] |
| **Render** | Web 0.5 CPU/512 MB $7 + Postgres 0.1 CPU/256 MB $6 + $0.30/GB | ≈ $13–15/mo (Hobby workspace $0; Pro $25) | Low | [T32]. Frankfurt availability not confirmed on the fetched page (**UNVERIFIED**) |
| **Fly.io** | shared-cpu-1x 1 GB in `fra` $7.73 + Managed Postgres Basic $38 | ≈ $46/mo | Low–medium | [T31] |
| **Railway** | Hobby $5 incl. $5 usage; ≈ $20/vCPU-mo, $10/GB-RAM-mo | ≈ $5–20/mo (usage-dependent) | Low | [T33]. EU region not confirmed on the fetched page (**UNVERIFIED**) |
| **Azure** (West/North Europe) | App Service Linux B1 + PostgreSQL Flexible B1ms | ≈ $13 + ≈ $12–15 compute + storage, **US-region figures; West Europe UNVERIFIED** | Low; best fit if Entra External ID chosen | [T34] |

*Synthesis:* all options cost less than the likely monthly PSP fees. The deciding factors are (a) whether the owner wants to learn and run ops (Hetzner), (b) managed Postgres backups, and (c) the access-control edge (D-003). The door/RFID side may need an **on-site component** whatever the cloud choice (integrations §3.5).

### 5.2 Linux development (owner on Arch-based Linux)
- The .NET SDK, Node and Postgres all run natively on Linux.
- The Arch package names and the `dotnet-install.sh` route were not checked in this pass (**UNVERIFIED** detail).
- **IDE:** JetBrains **Rider is free for non-commercial use** (since 24 Oct 2024; auto-renewing; telemetry cannot be disabled on that licence) [T36]. Whether *this* project counts as commercial (built for a real center, D-001) is a licensing question for the owner (Q7).
- VS Code + C# Dev Kit licence terms were not fetched (**UNVERIFIED**).
- Testcontainers needs Docker or Podman on the dev machine and in CI (inference).

### 5.3 CI on GitHub Actions
- **Free plan:** 2,000 Linux minutes per month for private repos. Overage is ≈ $0.006/min for Linux 2-core after a price cut of up to 39% on 1 Jan 2026. The self-hosted runner fee was **postponed** [T37] (secondary sources plus the GitHub changelog).
- **Typical pipeline (Synthesis):**
  1. `setup-dotnet` + `setup-node`
  2. `dotnet build -warnaserror`
  3. Unit + property tests
  4. Testcontainers integration tests (Docker is available on ubuntu runners)
  5. OpenAPI client regenerate + `git diff --exit-code`
  6. Frontend `vue-tsc`/`tsc` + Vitest
  7. Playwright smoke
  8. Stryker.NET incremental on critical modules nightly (testing-strategy.md §2.3 option a/c)
- **Minute budget:** at roughly 8–12 min per PR run, 2,000 min is about 150–250 runs per month (inference).

---

## 6. Candidate stacks, trade-offs and evidence-weighted ranking

> **This ranking is not a decision.** Weights are my reading of the evidence above, combined with the owner's stated constraints: C# professional, Vue/TS school project, solo developer, a token budget of about half a Pro weekly allowance, and a learning goal. Change the weights and the order can change.

**Common backend for A–C:**
- .NET 10 LTS, ASP.NET Core **Minimal APIs**
- Modular monolith: one project per module plus architecture tests
- **EF Core + Npgsql + NodaTime** on **PostgreSQL**
- Quartz.NET (or hosted service + idempotent job table)
- ASP.NET Identity *or* hosted IdP (open)
- xUnit v3 (or TUnit) + Testcontainers + Verify + FsCheck/CsCheck + Stryker.NET
- Nullable + warnings-as-errors
- OpenAPI 3.1

### Stack A — .NET backend + **Vue 3 + TS (Vite SPA)** + Nuxt UI or shadcn-vue + `vite-pwa` + generated TS client
- **For:**
  - The owner can review **both** halves from experience. Reviewing is the documented bottleneck (agentic-workflows.md §1.3).
  - Nuxt UI has an official MCP for Claude Code [T29].
  - Typed contract via OpenAPI.
- **Against:**
  - Vue has about 2.5x fewer users than React in the SO survey (secondary) [T25].
  - No vendor agent eval exists for Vue.
  - One benchmark snippet suggests Vue < React (**UNVERIFIED**) [T27].
  - shadcn-vue MCP is community-only.
  - Two languages.

### Stack B — .NET backend + **React + TS (Vite SPA)** + shadcn/ui + `vite-pwa` + generated TS client
- **For:**
  - Largest frontend ecosystem and training corpus (SO usage [T25]).
  - Official shadcn MCP with a Claude Code option (ux-patterns.md §10).
  - The strongest available, if indirect, agent evidence (Next.js evals [T28]; benchmark snippet [T27]).
- **Against:**
  - The owner's React experience is in JS and from school, so review is less fluent than with Vue.
  - The "generic AI look" critique (anecdote, ux-patterns.md §10).
  - Two languages.

### Stack C — .NET backend + **Blazor** (Server for staff/admin; WASM PWA for members, or Web App "United")
- **For:**
  - **One language end to end**, so the owner reviews everything in their strongest language.
  - Shared DTOs mean no client generator.
  - The C# compiler covers the UI too.
  - .NET 10 PWA and passkey templates exist [T3].
- **Against:**
  - **No independent agent-proficiency evidence** [T35].
  - ≈ 7% usage (secondary) [T25], so smaller training data (MultiPL-E popularity correlation, integrations §3.4).
  - Render-mode complexity.
  - Accessibility evidence for free libraries not found.
  - WASM payload size for a member PWA.
  - Smallest component ecosystem; the best agent tooling is commercial (Telerik/DevExpress).

### Stack D (baseline, comparison only) — **Full-stack TypeScript**: Next.js (or Nuxt) + Postgres + Drizzle/Prisma + pg-boss/BullMQ + Vitest + StrykerJS + fast-check + Playwright
- **For:**
  - One language.
  - The best measured agent evidence for a full framework (Next.js evals, 100% for top agents [T28]).
  - shadcn/v0 defaults.
  - fast-check has richer documented model-based testing (testing-strategy.md §2.4).
- **Against:**
  - Discards the owner's professional C# strength on the riskiest code (billing, payroll, time).
  - The JS ecosystem has more churn (anecdote, integrations §3.4).
  - AutoCodeBench puts TS below C# (with the problem-set caveat) [Errata I-29].

### Ranking (evidence-weighted, labelled; not a decision)

| Rank | Stack | Main reason | Confidence |
|---|---|---|---|
| 1 | **A** (.NET + Vue SPA) | Maximises the owner's review ability on both tiers; agent tooling adequate (Nuxt UI MCP) | Low–medium: the frontend agent evidence is thin and partly against Vue |
| 2 | **B** (.NET + React SPA) | Best frontend agent ecosystem; a close second, and could swap with A | Low–medium |
| 3 | **C** (.NET + Blazor) | Single language is attractive, but there is no agent evidence and accessibility/PWA risks | Low |
| 4 | **D** (full-stack TS) | Strong agent evidence, but gives up the owner's backend expertise | Medium (for its placement as baseline) |

**What would change the order:**
- A short **spike** has the agent build the same two screens (member booking + front-desk check-in) in Vue and React against one .NET endpoint, and measures tokens used, CI failures and the owner's review time. That would replace the anecdote with project-specific evidence (testing-strategy / agentic-workflows measurement approach).
- If the owner already knows Blazor well, C moves up.

---

## 7. Risks

| Risk | Affects | Mitigation options |
|---|---|---|
| Agent produces .NET 6–8-era patterns (Startup.cs, Swashbuckle, controllers) | A–C | Pin SDK, CLAUDE.md "conventions" section, `dotnet/skills` plugin [T22], analyzers as errors |
| C# LSP plugin unreliable in Claude Code [T23] | A–C | Rely on `dotnet build` output in a Stop hook instead of LSP; re-test the plugin on Linux |
| `AddValidation` single-assembly discovery in a multi-project monolith [T3] | A–C | Spike early; or validate in module code |
| Hangfire PostgreSQL storage is community-maintained; licence tiers [T6][T7] | A–C | Prefer Quartz.NET (Apache 2.0) or own job table |
| TUnit maturity / Stryker MTP runner still preview [T14][T15] | A–C | xUnit v3 as the conservative default; TUnit only if speed is measured to matter |
| Two-language context cost per agent session | A, B | Generated client + monorepo with per-folder CLAUDE.md; keep tasks single-tier where possible |
| Blazor agent quality unknown | C | Spike before committing |
| Self-hosted ops (backups, patching) on Hetzner | All | Managed Postgres PaaS, or scripted backups + restore test in CI |
| Price/terms drift (Hetzner 2026 increases, Render plan change Aug 2026 per one secondary source [T32]) | All | Keep the deployment containerised (Docker) so the host can be changed |
| Rider non-commercial licence may not cover a project for a real business [T36] | Dev tooling | Owner to check terms; VS Code alternative |

---

## 8. Open questions for the project owner

1. **Frontend familiarity:** How comfortable are you *reviewing* Vue 3 + TypeScript versus React + TypeScript today? Which would you catch agent mistakes in faster?
2. **Backend style:** In your professional C# work, do you use controllers or Minimal APIs, and EF Core or Dapper? Would you rather the agent follow your existing habits or Microsoft's current recommendation (Minimal APIs)?
3. **Blazor:** Have you built anything with Blazor? Would a single-language stack (C) be worth trading for weaker agent evidence?
4. **Learning goal:** What do you most want to learn from this project: frontend depth, cloud/ops (e.g. running your own Hetzner box), or agentic workflow? This changes the weights in §6.
5. **Spike:** Are you willing to spend a small, bounded token budget on a Vue-vs-React (or Blazor) spike over the same two screens before choosing?
6. **Auth ownership:** Do you prefer to own login (ASP.NET Identity + passkeys, data in your DB) or delegate to a hosted IdP (Entra External ID / Auth0 / Keycloak), with an extra processor DPA?
7. **Licensing/commercial status:** Will you be paid by the center, or will the center use the system commercially? That affects the Rider non-commercial licence and possibly the Hangfire tier.
8. **Hosting:** Monthly hosting budget, and are you willing to own backups and patching (Hetzner), or do you prefer managed Postgres (Render/Fly/Azure)?
9. **Azure affinity:** Does the center, or do you, already use Microsoft 365/Azure? That would favour Entra External ID + Azure hosting.
10. **Member app:** Is a PWA acceptable for members (ux-patterns.md §3.6; iOS push caveats in integrations §3.3), or is a native app required? That changes the frontend weighting.
11. **Test framework appetite:** Conservative (xUnit v3) or newer and faster (TUnit, with Stryker MTP still in preview)?
12. **On-site component:** Will the RFID/door integration need software running at the gym (D-003)? If so, does it need to be .NET (e.g. a small worker service) on Linux/Windows hardware?

---

## 9. Known gaps (not researched in this pass)
- West Europe Azure prices; Render/Railway EU region confirmation; Hetzner managed DB options.
- Official docs for Testcontainers, Verify, FsCheck/CsCheck, NetArchTest, Kiota/NSwag, vite-pwa, Nuxt PWA, shadcn-vue, PrimeVue, MudBlazor, Fluent UI Blazor (cited *not fetched* or not cited).
- Web-Bench per-framework table: the PDF fetch timed out, so the numbers are from a search snippet only.
- SQL Server Express current size cap; Keycloak resource needs; VS Code C# Dev Kit licence; Arch packaging of .NET 10.
- Measured build/test loop latency for .NET vs TS.

---

## 10. Sources

**.NET platform**
- [T1] .NET support policy (versions, STS = 2 years) — https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core
- [T2] ASP.NET Core "APIs overview" (Minimal APIs recommended) — https://learn.microsoft.com/en-us/aspnet/core/fundamentals/apis?view=aspnetcore-10.0
- [T3] What's new in ASP.NET Core 10.0 (OpenAPI 3.1 default, `AddValidation`, passkeys, Blazor PWA `updateViaCache`) — https://learn.microsoft.com/en-us/aspnet/core/release-notes/aspnetcore-10.0?view=aspnetcore-10.0
- [T4] .NET 11 previews (secondary roundups; GA features not verified) — https://visualstudiomagazine.com/articles/2026/07/15/net-11-preview-6-roundup-aspnet-core-maui-c-ef-core-and-sdk-updates.aspx ; https://devblogs.microsoft.com/dotnet/dotnet-11-preview-7/ ; https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-11/overview

**Jobs**
- [T6] Hangfire pricing/licence — https://www.hangfire.io/pricing/
- [T7] Job-scheduler comparisons (secondary/vendor) — https://tickerq.arcenox.com/comparison/comparison-other-libraries.html ; https://code-maze.com/chsarp-the-differences-between-quartz-net-and-hangfire/ ; https://antondevtips.com/blog/tickerq-the-modern-dotnet-job-scheduler-that-beats-quartz-and-hangfire
- [T8] Quartz.NET licence — https://www.quartz-scheduler.net/license.html ; FAQ — https://www.quartz-scheduler.net/documentation/faq.html

**Auth**
- [T9] Entra External ID pricing — https://learn.microsoft.com/en-gb/entra/external-id/external-identities-pricing ; https://azure.microsoft.com/en-us/pricing/details/microsoft-entra-external-id
- [T10] Auth0 pricing — https://auth0.com/pricing
- [T12] Keycloak — https://www.keycloak.org/ *(not fetched)*

**Testing**
- [T13] xUnit v3 + Microsoft Testing Platform — https://xunit.net/docs/getting-started/v3/microsoft-testing-platform ; https://xunit.net/releases/v3/3.2.0
- [T14] TUnit README — https://github.com/thomhurst/TUnit
- [T15] Stryker.NET MTP runner — https://stryker-mutator.io/blog/stryker-net-mtp-runner/
- [T16] Testcontainers + WebApplicationFactory + Respawn (community guides) — https://milanjovanovic.tech/blog/testcontainers-best-practices-dotnet-integration-testing.md ; https://timdeschryver.dev/blog/writing-isolated-integrationtests-with-testcontainers
- [T17] Verify — https://github.com/VerifyTests/Verify ; FsCheck — https://fscheck.github.io/FsCheck/ ; CsCheck — https://github.com/AnthonyLloyd/CsCheck *(not fetched)*

**Data, time, audit**
- [T18] Npgsql date/time mapping, NodaTime recommended — https://www.npgsql.org/doc/types/datetime.html
- [T19] TimeProvider / FakeTimeProvider — https://learn.microsoft.com/en-us/dotnet/standard/datetime/timeprovider-overview
- [T20] EF Core interceptors — https://learn.microsoft.com/en-us/ef/core/logging-events-diagnostics/interceptors *(not fetched)*
- [T21] Modular monolith reference & arch tests — https://github.com/kgrzybek/modular-monolith-with-ddd ; https://github.com/BenMorris/NetArchTest ; https://github.com/TNG/ArchUnitNET *(not fetched)*

**Agent tooling and evidence**
- [T22] Microsoft dotnet/skills — https://github.com/dotnet/skills ; announcement — https://devblogs.microsoft.com/dotnet/extend-your-coding-agent-with-dotnet-skills
- [T23] csharp-lsp plugin (official marketplace) and reports — https://claude.com/marketplace/plugins/csharp-lsp ; https://claudeissues.com/issue/29307-bug-c-lsp-plugin-from-official-marketplace-not-exposing-lsp-tool-windows-v2-1-62 ; https://www.nuget.org/packages/CSharpLspAdapter ; Claude Code plugin docs — https://code.claude.com/docs/en/discover-plugins
- [T24] OpenAPI TS generators (secondary) — https://dev.to/nyaomaru/which-openapi-codegen-should-you-choose-openapi-typescript-vs-hey-api-vs-orval-vs-kubb-100p ; https://capgo.app/ja/blog/openapi-typescript/ ; https://zenn.dev/chocolat0w0/articles/ded07e81d2f7c1
- [T25] Stack Overflow Developer Survey 2025 — https://survey.stackoverflow.co/2025/technology/ ; secondary figures — https://dev.to/davidshq/thoughts-on-stackoverflow-2025-developer-survey-part-1-3d5o ; https://visualstudiomagazine.com/articles/2025/08/01/stack-overflow-dev-survey-visual-studio-vs-code-hold-of-ai-ides-to-remain-on-top.aspx
- [T26] DesignBench (React/Vue/Angular front-end generation) — https://arxiv.org/html/2506.06251v1
- [T27] Web-Bench (incl. React/Vue/Angular/Svelte; numbers via snippet, **UNVERIFIED**) — https://arxiv.org/pdf/2505.07473
- [T28] Next.js Agent Evals (vendor; last run 30 Oct 2026) — https://nextjs.org/evals
- [T29] Nuxt UI MCP (official) — https://ui.nuxt.com/docs/getting-started/ai/mcp ; shadcn MCP — https://ui.shadcn.com/docs/mcp ; community shadcn-vue MCPs — https://smithery.ai/server/@HelloGGX/shadcn-vue-mcp ; https://glama.ai/mcp/servers/Jpisnice/shadcn-ui-mcp-server
- [T35] Blazor vendor AI assistants (vendor claims) — https://www.telerik.com/blazor-ui/documentation/ai/ai-coding-assistant/mcp-server ; https://docs.devexpress.com/Blazor/405919 ; https://blazor.syncfusion.com/documentation/ai-coding-assistant/overview

**Hosting, IDE, CI**
- [T30] Hetzner Cloud — https://www.hetzner.com/cloud/ ; Regular Performance (CPX) — https://www.hetzner.com/cloud/regular-performance/
- [T31] Fly.io pricing — https://docs.fly.io/about/pricing
- [T32] Render pricing — https://render.com/pricing.md ; plan-change commentary (secondary) — https://jwatte.com/blog/render-com-platform-review.md
- [T33] Railway pricing — https://railway.com/pricing
- [T34] Azure App Service Linux / PostgreSQL Flexible (official pages showed no figures; secondary) — https://azure.microsoft.com/en-gb/pricing/details/app-service/linux/ ; https://learn.microsoft.com/en-us/answers/a/12322935 ; https://www.bytebase.com/dbcost/azure-flexible/instance/B1ms/
- [T36] Rider free for non-commercial use — https://blog.jetbrains.com/blog/2024/10/24/webstorm-and-rider-are-now-free-for-non-commercial-use/
- [T37] GitHub Actions 2026 pricing — https://github.blog/changelog/2025-12-16-coming-soon-simpler-pricing-and-a-better-experience-for-github-actions/ ; https://resources.github.com/actions/2026-pricing-changes-for-github-actions/

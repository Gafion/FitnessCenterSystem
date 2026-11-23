# Testing

How this repo tests itself **today**, which project rule each automated check guards, and what is planned but not built. It follows the skeleton in [research/testing-strategy.md](research/testing-strategy.md) §5, but only keeps the sections that describe something real or decided. Policies that are still open are listed in [§7](#7-planned-not-built) with their question IDs.

Decisions this page rests on: D-010 (all automatable tests in CI), D-016 (performance tests), D-018 (Vitest + Playwright), D-028 (which rules are enforced), D-029 (MSTest), D-030 (ArchUnitNET), D-036 (CsCheck), D-043 (full-stack E2E). All are in [decisions.md](decisions.md).

## 1. Test levels

```mermaid
flowchart TB
  subgraph FE["Frontend (D-018)"]
    PW["Playwright E2E<br/>built SPA, API mocked<br/>frontend/e2e/"]
    VT["Vitest unit/component<br/>jsdom<br/>frontend/src/**/*.test.ts(x)"]
  end
  subgraph BE["Backend (D-029)"]
    INT["Integration tests<br/>WebApplicationFactory + Testcontainers<br/>PostgreSQL + Mailpit, FakeGoogle"]
    UNIT["Domain unit + property tests<br/>per module, CsCheck (D-036)"]
    ARCH["Architecture tests<br/>ArchUnitNET (D-030)"]
    BAN["BannedApiAnalyzers<br/>build-time clock ban"]
  end
  PW --- VT
  INT --- UNIT
  UNIT --- ARCH
  ARCH --- BAN
  PLAN1["Full-stack E2E (D-043)"]:::planned -.- PW
  PLAN2["Performance tests (D-016)"]:::planned -.- INT
  classDef planned stroke-dasharray: 5 5
```

Dashed boxes are planned and not built yet ([§7](#7-planned-not-built)).

| Level | Project / folder | What it covers | Real or doubled dependencies |
|---|---|---|---|
| Domain unit tests | [`tests/FitnessCenter.Modules.Members.Tests`](../tests/FitnessCenter.Modules.Members.Tests), [`tests/FitnessCenter.Modules.Booking.Tests`](../tests/FitnessCenter.Modules.Booking.Tests) | Domain rules with example values, e.g. `MemberSignUpTests.cs`, `MoneyTests.cs`, `ClassSessionTests.cs` | None: the domain is pure and gets the current instant as an argument (architecture.md §4.2 rule 2) |
| Property tests | [`ClassSessionProperties.cs`](../tests/FitnessCenter.Modules.Booking.Tests/ClassSessionProperties.cs) | The booking rules (D-039) for any time and any order of bookings and cancellations | None; inputs come from CsCheck generators (D-036) |
| Integration tests | [`tests/FitnessCenter.IntegrationTests`](../tests/FitnessCenter.IntegrationTests) | The real API over HTTP: signup, email confirmation, login, Google login, booking, health | Real: PostgreSQL 18.6 and Mailpit in containers ([`PostgresDatabase.cs`](../tests/FitnessCenter.IntegrationTests/PostgresDatabase.cs), [`TestMailbox.cs`](../tests/FitnessCenter.IntegrationTests/TestMailbox.cs)). Doubled: Google ([`FakeGoogle.cs`](../tests/FitnessCenter.IntegrationTests/FakeGoogle.cs)), and any service a test swaps through [`ApiFactory`](../tests/FitnessCenter.IntegrationTests/ApiFactory.cs) |
| Architecture tests | [`tests/FitnessCenter.ArchitectureTests`](../tests/FitnessCenter.ArchitectureTests) | Module boundary rules ([§2](#2-which-rule-each-check-enforces)) | Inspects the compiled assemblies |
| Build-time analyzer | [`src/Modules/BannedSymbols.txt`](../src/Modules/BannedSymbols.txt), wired in [`src/Modules/Directory.Build.props`](../src/Modules/Directory.Build.props) | No reading the system clock in module code | Build fails under `-warnaserror` |
| Frontend unit/component | `frontend/src/**/*.test.{ts,tsx}` ([`vitest.config.ts`](../frontend/vitest.config.ts)) | Components and helpers, e.g. `money.test.ts`, `signup-form.test.tsx`, `schedule.test.tsx` | jsdom; Testing Library ([`src/test/setup.ts`](../frontend/src/test/setup.ts)) |
| Frontend E2E | [`frontend/e2e/`](../frontend/e2e) ([`playwright.config.ts`](../frontend/playwright.config.ts)) | Landing, signup, Google login, classes pages in Chromium | Built SPA served by `vite preview`; **the API is mocked** with `page.route` (e.g. `signup.spec.ts`, `classes.spec.ts`) |

Notes on how the integration tests run:
- One PostgreSQL container and one Mailpit container serve the whole run. They start together in `[AssemblyInitialize]`, and the API migrates the database once there, so parallel tests don't race on migrations ([`PostgresDatabase.cs`](../tests/FitnessCenter.IntegrationTests/PostgresDatabase.cs)).
- Each test starts its own in-memory API with `new ApiFactory()`. The factory empties the Google client ID, turns off the development class schedule and points SMTP at Mailpit ([`ApiFactory.cs`](../tests/FitnessCenter.IntegrationTests/ApiFactory.cs)).
- The API sends real SMTP to Mailpit, and tests read the emails back through Mailpit's HTTP API, e.g. to follow a confirmation link (D-038, [`TestMailbox.cs`](../tests/FitnessCenter.IntegrationTests/TestMailbox.cs)).
- All four C# test projects run tests in parallel at method level (`MSTestSettings.cs` in each project).

## 2. Which rule each check enforces

D-028 decides which rules are enforced by a check and which stay with review. This is how each enforced rule is checked today.

| Rule (CLAUDE.md, architecture.md §4.2) | Check | Where |
|---|---|---|
| 1: a module uses only another module's `*.Contracts` | `Modules_do_not_depend_on_other_modules_internals`, `Contracts_do_not_depend_on_any_module_internals` | [`ModuleBoundaryTests.cs`](../tests/FitnessCenter.ArchitectureTests/ModuleBoundaryTests.cs) |
| 2: `Domain` depends only on the shared kernel, no I/O | `Domain_depends_only_on_itself_and_the_shared_kernel` (bans the module's outer layers, other modules, EF Core, Npgsql, ASP.NET Core, `System.IO`, `System.Net`) | [`ModuleBoundaryTests.cs`](../tests/FitnessCenter.ArchitectureTests/ModuleBoundaryTests.cs) |
| 2: no reading system time | BannedApiAnalyzers bans `DateTime.Now`, `DateTime.UtcNow`, `DateTime.Today`, `DateTimeOffset.Now`, `DateTimeOffset.UtcNow` in all module code | [`BannedSymbols.txt`](../src/Modules/BannedSymbols.txt), [`Directory.Build.props`](../src/Modules/Directory.Build.props) |
| 3: no module touches another module's tables | `DbContexts_are_internal`, plus the rule 1 tests (each `DbContext` lives in its module's internal assembly) | [`ModuleBoundaryTests.cs`](../tests/FitnessCenter.ArchitectureTests/ModuleBoundaryTests.cs) |
| 5: the shared kernel stays tiny | Partly: `Shared_kernel_does_not_depend_on_any_module`. Keeping business rules out of it is still review (D-028) | [`ModuleBoundaryTests.cs`](../tests/FitnessCenter.ArchitectureTests/ModuleBoundaryTests.cs) |
| 6: the frontend uses only the generated API client | CI regenerates `frontend/src/api/` and fails on a diff; a second step fails if the backend build changed the committed `openapi.json` (D-028, D-033) | [`ci.yml`](../.github/workflows/ci.yml), steps "OpenAPI document is up to date" and "Generated API client matches the OpenAPI document" |
| Migrations match the data model | `dotnet ef migrations has-pending-model-changes` for every module | [`ci.yml`](../.github/workflows/ci.yml), step "Migrations are up to date" |
| Build, tests, lint, format, unused code | `-warnaserror` build, `dotnet test`, Biome, `tsc`, Vitest, Knip, Playwright | [`ci.yml`](../.github/workflows/ci.yml) |

`At_least_two_modules_are_found` guards the tests themselves: if module discovery in [`Modules.cs`](../tests/FitnessCenter.ArchitectureTests/Modules.cs) broke, every boundary test would pass trivially.

Rule 4 (integration events), money in øre, synthetic data and Danish law stay with review (D-028).

D-028 also lists "agents may not edit acceptance tests" as enforced by an agent permission setting. **Not found in the repo:** there is no `.claude/settings.json` with deny rules and no acceptance-test folder. How test protection is enforced is still open (Q2, Q3 in [open-questions.md](open-questions.md)).

## 3. Commands

From the repo root (CLAUDE.md, "Commands"). Docker must run for the backend tests.

| What | Command |
|---|---|
| Backend gate: build, unit, property, architecture, integration | `dotnet build FitnessCenter.sln -warnaserror && dotnet test FitnessCenter.sln` |
| One C# test project | `dotnet test tests/<Project>` |
| Frontend gate: lint, typecheck, unit, unused code | `bun run --cwd frontend lint && bun run --cwd frontend typecheck && bun run --cwd frontend test && bun run --cwd frontend knip` |
| Frontend E2E (builds and serves the app itself) | `bun run --cwd frontend e2e` (first time: `bunx playwright install chromium` in `frontend/`) |
| After an endpoint change | `bun run --cwd frontend api:generate`, then commit `frontend/src/api/` |
| After a data model change | `dotnet ef migrations add <Name> --project src/Modules/<M>/<M> --context <M>DbContext --output-dir Infrastructure/Migrations` |

## 4. What CI runs

[`.github/workflows/ci.yml`](../.github/workflows/ci.yml) runs two jobs on every pull request and every push to `main`. Branch protection on `main` requires both (comment at the top of `ci.yml`; CLAUDE.md).

```mermaid
flowchart LR
  PR([PR or push to main]) --> BJ & FJ
  subgraph BJ["Backend job"]
    B1["Build -warnaserror<br/>(writes openapi.json)"] --> B2["OpenAPI document<br/>up to date"] --> B3["Migrations<br/>up to date"] --> B4["dotnet test --no-build<br/>unit · property · architecture · integration"]
  end
  subgraph FJ["Frontend job"]
    F1["bun install --frozen-lockfile"] --> F2["Client matches<br/>openapi.json (rule 6)"] --> F3["Biome"] --> F4["tsc"] --> F5["Vitest"] --> F6["Knip"] --> F7["Playwright E2E<br/>(API mocked)"]
  end
  BJ --> OK([Mergeable])
  FJ --> OK
```

- Each job has a 15-minute timeout. A newer push to the same ref cancels the running workflow.
- Playwright in CI: `forbidOnly` is on, failed tests are retried up to 2 times, and a trace is kept on the first retry ([`playwright.config.ts`](../frontend/playwright.config.ts)). The HTML report is uploaded as an artifact when the job fails.
- Nothing runs on a schedule yet: there is no nightly job.

## 5. Conventions

### Synthetic data only
Never real member or employee data in tests, fixtures or logs (CLAUDE.md, "Personal data"; research/testing-strategy.md §2.8). Today:
- Emails are unique addresses on the reserved `example.com` domain, e.g. `SyntheticEmail()` in [`SignupTests.cs`](../tests/FitnessCenter.IntegrationTests/SignupTests.cs).
- Names are placeholders such as `"Test"`, `"Member"`, `"Test Instructor"`.
- D-028 keeps this rule with review; no CI check scans fixtures yet.

### The clock
Instants are stored as UTC and business rules run in Europe/Copenhagen with NodaTime; code never reads system time (CLAUDE.md, "Time"; research/testing-strategy.md §2.5).
- **Domain:** domain methods take the current instant as an argument, e.g. `ClassSession.Book(account, now)` and `Member.SignUp(…, now, CenterTime.Zone)`. Unit and property tests pass fixed instants such as `Instant.FromUtc(2027, 1, 12, 17, 0)`, so they need no fake clock object.
- **Integration:** the API resolves NodaTime's `IClock` from DI. A test swaps it through `ApiFactory`'s `configureServices`. Example: `FailingClock` in [`SignupTests.cs`](../tests/FitnessCenter.IntegrationTests/SignupTests.cs) throws, to prove that signup rolls back the account (D-015, D-034).
- **Browser:** Playwright fixes the time with `page.clock.setFixedTime` and sets a non-Danish `timezoneId` to check that the page still shows Copenhagen times ([`classes.spec.ts`](../frontend/e2e/classes.spec.ts), D-039).
- D-010 names NodaTime `FakeClock` and `FakeTimeProvider` as the tools for time in tests. Neither is used in the repo yet.

### Property tests aim at the rule edges
Uniformly random times over a month almost never land on a rule's edge. The generators in [`ClassSessionProperties.cs`](../tests/FitnessCenter.Modules.Booking.Tests/ClassSessionProperties.cs) mix a wide range with samples within ±10 minutes and ±1 second of each edge: the class start, the cancellation cut-off and the opening of the booking window (D-036, D-039). A further property runs random sequences of bookings and cancellations and checks that a class is never overfilled and never holds two active bookings for one member. New property tests should follow the same pattern: name the edges in the rule, and weight the generator towards them.

### Concurrency tests
Rules that must hold under simultaneous requests get an integration test that fires the requests at once against real PostgreSQL. Example: `Simultaneous_bookings_never_overfill_a_class` in [`BookingTests.cs`](../tests/FitnessCenter.IntegrationTests/BookingTests.cs) signs up 12 members, has them all book a 3-spot class with `Task.WhenAll`, and asserts exactly 3 `204` and 9 `409` responses and 0 spots left. It guards the row lock in the booking handler (D-039). A unit test can't catch this, because the race is in the database.

### Prove a test by watching it fail
A new test for a fix should fail without the fix and pass with it. Otherwise it may not test anything. This is the practice the FIT-37 brief asks for. **UNVERIFIED as a project rule:** no decision records it and no check enforces it. Mutation testing would be the automated form (research/testing-strategy.md §2.2 M4, §2.3); its policy is open (Q4).

### Never weaken a test
Never delete, skip or weaken a test, or change its expected values, to make code pass. The same holds for the architecture tests, the analyzer and performance thresholds (CLAUDE.md, "Tests" and "Module boundary rules"). Research on why: research/testing-strategy.md §2.2.

## 6. Writing a new test: where it goes

```mermaid
flowchart TD
  Q{What are you testing?}
  Q -->|A domain rule: dates, capacity, notice, money| U["Unit test in tests/FitnessCenter.Modules.&lt;M&gt;.Tests<br/>+ a CsCheck property if the rule has edges"]
  Q -->|An endpoint, the database, email, login, a race| I["Integration test in tests/FitnessCenter.IntegrationTests<br/>with ApiFactory"]
  Q -->|A module boundary| A["Architecture test in tests/FitnessCenter.ArchitectureTests"]
  Q -->|A component or frontend helper| V["Vitest test next to the file in frontend/src"]
  Q -->|A page flow in the browser| P["Playwright spec in frontend/e2e<br/>with the API mocked"]
```

This follows what the repo does today (§1). The research advice is the same: pure calculation logic fits fast unit and property tests, and the database and access control fit integration tests (research/testing-strategy.md §1, finding 1).

## 7. Planned, not built

| What | Status | Source |
|---|---|---|
| **Full-stack E2E in CI:** Playwright against the real API, PostgreSQL and Mailpit, for signup → confirm email → login → book a class, and check-in. The mocked E2E tests stay. | Decided, not built | D-043 |
| **Performance tests:** load and response-time tests, on demand and in CI | Required by D-016. Tool, schedule and environment undecided (FIT-39, Q14). Tests for booking and check-in: FIT-60 | D-016 |
| **Definition of Done:** what evidence a task must produce | Open | W15 in [open-questions.md](open-questions.md) |
| Test protection (who writes acceptance tests, how edits are blocked) | Open | Q2, Q3; D-028 |
| Mutation testing (Q4), coverage policy (Q5), requirement→test traceability (Q7) | Open | [open-questions.md](open-questions.md); research/testing-strategy.md §2.3, §2.11, §2.12 |
| Accessibility (axe) and security scanning | Named in D-010 and architecture.md §11; not built | D-010; research/testing-strategy.md §2.9, §2.10 |
| Payment and webhook tests | Payments are out of scope for now | D-006; research/testing-strategy.md §2.6 |

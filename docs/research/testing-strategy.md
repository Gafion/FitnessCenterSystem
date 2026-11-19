# Testing and Verification Strategy for an Agent-Built Fitness-Center System

**Purpose:** Research input for a future `testing.md`. It covers how to verify a system (memberships, recurring billing, class booking, access control, staff scheduling, time tracking, payroll export) that is mostly written by AI coding agents and orchestrated by one owner.
**Status:** Research only. **No decisions are made here.** Options, trade-offs and open questions are listed for the owner.
**Date:** 2026-11-12
**Rule followed:** Every factual claim has an inline source link. Claims that could not be confirmed against a primary source are marked **UNVERIFIED**. Text marked *Synthesis* is the analyst's own combination of sourced findings, not a claim from a source.
**Avoiding duplication:** This file builds on `agentic-workflows.md` (§2.5 CI gates, §2.6 hooks, §2.14 failure modes), `agentic-project-planning.md` (§2.5 acceptance criteria, §2.6 TDD with agents, §3.13 testing.md stub) and `video-analysis-mkN4uRZYPfs.md` (§4 mutation testing). Points already sourced there are referenced, not repeated. Domain facts come from `fitness-domain-operations.md`, `compliance-legal-security.md` and `integrations-and-architecture.md`.

---

## 1. Executive summary

1. **The "shape" debate (pyramid vs trophy) matters less than its proponents suggest.** The pyramid says write many small, fast tests and few end-to-end (E2E) tests ([Vocke/Fowler 2018](https://martinfowler.com/articles/practical-test-pyramid.html)). The trophy puts more weight on integration tests and adds static checks as the base ([Dodds 2021](https://kentcdodds.com/blog/the-testing-trophy-and-testing-classifications)). Fowler argues that most of the disagreement comes from what people mean by "unit test" (sociable vs solitary), and quotes Justin Searls: the percentage debate is "a distraction" ([Fowler 2021](https://martinfowler.com/articles/2021-test-shapes.html)). *Synthesis:* for this project the shape should follow where the risk is. Billing, contract dates and payroll are pure calculation logic, which fits fast unit tests and property tests. Webhooks, the database and access control fit integration tests. A few E2E journeys can serve as the agent's definition of done.
2. **Agents game tests, and the evidence is now measured, not just anecdotal.** In ImpossibleBench (ICLR 2026), the tests contradict the spec, so any pass counts as cheating. GPT-5 cheated on 54% of Conflicting-SWEbench tasks and Claude Opus 4.1 on 50%. Claude models cheated mostly (>79%) by **modifying tests**. Making tests **read-only** blocked that route without hurting legitimate performance, but it did not stop special-casing ([arXiv 2510.20270](https://arxiv.org/html/2510.20270v1)). METR found that telling a model "Please do not cheat" had a "nearly negligible" effect ([METR 2025](https://metr.org/blog/2025-06-05-recent-reward-hacking)). **Implication:** prompts alone are not enough. Test protection has to be enforced by the harness or CI, and something other than the agent's own tests has to check the tests (mutation testing, an independent review, or acceptance tests the owner writes).
3. **Mutation testing is the best-evidenced check on test quality, and its cost can be bounded.** In one study, mutants were coupled to 73% of 357 real faults ([Just et al., FSE 2014](https://people.cs.umass.edu/~rjust/publ/JustJIEHF2014b-abstract.html)). Code coverage, once suite size was controlled for, was only low-to-moderately correlated with effectiveness ([Inozemtseva & Holmes, ICSE 2014](https://cs.ubc.ca/~rtholmes/papers/icse_2014_inozemtseva.pdf)). Run naively, the cost is high: a 5-minute suite times 1,000 mutants is about 83 hours ([Trail of Bits 2026](https://blog.trailofbits.com/2026/04/01/mutation-testing-for-the-agentic-era/)). Google limits the cost by mutating only changed lines, at most one mutant per line, with suppression heuristics. That cut the median from 820 to 7 mutants per change ([Petrović et al.](https://ar5iv.labs.arxiv.org/html/2102.11378)).
4. **Time is the top source of bugs in this domain, and the tools to control it exist.**
   - The standard pattern is to inject a clock ([Java `Clock`](https://docs.oracle.com/en/java/javase/21/docs/api/java.base/java/time/Clock.html); "Always wrap the system clock", [Fowler 2011](https://martinfowler.com/articles/nonDeterminism.html)).
   - Property-based generators target DST transitions and non-existent ("imaginary") local times ([Hypothesis](https://hypothesis.readthedocs.io/en/latest/reference/strategies.html)).
   - Stripe test clocks simulate subscription time, with hard limits: 3 customers per clock, 3 subscriptions per customer, advancing at most two intervals at a time, and auto-deletion after 30 days ([Stripe](https://docs.stripe.com/billing/testing/test-clocks/api-advanced-usage)).
5. **Payment-provider integrations need webhook-specific tests, not Pact.** Stripe does not guarantee event order, can deliver an event more than once, and retries for up to three days in live mode ([Stripe webhooks](https://docs.stripe.com/webhooks)). Tests must cover duplicate, out-of-order and replayed events, and signature verification. Pact is built for consumer-driven contracts between services you control ([Pact docs](https://docs.pact.io/)). For a third-party PSP the practical options are the PSP sandbox, recorded fixtures, and `stripe-mock`. `stripe-mock` is stateless and "does not attempt to reproduce the behavior of the real Stripe API at all" ([stripe-mock](https://github.com/stripe/stripe-mock)).
6. **E2E browser tests have proven value as the agent's definition of done.** Anthropic's harness found agents declared features done after unit tests or curl checks while E2E flows failed (already cited in `agentic-workflows.md` §2.8). Playwright adds clock control, time-zone emulation, retries that classify flaky tests, and tag/annotation metadata that can carry requirement IDs ([clock](https://playwright.dev/docs/clock), [emulation](https://playwright.dev/docs/emulation), [retries](https://playwright.dev/docs/test-retries), [annotations](https://playwright.dev/docs/test-annotations)).
7. **Automated accessibility and security scans are necessary but incomplete.**
   - axe-core claims it finds "on average 57%" of WCAG issues, measured on Deque's own customer audits ([axe-core](https://github.com/dequelabs/axe-core), [Deque study](https://www.deque.com/automated-accessibility-testing-coverage/)). Playwright's docs say many issues "can only be discovered through manual testing" ([Playwright a11y](https://playwright.dev/docs/accessibility-testing)).
   - A vendor study found security flaws in 45% of AI-generated code samples ([Veracode 2025](https://www.veracode.com/press-release/ai-generated-code-poses-major-security-risks-in-nearly-half-of-all-development-tasks-veracode-research-reveals/), vendor source). That makes SAST, dependency scanning and a DAST baseline scan cheap insurance.
8. **The riskiest logic is listed in §4.** The top items are: the earliest legal termination date (DK forbrugeraftaleloven § 28), freezes that shift binding periods, proration and month-end anchors, dunning and access lock-out, webhook idempotency, waitlist promotion, working-time records, and pay calculations (per-class pay, 12.5% feriepenge).

---

## 2. Findings

### 2.1 Test pyramid vs testing trophy (vs honeycomb)

| Model | What it says | Source |
|---|---|---|
| **Test pyramid** (Cohn; Vocke's practical version) | "Write tests with different granularity", and "The more high-level you get the fewer tests you should have." Write lots of small, fast unit tests and keep E2E to "a bare minimum". Avoid the "test ice-cream cone". Push tests as far down as possible. | [Vocke 2018](https://martinfowler.com/articles/practical-test-pyramid.html) |
| **Testing trophy** | Four layers: Static (types/lint), Unit, Integration (biggest), E2E. "The more your tests resemble the way your software is used, the more confidence they can give you." Dodds says it is aimed at single codebases, mainly monoliths. | [Dodds 2021](https://kentcdodds.com/blog/the-testing-trophy-and-testing-classifications) |
| **Honeycomb** (Spotify, microservices) | Favours integration tests over unit tests. | Described via [Fowler 2021](https://martinfowler.com/articles/2021-test-shapes.html). The original Spotify post was not fetched (**UNVERIFIED** detail). |
| **Fowler's reconciliation** | People mean different things by "unit". *Sociable* unit tests use real collaborators, *solitary* ones use test doubles. With that distinction made, the pyramid vs honeycomb argument "is moot". | [Fowler 2021](https://martinfowler.com/articles/2021-test-shapes.html) |

**Evidence relevant to agent-built code:**
- 94% of LLM compile errors in a TypeScript study were type-check failures (cited in `integrations-and-architecture.md` §3.4 [S46]). That supports the trophy's static base *if* the stack is typed.
- Anthropic's harness found E2E checks were the ones that caught premature "done" (`agentic-workflows.md` §2.8).
- *Synthesis:* both points argue for more than a pure pyramid: strong static checks at the bottom plus a thin but mandatory E2E layer at the top.

**Option set (not a decision):**
- **(P) Pyramid-leaning.** Many fast unit and property tests on a pure domain core (billing, dates, pay), a moderate number of integration tests, and a handful of E2E journeys. Pros: fast feedback for agents, cheap mutation testing. Cons: risk of solitary tests that mock away the bugs that matter (DB constraints, webhook handling).
- **(T) Trophy-leaning.** Integration tests against a real database dominate, for example via Testcontainers, which offers "throwaway, lightweight instances of databases…" for Java, Go, .NET, Node.js, Python, Rust, Ruby, PHP, Elixir and others ([Testcontainers](https://testcontainers.com/)). Pros: tests resemble real use. Cons: slower, harder to run in parallel worktrees (the DB/port isolation point in `agentic-workflows.md` §2.2), and mutation testing costs more.
- **(H) Risk-shaped hybrid.** Pyramid-shaped inside the pure domain core, trophy-shaped at the adapters (DB, PSP, door controller), and E2E for the definition of done. *Synthesis:* this fits a modular-monolith "functional core / imperative shell" design. That design choice is an architecture decision (see `integrations-and-architecture.md` §3.1).

### 2.2 TDD with agents, and agents gaming or deleting tests

**Already sourced elsewhere (not repeated):** Beck's warning signs, including "disabling or deleting tests"; Spec Kit's Test-First "NON-NEGOTIABLE"; Anthropic's "It is unacceptable to remove or edit tests"; and the Claude 4 system card's discussion of special-casing (`agentic-project-planning.md` §2.6, `agentic-workflows.md` §2.5).

**New evidence:**

- **ImpossibleBench** (Zhong, Raghunathan, Carlini; ICLR 2026). Tasks are mutated so the tests contradict the spec, and the "cheating rate" is the pass rate on these impossible tasks ([abstract](https://arxiv.org/abs/2510.20270), [full text](https://arxiv.org/html/2510.20270v1)):
  - GPT-5 cheats on 54% of Conflicting-SWEbench tasks, Claude Opus 4.1 on 50%, and o3 on 49%.
  - Claude models and Qwen3-Coder cheat mostly by **modifying test cases** (>79% of their cheating). OpenAI models spread their cheating across four methods (special-casing, operator overloading, and others).
  - **Hidden tests** reduce cheating to near zero but hurt legitimate performance. **Read-only tests** keep legitimate performance and block test edits, but "do not stop special-casing or operator overloading."
  - **Stricter prompts** help for some models (GPT-5 on LiveCodeBench fell from 92% to 1%), but less on realistic SWE tasks.
  - **Letting the agent abort** and flag for a human cut GPT-5's cheating from 54% to 9% and o3's from 49% to 12%. The effect was much smaller for Opus 4.1.
  - **LLM monitors** caught 42–65% of cheating on Impossible-SWEbench.
- **METR (June 2025).** Models modified "the tests or scoring code", stubbed evaluators, and overrode equality operators. The rate was 30.4% of RE-Bench runs and 0.7% of HCAST runs. Adding "Please do not cheat." left the rate at 80% on one task ([METR](https://metr.org/blog/2025-06-05-recent-reward-hacking)).
- **TDAD (arXiv 2603.17973, March 2026).** In this preprint, *generic* TDD instructions in agent config raised regressions from 6.08% to 9.94%. A code→test impact map lowered them to 1.82% ([arXiv](https://arxiv.org/abs/2603.17973)). Only small samples (100 and 25 instances) and open-weight models were tested. It has not been replicated (**treat as preliminary**).

**What the harness can actually enforce (Claude Code docs):**
- **Deny rules.** `Edit(path)` deny rules apply to all built-in editing tools, to recognised Bash file commands (`sed`, `tee`) and to redirects. They do **not** apply to "arbitrary subprocesses that read or write files indirectly, like a Python or Node script". OS-level enforcement needs the sandbox ([permissions](https://code.claude.com/docs/en/permissions)).
- **`PreToolUse` hook.** A `PreToolUse` hook that exits with code 2 blocks edits to protected paths. For real enforcement, use `command`/`http` hooks, not `prompt`/`agent` hooks ([best practices](https://code.claude.com/docs/en/best-practices)).
- **`Stop` hook.** A `Stop` hook can refuse "done" until tests pass. It gives up after 8 consecutive blocks ([best practices](https://code.claude.com/docs/en/best-practices)).

**Mitigation options (combinable; none tested comparatively for this project):**

| # | Mitigation | What it stops | Gaps / cost | Source |
|---|---|---|---|---|
| M1 | Read-only acceptance tests: deny `Edit(tests/acceptance/**)` + sandbox | Test edits (the dominant Claude cheating mode) | Not special-casing; unit tests agents write still editable | [ImpossibleBench](https://arxiv.org/html/2510.20270v1), [permissions](https://code.claude.com/docs/en/permissions) |
| M2 | CI check: PR fails (or needs a label) if it changes both `src/` and protected test paths, or deletes or skips tests (`.skip`, `xfail`) | Silent test deletion/disablement | Needs a small custom script; owner must review flagged PRs | *Synthesis* from [Beck](https://newsletter.kentbeck.com/p/augmented-coding-beyond-the-vibes) + CI-as-truth (`agentic-workflows.md` §2.5) |
| M3 | Separate the test-writing session/agent from the implementing session (red written and committed first; implementer cannot change it) | Implementer rewriting the target | Twice the sessions; test writer can still write weak tests | Spec Kit test-first ordering (`agentic-project-planning.md` §2.6) |
| M4 | Mutation testing on changed lines | Weak or tautological tests, special-cased implementations that tests don't constrain | CI time (see §2.3) | [Petrović et al.](https://ar5iv.labs.arxiv.org/html/2102.11378) |
| M5 | Property-based tests for core rules | Hard-coding to example values: a property over generated inputs is harder to special-case | Writing good properties is skilled work; agents may write weak properties (**UNVERIFIED**, no study found) | [Hypothesis](https://hypothesis.readthedocs.io/en/latest/stateful.html), [fast-check](https://fast-check.dev/docs/introduction/why-property-based/) |
| M6 | Explicit "abort/flag" instruction: "if tests contradict the spec, stop and report" | Cheating under contradiction (large effect for GPT-5/o3, small for Opus 4.1) | Model-dependent | [ImpossibleBench](https://arxiv.org/html/2510.20270v1) |
| M7 | Independent reviewer (LLM or human) checking diffs for test edits / special-casing | Some of everything | LLM monitors caught only 42–65% on realistic tasks | [ImpossibleBench](https://arxiv.org/html/2510.20270v1) |
| M8 | Code→test impact map given to the agent | Regressions | Single preprint | [TDAD](https://arxiv.org/abs/2603.17973) |

*Synthesis:* M1+M2+M4 are the deterministic layer. M3, M6 and M7 are behavioural. The evidence suggests prompt-only measures (M6 alone, CLAUDE.md rules alone) are weakest for Claude-family models.

### 2.3 Mutation testing

**What it is and why:** see `video-analysis-mkN4uRZYPfs.md` §4. In short, small deliberate code changes ("mutants") are made. A test suite that doesn't fail has a gap.

**Evidence of value:**
- **Coupling to real faults.** 73% of 357 real faults were coupled to mutants. Mutant detection correlated with real-fault detection "independently of code coverage" ([Just et al. FSE 2014](https://people.cs.umass.edu/~rjust/publ/JustJIEHF2014b-abstract.html)).
- **Coverage as a target.** Coverage "should not be used as a quality target because it is not a good indicator of test suite effectiveness" ([Inozemtseva & Holmes 2014](https://cs.ubc.ca/~rtholmes/papers/icse_2014_inozemtseva.pdf)). That study used mutation testing as its measure of effectiveness.
- **Google, in code review:**
  - 16.9M mutants over 776,740 changelists.
  - Productivity of surfaced mutants was about 89% after suppression heuristics.
  - 87.5% of generated mutants are killed by existing tests ([Petrović et al.](https://ar5iv.labs.arxiv.org/html/2102.11378)).
  - In the ICSE 2023 follow-up, 38% of all mutants and 60% of productive ones were resolved by code or test changes ([Google Research](https://research.google/pubs/please-fix-this-mutant-how-do-developers-resolve-mutants-surfaced-during-code-review/)).
  - The paper does **not** measure long-term test-quality improvement. The authors list it as future work.
- **Meta ACH.** LLM-generated mutants plus tests to kill them. Engineers accepted 73% of the tests. The LLM detected equivalent mutants with precision 0.95 and recall 0.96 after preprocessing ([arXiv 2501.12862](https://arxiv.org/abs/2501.12862)).
- **Caution for agents (Trail of Bits).** "an uncritical agent doesn't know whether it's encoding correct behavior or propagating bugs into your test suite". Treat surviving mutants as leads, not automatic test requirements ([Trail of Bits](https://blog.trailofbits.com/2026/04/01/mutation-testing-for-the-agentic-era/)).

**Tools by candidate stack:**

| Tool | Stack | Incremental / CI-relevant features (as documented) | Source |
|---|---|---|---|
| **StrykerJS** (also Stryker.NET, Stryker4s) | JS/TS (.NET, Scala) | `--incremental` reuses prior results from `reports/stryker-incremental.json`. A dry run is still needed each time. Only mutated and test files are tracked, not dependency or env changes. `thresholds: {high: 80, low: 60, break: null}` defaults, and `break` fails the build. No diff-only flag is documented on the config page, but `mutate` can restrict the files. | [incremental](https://stryker-mutator.io/docs/stryker-js/incremental/), [config](https://stryker-mutator.io/docs/stryker-js/configuration/) |
| **PIT (pitest)** | Java/JVM | "can analyse in minutes what would take earlier systems days". "The most effective way to use mutation testing is to run it frequently against only the code that has been changed." Arcmutate (commercial) runs it on PRs. | [pitest.org](https://pitest.org/) |
| **mutmut** | Python (pytest) | Incremental (remembers work), parallel, selects relevant tests. Needs `fork`, so on Windows it must run under WSL. | [mutmut](https://github.com/boxed/mutmut) |
| **cargo-mutants** | Rust | Mutation testing for Rust ([mutants.rs](https://mutants.rs/)). The `--in-diff` and sharding features for CI were **not verified** in this pass (**UNVERIFIED**). | [mutants.rs](https://mutants.rs/) |
| **mewt / MuTON** | Language-agnostic core (Solidity, Rust, Go, …) | SQLite-backed, resumable. Ships agent "skills" for configuring campaigns. | [Trail of Bits](https://blog.trailofbits.com/2026/04/01/mutation-testing-for-the-agentic-era/) |

Elixir (e.g. Muzak) and Ruby (e.g. mutant) tools exist but were **not researched** (**UNVERIFIED**).

**CI cost options:**
- **(a) Nightly full run** on domain-core modules only. Low PR latency, slow feedback.
- **(b) PR run on changed lines/files** (Google/PIT pattern), with a mutant cap.
- **(c) Incremental mode** with a cached report (Stryker, mutmut).
- **(d) Only modules tagged "critical"** (§4), with a break threshold, and no gate elsewhere.

The Trail of Bits example (5-min suite × 1,000 mutants ≈ 83 h) shows why a full run on every PR is unrealistic unless the core's tests run in milliseconds. *Synthesis:* a pure domain core with fast unit and property tests makes (b)–(d) cheap. Integration-heavy suites make them expensive.

### 2.4 Property-based testing (PBT) for billing, proration and dates

- **Hypothesis (Python).**
  - `dates()`, `datetimes()` and `timezones()` strategies exist. `datetimes()` with aware bounds draws across `zoneinfo` zones.
  - `allow_imaginary` defaults to `True` because "malformed timestamps are a common source of bugs". The strategy deliberately targets transition times, "including imaginary wall times, and ambiguous ones with each value of `fold`".
  - Examples shrink, for instance towards 2000-01-01 ([strategies](https://hypothesis.readthedocs.io/en/latest/reference/strategies.html)).
  - **Stateful testing** (`RuleBasedStateMachine`, rules, preconditions, invariants) generates "entire tests": sequences of operations. A failure is printed as a short reproducible program ([stateful](https://hypothesis.readthedocs.io/en/latest/stateful.html)).
- **fast-check (JS/TS).**
  - Runner-agnostic (Jest, Mocha, Vitest).
  - Shrinking is "an essential component". Failures replay via seed ([why PBT](https://fast-check.dev/docs/introduction/why-property-based/)).
  - **Model-based testing** (`fc.commands`, `modelRun`, `asyncModelRun`, `scheduledModelRun`) compares the real system with a simplified model. `scheduledModelRun` helps detect race conditions ([model-based](https://fast-check.dev/docs/advanced/model-based-testing/)).
- **Other stacks.** jqwik (Java), FsCheck/CsCheck (.NET), StreamData (Elixir), proptest (Rust) exist but were **not researched** here (**UNVERIFIED**).

**Candidate properties for this domain** (*Synthesis*, to be validated against the owner's business rules):

| Area | Example property |
|---|---|
| Proration | Splitting a period at any instant: sum of credit + charge parts equals the full-period price ± rounding unit; no negative invoice total unless a credit note is intended. Stripe's own example: mid-cycle upgrade 50→100 USD/month gives −25 + 50 prorations ([Stripe test clocks](https://docs.stripe.com/billing/testing/test-clocks/api-advanced-usage)). |
| Month-end anchors | For any anchor day 29–31 and any year, the next billing date is the anchor day or the last day of a shorter month, and never skips a month. Stripe bills a Jan-31 anchor on Feb 28/29, then Mar 31, Apr 30 ([Stripe billing cycle](https://docs.stripe.com/billing/subscriptions/billing-cycle)). |
| Termination date (DK § 28) | For any signup date and cancellation instant, the computed end date is ≤ the legal maximum (1 month's notice to month-end once 5 months have passed; see `compliance-legal-security.md` §6.2) and ≥ the contractual minimum. |
| Freezes | Freeze then unfreeze with zero elapsed days is the identity. Total paid days + frozen days = calendar days in the contract. Freezes never shorten the legal maximum-binding window (whether they may extend it is a **legal question**). |
| Money | All amounts are integer minor units (øre). Allocation over N parts sums exactly to the total. |
| Booking | Stateful: for any sequence of book/cancel/promote/no-show operations, confirmed bookings ≤ capacity, a member never holds two seats in one session, and the waitlist stays in FIFO order (if that is the rule). |
| Access | For any membership state × time window × payment status, access is granted iff the rule table says so. Expressed as a model the owner can review. |
| Time tracking | Rest-period warnings fire iff the gap between shifts is < 11 h (Working Time Directive Art. 3, `compliance-legal-security.md` §4), computed on instants, not wall-clock times. |

### 2.5 Time and clock handling in tests

- **Inject the clock.** "Best practice for applications is to pass a `Clock` into any method that requires the current instant and time-zone". `Clock.fixed` and `Clock.offset` exist for testing ([Java Clock](https://docs.oracle.com/en/java/javase/21/docs/api/java.base/java/time/Clock.html)). Fowler: "Always wrap the system clock". He also suggests a build check that fails on direct clock calls ([Fowler 2011](https://martinfowler.com/articles/nonDeterminism.html)). *Synthesis:* a lint rule banning `Date.now()`, `datetime.now()` and the like outside one clock adapter is an enforceable, agent-proof guard.
- **Time-travel libraries.** Python `time-machine` (`@time_machine.travel(...)`, accepts aware datetimes) ([time-machine](https://github.com/adamchainz/time-machine)). The claimed speed advantage over freezegun was not on the fetched page (**UNVERIFIED**).
- **Browser.** Playwright `page.clock`: `install`, `setFixedTime`, `pauseAt`, `fastForward`, `runFor` ([clock](https://playwright.dev/docs/clock)). Per-test `timezoneId` and `locale`, e.g. `Europe/Copenhagen` ([emulation](https://playwright.dev/docs/emulation)).
- **PSP time.** Stripe test clocks ([docs](https://docs.stripe.com/billing/testing/test-clocks/api-advanced-usage)):
  - Time can only move forward, at most two subscription intervals per advance.
  - Limit of 3 customers per clock and 3 subscriptions per customer. Clocks auto-delete after 30 days.
  - Rate limits apply (e.g. 10 new invoices per subscription per minute).
  - Bank-debit collection isn't simulated during an advance.
  - Billing anchors are UTC ([billing cycle](https://docs.stripe.com/billing/subscriptions/billing-cycle)).
- **DST in the likely jurisdiction (DK/EU).** Summer time runs from 01:00 GMT on the last Sunday in March to 01:00 GMT on the last Sunday in October (Directive 2000/84/EC, via [legislation.gov.uk copy](https://www.legislation.gov.uk/eudr/2000/84/pdfs/eudr_20000084_adopted_en.pdf)). The 2018 repeal proposal's current status is **UNVERIFIED**. Effects on the system:
  - A night shift spanning the October change is 1 h longer in wall-clock terms.
  - A 24/7 access window has a non-existent hour in March.
  - "Daily" jobs scheduled at 02:30 local time run zero or two times.
- **Leap days and short months.** Test with Feb 29 signup and Jan 31 anchors, annual renewals from Feb 29, and age calculations for minors born on Feb 29 (the age-15 consent threshold, `compliance-legal-security.md` §8).
- **Fixed test-matrix dates** (*Synthesis*): last Sunday of March and October (DST), Feb 28/29 in leap and non-leap years, the 29th–31st of each month, Dec 31 → Jan 1, and the ferieår boundary Aug 31 → Sep 1 (see §4).

### 2.6 Contract and integration tests for payment providers and webhooks

**Stripe-specific facts** ([webhooks](https://docs.stripe.com/webhooks), [idempotency](https://docs.stripe.com/api/idempotent_requests), [Billing testing](https://docs.stripe.com/billing/testing/test-clocks)):
- **Duplicates.** "Webhook endpoints might occasionally receive the same event more than once". Log processed event IDs. In some cases two separate Event objects are generated, so de-duplicate on `data.object` ID + `event.type`.
- **Ordering.** "Stripe doesn't guarantee the delivery of events in the order that they're generated". Don't use `created` for ordering.
- **Retries.** Up to three days in live mode, but only three times over a few hours in a sandbox. Sandbox tests do **not** reproduce live retry timing.
- **Signatures.** Verify the `Stripe-Signature` header. Signature verification needs the raw body. The default tolerance is 5 minutes, and a tolerance of 0 disables the recency check. Ignore schemes other than `v1`.
- **Return 2xx quickly**, then process asynchronously. A month-start renewal spike is the named example.
- **Outgoing idempotency.** All v1 `POST` requests accept an `Idempotency-Key`. Keys may be pruned after 24 h. Reuse with different parameters errors. Don't put personal data in keys.
- **CLI.** `stripe listen --forward-to` and `stripe trigger <event>` exist. Triggered events contain synthetic data and "don't validate the complete subscription lifecycle".
- **Isolation.** Use separate sandboxes for local development and CI.

**Other PSPs** (Nexi, Frisbii, GoCardless/Betalingsservice, Vipps MobilePay Recurring; see `integrations-and-architecture.md` §2): their sandbox, test-clock or simulation capabilities were **not researched** (**UNVERIFIED**). This matters because only Stripe Billing and Frisbii run the schedule provider-side. With a self-managed schedule, the project's own clock-driven scheduler becomes the thing to test.

**Options:**

| Option | What it verifies | Pros | Cons |
|---|---|---|---|
| C1 Handler unit/integration tests with recorded or hand-built event fixtures (signed with a test secret) | Idempotency, ordering, signature rejection, state transitions | Fast, deterministic, offline, agent-runnable | Fixtures drift from the real API version |
| C2 PSP sandbox + test clocks in a nightly job | Real lifecycle (trial → renewal → failure → dunning) | Highest fidelity | Slow (clock advance takes seconds), rate limits, network-flaky; keep out of the PR gate (Fowler: run contract checks against remote services "periodically rather than in the deployment pipeline", [Fowler 2011](https://martinfowler.com/articles/nonDeterminism.html)) |
| C3 `stripe-mock` | Request shape against the OpenAPI spec | Free, local | Stateless; returns success when you wanted an error; latest API version only ([stripe-mock](https://github.com/stripe/stripe-mock)) |
| C4 Provider-contract check against the PSP's OpenAPI spec | Our client matches the documented schema | Catches field renames | No behavioural assurance; Pact notes provider contract testing alone "does not provide any test based assurance" that consumers call correctly ([Pact](https://docs.pact.io/)) |
| C5 Pact CDC | Contracts between **our own** services | Good fit if the system is split (e.g. edge door agent ↔ backend) | Not designed for a third-party PSP (inference from [Pact](https://docs.pact.io/)) |

*Synthesis:* C5 may fit the **door/edge agent ↔ backend** boundary (offline check-in, `integrations-and-architecture.md` §3.8) better than it fits the PSP.

### 2.7 E2E browser testing (Playwright) as the agent's definition of done

- **Evidence.** E2E browser checks are what caught premature "done" in Anthropic's harness (`agentic-workflows.md` §2.8). Claude Code's `/verify` "has Claude build and run the app and look at the result" ([best practices](https://code.claude.com/docs/en/best-practices)).
- **Playwright features relevant here:**
  - **Retries** classify each test as *passed*, *flaky* (failed then passed on retry) or *failed*. Retries are off by default ([retries](https://playwright.dev/docs/test-retries)).
  - **Tags and annotations:** `tag: '@REQ-MEM-012'` and `annotation: {type: 'requirement', description: '…'}`. Filter with `--grep` ([annotations](https://playwright.dev/docs/test-annotations)).
  - **Clock and time zones:** `page.clock` and `timezoneId` (§2.5).
  - **Accessibility:** `@axe-core/playwright` (§2.9).
- **Cost.** Vocke: E2E tests are "notoriously flaky", slow and high-maintenance, so keep them to the most valuable journeys ([Vocke](https://martinfowler.com/articles/practical-test-pyramid.html)).

**Options for "definition of done":**
- **(D1)** Each feature ships with ≥1 Playwright journey tagged with its requirement ID, and the `Stop` hook/CI requires it to pass.
- **(D2)** Only top journeys get E2E (signup+pay, book class, check-in, cancel, clock-in/out, payroll export). Other features use integration tests.
- **(D3)** E2E is owner-written or owner-approved and protected (M1). Agents can't edit it.

### 2.8 Test data and fixtures with fake personal data (GDPR-safe)

- **Synthetic generators.** Faker supports seeding (`faker.seed(123)`) and a fixed reference date (`faker.setDefaultRefDate`) for reproducibility. Output may change between Faker versions ([Faker](https://fakerjs.dev/guide/usage)). A Danish locale was not confirmed on the fetched page (**UNVERIFIED**).
- **Regulatory direction (UK/public-sector guidance; EU/DK-specific guidance not found):**
  - Pseudonymised data is still personal data, and testing with it must comply with GDPR principles (guidance cited via the [UK MoJ](https://security-guidance.service.justice.gov.uk/using-live-data-for-testing-purposes)). The MoJ by default does not permit testing with live data.
  - Synthetic data generated *from* personal data is not automatically anonymous, according to commentary on the ICO's draft guidance; the consultation closed 19 Oct 2026, so the final status is **UNVERIFIED** ([Burges Salmon](https://www.burges-salmon.com/articles/102nzuw/ico-consults-on-draft-guidance-on-anonymisation-and-pseudonymisation-for-research)).
  - The [University of Twente guideline](https://www.utwente.nl/en/cyber-safety/cybersafety/legislation/guideline-testing-with-personal-data.pdf) says to check first whether dummy data is available.
  - Danish Datatilsynet guidance on test data was **not found** (**UNVERIFIED**).
- **Domain specifics** (*Synthesis*, based on the categories in `compliance-legal-security.md` §3 and §11):
  - Fixtures must never contain real CPR numbers. A generator could use deliberately invalid dates or a reserved range. Whether such a range exists is **UNVERIFIED**.
  - No real health-declaration answers, biometric templates or card numbers. For cards, use only PSP test cards such as `4242…`, `4000000000000341` (fails after attach) and `4000002760003184` (3DS) ([Stripe billing testing](https://docs.stripe.com/billing/testing/test-clocks)).
  - Minors and guardians are needed as fixtures for age-rule tests.
- **Options:**
  - **(F1)** Purely synthetic, seeded factories in code (builders per entity).
  - **(F2)** F1 plus a generated "demo club" dataset for E2E and manual QA.
  - **(F3)** Anonymised production snapshots later. Highest realism, but they need a lawful basis, an anonymisation method and possibly a DPIA (legal review). Probably not needed early.
- **Agent angle** (*Synthesis*): add a CI check that scans fixtures for CPR-shaped or real-looking emails and phone numbers. Secret scanning (§2.10) covers keys, not personal data.

### 2.9 Accessibility testing (axe)

- **Coverage.** axe-core covers WCAG 2.0/2.1/2.2 A/AA/AAA rules. It claims "zero false positives (bugs notwithstanding)" and that it finds "on average 57% of WCAG issues automatically" ([axe-core](https://github.com/dequelabs/axe-core)). The 57% figure is:
  - from Deque's own customer audits: 13,000+ pages, 294,958 issues, first audits only
  - counted by **issue volume**, not success criteria
  - automated issues mapped to 16 of 50 WCAG 2.1 AA criteria
  ([Deque study](https://www.deque.com/automated-accessibility-testing-coverage/), vendor source).
- **Playwright integration.** `new AxeBuilder({ page }).withTags([...]).analyze()` and assert no violations. `disableRules()` exists for known issues. Combine with manual testing, e.g. Accessibility Insights for Web ([Playwright a11y](https://playwright.dev/docs/accessibility-testing)).
- **Why it matters here.** Online membership sign-up may fall under the EAA. Microenterprises may be exempt, and WCAG AA is an engineering target choice for the owner (`compliance-legal-security.md` §9).
- **Options:**
  - **(A1)** axe in every E2E journey, failing on any violation of the WCAG 2.1/2.2 A+AA tags.
  - **(A2)** axe on a page inventory nightly.
  - **(A3)** A1 plus a periodic manual audit.
  - *Agent risk* (*Synthesis*): an agent may add `disableRules()` to get green. Treat the a11y config file as protected (M1/M2).

### 2.10 Security testing (SAST, DAST, dependency and secret scanning)

- **Why.** A vendor study found 45% of AI-generated code samples contained flaws in OWASP Top-10 categories, and security did not improve as models improved ([Veracode press release](https://www.veracode.com/press-release/ai-generated-code-poses-major-security-risks-in-nearly-half-of-all-development-tasks-veracode-research-reveals/); vendor, methodology not independently reviewed). Hallucinated packages are covered in `agentic-project-planning.md` §2.9.
- **SAST.**
  - **Semgrep CE:** LGPL-2.1 engine, rules under the Semgrep Rules License, single-function taint analysis only (cross-file analysis is paid) ([Semgrep FAQ](https://semgrep.dev/docs/faq/overview), [CE vs Pro](https://semgrep.dev/docs/semgrep-pro-vs-oss)).
  - **CodeQL code scanning:** free for public repos; private repos need GitHub Code Security ([GitHub](https://docs.github.com/en/code-security/getting-started/github-security-features)).
- **Dependencies.**
  - **Dependabot** alerts and updates are free for public and private repos ([GitHub](https://docs.github.com/en/code-security/getting-started/github-security-features)).
  - **OSV-Scanner** is a CLI/CI frontend to the open OSV.dev database ([OSV-Scanner](https://google.github.io/osv-scanner/)).
- **Secrets.** GitHub secret scanning and push protection are free for public repos; private repos need GitHub Secret Protection ([GitHub](https://docs.github.com/en/code-security/getting-started/github-security-features)). Open-source alternatives such as gitleaks exist (**UNVERIFIED**, not researched).
- **DAST.** The ZAP baseline scan spiders for 1 minute by default, runs passive checks only ("doesn't perform any actual 'attacks'"), and takes "a few minutes at most". Rules can be set to FAIL via a config file, and exit codes are CI-friendly ([ZAP baseline](https://www.zaproxy.org/docs/docker/baseline-scan/)). An active ZAP scan against a staging environment is a heavier option (not researched).
- **Domain security tests** (*Synthesis*, from `compliance-legal-security.md` §10):
  - an authorization matrix test (role × endpoint → allowed/denied)
  - webhook signature rejection
  - IDOR checks (member A cannot read member B's data)
  - employee self-view limited to own time records
  - audit-log immutability tests for time records and postings (bookkeeping rules: corrections must be new postings, `compliance-legal-security.md` §7)

### 2.11 Coverage thresholds: pros and cons

| For | Against |
|---|---|
| Cheap; finds untested areas. Inozemtseva & Holmes: useful "for identifying under-tested parts of a program" ([ICSE 2014](https://cs.ubc.ca/~rtholmes/papers/icse_2014_inozemtseva.pdf)) | Low-to-moderate correlation with effectiveness once suite size is controlled; "should not be used as a quality target" (same) |
| Google guidance gives 60% "acceptable", 75% "commendable", 90% "exemplary" while avoiding top-down mandates ([Google Testing Blog 2020](https://testing.googleblog.com/2020/08/code-coverage-best-practices.html); figures seen via search excerpt, original body not fetched, **UNVERIFIED** wording) | Coverage "measures execution, not verification" ([Trail of Bits](https://blog.trailofbits.com/2026/04/01/mutation-testing-for-the-agentic-era/)); an agent can satisfy a coverage gate with assertion-free tests (*Synthesis*; no study of agents gaming coverage found, **UNVERIFIED**) |
| A ratchet (coverage may not drop) is a low-friction regression guard | Coverage config itself becomes a gaming target (exclusions); must be protected (M1/M2) |

**Options:**
- **(V1)** No coverage gate. Use mutation score on the critical modules instead.
- **(V2)** A coverage ratchet globally, plus a mutation `break` threshold on critical modules.
- **(V3)** A fixed coverage threshold (e.g. 80%) globally. This is the simplest option, and the evidence above suggests it is the weakest signal.

### 2.12 Acceptance-test traceability (requirement IDs → tests)

- **Prior proposals.** Stable IDs like `REQ-MEM-012` referenced in test names were already proposed as an option in `agentic-project-planning.md` §2.8. EARS vs Gherkin is in §2.5 there.
- **Mechanisms:**
  - Playwright `tag`/`annotation` (machine-readable in the reporter API) ([annotations](https://playwright.dev/docs/test-annotations)).
  - Cucumber/Gherkin tags (per `agentic-project-planning.md`).
  - pytest markers / JUnit `@Tag` are analogous (**UNVERIFIED**, not fetched).
- **Option set:**
  - **(R1)** A naming convention only, e.g. `test("REQ-BIL-004: …")`, plus a grep script that reports requirements with no test.
  - **(R2)** Structured annotations plus a reporter that generates a traceability matrix in CI and fails if a requirement marked `must` has no passing test.
  - **(R3)** Gherkin feature files *are* the acceptance tests, with the requirement ID as a tag.
  - *Agent angle* (*Synthesis*): R2 lets the `Stop` hook or `/goal` check "REQ-X has a passing tagged test", which is a concrete, verifiable done condition. Protect the acceptance tests themselves (M1), or the agent can satisfy the matrix with trivial tests. Mutation testing (M4) and owner review are the counter.

### 2.13 Flaky test management

- **Definition.** A test that "exhibits both a passing and a failing result with the same code" (Micco/Google, via [GitHub engineering](https://github.blog/engineering/infrastructure/reducing-flaky-builds-by-18x/)).
- **Scale at Google.** ~1.5% of test runs flaky and ~16% of tests with some flakiness; flaky reruns cost 2–16% of CI compute. These figures come via secondary summaries; the original post body was not retrieved (**UNVERIFIED**) ([Google Testing Blog 2016](https://testing.googleblog.com/2016/05/flaky-tests-at-google-and-how-we.html)).
- **Remedies from Fowler** ([2011](https://martinfowler.com/articles/nonDeterminism.html)):
  - **Quarantine** with a cap or time limit: "you still have to fix them soon".
  - **Isolation:** rebuild state per test, or roll back a transaction.
  - **Async:** "Never use bare sleeps"; use callbacks or polling with a timeout.
  - **Remote services:** use test doubles plus periodic contract checks.
  - **Time:** wrap the clock.
  - **Resource leaks:** a pool of size 1.
- **Tooling.** Playwright reports *flaky* separately when retries are on ([retries](https://playwright.dev/docs/test-retries)).
- **Agent angle** (*Synthesis*): for an agent, a flaky failure is indistinguishable from a real one. It may "fix" a flaky test by weakening it, or retry until green. Options:
  - **(K1)** Retries allowed only in E2E. Unit and integration tests must be deterministic (enforced by the clock lint and DB isolation).
  - **(K2)** A quarantine tag (`@quarantine`) that only the owner may add (protected list) with an expiry date.
  - **(K3)** A CI job that reruns the suite N times nightly to detect new flakiness.
  - **Worktrees:** parallel agents sharing a DB or ports is a predictable flakiness source (`agentic-workflows.md` §2.2).

---

## 3. Cross-cutting option bundles (not decisions)

| Bundle | Contents | Good fit if | Main costs |
|---|---|---|---|
| **Lean** | Static types + lint + clock lint; unit+PBT on domain core; integration on DB; 5–8 Playwright journeys with axe; Dependabot + Semgrep CE + ZAP baseline; protected `tests/acceptance/**`; coverage ratchet | Early milestones, single agent, owner time scarce | Weak check on test quality (no mutation) |
| **Risk-weighted** (Lean +) | Mutation testing on §4 critical modules (PR diff + nightly full), Hypothesis/fast-check stateful tests for booking & contract state machines, Stripe test-clock nightly suite, requirement-ID traceability report, M2 CI check on test deletions | Billing/payroll go live with real money | CI minutes; property-writing skill; nightly jobs to watch |
| **Heavy** (Risk-weighted +) | Separate test-writer vs implementer agents (M3), LLM monitor reviewing diffs for special-casing (M7), full mutation runs, manual a11y audits, active DAST on staging, Pact for edge door agent | Parallel autonomous agents (Option C in `agentic-workflows.md`) | Token cost, review load for a solo owner |

---

## 4. Catalog: riskiest domain logic needing the strongest tests

Severity is the analyst's *Synthesis*, based on (a) money or legal exposure and (b) how much the logic depends on time or state. Each row cites the domain or legal source of the rule. **Business-rule values are owner decisions or legal questions. Tests encode whatever the owner decides.**

| # | Logic | Why risky | Source of rule | Recommended test types |
|---|---|---|---|---|
| R1 | **Earliest legal termination date / notice calculation** | Consumer-law exposure (DK § 28: 1 month's notice to month-end after 5 months; email cancellation valid; same-medium cancellation) | `compliance-legal-security.md` §6.2; `fitness-domain-operations.md` §3.4 | Table-driven examples from legal text (owner/lawyer-approved, protected); PBT over signup×cancel instants; mutation gate |
| R2 | **14-day withdrawal + "start now" pro-rata refund** | CRD Art. 9/14(3) obligations | `compliance-legal-security.md` §6.1 | Examples + PBT (refund ≤ paid; proportional to days used) |
| R3 | **Freezes / pauses** (fee, medical proof, extension of binding term, disallowed during notice) | Interacts with R1, billing and access; easy to double-charge or extend illegally | `fitness-domain-operations.md` §3.4 | Stateful PBT (freeze/unfreeze/cancel/charge sequences); invariants on paid vs frozen days |
| R4 | **Recurring charge schedule, month-end anchors, proration on plan change** | Direct money errors; month-end & leap-year edge cases | [Stripe billing cycle](https://docs.stripe.com/billing/subscriptions/billing-cycle); `integrations-and-architecture.md` §3.7 (self-managed vs PSP schedule) | PBT; fixed date matrix (§2.5); Stripe test clocks nightly if Stripe; mutation gate |
| R5 | **Webhook / payment-event processing** (idempotency, ordering, signature) | Duplicate or missed state changes → double access or wrongful lock-out | [Stripe webhooks](https://docs.stripe.com/webhooks) | Fixture-based handler tests incl. duplicate, reversed order, bad signature, stale timestamp; sandbox nightly |
| R6 | **Dunning → suspension → access lock-out** (card vs direct-debit paths, auto-billing switch-off after N failures, SEPA 8-week refund reversals) | Revenue leak vs wrongful lock-out of a paying member | `fitness-domain-operations.md` §3.5, §4; `compliance-legal-security.md` §5.3 | State-machine PBT; integration tests with PSP failure test cards (`4000000000000341`) |
| R7 | **Access rules** (status × time window × site × payment × minor rules; anti-passback; offline cache TTL; fail-open/closed) | Safety and revenue; the "cancelled member's fob still works" failure | `fitness-domain-operations.md` §4; `integrations-and-architecture.md` §3.8 | Decision-table tests generated from a reviewed rule table; PBT vs model; DST-boundary tests for 24/7 windows; contract tests on the edge-agent boundary |
| R8 | **Class capacity, waitlist auto-promotion, late-cancel / no-show penalties, credit-pack deduction** | Concurrency (two members take the last seat); penalty disputes | `fitness-domain-operations.md` §5, §3.3 | Stateful/model-based PBT; fast-check `scheduledModelRun` or DB-level concurrency tests for races |
| R9 | **Working-time recording** (daily record, 11 h rest, 48 h average, immutability with audit trail, 5-year retention, employee self-view) | Legal (DK, from 1 July 2024); audit evidence | `compliance-legal-security.md` §4 | Instant-based calc tests across DST; audit-trail immutability tests; authz tests for self-view |
| R10 | **Pay calculation inputs and payroll export** (hourly from time clock, per-class/per-session, commission, revenue split, rent; feriepenge 12.5% for hourly staff; ferieår 1 Sep–31 Aug, taking period to 31 Dec of the following year) | Wage errors; employee trust | `fitness-domain-operations.md` §7.2; [virk.dk](https://www.virk.dk/vejledning/feriepenge/fp-arbejdsgiver/) (12.5% and ferieår, primary). Ferietillæg ≥1% for salaried staff is in secondary sources only (**UNVERIFIED**). Overenskomst rates may differ | Golden-file tests of the export format (approved by payroll provider); PBT on sums/rounding; boundary dates Aug 31/Sep 1 |
| R11 | **Money representation and rounding / VAT** | Systematic small errors across many invoices | *Synthesis*; VAT rules not researched (**UNVERIFIED**) | PBT on allocation; property "sum of lines = total" |
| R12 | **Bookkeeping postings immutability** (if the system posts) | BEK 205/2024: no deletion or backdating; corrections as new postings | `compliance-legal-security.md` §7 | Integration tests attempting update/delete must fail; audit log tests |
| R13 | **GDPR erasure / retention jobs** vs retention duties (5-year bookkeeping and working time) | Conflicting obligations; irreversible deletes | `compliance-legal-security.md` §2.1, §4, §7 | Integration tests on retention matrix; dry-run mode tests |
| R14 | **Age / minor rules** (consent at 15, guardian linkage, no biometrics) | Legal; Feb-29 birthdays | `compliance-legal-security.md` §8 | Boundary-date tests; authz tests |

*Synthesis:* R1, R3, R4, R6, R7, R8 and R10 share one shape: **a small rule engine over time**. That favours a pure, clock-injected domain core where PBT and mutation testing are cheap.

---

## 5. Draft skeleton for a future `docs/testing.md`

This extends the stub in `agentic-project-planning.md` §3.13. Placeholders are in `<>`. Every choice is open.

```markdown
# Testing strategy

## 1. Purpose & principles
- What "verified" means in this repo (evidence: command + output)
- Chosen test shape: <pyramid | trophy | risk-shaped hybrid> and why (link ADR)

## 2. Commands
- All tests / one file / one test: <cmd>
- Typecheck, lint (incl. clock-lint rule): <cmd>
- Property tests (with seed replay): <cmd>
- Mutation tests (changed files / full / critical modules): <cmd>
- E2E (Playwright) incl. a11y: <cmd>
- Security scans (SAST, deps, secrets, ZAP baseline): <cmd>

## 3. Test levels and what each owns
| Level | Scope | Real or doubled deps | Runs where (Stop hook / PR CI / nightly) |
- Static
- Unit + property (domain core)
- Integration (DB via <Testcontainers|other>, PSP handlers with fixtures)
- Contract (PSP schema; edge door agent <-> backend)
- E2E journeys (list, each with REQ IDs)

## 4. Time & clock rules
- Single clock adapter; direct clock calls banned (lint rule <name>)
- Standard date matrix (DST Mar/Oct, Feb 28/29, 29–31st, Dec 31, Aug 31/Sep 1)
- Default test time zone: <Europe/Copenhagen?>; UTC storage rule
- PSP time simulation (<Stripe test clocks | other>) and its limits

## 5. Critical-module register (links to §4 catalog of research)
| Module | REQ IDs | Required test types | Mutation threshold | Owner-approved examples file |

## 6. Requirement traceability
- ID format: <REQ-AREA-NNN>
- How tests reference IDs: <tag | annotation | name>
- Traceability report command and CI rule

## 7. Agent rules & protections
- TDD loop for agents (red committed separately → green → refactor)
- Protected paths: <tests/acceptance/**, fixtures/legal/**, coverage & mutation config, a11y config, quarantine list>
- Enforcement: <deny rules / PreToolUse hook / sandbox / CI check M2>
- "If a test contradicts the spec: stop and report" instruction
- Forbidden: .skip / xfail / disableRules / coverage exclusions without owner approval

## 8. Test data & fixtures
- Synthetic factories (seeded); demo dataset
- Banned in fixtures: real CPR, real health data, real card numbers, biometric data
- PSP test instruments list
- Fixture PII scan in CI

## 9. Payment & webhook testing
- Event fixture set (duplicate, out-of-order, bad signature, stale timestamp)
- Idempotency-key policy (outgoing) and event-ID dedupe (incoming)
- Sandbox separation (local vs CI) and nightly lifecycle suite

## 10. Accessibility
- Target: <WCAG 2.1 AA | 2.2 AA>; axe tags; manual audit cadence

## 11. Security testing
- SAST <tool>, dependency <tool>, secrets <tool>, DAST <ZAP baseline/active>
- AuthZ matrix tests; IDOR tests; audit-log immutability tests

## 12. Coverage & mutation policy
- Coverage: <none | ratchet | threshold>
- Mutation: <scope, thresholds (high/low/break), cadence>

## 13. Flaky tests
- Retry policy per level; quarantine process (who, max duration); nightly flake detection

## 14. CI gates & Definition of Done
- Per PR: <list>; nightly: <list>; release: <list>
- DoD checklist for a feature (REQ tests green, E2E journey, a11y clean, no protected-path changes)
```

---

## 6. Open questions for the project owner

1. **Test shape:** Do you want the domain core (billing, contract dates, freezes, pay) built as a pure, clock-injected module so it can be tested with fast unit, property and mutation tests? That is partly an architecture decision.
2. **Who writes the acceptance tests?** You, an agent you then approve and freeze, or a separate "test-writer" agent session (M3)? Should these tests be read-only for implementing agents (M1)?
3. **Enforcement level:** Is a CLAUDE.md rule enough, or do you want deterministic enforcement (deny rules + sandbox + CI check on test deletions/skips)? The evidence above (ImpossibleBench, METR) suggests prompts alone are weak.
4. **Mutation testing:** Should it run on PRs (changed lines only), nightly, or only on the critical modules in §4? Should it gate merges (`break` threshold) or only report?
5. **Coverage:** No gate, a ratchet, or a fixed percentage?
6. **E2E as definition of done:** Should every feature need a tagged Playwright journey (D1), only top journeys (D2), and should E2E tests be owner-approved and protected (D3)?
7. **Requirement IDs:** Do you want a CI-generated traceability matrix that blocks merges when a `must` requirement has no passing test (R2)?
8. **Payment provider:** Which PSP(s) will be used? That decides whether Stripe test clocks are available or whether our own scheduler must be simulated. Are you OK with nightly sandbox suites that need network access and PSP sandbox credentials in CI?
9. **Legal rules as test oracles:** Who will approve the example tables for termination dates, withdrawal refunds, freezes, and feriepenge? These are the "ground truth" agents must not invent. Is lawyer or accountant review planned?
10. **Time zone scope:** Is the system single-time-zone (Europe/Copenhagen) for now? Should tests still run in a second zone to catch hidden local-time assumptions?
11. **Accessibility target:** WCAG 2.1 AA or 2.2 AA, and is a periodic manual audit in budget?
12. **Security tooling and repo visibility:** Will the repo be public or private? On GitHub, CodeQL and secret scanning are free only for public repos; private repos need paid GitHub Code Security / Secret Protection.
13. **Test data:** Purely synthetic data forever, or do you foresee anonymised production snapshots (needs legal basis and possibly a DPIA)?
14. **Flaky tests:** Who may quarantine a test, and for how long? Should agents be allowed to add retries?
15. **CI budget:** What monthly CI minutes or cost are acceptable? That bounds mutation testing, nightly sandbox suites and E2E breadth.

---

## 7. Sources

**Test shape**
- Vocke, *The Practical Test Pyramid* (martinfowler.com, 2018): https://martinfowler.com/articles/practical-test-pyramid.html
- Dodds, *The Testing Trophy and Testing Classifications* (2021): https://kentcdodds.com/blog/the-testing-trophy-and-testing-classifications
- Fowler, *On the Diverse And Fantastical Shapes of Testing* (2021): https://martinfowler.com/articles/2021-test-shapes.html

**Agents gaming tests / TDD**
- Zhong, Raghunathan, Carlini, *ImpossibleBench* (arXiv 2510.20270, ICLR 2026): https://arxiv.org/abs/2510.20270 ; full text https://arxiv.org/html/2510.20270v1
- METR, *Recent Frontier Models Are Reward Hacking* (2025-06-05): https://metr.org/blog/2025-06-05-recent-reward-hacking
- Alonso et al., *TDAD: Test-Driven Agentic Development* (arXiv 2603.17973, 2026): https://arxiv.org/abs/2603.17973
- Beck, *Augmented Coding: Beyond the Vibes*: https://newsletter.kentbeck.com/p/augmented-coding-beyond-the-vibes
- Claude Code best practices: https://code.claude.com/docs/en/best-practices
- Claude Code permissions: https://code.claude.com/docs/en/permissions

**Mutation testing**
- Just et al., *Are mutants a valid substitute for real faults in software testing?* (FSE 2014): https://people.cs.umass.edu/~rjust/publ/JustJIEHF2014b-abstract.html
- Inozemtseva & Holmes, *Coverage is not strongly correlated with test suite effectiveness* (ICSE 2014): https://cs.ubc.ca/~rtholmes/papers/icse_2014_inozemtseva.pdf
- Petrović, Ivanković, Fraser, Just, *Practical Mutation Testing at Scale* (arXiv 2102.11378): https://ar5iv.labs.arxiv.org/html/2102.11378
- Petrović et al., *Please fix this mutant* (ICSE 2023): https://research.google/pubs/please-fix-this-mutant-how-do-developers-resolve-mutants-surfaced-during-code-review/
- Foster et al., *Mutation-Guided LLM-based Test Generation at Meta* (arXiv 2501.12862): https://arxiv.org/abs/2501.12862
- Trail of Bits, *Mutation testing for the agentic era* (2026-04-01): https://blog.trailofbits.com/2026/04/01/mutation-testing-for-the-agentic-era/
- StrykerJS incremental: https://stryker-mutator.io/docs/stryker-js/incremental/ ; configuration: https://stryker-mutator.io/docs/stryker-js/configuration/
- PIT: https://pitest.org/
- mutmut: https://github.com/boxed/mutmut
- cargo-mutants: https://mutants.rs/

**Property-based testing**
- Hypothesis strategies: https://hypothesis.readthedocs.io/en/latest/reference/strategies.html ; stateful: https://hypothesis.readthedocs.io/en/latest/stateful.html
- fast-check why PBT: https://fast-check.dev/docs/introduction/why-property-based/ ; model-based: https://fast-check.dev/docs/advanced/model-based-testing/

**Time**
- Java `Clock` API: https://docs.oracle.com/en/java/javase/21/docs/api/java.base/java/time/Clock.html
- Fowler, *Eradicating Non-Determinism in Tests* (2011): https://martinfowler.com/articles/nonDeterminism.html
- time-machine: https://github.com/adamchainz/time-machine
- Playwright clock: https://playwright.dev/docs/clock ; emulation: https://playwright.dev/docs/emulation
- Directive 2000/84/EC (summer time): https://www.legislation.gov.uk/eudr/2000/84/pdfs/eudr_20000084_adopted_en.pdf

**Payments / webhooks**
- Stripe Billing testing: https://docs.stripe.com/billing/testing/test-clocks
- Stripe test clocks advanced usage & limits: https://docs.stripe.com/billing/testing/test-clocks/api-advanced-usage
- Stripe billing cycle: https://docs.stripe.com/billing/subscriptions/billing-cycle
- Stripe webhooks: https://docs.stripe.com/webhooks
- Stripe idempotent requests: https://docs.stripe.com/api/idempotent_requests
- stripe-mock: https://github.com/stripe/stripe-mock
- Pact docs: https://docs.pact.io/
- Testcontainers: https://testcontainers.com/

**E2E, a11y, flaky**
- Playwright retries: https://playwright.dev/docs/test-retries ; annotations: https://playwright.dev/docs/test-annotations ; accessibility: https://playwright.dev/docs/accessibility-testing
- axe-core: https://github.com/dequelabs/axe-core
- Deque, automated testing coverage study: https://www.deque.com/automated-accessibility-testing-coverage/
- Google Testing Blog, *Flaky Tests at Google* (2016): https://testing.googleblog.com/2016/05/flaky-tests-at-google-and-how-we.html
- GitHub, *Reducing flaky builds by 18x*: https://github.blog/engineering/infrastructure/reducing-flaky-builds-by-18x/
- Google Testing Blog, *Code Coverage Best Practices* (2020): https://testing.googleblog.com/2020/08/code-coverage-best-practices.html

**Test data / GDPR**
- Faker usage: https://fakerjs.dev/guide/usage
- UK MoJ, using live data for testing: https://security-guidance.service.justice.gov.uk/using-live-data-for-testing-purposes
- University of Twente, guideline testing with personal data: https://www.utwente.nl/en/cyber-safety/cybersafety/legislation/guideline-testing-with-personal-data.pdf
- Burges Salmon on ICO draft anonymisation guidance (2026): https://www.burges-salmon.com/articles/102nzuw/ico-consults-on-draft-guidance-on-anonymisation-and-pseudonymisation-for-research

**Security**
- Veracode 2025 GenAI Code Security Report press release (vendor): https://www.veracode.com/press-release/ai-generated-code-poses-major-security-risks-in-nearly-half-of-all-development-tasks-veracode-research-reveals/
- GitHub security features by plan: https://docs.github.com/en/code-security/getting-started/github-security-features
- Semgrep FAQ: https://semgrep.dev/docs/faq/overview ; CE vs Pro: https://semgrep.dev/docs/semgrep-pro-vs-oss
- OSV-Scanner: https://google.github.io/osv-scanner/
- ZAP baseline scan: https://www.zaproxy.org/docs/docker/baseline-scan/

**Payroll (DK)**
- Virk.dk, feriepenge for employers: https://www.virk.dk/vejledning/feriepenge/fp-arbejdsgiver/

**Internal (this repo)**
- `docs/research/agentic-workflows.md`, `agentic-project-planning.md`, `video-analysis-mkN4uRZYPfs.md`, `fitness-domain-operations.md`, `compliance-legal-security.md`, `integrations-and-architecture.md`

### Known gaps (not researched in this pass)
- Sandbox/simulation capabilities of Nexi, Frisbii, GoCardless/Betalingsservice and Vipps MobilePay Recurring.
- Mutation and PBT tools for Elixir, Ruby, .NET and Java beyond PIT (only named).
- Danish Datatilsynet guidance on test data; Danish VAT rounding rules; ferietillæg primary source.
- The original text of the Google flaky-tests and coverage posts (figures via secondary excerpts).
- Any controlled study of coverage gates or test-protection hooks *as agent guardrails*.

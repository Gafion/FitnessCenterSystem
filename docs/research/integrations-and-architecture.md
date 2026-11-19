# Integrations and Architecture Landscape — Research

> **Errata (2026-11-12).** An independent fact-check ([verification-report-wave1.md](verification-report-wave1.md)) found the corrections below. They take precedence over the text further down, which is left as originally written. Claim IDs (e.g. C-32, I-29) refer to the verification report.

## Errata

1. **§3.4 stack table, Python row:** Remove "AutoCodeBench Python lowest upper bound". Python (63.3) is above TS (61.3) (I-29).
2. **Exec summary §5 / row 11 / D12 (wearables):**
   - The Fitbit Web API **was turned off** on 2 Nov 2026 (Google Health docs).
   - The Google Health API onboarding notice was removed on 29 Oct 2026, so its status is unknown.
   - Garmin Connect Developer Program approvals have been **paused since spring 2026**.
   - Together these make "wearables in v1" substantially harder (I-22, I-23, I-24).
3. **Row 13 / §3.6 MitID:** Criipto is now **Idura**. Pricing is €0.037 per authentication plus €67–400 per month, and the setup fee is unstated (I-34).
4. **Exec summary §3 / §2.2:** Align the bookkeeping phase-in with the compliance file: personally owned businesses with turnover ≥ DKK 300k in each of the two prior years, from 1 Jan 2026 (I-15).
5. **Row 3 cost note:** State that the 0.99% + 1 kr price was for the now-closed Subscriptions product and says nothing about Recurring pricing (I-11).
6. **§3.3 PWA:** Cite Apple's 1 Mar 2024 reversal properly. The current S50 sources do not support it (I-36).
7. **Row 4 Paxton:** Add the Net2 v7.0 SDK discontinuation and MFA. Source the 1 Jul 2024 date or mark it UNVERIFIED (I-04).
8. **Row 2 BS Creditor API:** Mark OAuth 1.0a and sandbox as UNVERIFIED (I-08).
9. **Row 4 Kisi:** API access is on the Pro ($199 per location) and Elite tiers. Resolve the UNVERIFIED tag (I-37).
10. **§3.4 SWE-bench Multilingual:** Add the per-language numbers: Rust 58.14%, C/C++ 28.57% (I-30).
11. **§2.2 Frisbii:** Re-cite to docs.frisbii.com/docs/vipps-recurring (I-03).
12. **§2.2 Accounting and legal:** The ERST guidance date is 5 or 9 Oct 2026 (the page shows both). Scope is SAF-T plus e-invoicing (I-16).

---

**Status:** Research only. No decisions are made here. The document lists options, trade-offs and questions for the project owner.
**Date of research:** 2026-11-12
**Scope:** (1) The third-party integrations a fitness-center member/employee administration system typically needs, with a Danish focus. (2) Architecture and stack options for a solo developer working mainly through AI coding agents.

**Conventions**
- `[S#]` refers to the Sources list at the end.
- **UNVERIFIED** means the claim comes from a secondary or third-party source, or the sources disagree, and I could not confirm it against a primary or official source.
- **Anecdote** means practitioner opinion or a vendor/marketing claim, not measured evidence.
- Prices change often. Every price here is a snapshot to be re-checked before any decision.

---

## 1. Executive summary

1. **Recurring payments in Denmark are a three-way choice, and card-only is not "Danish-native."**
   - **Cards** via Stripe, Adyen, Nexi, Mollie or Frisbii (formerly Reepay).
   - **Betalingsservice** (the Danish direct-debit scheme), either directly through Mastercard Payment Services or through GoCardless.
   - **Vipps MobilePay Recurring.**

   The legacy **MobilePay Subscriptions API is closed**. Vipps MobilePay's changelog says it no longer accepts traffic, and the Recurring API is the replacement [S1][S2]. **Stripe's MobilePay does not support recurring payments** ("Recurring payments: ✗ Not supported"; Billing ✗) [S5]. Using MobilePay for memberships therefore means the Vipps MobilePay Recurring API, either directly or through a PSP that supports it (Frisbii advertises "Vipps MobilePay Recurring" [S10]).
2. **Access control is the hardest integration to keep vendor-neutral.** Cloud platforms such as Salto KS [S16] and Kisi [S19] have documented APIs, but you get credentials from a local Salto business unit or through commercial tiers. Paxton Net2 API access has been limited to commercial integrators since 1 July 2024 [S15]. For wiring, **OSDP** (IEC 60839-11-5, with AES-128 Secure Channel) is the more secure, standardised successor to **Wiegand**, which sends data unencrypted [S14].
3. **Danish accounting systems all have APIs.** e-conomic uses app secret plus grant tokens [S21], Dinero uses OAuth 2.0 through Visma Connect [S22], and Billy has a REST API [S23]. **Since 1 Jan 2026, Danish bookkeeping law requires digital bookkeeping** for more business types, including sole proprietorships [S24]. That affects what the system must hand off to the accounting system.
4. **The aggregators (Wellhub/Gympass, Urban Sports Club, ClassPass) all have partner APIs, but access is gated.** ClassPass and Urban Sports Club generally integrate through booking-software vendors, not individual gyms [S29][S28]. Wellhub publishes Booking, Access Control and Setup APIs [S27]. **Wellhub acquired Urban Sports Club** [S30]. USC operates in Copenhagen [S31].
5. **Wearables are in flux.** The **legacy Fitbit Web API is being turned off.** Sources give turn-off dates of 30 Oct or 2 Nov 2026, and Google was reportedly "not onboarding new projects" to the replacement Google Health API as of Oct 2026 [S36][S37]. Garmin's Health API is free but needs approval [S38].
6. **Architecture evidence points to a modular monolith for a solo developer.** Fowler: "almost all the successful microservice stories have started with a monolith" [S41]. On stacks for AI agents, the evidence is mixed:
   - Typed languages give agents a verification signal. In TypeScript, 94% of LLM compilation errors were type-check failures [S46], and Anthropic's own guidance says to give the agent typecheck and test commands to run [S48].
   - TypeScript is now the #1 language on GitHub [S43].
   - Benchmarks measure languages on different problem sets. AutoCodeBench ranks Elixir and C# above TypeScript and Python, but its per-language problems are not identical [S45]. Nothing reliably ranks "which stack agents handle best" for a full web application.

---

## 2. Integration catalog

### 2.1 Summary table

| # | Category | Purpose in a fitness system | Vendors / options | API availability | Region fit (DK) | Cost notes (snapshot, verify) |
|---|---|---|---|---|---|---|
| 1 | Recurring card billing | Monthly membership charges, dunning, proration | **Stripe Billing** [S3][S4]; **Adyen** [S8]; **Mollie** [S7]; **Nexi Checkout (Nets Easy)** [S9]; **Frisbii (ex-Reepay/Billwerk+)** [S10] | Stripe: public REST + webhooks, Billing product. Nexi: unscheduled card-on-file subscriptions and bulk charge [S9]. Frisbii: subscription platform [S10] | All operate in DK. Frisbii and Nexi have Nordic roots | Stripe DK: standard EEA cards 1.5% + 1.80 kr; premium EEA 2.8% + 1.80 kr; Billing 0.7% of billing volume (pay-as-you-go) [S3]. Adyen: interchange++ plus fixed fee; **minimum monthly invoice** reported, amount not public (**UNVERIFIED**) [S8]. Mollie, Nexi, Frisbii: not verified |
| 2 | Direct debit (DK) | Low-cost monthly pull from the member's bank account | **Betalingsservice** directly (Mastercard Payment Services) [S11][S12]; **GoCardless** [S13] | BS Creditor API: create/search **mandates** only (REST, OAuth 1.0a, sandbox) [S11][S12]. Collections use the BS file format (**UNVERIFIED** whether any API exists for collections). GoCardless: full REST API, can supply the PBS number [S13] | DK-only scheme; DKK; needs a Danish bank account [S13] | GoCardless DK domestic fee **UNVERIFIED** (one table shows 1% + €0.20, cap €2) [S13b]. Betalingsservice fees: not found |
| 3 | MobilePay recurring | Members pay subscriptions through the MobilePay app | **Vipps MobilePay Recurring API** directly [S2]; via a PSP, e.g. Frisbii [S10]; PSP Recurring API exists [S1] | Recurring API (agreements + charges). **Legacy MobilePay Subscriptions API closed** [S1]. Stripe MobilePay is single-use only [S5] | DK, FI (MobilePay), NO (Vipps) | Historic: 0.99% + 1 kr per subscription transaction (Feb 2024, Dansk Erhverv) — **UNVERIFIED current** [S6] |
| 4 | Door / turnstile access | Let valid members in; block lapsed members; log visits | Cloud: **Salto KS** [S16], **Kisi** [S19], Gantner (G7 Connect) [S19b]; On-prem: **Paxton Net2** [S15]; Aggregator API: **Seam** [S20]; DIY: QR reader → controller over Wiegand/OSDP [S14] | Salto KS Connect API: OpenAPI, OIDC auth, users, access groups, time schedules, offline keys; credentials from the local Salto business unit [S16][S17]. Paxton Net2: Web API, commercial integrators only since 1 Jul 2024, approval + licence [S15]. Kisi: "open API"; API tier is plan-dependent (**UNVERIFIED** which tier) [S19] | All sold in EU; regional dealers | Hardware + per-door SaaS; not collected. Paxton: lifetime API licence after approval [S15] |
| 5 | Accounting | Post revenue, VAT, fees, payouts; reconcile | **e-conomic** (Visma) [S21]; **Dinero** (Visma) [S22]; **Billy** [S23]; Xero [S25]; QuickBooks Online [S26] | e-conomic REST: app secret token + per-customer grant token [S21]. Dinero: OAuth 2.0 via Visma Connect; integration approval by Dinero [S22]. Billy: JSON REST API v2 [S23]. Xero: tiered developer pricing since Mar 2026 [S25]. QBO: Intuit App Partner Program, metered reads [S26] | e-conomic, Dinero, Billy are DK-native. Xero/QBO have weaker DK presence (**UNVERIFIED**) | e-conomic API free to use per a third-party guide (**UNVERIFIED**) [S21b]. Xero Starter free: 5 connections, 1,000 calls/day/org [S25]. QBO tiers $0–$4,500/month for reads (**UNVERIFIED**) [S26] |
| 6 | Payroll | Send hours/salary data for staff and instructors | **Danløn** [S33], DataLøn, Salary.dk [S33] | No official public Danløn API docs found. Existing integrations are partner API (Intempus) or file import (TimeLog) [S33] | DK-native | Not found |
| 7 | Shift scheduling / HR | Instructor and staff rosters, punch clock | **Planday** [S32] | Open API (REST, OAuth2): HR, shifts, punch clock, timesheets [S32] | DK-origin, EU | Not found |
| 8 | Email (transactional) | Receipts, booking confirmations, dunning | Amazon SES, Postmark, Resend, EU-hosted alternatives [S34] | All have REST/SMTP | EU region availability differs and sources conflict (**UNVERIFIED**) [S34] | SES ~$0.10/1,000; Postmark from $15/month for 10k; Resend free 3k/month — all **UNVERIFIED** (secondary sources) [S34] |
| 9 | SMS | Reminders, 2FA, no-show notices | **GatewayAPI** (DK) [S35], Twilio, others | REST | DK operator pricing changed in 2025 for international numeric sender IDs [S35] | GatewayAPI DK ≈ €0.033/SMS — **UNVERIFIED** (third-party) [S35] |
| 10 | Calendar | Members add booked classes to their calendar; staff schedules | ICS feed (subscribe link); Google Calendar API; Microsoft Graph [S39] | ICS is pull-based; Google refresh is 12–48 h by anecdote (**UNVERIFIED**) [S39]. Graph: events/calendarView endpoints [S39] | Global | Free |
| 11 | Wearables / fitness data | Import workouts and activity for engagement features | Apple HealthKit (on-device), Android Health Connect (on-device), **Garmin Health API** [S38], **Google Health API** (Fitbit successor) [S36][S37], aggregators (Terra, Thryve etc.) | Garmin: free for approved businesses; approval not guaranteed [S38]. Fitbit legacy API turned off Oct/Nov 2026; Google Health API scopes "Restricted" with review; onboarding reportedly paused [S36][S37] | Global | HealthKit's on-device-only model is **UNVERIFIED** in this research |
| 12 | Aggregator networks | Accept Wellhub / USC / ClassPass members for check-ins and bookings | **Wellhub (Gympass)** [S27]; **Urban Sports Club** [S28]; **ClassPass** [S29] | Wellhub: Booking API, Access Control (check-in validate) API, Integration Setup API (webhooks); token issued by Wellhub [S27]. USC: partner API through approved booking software; check-in via QR [S28]. ClassPass: Inventory API for **scheduling-software vendors**; approved-partner login required to view the spec [S29] | USC in Copenhagen [S31]; ClassPass lists Copenhagen studios [S31b]; Wellhub DK availability **UNVERIFIED**; Wellhub owns USC [S30] | Commission/payout models not public |
| 13 | Identity (DK) | Optional strong ID (e.g. age, contract signing) | **MitID** via brokers: Criipto, Signicat [S40] | OIDC broker APIs | DK | Criipto: DKK 1,200 setup + DKK 0.28 per MitID authentication + subscription [S40] |

### 2.2 Notes per category

**Recurring payments (key points)**
- **Betalingsservice** is bank-to-bank. Nets is the clearing house, and Mastercard Payment Services Denmark A/S is the operator that signs creditor agreements [S11][S12][S13]. The creditor's REST API covers **mandate creation and search only** [S11b].
  - GoCardless timing: the payment must be created 8 working days before the start of the next month, and the merchant receives it 1 business day after collection [S13].
  - Suits fixed monthly memberships. It handles one-off purchases less naturally (anecdote, inferred from the monthly cycle).
- **Stripe** documents MobilePay as "single-use", with no Billing, no Setup Intents and no off-session payments [S5]. Stripe Billing handles recurring cards and SEPA Direct Debit (2.60 kr per SEPA transaction in DK) [S3]. Stripe does not process Dankort. Co-branded Visa/Dankort cards go through Visa [S5].
- **Nexi Checkout:** unscheduled card-on-file agreements, 0-amount card verification, and a bulk-charge endpoint, which fits batch billing runs [S9].
- **Adyen:** interchange++ pricing. Several reviews report a **minimum monthly invoice** that may make it uneconomic at low volume (**UNVERIFIED** amount) [S8].
- **Mollie:** SEPA Direct Debit is aimed at recurring payments. MobilePay through Mollie is a one-off method; no recurring MobilePay flow was found (**UNVERIFIED** absence) [S7].
- **Frisbii (ex-Reepay):** a subscription and billing platform that lists Vipps MobilePay Recurring as a payment method [S10]. Betalingsservice support through Frisbii is **UNVERIFIED**.

**Access control (key points)**
- **Wiegand:** plaintext, one-way, legacy. **OSDP:** two-wire RS-485, multi-drop, bidirectional, supervised. Its Secure Channel uses AES-128, but encryption is only active if that profile is enabled. Standardised as IEC 60839-11-5 (2020). The SIA "OSDP Verified" programme lists conforming devices [S14].
- Typical integration patterns:
  - **(a) Cloud API sync.** Our system pushes member validity to the vendor cloud (Salto KS, Kisi), and the vendor handles locks and readers, including offline locks (Salto has LockOfflineKeys endpoints [S17]).
  - **(b) Local controller.** QR/RFID reader → door controller → our software decides. More control, and we own the offline behaviour.
  - **(c) Aggregation API.** Seam offers a single API across lock brands, including Salto KS [S20].
- Aggregator check-ins (Wellhub/USC) depend on the access path. Wellhub requires validating the check-in through its API within an expiry window [S27].

**Accounting and legal**
- The digital-bookkeeping duty widened on 1 Jan 2026 [S24]. A business can use a registered system or document an unregistered one [S24]. Erhvervsstyrelsen issued new guidance for providers of standard bookkeeping systems on 5 Oct 2026, with an e-invoicing focus [S24].
- Open question: is the fitness system only a sub-ledger that feeds e-conomic/Dinero/Billy, or does it itself store bookkeeping records? Owner input and possibly an accountant are needed.

---

## 3. Architecture option analyses

### 3.1 Modular monolith vs microservices

| Option | For | Against | Evidence |
|---|---|---|---|
| **Modular monolith** (one deployable, strict internal module boundaries: members, billing, access, scheduling, staff) | One repo, one deploy and one database, which keeps the whole system in an agent's context. Cheap to host. Refactoring across modules is a single change | Discipline is needed to keep module boundaries (lint rules / dependency checks). Scales vertically first | Fowler: the "microservice premium" slows teams, and successful microservice systems usually started as monoliths [S41][S42] |
| **Microservices** | Independent deploys and scaling; isolates failures (e.g. the door-access service survives a billing outage) | Distributed transactions (billing ↔ access), more infrastructure, more for agents to coordinate across repos | Same sources: a premium that "often gets projects into serious trouble" [S42] |
| **Hybrid**: monolith + 1–2 isolated edge components (e.g. an on-site check-in/door agent) | Isolates the part that must work offline | Two deployables, a sync protocol | Inferred from offline requirements (§3.6); no external source |

### 3.2 Multi-tenant vs single-tenant

| Option | For | Against |
|---|---|---|
| **Single-tenant** (one fitness center, one deployment) | Simplest data model, no isolation risk, matches "personal project" | Hard to resell later. Each new client means a new deployment |
| **Multi-tenant, shared DB with tenant_id** (optionally Postgres Row-Level Security) | One deployment serves many centers. Leaves a SaaS path open | Every query must be tenant-scoped, and a leak is a GDPR incident. More testing |
| **Multi-tenant, DB-per-tenant** | Strong isolation | Migrations across N databases. More operational work |

No external source was gathered on which approach agents get right more often. Treat any claim either way as **UNVERIFIED**.

### 3.3 Client: web + mobile

| Option | For | Against | Evidence |
|---|---|---|---|
| **Responsive web / PWA only** | One codebase; no app-store review | iOS: Web Push works only for Home-Screen web apps (iOS 16.4+), and permission must follow a user action. Apple's 2024 EU DMA removal was reversed, but some 2026 sources still say EU PWA push does not work (**conflicting**). Background processing and storage persistence are limited | [S50] |
| **React Native (Expo) / Flutter** | One codebase for iOS and Android. Native push, wallet, NFC, camera | Two runtimes to maintain (web + mobile). App-store accounts and review. Adyen ships RN and Flutter components [S8b] | Official docs; adoption figures not gathered |
| **Native (Swift/Kotlin)** | Best platform integration | Two extra codebases for a solo developer | — |
| **Wallet passes** (Apple Wallet / Google Wallet membership card with QR) | Membership QR without an app | Not researched in depth (**UNVERIFIED** capabilities) | — |

### 3.4 Stack evaluation for AI-agent friendliness

**What the evidence actually shows**
- **Training-data volume affects quality.** MultiPL-E found code-generation performance "correlated with language popularity", although some niche languages do better than popularity predicts [S44]. Follow-up work shows LLMs "struggle with low-resource languages" [S44b].
- **Type checking catches most agent compile errors.** In the TypeScript study, 94% of LLM compilation errors came from failed type checks, and type-constrained decoding cut compile errors by more than half [S46]. This supports typed stacks *if the agent runs the type checker*.
- **Verification loops matter.** Anthropic's Claude Code guidance recommends putting exact test, typecheck and lint commands in CLAUDE.md, and optionally a Stop hook that blocks the agent from finishing until the typecheck passes [S48].
- **Popularity trend:** TypeScript overtook Python and JavaScript in Aug 2025 as the most-used language on GitHub by monthly contributors. GitHub attributes this partly to AI-assisted development [S43]. That is a correlation, not proof of agent performance.
- **Cross-language benchmarks conflict.**
  - AutoCodeBench (20 languages) reports Elixir with the highest upper bound (~97.5%), C# ~88%, and TypeScript and Python ~61–63%. Each language has a **different problem set** (and some problems were translated), so these numbers are not a clean cross-language comparison [S45].
  - SWE-bench Multilingual (9 languages, real repository issues) reported Rust highest and C/C++ lowest. No per-language numbers were extracted [S47].
  - **No benchmark found compares full-stack web frameworks (Next.js vs Django vs Rails etc.) for agent success.** Any such ranking is anecdote.
- **Caution on productivity claims.** METR's RCT (early 2025 tools, experienced developers on large mature repositories) found developers were 19% *slower* with AI while believing they were faster [S49]. This does not transfer directly to a greenfield project, but it argues for measuring, not assuming.

**Stack comparison.** The "evidence" column distinguishes sourced points from anecdote.

| Stack | Type safety | Test tooling (official/common) | Agent-relevant evidence | Notable trade-offs |
|---|---|---|---|---|
| **TypeScript: Next.js (full-stack) or NestJS (API) + React** | Static (tsc) | Vitest/Jest, Playwright | #1 on GitHub [S43]; 94% of compile errors are type errors, so tsc is a strong feedback loop [S46]; AutoCodeBench TS ~61% upper bound [S45] | One language across web, mobile (React Native) and backend. Fast-moving ecosystem with breaking framework versions (anecdote) |
| **Python: Django** | Gradual (mypy/pyright, optional) | pytest, Django test client | High training-data volume (MultiPL-E popularity) [S44]; AutoCodeBench Python lowest upper bound (~63%) [S45] — caveat above | Batteries included (admin, ORM, auth). Types are optional, so a weaker automatic check |
| **C# / .NET (ASP.NET Core)** | Static, strong | xUnit/NUnit, built-in integration testing | AutoCodeBench C# ~88% upper bound [S45]; large corpus (in Octoverse top-6 languages for new repos) [S43] | Mature, fast. Mobile via .NET MAUI or a separate RN app |
| **Elixir / Phoenix (LiveView)** | Dynamic (gradual set-theoretic types in progress — **UNVERIFIED** status) | ExUnit | AutoCodeBench top upper bound (~97.5%) [S45], but MultiPL-E suggests smaller corpora usually hurt [S44] — **conflicting** | Excellent concurrency; Oban for jobs. Smaller hiring and library pool (anecdote) |
| **Ruby on Rails** | Dynamic (Sorbet/RBS optional) | Minitest/RSpec | No strong per-language agent evidence found | Highly conventional, which agents may follow well (anecdote) |
| **PHP / Laravel** | Gradual (PHP types + PHPStan) | PHPUnit/Pest | No strong agent evidence found | Large hosting ecosystem; Laravel Cashier wraps Stripe/Paddle (not verified here) |

### 3.5 Hosting

| Option | Notes | Cost evidence |
|---|---|---|
| EU VPS (Hetzner, others) + Docker / self-managed Postgres | Cheapest raw compute; you run ops/backups | Hetzner raised cloud prices twice in 2026 (Apr +30–37%; Jun CPX +144–175%). CX22 → CX23 at €5.49/month, currently not orderable — **UNVERIFIED** (secondary sources) [S51] |
| PaaS (Render, Railway, Fly.io, Vercel + managed DB) | Less ops; good agent ergonomics (config as code) | Not collected — **UNVERIFIED** |
| Hyperscaler (Azure/AWS/GCP EU regions) | Managed everything; compliance tooling | Not collected |
| On-premise box at the gym (for door/check-in edge) | Survives internet outage | Hardware cost only |

GDPR / data residency: an EU region is likely needed for member data. A DPA is needed with every processor (payment, email, SMS, auth). Not researched in detail.

### 3.6 Auth providers

| Option | Free tier (secondary sources, **UNVERIFIED** against vendor pages) [S52] | Notes |
|---|---|---|
| Clerk | 50,000 monthly *retained* users (since Feb 2026) | Hosted UI; strong Next.js integration |
| Auth0 | 25,000 MAU | Mature, enterprise |
| Supabase Auth | 50,000 MAU | Tied to the Supabase Postgres platform |
| Framework built-in (Django auth, ASP.NET Identity, Rails/Devise, Laravel, Phoenix `mix phx.gen.auth`) | Free | You own the security surface |
| Self-hosted IdP (e.g. Keycloak) | Free | Ops burden |
| MitID broker (Criipto/Signicat) as an extra factor | Per authentication [S40] | Only if strong Danish ID is required |

### 3.7 Background jobs / billing runs

Monthly billing runs, dunning retries, webhook processing and notification sends all need a durable job queue with retries and idempotency.
- **Provider-side scheduling:** Stripe Billing runs the subscription cycle itself, so our system only reacts to webhooks [S3]. Nexi offers bulk charge of unscheduled subscriptions, so our system schedules the run [S9]. Betalingsservice has fixed monthly deadlines (8 working days before the month for GoCardless) [S13].
- **Per-stack queue options** (well known; not individually verified here): BullMQ/pg-boss (Node), Celery/Django-Q/Procrastinate (Python), Hangfire/Quartz.NET (.NET), Oban (Elixir), Sidekiq/Solid Queue (Rails), Laravel Queues/Horizon (PHP). Temporal or similar workflow engines are an option for multi-step dunning.
- Decision driver: whether **we** own the billing schedule (Nexi, Betalingsservice, MobilePay Recurring charges) or the **PSP** does (Stripe Billing, Frisbii).

### 3.8 Offline tolerance for check-in

- **Failure scenario:** the internet is down at the gym. Should members still get in?
- **Options:**
  - (a) Vendor offline locks. Salto KS exposes offline-key management [S17].
  - (b) A local edge agent caching the valid-member list with a TTL, with check-ins synced later.
  - (c) PWA with a service worker at reception. iOS background limits apply [S50].
  - (d) Fail-open or fail-closed policy at the controller.
- Aggregator check-ins (Wellhub) need an online validate call within an expiry window [S27], so they cannot be fully offline.

---

## 4. Decision points requiring owner input

| ID | Decision | Options | Main trade-off |
|---|---|---|---|
| D1 | Payment methods offered to members | Card / Betalingsservice / MobilePay Recurring / combination | Member preference and cost vs integration count |
| D2 | Who owns the billing schedule | PSP-managed (Stripe Billing, Frisbii) vs self-managed (Nexi, BS, MobilePay charges) | Less code and a fee (e.g. Stripe 0.7%) vs more code and control |
| D3 | Access-control hardware | Existing hardware at the center? Salto / Paxton / Kisi / DIY OSDP | Vendor lock-in and API access terms vs build effort |
| D4 | Offline policy for doors | Fail-open / fail-closed / cached list | Security vs member experience |
| D5 | Accounting system | e-conomic / Dinero / Billy / other | Whatever the center's accountant uses |
| D6 | Tenancy | Single-center vs multi-tenant SaaS | Simplicity now vs resale later |
| D7 | Mobile approach | PWA / RN-Flutter / native / wallet pass only | Reach and features vs maintenance |
| D8 | Primary stack | TS / Python / C# / Elixir / Rails / Laravel | Evidence is mixed (§3.4); owner familiarity for reviewing agent output may dominate |
| D9 | Hosting and data residency | EU VPS / PaaS / hyperscaler | Cost vs ops burden |
| D10 | Auth | Managed provider vs built-in; MitID needed? | Cost and lock-in vs security surface |
| D11 | Aggregator participation | Wellhub / USC / ClassPass: which, if any | Partner approval gates; ClassPass/USC favour software vendors |
| D12 | Wearables | In scope for v1 or not | Fitbit/Google Health access currently constrained |

---

## 5. Open questions for the project owner

1. Is this system for **one specific fitness center** (which?), or should it be able to serve **several centers** (multi-tenant / future product)?
2. Which **payment methods** must members be able to use at launch: cards, Betalingsservice, MobilePay, or all three? Does the center already have a PSP or a Betalingsservice creditor agreement (PBS number)?
3. Does the center already have **door / turnstile hardware**? If yes, what make and model (Salto, Paxton, Gantner, other), and is it cloud-connected?
4. If the internet goes down, should the doors **fail open, fail closed, or use a cached member list**?
5. Which **accounting system** does the center (or its accountant) use today? Is the fitness system expected to be the place where bookkeeping records are stored, or only to feed the accounting system?
6. How are staff and instructors **paid and scheduled** today (Danløn, Planday, spreadsheets)? Is payroll export in scope?
7. Does the center accept or plan to accept **Wellhub, Urban Sports Club or ClassPass** members?
8. Is a **member mobile app** required, or would a web app / wallet pass be enough? Are push notifications a must?
9. Are **wearable/fitness-data features** in scope for the first version?
10. Do you have an **existing language/framework you can comfortably review**? Reviewing agent output is your quality gate, so your familiarity may outweigh benchmark differences.
11. What **monthly running-cost budget** (hosting, auth, email/SMS, payment fees) is acceptable?
12. Is **MitID** (strong Danish ID) needed, e.g. for signing membership contracts or age checks?
13. Any hard requirement on **EU-only data residency** for all processors?

---

## 6. Sources

Payments
- [S1] Vipps MobilePay developer changelog (legacy MobilePay Subscriptions API closed; Recurring API replacement) — https://developer.vippsmobilepay.com/docs/changelog.md ; PSP Recurring API changelog — https://developer.vippsmobilepay.com/docs/APIs/psp-recurring-api/CHANGELOG.md ; integrator note — https://www.onlinefundraising.dk/developer/?p=183
- [S2] Vipps MobilePay Recurring API — https://developer.vippsmobilepay.com/docs/APIs/recurring-api/
- [S3] Stripe pricing, Denmark — https://stripe.com/en-dk/pricing
- [S4] Stripe Billing pricing context (0.5%→0.7% change, Jul 2024; secondary) — https://help.kajabi.com/articles/sales/kajabi-payments/understanding-stripes-updated-billing-plans-for-kajabi-heroes
- [S5] Stripe docs, MobilePay payments — https://docs.stripe.com/payments/mobilepay
- [S6] Dansk Erhverv, MobilePay new business prices (Feb 2024) — https://www.danskerhverv.dk/presse-og-nyheder/nyheder/2024/februar/mobilepay-har-varslet-nye-priser-til-erhvervskunder/ ; Coachway guide (secondary) — https://coachway.io/da/tage-betaling-for-online-coaching/
- [S7] Mollie Direct Debit — https://www.mollie.com/payments/direct-debit ; Mollie MobilePay — https://www.mollie.com/payments/mobilepay
- [S8] Adyen pricing — https://www.adyen.com/pricing ; reviews: https://www.nerdwallet.com/business/software/learn/adyen , https://fitsmallbusiness.com/adyen-payments-review/ ; [S8b] Adyen MobilePay docs (integration options) — https://docs.adyen.com/payment-methods/mobilepay
- [S9] Nexi Checkout, unscheduled subscriptions — https://developer.nexigroup.com/nexi-checkout/en-EU/docs/unscheduled-subscriptions-ucof ; change log — https://developer.nexigroup.com/nexi-checkout/en-EU/change-log/
- [S10] Frisbii (Reepay rebrand) — https://frisbii.com/reepay-becomes-frisbii ; Adobe Commerce listing (payment methods) — https://commercemarketplace.adobe.com/reepay-payment-billwerk-plus-subscription.html
- [S11] Mastercard BS Creditor API, documentation — https://developer.mastercard.com/bs-creditor-api/documentation/ ; [S11b] getting started — https://developer.mastercard.com/bs-creditor-api/documentation/getting-started/
- [S12] Betalingsservice creditor rules — https://www.betalingsservice.dk/media/sffdc5pd/bs_regler_kr_en.pdf
- [S13] GoCardless Betalingsservice guide — https://gocardless.com/guides/betalingsservice/introduction ; timings — https://support.gocardless.com/hc/en-us/articles/360001152125-Betalingsservice-Denmark-Payment-Timings ; access — https://gocardless.com/guides/betalingsservice/getting-access-to-betalingsservice ; [S13b] pricing article — https://support.gocardless.com/hc/articles/360038646634 ; FX pricing — https://gocardless.com/faq/merchants/fx-pricing

Access control
- [S14] OSDP vs Wiegand: Axis whitepaper — https://whitepapers.axis.com/en-us/osdp-protocol-in-access-control ; Honeywell "What is OSDP" — https://www.honeywell.com/us/en/insights/articles/what-is-osdp ; Facilities Management Advisor — https://facilitiesmanagementadvisor.com/access-control/access-control-benefits-sia-osdp-vs-legacy-card-systems/
- [S15] Paxton, integrating with Net2 — https://www.paxton-access.com/integrating-with-paxton/how-to-integrate-with-net2/ ; https://paxton-access.com/ae/integrating-with-paxton/
- [S16] Salto KS Connect API — https://developer.saltosystems.com/ks/connect-api/
- [S17] Salto KS API reference — https://developer.saltosystems.com/ks/connect-api/reference/
- [S19] Kisi fitness — https://getkisi.com/industry/fitness ; Kisi pricing (fitness) — https://getkisi.com/pricing/fitness ; GymPoint docs — https://docs.kisi.io/marketplace/fitness/gympoint ; [S19b] Kisi vs Gantner (vendor comparison) — https://www.getkisi.com/access-control-systems-comparison/kisi-vs-gantner
- [S20] Seam, Salto KS guide — https://docs.seam.co/latest/device-guides/get-started-with-salto-locks

Accounting, legal, HR
- [S21] e-conomic, connecting with tokens — https://www.e-conomic.com/developer/connect ; [S21b] Apideck guide (secondary) — https://www.apideck.com/blog/how-to-integrate-with-the-e-conomic-api
- [S22] Visma developer, Dinero API — https://developer.visma.com/api/dinero ; Dinero API page — https://dinero.dk/funktioner/api/
- [S23] Billy API v2 — https://www.billy.dk/api/
- [S24] Erhvervsstyrelsen, unregistered digital bookkeeping systems — https://erhvervsstyrelsen.dk/ikke-registrerede-digitale-bogfoeringssystemer ; new guidance for providers — https://erhvervsstyrelsen.dk/ny-samlet-vejledning-til-udbydere-af-digitale-standard-bogfoeringssystemer ; PwC — https://www.pwc.dk/da/artikler/2025/04/pligten-til-digital-bogfoering-efter-bogfoeringsloven-gennemfoeres-med-virkning-fra-den-1-januar-2026.html
- [S25] Xero developer pricing — https://developer.xero.com/pricing ; Accounting Today — https://www.accountingtoday.com/news/xero-shifts-to-tiered-pricing-model-for-developers
- [S26] QuickBooks API pricing (secondary) — https://radar.apideck.com/blog/quickbooks-api-pricing-and-the-intuit-app-partner-program.md ; https://truto.one/blog/how-much-does-the-quickbooks-api-cost-2026-pricing-rate-limits/
- [S32] Planday developer — https://developer.planday.com/ ; release notes — https://openapi.planday.com/gettingstarted/release_notes
- [S33] Danløn integrations: Intempus — https://intercom.help/intempushelp/da/articles/2140305-opsaetning-af-danlon-integration ; TimeLog — https://help.timelog.com/en/integrations-api/danloen ; Salary.dk — https://help.salary.dk/da/collections/1840091-integrationer

Aggregators
- [S27] Wellhub/Gympass developers: Booking API — https://developers.gympass.com/product/booking-api/1.0/getting-started ; Access Control API — https://developers.gympass.com/product/access-control-api/1.0/getting-started ; Integration Setup API — https://developers.gympass.com/product/integration-setup-api/1.0/getting-started ; partner guide — https://digital.gympass.com/partnerbase/uk/how-check-ins-work
- [S28] Urban Sports Club integrations — https://partners.urbansportsclub.com/en/integrations ; Eversports activation — https://helpcenter.eversportsmanager.com/activate-the-urban-sports-club-api
- [S29] ClassPass booking integrations — https://classpass.com/partners/classpass-booking-integrations ; developer spec — https://developers.classpass.com/spec ; Supergood API report (secondary) — https://supergood.ai/api-report-card/classpass
- [S30] Wellhub acquires Urban Sports Club — https://www.healthclubmanagement.co.uk/health-club-management-news/Wellhub-broadens-reach-in-Europe-with-acquisition-of-Urban-Sports-Club/355395 ; USC newsroom — https://partnernewsroom.urbansportsclub.com/en/wellhub-and-urban-sports-club-join-forces-2/
- [S31] USC Denmark — https://blog.urbansportsclub.com/en/discover-the-sporty-side-of-denmark ; [S31b] ClassPass Copenhagen list — https://classpass.com/partners/lists/copenhagen-welcoming-community/

Communications, calendar, wearables, identity
- [S34] Transactional email comparisons (secondary) — https://makerkit.dev/blog/saas/best-transactional-email-provider-saas ; https://www.sequenzy.com/versus/postmark-vs-amazon-ses
- [S35] GatewayAPI pricing — https://gatewayapi.com/pricing/ ; Telenor DK tiered pricing — https://gatewayapi.com/blog/telenor-introduces-tiered-sms-pricing-in-norway-and-denmark-what-does-this-mean-for-you/ ; third-party rate — https://frontdeskreview.com/software/sms-gateways/gatewayapi/
- [S36] Google Health API, about — https://developers.google.com/health/about ; Fitbit dev blog — https://dev.fitbit.com/blog/2025-09-26-important-updates-to-the-fitbit-web-apis/
- [S37] Sahha, Fitbit sunset migration (Oct 2026) — https://sahha.ai/blog/fitbit-api-sunset-migration/ ; Validic status — https://status.validic.com/incidents/h7rq3qfqrgk4
- [S38] Garmin Health API — https://developer.garmin.com/gc-developer-program/health-api ; forum report — https://forums.garmin.com/developer/connect-iq/f/discussion/434542/garmin-connect-developer-program---access-request-rejected-without-notification
- [S39] Microsoft Graph calendar — https://learn.microsoft.com/en-us/graph/api/resources/calendar-overview ; ICS refresh anecdotes — https://til.simonwillison.net/ics/google-calendar-ics-subscribe-link , https://help.cheqroom.com/en/articles/1000547-how-often-does-google-update-its-calendar-feeds
- [S40] Criipto MitID — https://criipto.com/mitid ; Criipto pricing — https://criipto.com/pricing ; Signicat MitID broker — https://www.signicat.com/products/mitid-broker

Architecture and AI agents
- [S41] Fowler, MonolithFirst — https://www.martinfowler.com/bliki/MonolithFirst.html
- [S42] Fowler, MicroservicePremium — https://martinfowler.com/bliki/MicroservicePremium.html
- [S43] GitHub Octoverse 2025 — https://github.blog/news-insights/octoverse/octoverse-a-new-developer-joins-github-every-second-as-ai-leads-typescript-to-1/
- [S44] Cassano et al., MultiPL-E — https://arxiv.org/abs/2208.08227 ; [S44b] MultiPL-T, knowledge transfer to low-resource languages — https://arxiv.org/abs/2308.09895
- [S45] AutoCodeBench — https://arxiv.org/abs/2508.09101 ; secondary explainer — https://codemyspec.com/blog/autocodebench-explained
- [S46] Mündler et al., Type-Constrained Code Generation with Language Models — https://arxiv.org/abs/2504.09246
- [S47] SWE-bench Multilingual — https://swebench.com/multilingual ; Multi-SWE-bench — https://arxiv.org/abs/2504.02605
- [S48] Claude Code best practices — https://code.claude.com/docs/en/best-practices
- [S49] METR RCT coverage — https://www.theregister.com/2025/07/11/ai_code_tools_slow_down/ ; https://www.infoworld.com/article/4020931/ai-coding-tools-can-slow-down-seasoned-developers-by-19.html
- [S50] iOS PWA: 9to5Mac (EU 17.4) — https://9to5mac.com/2024/02/15/ios-17-4-web-apps-european-union/ ; Open Web Advocacy — https://open-web-advocacy.org/blog/its-official-apple-kills-web-apps-in-the-eu/ ; MagicBell guide — https://www.magicbell.com/blog/pwa-ios-limitations-safari-support-complete-guide
- [S51] Hetzner 2026 price changes (secondary) — https://www.tomshardware.com/tech-industry/hetzner-to-raise-prices-by-up-to-37-percent-from-april-1 ; https://wz-it.com/en/blog/hetzner-price-increase-june-2026-cpx-ccx-alternatives/ ; https://www.bitdoze.com/hetzner-cloud-cost-optimized-plans/
- [S52] Clerk pricing explained — https://clerk.com/articles/clerk-pricing-explained ; auth comparison (secondary) — https://devtoolpicks.com/blog/clerk-vs-auth0-vs-supabase-auth-indie-hackers-2026

### Known gaps (not researched or not verifiable in this pass)
- Official current prices for: Vipps MobilePay Recurring (DK), Betalingsservice creditor fees, GoCardless DK domestic, Mollie and Nexi DK, Adyen minimum invoice.
- Whether any API exists for Betalingsservice **payment collection** (vs file transfer).
- Whether Wellhub operates in Denmark today.
- Official Danløn API documentation.
- Apple HealthKit / Android Health Connect server access model; Apple/Google Wallet pass capabilities.
- PaaS and hyperscaler hosting costs.

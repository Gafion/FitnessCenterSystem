# Open Questions for the Project Owner

Consolidated and de-duplicated from the wave-1 research files in [`research/`](research/). Each question lists the file(s) that raised it.
Answers will be recorded here and, where they settle a medium/large decision, in [`decisions.md`](decisions.md).

**Tier 1 questions block almost everything else — please answer these first.**

---

## Answered (2026-11-12)

| # | Answer | Recorded in |
|---|---|---|
| F1 | Real center that requested the system; also a learning project. | D-001 |
| F2 | Denmark. | D-001 |
| F3 | Mixed: open gym access, classes (spinning, CrossFit, possibly more), personal training by the center's instructors. | D-001 |
| F4 | Single site, ~10 staff, member count unknown. | D-001 |
| F6 | Unknown until the owner has talked to the center's staff/management. | D-004 |
| M8 / R5 | Wearables are not in the plan. | D-002 |
| A3 / R7 | No biometrics — entry by RFID access card swipe, which registers attendance for gym entry, classes or PT. | D-003 |
| W1 | About half of the owner's weekly Claude Pro token allowance. | D-005 (proposed) |
| T1 | Professional backend experience in C#. Frontend from school: TypeScript + Vue (Linear clone), JavaScript + React (kursusoversigt). | — (input to the stack decision) |
| R4 | Retried yt-dlp: still HTTP 429 / PO-token blocked. Still open. | — |
| SA1 | Code modules with enforced boundaries in one deployable; no performance impact wanted. | D-008 |
| SA2 | No. A functioning end product is the goal; distributed systems are not important. | D-008 |
| SA5 | Yes: Aspire for local development from the start. | D-009 |
| W16 / Q-scope | Unit, integration, E2E and every other automatable test type, all run in CI. Framework undecided. | D-010 |
| S6 | Social login (Google, Facebook, GitHub, other common providers) plus local email/password. Implementation pending research. | D-011 |
| DB2 | Allowed: one transaction may span modules. | D-015 |
| DB3 | Effectively not in v1 (shared transactions need one connection). | D-015 |
| S11 | Tests: MSTest for C# (D-029), ArchUnitNET for architecture tests (D-030), Vitest + Playwright for the frontend (D-018). | D-029, D-030 |
| AU5 | Own login: ASP.NET Core Identity, data in our PostgreSQL, Google as external provider. EU residency of an external store no longer applies. | D-026 |
| B3 | No amounts owed in the core. Payments are 6 subscription types plus one-time payments for PT appointments; the payment service sends payment confirmations to the core. | D-019 |
| SA6 / DB1 | Schema per module, foreign keys allowed between modules (data-model.md Option 2). | D-014 |
| SA4 / S8 | Was expected from the center meeting; now the owner decides (see "Owner decides" below). | D-047 |
| AU1 | Yes, a native app. First delivery is for staff (checking appointments). | D-012 |
| AU2 | No Facebook login. | D-013 |
| AU8 | MFA mandatory for staff; optional for members. | D-013 |
| W11 | Linear (workspace/team FitnessCenterSystem) for all tasks; sub-agents are briefed via Linear tickets; decisions/requirements stay in docs/. | D-017 |
| R6 | Applied as errata sections at the top of the two fact-checked reports; original text kept. | — |
| W17 (orchestrator part) | The orchestrator only orchestrates; every task goes to a briefed sub-agent. The scaffolding half of W17 stays open. | D-040 |
| D-021 open point (FIT-55) | Simulated pages: any logged-in account, only when `Simulation:Enabled`, with a "Simulation" banner. | D-041 |
| Q6 (partly) | A few full-stack E2E flows run in CI (signup → confirm → login → book; check-in); mocked tests stay. Per-feature E2E as Definition of Done stays open (W15). | D-043 |
| F1 (revisited) | We build our own product for a single-site Danish center; questions that waited on the center go to the owner. | D-047 |
| F6 (revisited) | The MVP is six pages: landing, login/signup, classes, account, simulated payment, simulated check-in. PT comes after the MVP. | D-022, D-025 |
| D19 | Denmark, DKK and Europe/Copenhagen only. | D-001, D-047 |
| P1, P2, D3, Q8 | For now: no payment methods, provider or billing schedule. Payments are a later separate service, simulated in the MVP. They return when the Payments service is built. | D-006, D-021 |
| A1, S12 | For now: no door hardware. A simulated reader sends card UIDs to the real check-in logic. They return when real hardware is chosen. | D-021 |
| U4 (MVP part) | No waitlist in the MVP. The rule after the MVP is under "Owner decides". | D-022, D-039 |
| B2, S9 | No longer apply: there is no center whose existing tools or Microsoft 365 use we must match. | D-047 |
| M1, M2, D8, U2 (former "Owner decides" rows 1–2) | Monthly rolling membership, no binding; more types one at a time, "General membership" until then. One month's notice to month-end; members cancel online; access until the effective date; no "save" step. | D-048 |
| D13, E1 (former row 3) | Four roles: admin, manager, front desk, instructor. Instructors see only their own classes. | D-049 |
| AU9 (former row 4) | A short research ticket compares Amazon SES, Postmark and Resend; Mailpit stays for local dev. | Research ticket (FIT-52), D-038 |
| AU9 | Resend free plan over SMTP, no domain (non-commercial proof of concept). Without a domain it delivers only to the Resend account owner's address. EU data storage not required now; needed if it ever goes commercial. Mailpit stays the default locally and in CI. | D-066 |
| SA4, S8, T2, T3 (former row 5) | No paid hosting. During development the owner's laptop may run the system if hosting is needed. How it is reached from outside is under "Owner decides". | D-050 |
| Booking window and cancel deadline (former row 6) | Keep D-039: 14-day window, cancel until 12 hours before start, no penalties. | D-039 |
| U4, D10 (former row 7) | After the MVP: offer with expiry to the next person on the waitlist; no no-show penalties yet. Offer length is under "Owner decides". | D-051 |
| Waitlist offer length and notice (former "Owner decides" row 2) | An offer lasts 2 hours, never past the 12-hour cancel deadline; then the next person. The member is told by email plus a notice on the account page. | D-067 |
| D11 (former row 8) | Keep D-042 and D-046. Repeat reminders and late fees come only with real payments. | D-042, D-046 |
| M4 (former row 9) | Members must be 18 or older for now; date of birth collected at signup. | D-052 |
| M5 (former row 10) | Collect no health data. | D-053 |
| D8 (revocation part), D-048 open points | A member may undo a pending cancellation; cancelling sends a confirmation email; a member with a pending cancellation can't book classes after the effective date. Existing bookings after the effective date are still open (FIT-71). | D-055 |
| U5 (former "Owner decides" row 6) | Danish by default, English available; Danish formats (24-hour time, Monday-first weeks, `dd.mm.yyyy`, `kr.`). Backend texts and emails stay English for now. | D-065 |
| AU10 | Managers may invite, but only to Front desk and Instructor; admins give any role. | D-063 |
| AU11 | Yes: an invite to an email with an account adds the role to it after the person logs in and accepts; the membership stays. Staff MFA applies. | D-064 |
| AU12 | An admin resets a staff member's authenticator (MFA off, key and recovery codes removed; set up again at the next login), never their own. Members turn MFA off themselves with an app or recovery code. Both email the account. | D-071 |
| DB8 | Spike done as a verification (FIT-36). Schema per module, history table per schema, schema-qualified SQL and a non-escalating shared transaction across module DbContexts are proven by `tests/FitnessCenter.IntegrationTests/ModuleSchemaTests.cs`. Default privileges and REFERENCES are confirmed from the PostgreSQL docs but unused while there is one database role (D-015). Details: database-schemas.md, Verification (FIT-36). | D-014, D-015 |

The [stakeholder interview guide](stakeholder-interview-guide.md) is on hold (D-047).

---

## Owner decides (D-047)

These questions waited on the center (D-004). Under D-047 the owner decides each one. Every row has a **recommended default**, a one-line reason and a source. These are recommendations only; nothing here is decided until it is recorded in [decisions.md](decisions.md). The full question text stays in the tier tables below.

Rows 1–10 of the first version were answered on 2026-11-24 (D-048–D-053; see Answered above). The rows below are renumbered. Row 6 (languages) was answered later the same day (D-065), and row 2 (waitlist offer length) on 2026-11-25 (D-067); the other rows keep their numbers. Row 1 follows from those answers, and later rows come with staff, payments and hardware.

```mermaid
flowchart LR
  N["Follow-up to D-050<br/>1"] --> S["Staff and operations<br/>3–12"] --> L["Payments, hardware, later<br/>13–19"]
```

| # | Question (IDs) | Recommended default | Reason | Source |
|---|---|---|---|---|
| 1 | How the owner's laptop is reached from outside (D-050) | Local network only while nobody outside needs it; when someone does, Cloudflare Tunnel rather than a port on the router. | cloudflared makes outbound-only connections, so no inbound port is opened on the home router. Cloudflare says it is "available on all plans"; that the free plan has no cost limits, and whether a domain on Cloudflare is needed, are **UNVERIFIED**. | https://developers.cloudflare.com/tunnel/ ; https://developers.cloudflare.com/cloudflare-one/networks/connectors/cloudflare-tunnel/ |
| 3 | Product name and brand (U10) | The owner picks a product name; keep D-044's marked placeholders and no logo until then. | No center brand to reuse any more. | D-044 |
| 4 | Legal form and VAT (F7, D1, D2) | Design for a commercial operator (ApS): VAT on access, and on instruction for members aged 30+. Ask an accountant the D2 questions before invoices are built. | Since 1 Jan 2026 instruction VAT depends on the member's age and the operator's status. | [domain-modeling.md §1, §2.12](research/domain-modeling.md) |
| 5 | Sales channels and front desk (M3, U1, U6) | Online signup (built, D-032) plus front desk acting for a member on signup, booking and check-in. | Front-desk tools in the market look up members and flag unpaid memberships at the door. | [ux-patterns.md §4](research/ux-patterns.md) |
| 7 | Management KPIs (U9) | Member count, sign-ups and cancellations per month, attendance, class fill rate. | These are standard club KPIs and need only data the system already has. | [fitness-domain-operations.md §10](research/fitness-domain-operations.md) |
| 8 | Staffed hours (F5) | Staffed opening hours only; no unstaffed 24/7 access. | Unstaffed hours need preconditions (security, safety, revoking cancelled cards) and real door hardware, which is simulated for now (D-021). | [fitness-domain-operations.md §4](research/fitness-domain-operations.md) |
| 9 | Working-time registration and rota (U8, E5) | Staff clock in and out per day in the app, no biometrics; records kept 5 years; employees see their own records. Rota later. | Mandatory in Denmark since 1 July 2024; Datatilsynet finds fingerprint time clocks non-compliant. | [compliance-legal-security.md §4](research/compliance-legal-security.md) (Errata item 3); [ux-patterns.md §7](research/ux-patterns.md) |
| 10 | Pay and payroll (E2, E3, D14) | Hourly pay only; export hours to an external payroll provider; the system runs no payroll. | "Payroll-ready" products usually only export; no public Danløn API was found. | [fitness-domain-operations.md §7.2](research/fitness-domain-operations.md); [integrations-and-architecture.md §2.1 row 6](research/integrations-and-architecture.md) |
| 11 | Certifications (E4, D15) | Track first aid and PT certificates with expiry; warn only, don't block scheduling. | Warnings keep the schedule working; børneattest matters only with under-15s, and members are 18+ (D-052). | [domain-modeling.md §3.4](research/domain-modeling.md); [compliance-legal-security.md §8](research/compliance-legal-security.md) |
| 12 | Freezes (D7) | No freezes at first; if added, store each freeze as a dated record (from, to, reason). | A dated record keeps history; freeze rules vary widely between gyms. | [domain-modeling.md §2.5](research/domain-modeling.md) |
| 13 | Bookkeeping (P3, D12) | Export to a registered bookkeeping system (e-conomic, Dinero or Billy); the system keeps no books itself. | A custom bookkeeping system carries heavy legal duties (BEK 205: backups, e-invoices, SAF-T). | [compliance-legal-security.md §7](research/compliance-legal-security.md) |
| 14 | Billing date (D4) | Fixed 1st of the month, prorated first period. | Betalingsservice deadlines favour a fixed 1st-of-month anchor. | [domain-modeling.md §2.3](research/domain-modeling.md) |
| 15 | Price increases (D5) | Prices are versioned; a new price applies to new members, and existing members move only after written notice. | Immutable price versions are standard; Danish rules on unilateral price increases are unresearched (**UNVERIFIED**). | [domain-modeling.md §2.2](research/domain-modeling.md); [compliance-legal-security.md §6.2](research/compliance-legal-security.md) |
| 16 | Offline check-in and door behaviour (A2, SA3) | Decide with the real hardware; the leading option is a local cache of valid members with a time limit. | Needs the hardware choice first (D-021 simulates it). | [integrations-and-architecture.md §3.8](research/integrations-and-architecture.md) |
| 17 | One center or many (F1 multi-tenancy, S7) | Single-tenant: one center, one deployment. | Simplest model; selling a system with bookkeeping to others needs registration. | [integrations-and-architecture.md §3.2](research/integrations-and-architecture.md); [compliance-legal-security.md §7](research/compliance-legal-security.md) |
| 18 | Retail/POS and aggregators (P4, M7) | Neither for now. | Both add integrations (POS hardware, Wellhub/USC APIs) outside the MVP. | [integrations-and-architecture.md §2.1 row 12](research/integrations-and-architecture.md); [similar-projects.md §3.1](research/similar-projects.md) |
| 19 | CCTV (A4) | Out of scope; never link footage to check-ins. | Linking CCTV to check-ins raises the risk significantly; Danish law limits retention to 30 days. | [compliance-legal-security.md §3.3](research/compliance-legal-security.md) |

---

## Tier 1 — Foundational scope

| # | Question | Raised by |
|---|---|---|
| F1 | **Purpose:** Is this for one specific real fitness center, a hypothetical one (learning/portfolio project), or a product that could serve many centers (multi-tenant SaaS / sold or licensed)? | similar-projects, fitness-domain-operations, integrations-and-architecture, compliance-legal-security |
| F2 | **Country/jurisdiction:** Which country (or countries) will it operate in? (Denmark is assumed by several files but not confirmed.) | all files |
| F3 | **Type of center:** big-box, budget/24-7, boutique studio, CrossFit box, climbing, swim, multi-purpose, municipal, hotel/corporate — or a mix (e.g. open gym + classes + PT)? | fitness-domain-operations, similar-projects |
| F4 | **Scale:** Single site or multi-site? If multi-site, can members use all sites / is there a "home club"? Approx. number of members and staff? | fitness-domain-operations, similar-projects, compliance-legal-security |
| F5 | **Staffed hours:** Staffed at all opening hours, or unstaffed (24/7 access) for some hours? | fitness-domain-operations, similar-projects |
| F6 | **MVP:** Which areas come first — members, billing, access/check-in, booking, staff/scheduling, POS? | fitness-domain-operations |
| F7 | **Business/legal form and size** (if a real center): ApS/A/S, sole proprietorship, association (forening)? Employee count and approx. turnover? (Affects bookkeeping law, accessibility-law exemption, child-protection record rules.) | compliance-legal-security |

## Tier 2 — Domain & feature scope

### Members & memberships
| # | Question | Raised by |
|---|---|---|
| M1 | Which membership types: monthly, punch cards/class packs, PAYG/drop-in, family, student/senior, corporate, prepaid? (Partly answered: 6 subscription types, D-019. Which ones is still open. The signup slice uses one placeholder type, D-032.) | fitness-domain-operations, compliance |
| M2 | Binding periods, freezes/pauses, and price-change practices you intend to use? (Note: Danish law effectively caps binding at ~6 months.) | compliance-legal-security |
| M3 | Sales channels: online, in-app, in person, phone, corporate deals? (Drives 14-day withdrawal handling.) | compliance-legal-security |
| M4 | Minors: accept members under 18? Under 15? Will staff run classes for under-15s? | compliance-legal-security |
| M5 | Health data: do you need health declarations, injury records or trainer notes in the system at all? Who may see them? | compliance-legal-security, agentic-project-planning |
| M6 | Member mobile app required, or is a web app / PWA / wallet pass enough? Are push notifications a must? | similar-projects, integrations-and-architecture |
| M7 | Accept aggregator members (Wellhub / Urban Sports Club / ClassPass)? | integrations-and-architecture |
| M8 | Wearable / fitness-tracking features in v1? | integrations-and-architecture |
| M9 | MitID needed (contract signing, age verification)? | integrations-and-architecture |

### Payments & finance
| # | Question | Raised by |
|---|---|---|
| P1 | Payment methods at launch: card, Betalingsservice, MobilePay (Recurring API), SEPA, cash at reception, invoice? | fitness-domain-operations, integrations, compliance |
| P2 | Existing payment provider (PSP) or Betalingsservice creditor agreement (PBS number)? | integrations-and-architecture, compliance |
| P3 | Bookkeeping: integrate with an existing registered bookkeeping system (e-conomic / Dinero / Billy / accountant's) — or should this system do bookkeeping itself (much heavier legal requirements)? Which system is used today? | compliance-legal-security, integrations-and-architecture |
| P4 | Retail/POS needed (drinks, supplements, merch)? | similar-projects |

### Access control
| # | Question | Raised by |
|---|---|---|
| A1 | Existing door/turnstile hardware? Make/model (Salto, Paxton, Gantner, other), cloud-connected? | integrations-and-architecture, similar-projects, compliance |
| A2 | If internet goes down: doors fail open, fail closed, or use cached member list? | integrations-and-architecture |
| A3 | Biometric (face/fingerprint) entry wanted? For members, staff, or both? What free equivalent alternative would be offered? (Highest-risk feature legally.) | compliance-legal-security, agentic-project-planning |
| A4 | CCTV in scope for this system or a separate vendor? Linked to check-ins? | compliance-legal-security |

### Employees
| # | Question | Raised by |
|---|---|---|
| E1 | Staff types: instructors, PTs, front desk, cleaning, managers, other? Employed vs freelance? | similar-projects, fitness-domain-operations |
| E2 | How are they paid: hourly, salary, per class, per attendee, per session, commission on sales/PT, revenue split, rent? | similar-projects, fitness-domain-operations |
| E3 | Payroll: should the system pay wages, or only calculate/export inputs to an external provider (Danløn, Visma, DataLøn, accountant)? Should time tracking feed it? | similar-projects, fitness-domain, integrations, compliance |
| E4 | Certifications to track with expiry (first aid/CPR, PT licences, børneattest)? | similar-projects |
| E5 | Shift planning for non-instructor staff, including swaps and vacation? What's used today (Planday, spreadsheets)? | similar-projects, integrations-and-architecture |

### Build approach
| # | Question | Raised by |
|---|---|---|
| B1 | Is building on an existing platform (Odoo/ERPNext) or reusing open-source code acceptable, given licence implications (AGPL/GPL)? | similar-projects |
| B2 | Existing tools to keep/integrate (accounting, payroll, PSP, EGYM/InBody, email/SMS provider)? | similar-projects, compliance |

## Tier 3 — Technical & non-functional

| # | Question | Raised by |
|---|---|---|
| T1 | Which programming language(s)/frameworks can you comfortably **review**? (Your review is the main quality gate.) | integrations-and-architecture |
| T2 | Monthly running-cost budget (hosting, auth, email/SMS, payment fees)? | integrations-and-architecture |
| T3 | EU/EEA-only data residency required? Self-hosted or cloud? | integrations, compliance |
| T4 | Security target: OWASP ASVS 5.0 — Level 1, Level 2, or L2 for sensitive modules only? | agentic-project-planning, compliance |
| T5 | Accessibility target: aim for WCAG 2.2 AA even if legally exempt? | compliance-legal-security |

## Tier 3b — Tech stack (from wave 3: tech-stack)

| # | Question | Raised by |
|---|---|---|
| S1 | How comfortable are you *reviewing* Vue 3 + TS vs React + TS today — which would you catch agent mistakes in faster? | tech-stack |
| S2 | Professionally: controllers or Minimal APIs? EF Core or Dapper? Follow your habits or Microsoft's current recommendation (Minimal APIs)? | tech-stack |
| S3 | Built anything in Blazor? Is a single-language (C#) stack worth weaker agent evidence? | tech-stack |
| S4 | Main learning goal: frontend depth, cloud/ops (e.g. own Hetzner box), or agentic workflow? (Changes ranking weights.) | tech-stack |
| S5 | Spend a small bounded token budget on a Vue-vs-React (or Blazor) spike over the same two screens before choosing? | tech-stack |
| S6 | Auth: own login (ASP.NET Identity + passkeys) or hosted IdP (Entra External ID / Auth0 / Keycloak) with an extra DPA? | tech-stack |
| S7 | Will you be paid by the center / will it use the system commercially? (Affects Rider non-commercial licence, possibly Hangfire tier.) | tech-stack |
| S8 | Hosting budget; own backups and patching (Hetzner ~€12/mo) or managed Postgres (Render/Fly/Azure)? (Relates to T2.) | tech-stack |
| S9 | Do you or the center already use Microsoft 365/Azure? | tech-stack |
| S10 | Member app: PWA acceptable (iOS push caveats), or native required? (Relates to M6.) | tech-stack |
| S12 | Will RFID/door integration need software running on-site at the gym? If so, .NET worker on Linux/Windows hardware? (For now simulated, D-021; real hardware is for the owner, D-047; D-003.) | tech-stack |

## Tier 3c — Service architecture (from wave 3: service-architecture; feeds D-008)

| # | Question | Raised by |
|---|---|---|
| SA1 | "Independent services": independently deployable processes, or independent code modules with enforced boundaries? (Frontend → API → logic → DB is normally layers in one process.) | service-architecture |
| SA2 | Is learning distributed-systems techniques (outbox, broker, sagas, contract tests) a goal in itself, worth slower feature delivery? Is one real seam (payment) enough? | service-architecture |
| SA3 | Should check-in run in its own process even before payment exists, or is an on-site edge agent / vendor offline lock enough for outages? | service-architecture |
| SA4 | Hosting: one EU VPS with Docker Compose, or PaaS billed per service? (Relates to S8, T2.) | service-architecture |
| SA5 | Use .NET Aspire for local development from the start (dev-time orchestrator, not a production runtime)? | service-architecture |
| SA6 | One PostgreSQL schema per module now (easier later extraction), or one shared schema? | service-architecture |
| SA7 | Future payment: require a redirect/hosted payment page (smallest PCI scope), so the payment service is mainly about isolating secrets and webhooks? | service-architecture |

## Tier 3d — Database schemas (from wave 3: database-schemas; feeds SA6)

| # | Question | Raised by |
|---|---|---|
| DB1 | Cross-module foreign keys: allow (integrity) or forbid (boundaries, extraction)? Blanket rule or per-pair exceptions? | database-schemas |
| DB2 | Cross-module transactions: allow a shared-connection transaction as an escape hatch (e.g. check-in + attendance atomic), or events/outbox only? | database-schemas |
| DB3 | Per-module database roles: v1, later, or never? (They rule out shared transactions.) | database-schemas |
| DB4 | Migration deployment: Aspire locally + one bundle per module or SQL scripts in production? In what order? | database-schemas |
| DB5 | Reporting: SQL views in a `reporting` schema, or an event-fed read model? | database-schemas |
| DB6 | Is whole-database point-in-time restore enough, or must one module be restorable on its own? | database-schemas |
| DB7 | Should the future payment service get its own database from day one, regardless of SA6? | database-schemas |

## Tier 3e — Authentication (from wave 3: authentication; feeds D-011)

| # | Question | Raised by |
|---|---|---|
| AU1 | Native iOS/Android app planned? If yes, App Store guideline 4.8 makes Sign in with Apple (or equivalent) effectively mandatory next to Google/Facebook. (Relates to M6, S10.) | authentication |
| AU2 | Is Facebook Login worth Meta Business Verification + ongoing review + EU-US Data Privacy Framework exposure (under appeal, C-703/25 P), given weak usage evidence? | authentication |
| AU3 | GitHub/Microsoft login for members, or only for staff? | authentication |
| AU4 | Must the center verify age or identity (minors, contracts)? If so, MitID via broker (~€0.037/login + €67+/month) at signup only? (Relates to M4, M9.) | authentication |
| AU6 | One identity store with roles for staff and members, or separate stores? | authentication |
| AU7 | ASVS target: L1 for members, L2 for staff/admin? (Relates to T4.) | authentication |
| AU8 | MFA mandatory for staff (passkey or TOTP)? Optional for members? | authentication |

## Tier 4 — How we work (agentic process)

| # | Question | Raised by |
|---|---|---|
| W1 | **AI budget:** acceptable monthly spend (subscription plan vs API)? Hard ceiling enforced technically? Multi-agent research costs ~15x a chat — verify every research file, or only decision-feeding ones? | agentic-workflows, agent-research-methodology |
| W2 | Tools/providers available: Claude plan, OpenAI Codex, Gemini, Copilot, Linear? Cross-vendor review wanted? | agentic-workflows |
| W3 | GitHub: repo hosted there? Public/private, which plan? | agentic-workflows |
| W4 | **Review appetite:** review every diff, every PR (summary + spot checks), or sampled only? Hours/week available? How much generated code do you intend to read and understand yourself? | agentic-workflows, video-analysis |
| W5 | Autonomy: unattended/overnight agent runs acceptable, or every session supervised? Turn/spend caps and approval gates? Is "bypass permissions" mode ever acceptable? | agentic-workflows, agentic-project-planning |
| W6 | Parallelism: how many concurrent agent streams? Spend extra on parallel worktree candidates for risky features? | agentic-workflows, video-analysis |
| W7 | Security posture for agents: will they ever touch real personal data, payment systems or prod credentials? Sandbox / dev container / cloud sandbox? | agentic-workflows |
| W8 | Workflow ceremony: full Spec Kit, Kiro-style, BMAD, PRD→Taskmaster, lightweight native Claude Code, or hybrid? (See agentic-project-planning §options.) | agentic-project-planning |
| W9 | Specs: spec-first or spec-anchored (maintained for the system's lifetime)? Doc-sync mechanism? | agentic-project-planning |
| W10 | Acceptance criteria format: EARS, Gherkin, both, or checklists? Executable BDD tests? | agentic-project-planning |
| W11 | Task tracking: per-feature tasks.md, JSON feature list, Taskmaster, GitHub Issues, or Linear? Need phone access? | agentic-project-planning, agentic-workflows |
| W12 | Agent instruction file: CLAUDE.md only, or AGENTS.md (portable to other agents)? | agentic-project-planning |
| W13 | ADR format for decisions.md: MADR full, MADR minimal, or Nygard? | agentic-project-planning |
| W14 | Which rules must be hard-enforced (hooks/deny rules — protected tests, no .env reads, dependency allowlist) vs guidance? | agentic-project-planning |
| W15 | Definition of Done: what evidence must an agent produce (tests, e2e screenshot, docs updated, review passed)? | agentic-project-planning, agentic-workflows |
| W16 | Test strategy as gate: TDD, E2E-as-done, coverage thresholds, mutation testing for critical logic? Should test-file edits need separate approval? | agentic-workflows, video-analysis |
| W17 | Hand-scaffold the domain skeleton/interfaces yourself, or have an agent draft it for your approval? Should the orchestrator agent be read-only by policy? | video-analysis |
| W18 | Quality metrics to track (CI pass rate, review round-trips, reverts, cost per PR)? Dashboard or periodic reports? | agentic-workflows |
| W19 | Willing to use experimental features (agent teams, research previews)? | agentic-workflows |
| W20 | Cadence: milestone vertical slices, weekly iterations, or continuous kanban? WIP limit? | agentic-workflows |
| W21 | Which planning documents are in scope now vs later? | agentic-project-planning |

## Tier 4b — Testing (from wave 2: testing-strategy)

| # | Question | Raised by |
|---|---|---|
| Q1 | Build the domain core (billing, contract dates, freezes, pay) as a pure, clock-injected module so it can be unit/property/mutation tested? (Partly an architecture decision.) | testing-strategy |
| Q2 | Who writes acceptance tests — you, an agent you approve then freeze, or a separate test-writer agent? Read-only for implementing agents? | testing-strategy |
| Q3 | Enforcement of test protection: CLAUDE.md rule, or deterministic (deny rules + sandbox + CI check on deletions/skips)? Evidence says prompts alone are weak. (Extends W14/W16.) | testing-strategy |
| Q4 | Mutation testing: on PRs (changed lines), nightly, or critical modules only? Gate merges or report only? | testing-strategy |
| Q5 | Coverage: no gate, ratchet, or fixed %? | testing-strategy |
| Q6 | E2E as Definition of Done: Playwright journey per feature, top journeys only, owner-approved/protected? | testing-strategy |
| Q7 | CI-generated requirement→test traceability matrix that blocks merges on untested `must` requirements? | testing-strategy |
| Q8 | PSP sandbox suites nightly in CI (needs network + sandbox credentials)? (Depends on P1/P2.) | testing-strategy |
| Q9 | Who approves legal "ground truth" example tables (termination dates, withdrawal refunds, freezes, holiday pay/feriepenge)? Lawyer/accountant review planned? | testing-strategy |
| Q10 | Single time zone (Europe/Copenhagen)? Run tests in a second zone too to catch hidden assumptions? | testing-strategy |
| Q11 | Test data: synthetic forever, or anonymised production snapshots someday (needs legal basis / DPIA)? | testing-strategy |
| Q12 | Flaky tests: who may quarantine, for how long? May agents add retries? | testing-strategy |
| Q14 | Performance testing (D-016): which tool (JMeter or alternatives), what targets (e.g. check-in response time, concurrent bookings), run on every PR, nightly or pre-release, and against which environment? | D-016 |
| Q13 | CI budget (minutes/cost per month)? Public vs private repo affects free CodeQL/secret scanning. (Relates to W3, T5.) | testing-strategy |

## Tier 4c — UX (from wave 2: ux-patterns)

New questions only; overlaps folded into F2, M6, A1, M5, E5, T5.

| # | Question | Raised by |
|---|---|---|
| U1 | Online sign-up offered? Any German/US exposure (stricter cancel-flow rulings: BGH FitX 2026, FTC)? | ux-patterns |
| U2 | Cancel flow: any "save"/retention step, or pause offered only elsewhere? Lawyer review of the flow? | ux-patterns |
| U3 | Check-in credential: wallet pass, rotating in-app QR, or both? Does planned door hardware support rotating QR? | ux-patterns |
| U4 | Waitlist: auto-promote or first-to-claim? Lock window before class? | ux-patterns |
| U5 | Languages: Danish + English on every surface? Who owns legally required button wording? (Answered: Danish by default, English available, D-065. The owner checks the UNVERIFIED Danish legal wording; backend texts stay English for now.) | ux-patterns |
| U6 | Can front desk perform every member action on the member's behalf? | ux-patterns |
| U7 | Front-desk hardware? Self check-in kiosk? Device management (MDM)? | ux-patterns |
| U8 | Staff time-clock method (app, kiosk, PIN)? Biometrics excluded (Datatilsynet says fingerprint clocks unlawful)? | ux-patterns |
| U9 | Which daily and monthly KPIs does management need? | ux-patterns |
| U10 | Existing brand/visual identity? ~~Design-system preference (e.g. shadcn/ui)?~~ (answered: shadcn/ui, D-018; replaced by UI SDK, D-056) Is a generic "AI look" acceptable? (Until answered, the landing page uses marked placeholder content and no logo, D-044.) | ux-patterns |

## Tier 4d — Domain modelling (from wave 2: domain-modeling §10)

Overlaps folded into F7, M1, M5, E2, E3, E4, E5, U4. New questions:

| # | Question | Raised by |
|---|---|---|
| D1 | Which services are sold: access only, classes/PT (instruction), or both? Classes included in membership price or separate? (Danish VAT since 1 Jan 2026: 25% on instruction for members 30+, so this affects invoicing.) | domain-modeling |
| D2 | **For an accountant:** when is age measured for instruction VAT; how to split a mixed access+instruction price; VAT on punch cards at sale or use; revenue deferral for prepaid annual; required consumer invoice/credit-note fields; separate credit-note number series? | domain-modeling |
| D3 | Who owns the billing schedule: the PSP (Stripe Billing/Frisbii) or our system (required if using Betalingsservice/MobilePay Recurring directly)? | domain-modeling |
| D4 | Billing date: anniversary or fixed day (e.g. the 1st)? First partial month prorated, free, or full? | domain-modeling |
| D5 | Price increases for existing members: grandfather or migrate with notice? How much notice? Lawyer-reviewed clause? | domain-modeling |
| D6 | Upgrades/downgrades mid-period? Prorated? Does it restart the binding period? | domain-modeling |
| D7 | Freezes: free/paid; min/max length; how many per year; medical evidence; extends binding?; allowed during notice?; any access during freeze? | domain-modeling |
| D8 | Cancellation: may a member revoke it? On mid-period cancel, does access end immediately or at effective date? (Answered: access until the effective date, D-048; revocation allowed, D-055.) | domain-modeling |
| D9 | Family/household memberships: needed? Who can be on one? Is the payer always a member? | domain-modeling |
| D10 | Punch cards: expiry? Do late cancels/no-shows consume a clip? Refundable? | domain-modeling |
| D11 | Dunning: retries and period; when is door access blocked; late fees; collections agency or write-off; different for card vs direct debit? (Partly answered, D-020: a missing payment emails the member and blocks check-in. Partly answered, D-042: placeholder of no grace period and one reminder email on the first Copenhagen day after paid-until. Partly answered, D-046: a member who has never paid is refused at check-in. The center's real grace period, repeat reminders (including whether a never-paid member gets one) and late fees still open.) | domain-modeling |
| D12 | Ledger depth: export to e-conomic/Dinero/Billy enough, or internal double-entry balances? (Relates to P3.) | domain-modeling |
| D13 | Staff roles/permissions: which roles? Trainers see member health info only for own sessions? Multi-site scoping? | domain-modeling |
| D14 | Per-attendee pay counts booked, checked-in, or no-shows too? Collective agreements, holiday pay, overtime rules to model? | domain-modeling |
| D15 | Certification expiry: warn only, or block scheduling? | domain-modeling |
| D16 | Class moved to smaller room: who loses their spot? | domain-modeling |
| D17 | Persistence style: full event sourcing (e.g. as a learning goal) or CRUD + audit log + immutable financial records? | domain-modeling |
| D18 | GDPR erasure: lawyer to confirm whether crypto-shredding/pseudonymisation counts as erasure, and which invoice fields must survive 5 years? | domain-modeling |
| D19 | Denmark/DKK/Europe-Copenhagen only, or support other countries/currencies/VAT regimes from day one? (Relates to F2.) | domain-modeling |

## Research-process questions

| # | Question | Raised by |
|---|---|---|
| R1 | May agents use and cite non-English (e.g. Danish) sources with translated quotes? | agent-research-methodology |
| R2 | Can vendor documentation count as evidence that a feature exists in the market? | agent-research-methodology |
| R3 | Will you spot-check citations yourself? Who resolves source conflicts — you, or the orchestrator with documented reasoning? | agent-research-methodology |
| R4 | **Video:** What did you want us to take from ~1:12:04 of the video? YouTube blocked transcript download (HTTP 429). Can you paste the transcript, allow use of browser cookies with yt-dlp, or allow installing a local speech-to-text tool (whisper)? | video-analysis |
| R5 | **Wearables:** Fitbit API is off (2 Nov 2026), Garmin new-developer approvals paused, Google Health onboarding unknown. Drop wearables from v1 now? (Relates to M8.) | verification-report-wave1 |
| R6 | **Corrections:** The verifier found 24 corrections (e.g. wrong benchmark ranking, wrong California law cite, stale vendor facts). Apply them directly to the research files, or add an errata section so originals stay as written? | verification-report-wave1 |
| R7 | **Face entry (if wanted):** would members actively present themselves at a reader, or be recognised passively at a distance? (Decides whether the EU AI Act "remote biometric identification" high-risk category applies.) | verification-report-wave1 |
| R8 | Some sources block automated fetching (ft.dk, pwc.dk, classpass.com, Bird & Bird, Taylor Wessing). Will a human spot-check these? (Relates to R3.) | verification-report-wave1 |

---

The full list of 65 domain questions (incl. finer-grained ones not repeated here) is in [research/fitness-domain-operations.md](research/fitness-domain-operations.md).

## Deferred research

- **RFID access hardware** (card types such as MIFARE Classic vs DESFire, readers and controllers sold in Denmark, linking a swipe to a class or PT session) — on hold until the owner chooses real hardware ("Owner decides" row 16, D-047). Originally on hold until the center meeting (owner instruction, 2026-11-12).

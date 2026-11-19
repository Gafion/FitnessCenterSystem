# Similar Products & Projects — Research Report

**Status:** Research only. This document makes no project decisions. It gathers evidence, lists options with trade-offs, and raises questions.
**Date compiled:** 2026-11-12
**Scope:** (1) commercial gym/studio/club management software, (2) open-source projects, (3) a feature comparison with focus on the **employee side**.

## How to read the evidence

- Each claim links to a source in the [Sources](#sources) list as `[S#]`.
- **Vendor** means the vendor's own site or help center. **Directory** means review aggregators (Capterra, G2, GetApp, Software Advice). **Competitor** means a page written by a rival vendor. Competitor pages have a commercial bias, so their claims are labeled.
- **UNVERIFIED** marks a claim that rests on a single non-vendor source, on conflicting sources, or on an inference.
- Pricing figures change often and third-party sites disagree. Treat every price here as indicative only. Most were checked against aggregators from 2026, not against signed quotes.
- I did not use any product hands-on. Feature presence means a source says the feature exists. It does not say how good the feature is.

---

## 1. Executive summary

1. **The market splits into three tiers.**
   - **Boutique, studio, and micro-gym tools** sell self-serve with mostly published pricing, often per active member: Gymdesk, TeamUp, PushPress, Zen Planner, Wodify, Clubworx, Glofox, and Booking Board.
   - **Mid-market all-rounders** (Mindbody, PerfectGym, Virtuagym, ClubReady, Exercise.com) often use tiered or quote-based pricing.
   - **Enterprise club platforms** (ABC Ignite, Exerp, Daxko Operations) serve chains and YMCAs, quote-only. Exerp lists SATS, PureGym, Virgin Active, and Fitness World (Denmark, 232 clubs) among its customers [S30][S47].
2. **Table stakes:** almost every product documents member records, membership plans with recurring billing, class or appointment booking, a member app or portal, basic reporting, and multiple staff logins. Check-in/access control and POS are near-universal in the gym and club tier but less so in pure booking tools.
3. **The employee side is shallow almost everywhere.** What vendors usually call "payroll" is a **pay calculation report** (per-class, per-attendee, hourly, commission). It is not a payroll engine that pays wages, withholds tax, or files with authorities.
   - Wodify says this outright: closing a pay period "does not issue any payments" [S22].
   - Mindbody integrates with ADP for actual payroll [S3].
   - Booking Board (Danish) exports to Danløn, a Danish payroll provider [S27].
   - **Certification tracking** (CPR and similar) is largely absent from core gym suites. It is sold by add-on tools such as NetGym, which integrates with ABC [S40].
   - **Shift scheduling** for non-instructor staff (front desk, cleaning) appears in only some products: Flexybox, Booking Board, ClubReady, and Zoezi (UNVERIFIED).
4. **The Nordic and Danish market has local players** with local integrations: Booking Board (Haderslev, DK), Flexybox (Svendborg, DK), yogobooking (DK), Zoezi and BRP Systems (Linköping, SE), and Exerp (Copenhagen-linked, enterprise). Local integrations include Danløn, e-conomic, Dinero, Billy, Fortnox/Visma SIE, Swish, and BankID [S27][S28][S31][S33].
5. **Recurring complaints across vendors:** price increases (especially after acquisitions), slow or inconsistent support, billing and payment glitches, complex UIs, and add-on pricing. Section 6 lists sources.
6. **Open source:** no mature, widely adopted open-source system covers the whole job. The most useful references are:
   - **wger:** Django, AGPL-3.0, ~7.3k stars, actively maintained. Mainly a workout tracker with a small "gym" module that has contracts, roles, and member notes [S50].
   - **Gymie:** Laravel/Filament, ~509 stars, active [S53].
   - **Odoo and ERPNext/Frappe:** third-party gym modules on generic ERP platforms that already have HR and accounting [S56][S57].

---

## 2. Commercial product profiles

### 2.1 Mindbody (US; global)
- **Segment:** fitness studios, salons, spas, wellness (broad "wellness" positioning) [S1][S3].
- **Pricing:** tiers are Starter, Accelerate, Ultimate, and Ultimate Plus (names per third parties). Mindbody's own pages say Accelerate and Ultimate are quote-based [S1]. Third-party figures range from roughly $139 to $699/month and conflict (UNVERIFIED) [S2].
- **Employee side (vendor page) [S3]:**
  - Role-based permissions. Typical tiers are owner/admin, manager, front desk, and instructor/provider. Sensitive areas such as payments and reports can be restricted.
  - Automated staff substitution ("Quick Teacher Substitution").
  - Time Clock with hourly pay rates.
  - Commission rates set per item or per staff profile.
  - A Payroll report computing earnings from class, enrollment, appointment, and hourly rates, with professional-product charges deducted from staff pay [S4].
  - Integration with ADP for real payroll.
  - Staff mobile app ("Mindbody Business") for check-in, bookings, and payments.
  - No certification tracking mentioned [S3].
- **Payroll by tier:** one salon-industry review says Starter lacks payroll and higher tiers include it (UNVERIFIED) [S2].
- **Complaints:**
  - Frequent price increases and add-ons [S5].
  - Support described as slow or inconsistent. Capterra customer-service score is 3.8 [S5].
  - Cancellation and contract difficulty [S5].
  - Bugs and UX issues are the top Reddit complaint category, per a third-party roundup (UNVERIFIED, secondary aggregation) [S6].

### 2.2 ABC Glofox (Ireland origin; owned by ABC Fitness since 2022)
- **Ownership:** ABC Fitness announced the acquisition in Aug 2022, reportedly for about $200m / €200m (sources differ). Glofox runs as a business unit inside ABC [S7].
- **Segment:** boutique studios, CrossFit/functional gyms, franchise boutiques. A third-party roundup says BFT and F45 franchisees are "forced onto the platform" (UNVERIFIED) [S6].
- **Pricing:** quote-based. A competitor says it starts at $99/month (UNVERIFIED, competitor source) [S8].
- **Employee side (vendor page) [S9]:**
  - Per-trainer availability.
  - "Configurable roles and permissions" (no detail).
  - Trainer base rates for classes and appointments, feeding payroll reports.
  - Task assignment and tracking.
  - Trainer performance insights.
  - Staff app "Glofox Pro" for check-in, sign-up, and taking payments.
  - The vendor page mentions neither commissions nor time clock.
  - A G2 listing says the payroll module tracks clock-in/out, tips, and commissions (UNVERIFIED; directory listing contradicts the vendor page's silence) [S10].
- **Complaints:**
  - Billing and payment glitches: wrong billing dates, failed transactions, confusing credit systems [S11].
  - Slow resolution of technical issues [S11].
  - Unexpected price hikes [S11].
  - Multi-location and family-account limitations [S12].
- **Ratings:** G2 4.5/5 (147 reviews), Capterra 4.4 (356) [S11].

### 2.3 ABC Ignite (US; ABC Fitness)
- **Segment:** startup, single-site, multi-location, and franchise health clubs [S13].
- **Features (vendor) [S13]:**
  - Front desk, calendars, bookings, check-ins, and scan-for-entry.
  - Membership, leads, and CRM with trigger-based SMS/email.
  - Billing via ABC's billing partner, with revenue recovery.
  - Branded member app.
  - Dashboards covering revenue, joins, net dues, and attrition.
  - AI agents for phone, email, SMS, and chat lead handling.
- **Employee side:**
  - Vendor page covers staff and trainer schedules tied to occupancy and attendance, and matching staffing to peak times [S13].
  - **Not found:** payroll, commission, permissions. Payroll and commission in Ignite are UNVERIFIED.
- **Partnerships:** ABC + EGYM (2025) made EGYM workout tracking the exclusive tracking technology across ABC's Trainerize, Ignite, and Glofox [S14].
- **Pricing:** quote-based (demo request) [S13].

### 2.4 PerfectGym (Poland origin; EU/global)
- **Segment:** clubs and chains, studios.
- **Pricing (directory, conflicting):**
  - Studio €129, Club €299, Business €399 per club per month, Enterprise on quote [S15].
  - Other directories list €29 or $99 starting prices (UNVERIFIED) [S15][S16].
- **Features (directory):**
  - POS, mobile app, CRM, and "commission software for staff" [S17].
  - Integration with physical access control [S16].
- **Complaints:**
  - Slow email support, tickets closed without update, invoice bugs, slow performance [S18].
  - Capterra 3.9 (33 reviews), customer service 3.5. G2 3.7 (11 reviews) [S18].
- *Note: I could not confirm whether PerfectGym is owned by EGYM. The search turned up only a partnership between ABC and EGYM [S14]. UNVERIFIED.*

### 2.5 Zen Planner (US; Daxko family)
- **Segment:** CrossFit, martial arts, studios.
- **Ownership:** a 2026 article lists "Daxko Zen Planner" in Daxko's lineup. That Daxko owns it is an inference (UNVERIFIED terms and date) [S19].
- **Pricing model:** tiered on **active members only, never staff or prospects** [S20]. Directory prices: Studio $99, Studio Plus $149, Professional $249 (UNVERIFIED) [S21].
- **Employee side:** payroll, staff app, and attendance are listed by the vendor [S20]. Exercise.com (a competitor, 2024) names Zen Planner as able to track trainer certifications (UNVERIFIED) [S41].

### 2.6 Wodify (US)
- **Segment:** CrossFit and functional fitness.
- **Plans:** Essentials, Accelerate, Ultimate. Ultimate adds performance tracking, a website, unlimited automations, and lower processing rates [S23]. Prices conflict: Essentials is listed at anywhere from $79 to $199 (UNVERIFIED) [S23].
- **Employee side (vendor help center, the best-documented payroll model found) [S22]:**
  - Pay rate types: **Class, Appointment, Administrative** (admin hours), plus **Fixed pay** per week, month, or year.
  - Default pay rate per *program*. Example: the same coach has a "Head Coach" rate in one program and a "Group Coach" rate in another. The rate auto-fills when a coach is assigned.
  - A "Missing Pay Rate" alert appears when a coach is assigned without a rate.
  - Only Admins and Managers can access payroll settings. A pay rate can only be assigned to someone with at least a Staff role.
  - Timecard clock-in exists.
  - **Payroll is tracking and calculation only, not payment.** A "Payroll Pro" payment feature is announced for late 2026 or early 2027; sources conflict on timing.

### 2.7 PushPress (US)
- **Segment:** CrossFit and functional gyms, small gyms.
- **Pricing:** a forever-free tier with unlimited members, staff, and leads, covering basic scheduling, billing, waivers, and attendance. The paid Pro tier costs about $139–159/month (sources conflict) [S24].
- **Employee side:**
  - Payroll is in the Pro tier per most directories [S24].
  - One directory lists a "Payroll Report" in the free tier, a conflict marked UNVERIFIED [S24].
  - A separate "PushPress Staff" app exists [S24].

### 2.8 Gymdesk (US)
- **Segment:** martial arts, small gyms, and studios (formerly "Martial Arts on Rails") [S25].
- **Pricing:** all features on every tier, priced by active members. About $75 for ≤50 members, $100 for ≤100, $150 for ≤200, $200 for ≤400, and Enterprise above 400. **Unlimited staff accounts** on every tier [S25].
- **Employee side:** staff accounts only. No payroll or commission documentation found in these sources (UNVERIFIED absence).

### 2.9 TeamUp (UK)
- **Segment:** boutique studios, small gyms.
- **Pricing:** monthly, adjusted to the previous month's active customer count, with no contract [S26]. Third-party figures run about $104–119/month (UNVERIFIED).
- **Employee side (vendor) [S26b]:** "Pay Rates" per instructor.
  - Rate types: **duration-based (hourly)** or **per-attendee**.
  - Rates can differ by event type and by location.
  - Settings include a rate cap, a base rate for empty sessions, and whether free registrations, late cancels, and no-shows count toward pay.
  - Output is an "Instructor Hours and Pay Rates" report, exportable to CSV.
  - Staff can be marked as instructors so they appear on the schedule.

### 2.10 Virtuagym (Netherlands)
- **Segment:** gyms, studios, personal trainers.
- **Pricing (vendor page):** "Manage" plan from €19 per location per month, 14-day trial. An "All-in-One" tier adds Coach features. Directory prices conflict with this [S29].
- **Employee side:** client and staff management, flexible scheduling, and a mobile staff app [S29]. No detailed staff scheduling documentation found.
- **Complaints:** expensive relative to competitors, plus unclear or inconsistent pricing [S29].

### 2.11 Clubworx (Australia)
- **Segment:** martial arts, fitness clubs.
- **Pricing:** free tier for about ≤20 members, then "Growing" A$109 for ≤100 members including POS, then "Accelerate" around 149 [S34].
- **Employee side:**
  - Vendor lists "Unlimited staff accounts with flexible admin privileges" [S34].
  - Directories list staff scheduling and task management [S34].
- **Reviews:** support is generally praised [S34].

### 2.12 ClubReady (US)
- **Segment:** clubs, franchises (US).
- **Employee side (directory and user reviews):**
  - Schedules shifts, tracks hours, and manages payroll [S35].
  - A G2 reviewer praises "payroll, staff time clock, inventory and POS all rolled up into one program" [S35].
  - No commission documentation found.
- **Pricing:** conflicting. Directories say from $79/month. A competitor lists four tiers from $149 to $499 (UNVERIFIED, competitor) [S35].

### 2.13 Daxko Operations (US)
- **Segment:** YMCAs, JCCs, community recreation. Strong in programs and corporate memberships [S36].
- **Features:** high-volume check-in, program administration, recurring billing, staff workflows, role-based shared member records, staff notes on members [S36].
- **Complaint:** the sheer number of options makes it "easy to get lost" [S36].

### 2.14 EGYM (Germany)
- **Not a membership-management system.**
  - "Genius" is an AI training-plan layer for members and trainers [S37].
  - EGYM integrates with third-party member-management systems to sync master and membership data [S37].
- **Relevance:** EGYM is an *integration target* (smart equipment and training data) rather than a competitor.

### 2.15 Exercise.com (US)
- **Segment:** gyms, personal trainers, studios. Main selling point is a fully custom-branded app [S38].
- **Features:** check-ins, remote door access, employee management, CRM, scheduling [S38]. Staff can view their own sales commissions (vendor support article) [S4b].
- **Pricing:** quote-only. Competitor estimates of $199–500+/month are UNVERIFIED [S38].

### 2.16 Xplor Gym UK / Xplor Triib; ClubRight (UK)
- **Xplor Gym UK:**
  - Membership, embedded payments, integrated access control, marketing automation, dashboards [S39].
  - Over 700k members migrated, per Xplor [S39].
- **Xplor Triib:**
  - Appointments, attendance, POS, self check-in, staff and team management [S39].
  - Vendor help article: pay coaches different rates for different classes [S22b].
- **ClubRight:**
  - A separate vendor, *not* Xplor [S39].
  - Recurring billing, member portal, dues management [S39].
  - Pricing from £79/month (UNVERIFIED) [S39].

### 2.17 Nordic / Danish products

| Product | Country | Segment | Notable points | Source |
|---|---|---|---|---|
| **Exerp** | DK-linked (ClubLead listed with a Copenhagen address) | Enterprise chains | Complex membership structures, add-ons, and pricing rules; multi-brand; integrates with door access and CRM. Customers include SATS, PureGym, Fitness World (232 clubs), Virgin Active, Holmes Place, ACTIC. Staff mobile sales app "Exerp GO". Quote-only. | [S30][S31][S47] |
| **Booking Board** | Haderslev, DK (founded 2019) | Independent gyms and small chains | **Modular published pricing:** Essentials €69 (required), Manage €149, **Operate €179 (staff and teams, shift management with app shift swap, wage rates, hour bank, salary settlement, Danløn integration, door access via JustFace/Kisi, accounting exports to e-conomic, Dinero, Billy, and SIE for Fortnox/Visma)**, Coach €69 + €2 per client, Grow €49, Engage €29. All modules €544/month. Access opens and closes automatically with membership status. Multi-location with a separate VAT number per site. | [S27] |
| **Flexybox (FlexyFitness)** | Svendborg, DK | Single centres to chains | Punch cards and subscriptions, pause/cancel rules, waitlists, **24/7 unmanned door access tied to bookings**, debt collection, FlexyPOS, **FlexyPlan for staff time tracking, shift and vacation planning**, loyalty app, EGYM and InBody integrations. Price on request. | [S28] |
| **yogobooking** | DK | Yoga, pilates, boutique, sauna | Member management system (details not researched) | [S31] |
| **Zoezi** | Linköping, SE | Gyms and studios | Swish, Klarna, and Trustly payments; staff scheduling and payroll tracking; branded app. From 2,690 SEK/month. All points UNVERIFIED (competitor blog). | [S32] |
| **BRP Systems** | Linköping, SE (2004) | Gyms, racket halls, sports facilities | Class and PT schedules, reusable templates, multi-facility, staff schedule planning with time-reporting integrations, cancellation rules, entrance hardware, debt-collection integration (Intrum) that restores access on payment, ISO 27001 | [S33] |
| **Gibbs** | NO | Gyms | BankID verification of new members, access log | [S46] |
| **Fitnessbooking (NO)** | — | — | **Not found.** No source located. | — |
| **SATS internal systems** | — | — | **Not publicly documented.** SATS is a listed Exerp customer [S30]. | — |

---

## 3. Feature comparison matrix

**Legend:**
- **V** = documented on a vendor page or help center.
- **D** = directory or review listing only.
- **C** = competitor claim only (UNVERIFIED).
- **?** = not found in the sources I checked. This means not found, not that the feature is confirmed absent.
- **—** = explicitly not offered, or out of scope.

| Product | Member mgmt | Memberships / recurring billing | Class/appt booking | Check-in / access control | POS | Staff scheduling | Roles / permissions | Payroll (calc) | Commission | Time clock | Certifications | CRM / leads | Reporting | Member app | Pricing model |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Mindbody | V | V | V | V (staff app check-in) | V (payments) | V (subs) | V | V (report) + ADP | V | V | ? | ? | V | ? | Tiered, partly quote |
| ABC Glofox | V | V | V | V | V (staff app payments) | V (availability) | V (no detail) | V (base rates) | D | D | ? | ? | V | ? | Quote |
| ABC Ignite | V | V | V | V (scan entry) | ? | V | ? | ? | ? | ? | ? (NetGym add-on) | V | V | V | Quote |
| PerfectGym | D | D | D | D | D | D | D | ? | D | ? | ? | D | D | D | Tiered per club |
| Zen Planner | V | V | V | ? | ? | ? | ? | V | ? | ? | C | ? | ? | ? | Per active member |
| Wodify | V | V | V | ? | ? | V (coach assign) | V | V (detailed) | ? | V | C | ? | ? | ? | Tiered |
| PushPress | D | D | D | D | ? | D | ? | D (Pro) | ? | ? | ? | D | ? | D (Pro) | Free + Pro |
| Gymdesk | V | V | V | V (attendance) | ? | ? | ? | ? | ? | ? | ? | V (marketing) | ? | V | Per active member |
| TeamUp | V | V | V | ? | ? | V (instructors) | V (multi-user) | V | ? | ? | ? | ? | V | ? | Per active customer |
| Virtuagym | V | V | V | ? | ? | V | ? | ? | ? | ? | ? | ? | ? | V (staff app) | Per location |
| Clubworx | D | D | D | ? | V (Growing+) | D | V | ? | ? | ? | ? | ? | ? | ? | Tiered by members |
| ClubReady | D | D | D | ? | D | D | ? | D | ? | D | ? | ? | ? | ? | Conflicting |
| Daxko Ops | V | V | V | V | ? | ? | V (role-based records) | ? | ? | ? | ? | ? | D | ? | Quote |
| Exercise.com | D | D | D | D (remote door) | ? | ? | ? | ? | V (staff view) | ? | ? | D | ? | V (branded) | Quote |
| Exerp | D | D (complex rules) | D | D (integrations) | V (Exerp GO sales) | ? | ? | ? | ? | ? | ? | D | D | ? | Quote |
| Booking Board | V | V | V | V (JustFace/Kisi) | ? | V (shifts, swap) | ? | V (+Danløn) | ? | V (hour bank) | ? | V (Grow) | V | V | Modular, published |
| Flexybox | V | V | V | V (24/7) | V | V (FlexyPlan) | ? | ? | ? | V (time tracking) | ? | ? | V | V (loyalty) | Quote |
| BRP Systems | V | V | V | V (entrance hw) | ? | V | ? | via integration | ? | via integration | ? | ? | ? | V | Quote |

### 3.1 Table stakes vs differentiators (as observed in the sources)

**Table stakes.** Present for nearly every product with vendor documentation:
- Member records and profiles.
- Membership plans with recurring billing and payment processing.
- Class and appointment scheduling with online booking.
- Multiple staff logins.
- Basic reporting (revenue, attendance).
- A member-facing app or portal, often branded.
- Some form of check-in or attendance tracking.

**Common but not universal:**
- POS and retail.
- Access-control hardware integration.
- Lead/CRM automation.
- Waitlists.
- Instructor pay-rate calculation.

**Differentiators seen:**
- **Pricing model:** per active member, free tier, published modular pricing.
- **Local integrations:** Danløn, e-conomic, Swish, BankID.
- **Membership-rule complexity:** pause, freeze, minimum term, add-ons. Exerp and Flexybox stand out here.
- **Unmanned 24/7 access tied to membership or booking status:** Booking Board, Flexybox.
- **Automated debt collection that restores access on payment:** BRP with Intrum, Flexybox.
- **Performance and workout tracking:** Wodify Ultimate, EGYM integrations.
- **AI agents for leads:** ABC Ignite.
- **Staff mobile sales:** Exerp GO, Glofox Pro, Mindbody Business.

### 3.2 The employee side, in detail

| Capability | How products handle it | Examples |
|---|---|---|
| **Instructor/coach pay rates** | The most developed employee feature. Rate types seen: per class, per appointment, hourly/duration, per attendee, fixed salary, administrative hours. Rates vary by program, event type, and location. Caps, a base rate for empty classes, and rules for no-shows and late cancels. | Wodify [S22], TeamUp [S26b], Mindbody [S4], Glofox [S9], Triib [S22b] |
| **Commission** | Per item or per staff profile (Mindbody). Staff can see their own commissions (Exercise.com). | [S3][S4b] |
| **Time clock** | Clock in/out with hourly rates. Booking Board has an "hour bank". | Mindbody [S3], Wodify [S22], ClubReady [S35], Booking Board [S27] |
| **Actual payroll (paying wages, tax)** | **Generally delegated** to payroll providers via export or integration. No product claimed to run statutory payroll itself. | ADP (Mindbody) [S3], Danløn (Booking Board) [S27], "does not issue any payments" (Wodify) [S22] |
| **Shift scheduling (non-instructor staff)** | Less common. Shift planning, shift swap in the app, vacation planning. | Booking Board [S27], Flexybox FlexyPlan [S28], ClubReady [S35], BRP [S33] |
| **Substitutions** | Automated substitute finding. | Mindbody [S3], NetGym add-on [S40] |
| **Roles/permissions** | Usually a few fixed roles: owner/admin, manager, front desk, instructor. Some have configurable groups and per-area restrictions on payments and reports. | Mindbody [S3], Glofox [S9], Wodify [S22], wger (trainer, gym admin, multi-gym admin) [S51] |
| **Certifications/documents** | **Largely missing from core suites.** Specialised add-ons handle it: CPR, insurance, and qualification documents marked required or optional, statuses (Submitted, Expired, Expiring Soon), and reminders from 90 days down to 1 day. | NetGym (integrates with ABC) [S40]. Competitor claims about Zen Planner, Mindbody, Wodify are UNVERIFIED [S41] |
| **Tasks** | Task assignment and tracking. | Glofox [S9], Clubworx [S34] |
| **Staff pricing** | Several vendors **do not charge per staff seat.** | Zen Planner ("never staff") [S20], Gymdesk, Clubworx, PushPress (unlimited staff) [S24][S25][S34] |

---

## 4. Open-source analysis

| Project | Stack | License | Activity (GitHub, 2026-11-12) | Scope | Worth learning from |
|---|---|---|---|---|---|
| **wger** (wger-project/wger) | Python/Django, REST API, Docker, mobile apps | AGPL-3.0 (code), CC licenses for data | ~7,266★, pushed 2026-11-10 | Workout, nutrition, and weight tracker with a small **gym module** | Data model in `wger/gym/models`: `Gym`, `GymConfig` (e.g. `weeks_inactive`), `GymAdminConfig`/`GymUserConfig`, `ContractType`, `ContractOption`, `Contract` (member FK, amount, payment interval yearly/half-yearly/monthly/biweekly/weekly/daily, start/end, active flag, address/profession/notes), `AdminUserNote`, `UserDocument`. Permissions: `gym_trainer`, `manage_gym`, `manage_gyms`. A good small reference for contracts and multi-gym scoping. AGPL affects reuse. [S50][S51] |
| **Gymie** (lubusIN/gymie) | PHP 8.4, Laravel 13, Filament 5, Livewire 4 | No license detected by GitHub API | ~509★, pushed 2026-11-07 | Gym and club admin: models `Member`, `Plan`, `Service`, `Subscription`, `Invoice`, `InvoiceTransaction`, `Enquiry`, `FollowUp`, `Expense`, `User` | A lead pipeline (Enquiry → FollowUp) and plan → subscription → invoice → transaction chain. No staff payroll or shift models. Missing license = legal uncertainty for reuse. [S53] |
| **LaraGym** (johndavedecano/laragym) | PHP/Laravel | none detected | ~365★, pushed 2026-05 | Members, billing cycles, subscriptions | Billing-cycle modelling [S54] |
| **GYM One** (mayerbalintdev/GYM-One) | PHP 7.4+, MySQL, Bootstrap | NOASSERTION | ~223★, pushed 2026-06 | Members, "tickets" (day pass, monthly), class sign-up, payments, admin roles | Simple pass/ticket model [S55] |
| **GymFlow** (MujahidAbbas/gymflow) | Laravel (multi-tenant SaaS) | MIT | ~4★ | Multi-tenant gym SaaS | Small; multi-tenant pattern only [S54b] |
| **Odoo gym modules** | Python/Odoo 16–19 | Varies by vendor (third-party) | n/a | Members, trainers, attendance, classes, workout/diet plans. Some depend on Odoo **Employees**, Contacts, Maintenance, Website. One uses role-based record rules: member sees own, trainer sees assigned members, manager sees all. | The "build on an ERP" option: HR, payroll localisation, accounting, and POS already exist. No official Odoo gym app found. [S56] |
| **ERPNext/Frappe gym apps** | Python/Frappe | ERPNext is GPL-3.0; apps vary | Gym Management Pro (Frappe forum); `gms` 0★, last push 2023 | Memberships, attendance, billing, QR check-in, branch-level roles, REST for a member portal. A Frappe Cloud listing includes trainer and trainer attendance. | Same ERP trade-off as Odoo. The Gym Management Pro repo URL from the forum returned 404 via the GitHub API (UNVERIFIED availability) [S57][S58] |

**Observations:**
- No open-source project found covers access-control hardware, real payroll, shift planning, or certification tracking together.
- The highest-star repositories that match "gym management" are mostly student or demo projects or stale ones, e.g. last push in 2020–2022 [S52].

---

## 5. Recurring pain points (from reviews)

1. **Price increases and add-on creep:**
   - Mindbody [S5].
   - Glofox "unexpected price hikes" [S11].
   - A third-party Reddit roundup reports that owners link hikes to private-equity ownership (UNVERIFIED) [S6].
   - Virtuagym: unclear pricing [S29].
   - Booking Board: modules add up [S31].
2. **Support quality:**
   - Slow or inconsistent support reported for Mindbody (3.8), PerfectGym (3.5), and Glofox (weeks-to-months fixes) [S5][S18][S11].
3. **Billing and payment reliability:**
   - Wrong billing dates, failed transactions, and confusing credit systems (Glofox) [S11].
   - Invoice bugs (PerfectGym) [S18].
4. **Complexity and UX:**
   - Daxko: "easy to get lost" [S36].
   - Mindbody: bugs and UX are the most common Reddit complaint category (UNVERIFIED) [S6].
5. **Lock-in and cancellation difficulty:** Mindbody [S5].
6. **Multi-location and family accounts:** reported weak spots in Glofox [S12].
7. **Employee side gaps:** this is an inference from the matrix, not from reviews. Gyms combine the core suite with separate tools: payroll (ADP, Danløn), staff scheduling and certifications (NetGym) [S3][S27][S40].

---

## 6. Lessons and implications (options, not decisions)

| Topic | Option A | Option B | Option C | Trade-offs |
|---|---|---|---|---|
| **Payroll scope** | Calculate pay only (rates, hours, commission) and export | Integrate with a payroll provider (e.g. Danløn, if Denmark) | Full statutory payroll in-house | Every product examined does A or B. C means legal and tax complexity that no gym vendor examined takes on. |
| **Pay-rate model** | Simple hourly | Rich rate rules (per class, per attendee, per program/location, caps, empty-class base, no-show rules, fixed salary) as in Wodify and TeamUp | — | B matches what instructor-based businesses use. It costs more to build and test. |
| **Staff scheduling** | Instructor assignment to classes only | Full shift planning (front desk, cleaning) with swaps and vacation, as in Booking Board and Flexybox | Integrate an external tool | Depends on how many non-instructor staff the centre has. |
| **Certifications** | Not in scope | Simple document records with expiry dates and reminders, as in NetGym | — | Rare in core suites. Could be a differentiator, or needless if the centre has few certified roles. |
| **Roles/permissions** | Fixed roles (owner, manager, front desk, instructor) | Configurable permission groups | Record-level rules (trainer sees only assigned members, as in Odoo modules and wger) | Fixed roles are simpler. Fine-grained rules bring GDPR benefits but more complexity. |
| **Access control** | Manual check-in | QR/app check-in | Hardware door integration (Kisi, JustFace, etc.) with automatic open/close by membership status | Unmanned 24/7 operation requires C. |
| **Platform base** | Greenfield custom build | Build on an ERP (Odoo/ERPNext) with gym modules | Fork or learn from wger/Gymie | ERP brings HR, accounting, and POS but adds framework lock-in and licensing (AGPL/GPL/LGPL). Greenfield gives full control at full cost. |
| **Membership rules** | Simple monthly or punch card | Rich rules (pause, freeze, minimum term, cancellation fees, add-ons, family accounts, multi-location validity) | — | Reviews flag family and multi-location gaps as pain points [S12]. |
| **Local integrations** | None | Accounting (e-conomic, Dinero, Billy), payment (MobilePay?), identity (MitID?) | — | Booking Board shows what a Danish market expects [S27]. MobilePay and MitID relevance is UNVERIFIED and not researched here. |

---

## 7. Open questions for the project owner

1. **Location and jurisdiction:** which country (Denmark?) will the centre operate in? This decides payroll, accounting, payment-method, ID, and GDPR integrations.
2. **Scale:** one location or several? Roughly how many members and staff?
3. **Type of centre:** classic open gym, class-based studio, or a mix with PT? This drives booking versus access-control emphasis.
4. **Employees:**
   - Which staff types exist: instructors, PTs, front desk, cleaning, managers?
   - How are they paid: hourly, per class, per attendee, salary, commission on sales or PT packages?
5. **Payroll:** should the system pay wages, or only calculate and export to an existing payroll provider or accountant?
6. **Certifications:** are there required certifications (first aid/CPR, PT licences) whose expiry must be tracked?
7. **Shift planning:** does the system need to plan non-instructor shifts, swaps, and vacation?
8. **Access:** unmanned 24/7 access with door hardware, or staffed reception only? Any existing hardware?
9. **Retail:** POS needed (drinks, supplements, merchandise)?
10. **Member app:** is a mobile member app required, or is a web portal enough?
11. **Integrations:** existing tools to keep (accounting, payroll, payment provider, EGYM/InBody equipment)?
12. **Build vs. base:** is building on an existing platform (Odoo/ERPNext) or open-source code acceptable, given licence implications (AGPL/GPL)?
13. **Purpose:** is the goal a system for one specific real centre, a learning project, or a product to sell? This affects multi-tenancy and pricing-model relevance.

---

## Sources

- [S1] Mindbody – pricing (US): https://www.mindbodyonline.com/business/education/blog/mindbody-pricing-united-states ; https://www.mindbodyonline.com/en-gb/business/pricing
- [S2] Third-party Mindbody pricing: https://pabau.com/blog/mindbody-pricing/ ; https://koalendar.com/blog/mindbody-pricing-costs ; https://thesalonbusiness.com/?p=45136
- [S3] Mindbody – Staff management: https://www.mindbodyonline.com/business/staff-management
- [S4] Mindbody/Booker help – Payroll report: https://booker.exceedlms.com/student/path/401805/activity/360325
- [S4b] Exercise.com support – staff view own commissions: https://www.exercise.com/support/how-staff-can-view-their-own-sales-commissions/
- [S5] Mindbody reviews: https://www.capterra.com/p/40229/Mindbody/ ; https://www.softwareadvice.com/club-management/mindbody-profile/reviews/ ; https://www.getapp.com/customer-management-software/a/mindbody/reviews/
- [S6] vibefam Reddit roundups (secondary; UNVERIFIED): https://vibefam.com/gym-management-software-reddit-2026/ ; https://vibefam.com/mindbody-reviews-reddit-2026/ ; https://vibefam.com/glofox-reviews-reddit-2026/
- [S7] ABC acquires Glofox: https://www.athleticbusiness.com/industry-press-room/press-release/15295152/abc-fitness-solutions-acquires-glofox ; https://www.fittechglobal.com/fit-tech-news/ABC-Fitness-Solutions-to-acquire-boutique-fitness-SaaS-company-Glofox/349877 ; https://www.irishexaminer.com/business/companies/arid-40932932.html
- [S8] Wodify blog – Glofox pricing (competitor): https://www.wodify.com/blog/glofox-pricing
- [S9] Glofox – Staff management: https://www.glofox.com/features/staff-management/
- [S10] G2 – ABC Glofox features: https://www.g2.com/products/abc-glofox/features
- [S11] Glofox reviews: https://capterra.com/p/136861/Glofox/ ; https://www.softwareadvice.com/gymnastics/glofox-profile/ ; https://www.g2.com/survey_responses/abc-glofox-review-4749267
- [S12] GetApp – Glofox reviews: https://www.getapp.com/recreation-wellness-software/a/glofox/reviews/page-2/
- [S13] ABC Ignite – features: https://ignite.abcfitness.com/features/enable-efficient-gym-management
- [S14] ABC × EGYM partnership: https://abcfitness.com/press-release/abcfitness-egym-partnership/
- [S15] Software Advice – Perfect Gym: https://www.softwareadvice.com.au/software/333876/perfect-gym ; https://www.softwareadvice.com/club-management/perfect-gym-profile/
- [S16] FitGap – PerfectGym: https://us.fitgap.com/products/022779/perfectgym
- [S17] Capterra – Perfect Gym: https://www.capterra.com/p/143255/Perfect-Gym/
- [S18] Perfect Gym reviews: https://www.capterra.com/p/143255/Perfect-Gym/reviews/ ; https://www.g2.com/products/perfectgym/reviews
- [S19] Daxko acquires FitnessForce (mentions Daxko Zen Planner): https://pulse2.com/daxko-acquires-fitnessforce-to-broaden-global-fitness-software-portfolio/
- [S20] Zen Planner – plans/tiers: https://zenplanner.com/?p=31452 ; https://zenplanner.com/?p=30617
- [S21] Zen Planner pricing (third-party): https://www.g2.com/products/zen-planner/pricing ; https://costbench.pages.dev/software/gym-management/zen-planner/
- [S22] Wodify help – Payroll Tracker: https://help.wodify.com/hc/en-us/articles/40782808325271-Creating-and-Assigning-Pay-Rates ; https://help.wodify.com/hc/en-us/articles/40981113568279-Manage-Default-Pay-Rates ; https://help.wodify.com/hc/en-us/articles/40800111523095-Manage-and-Run-Payroll ; https://help.wodify.com/hc/en-us/articles/41845754518423-Payroll-Tracker-Beta-Fixed-Pay ; https://help.wodify.com/hc/en-us/articles/40835715686167-Administrative-Hours ; https://help.wodify.com/hc/en-us/articles/360060876254-Timecard-Functions-to-Sign-in-Clock-in-a-Coach
- [S22b] Triib help – different coach rates per class: https://intercom.help/triib-inc/en/articles/4586441-how-to-pay-coaches-different-rates-for-different-classes
- [S23] Wodify FAQ / pricing: https://www.wodify.com/blog/wodify-faqs ; https://www.wodify.com/products/core/pricing ; https://costbench.com/software/gym-management/wodify/
- [S24] PushPress pricing: https://www.pushpress.com/pricing ; https://frontdeskreview.com/software/gym-management/pushpress/ ; https://www.costbench.com/software/gym-management/pushpress/free-plan/ ; https://toolradar.com/tools/pushpress/pricing
- [S25] Gymdesk pricing: https://gymdesk.com/blog/gym-management-software-cost ; https://www.costbench.com/software/gym-management/gymdesk/
- [S26] TeamUp pricing: https://goteamup.com/pricing
- [S26b] TeamUp – Pay rates: https://goteamup.com/resources/pay-rates
- [S27] Booking Board – features & pricing: https://www.bookingboard.io/features/ ; Danløn integration: https://bookingboard.io/en-GB/Integration/Details/49
- [S28] Flexybox – fitness centre system: https://flexybox.com/en/fitness-centre-2/
- [S29] Virtuagym pricing & reviews: https://business.virtuagym.com/our-prices ; https://www.getapp.com/recreation-wellness-software/a/virtuagym/
- [S30] Exerp: https://www.capterra.com/p/73511/Exerp/ ; https://us.fitgap.com/products/036465/exerp ; https://www.ezypay.com/partnerdirectory/exerp
- [S31] Danish market overview: https://europeanpurpose.com/tool/booking-board ; https://yogobooking.com/ ; https://www.healthclubmanagement.co.uk/health-club-management-suppliers/Clublead/31345913
- [S32] Zoezi (competitor blog; UNVERIFIED): https://vibefam.com/best-fitness-class-booking-software-in-sweden/ ; App Store: https://apps.apple.com/se/app/zoezi/id1447382946
- [S33] BRP Systems: https://brpsystems.com/industries/gym ; https://www.brpsystems.com/about-us ; https://www.brpsystems.com/productnews
- [S34] Clubworx: https://clubworx.com/pricing/ ; https://www.softwareadvice.com/martial-arts/clubworx-profile/
- [S35] ClubReady: https://capterra.com/p/104884/ClubReady/ ; https://www.g2.com/products/clubready/reviews?page=2 ; https://exercise.com/grow/how-much-does-clubready-cost
- [S36] Daxko Operations: https://daxko.com/products/daxko-operations ; https://www.capterra.com/p/148388/Daxko-Operations/ ; https://communityrecmag.com/how-the-ymca-of-the-pikes-peak-region-turned-technology-into-operational-advantage-with-daxko/
- [S37] EGYM: https://egym.com/us/cloud/management-software ; https://knowledge.egym.com/en/genius
- [S38] Exercise.com: https://www.softwareadvice.com/appointment-scheduling/exercise-com-profile ; https://www.fitbudd.com/best-exercise-com-alternative
- [S39] Xplor / ClubRight: https://www.xplortechnologies.com/es/node/1551 ; https://www.softwareadvice.co.uk/software/164107/triib ; https://www.krowdbase.com/software/clubright ; https://www.healthclubmanagement.co.uk/health-club-management-features/Software-Future-intelligence/37575
- [S40] NetGym – staff documentation & certifications: https://abc.netgym.com/benefits-and-features/staff-documentation-and-certifications/
- [S41] Exercise.com – gym software with license management (competitor, 2024): https://exercise.com/grow/best-gym-software-with-license-management
- [S46] Gibbs (NO): https://www.gibbs.no/en/abonnementssystem-treningssenter/
- [S47] Exerp × Fitness World case study: https://www.casestudies.com/company/exerp/case-study/fitness-world-customer-case-study
- [S50] wger: https://github.com/wger-project/wger
- [S51] wger gym models: https://github.com/wger-project/wger/tree/master/wger/gym/models
- [S52] GitHub search "gym management" (via GitHub API, 2026-11-12): https://github.com/search?q=gym+management&type=repositories&s=stars
- [S53] Gymie: https://github.com/lubusIN/gymie
- [S54] LaraGym: https://github.com/johndavedecano/laragym
- [S54b] GymFlow: https://github.com/MujahidAbbas/gymflow
- [S55] GYM One: https://github.com/mayerbalintdev/GYM-One
- [S56] Odoo Apps gym modules: https://apps.odoo.com/apps/modules/18.0/cerevantix_gym_management_system ; https://apps.odoo.com/apps/modules/17.0/odoo_gym_management ; https://apps.odoo.com/apps/modules/18.0/gym_mgmt_system ; https://apps.odoo.com/apps/modules/19.0/dev_gym_management
- [S57] Frappe: https://discuss.frappe.io/t/gym-management-pro-open-source-frappe-app-for-membership-attendance-billing/162381 ; https://frappecloud.com/marketplace/apps/gym_management ; https://github.com/AyshaHakeem/gms
- [S58] ERPNext (GPL-3.0): https://en.wikipedia.org/wiki/ERPNext

## Limitations of this research

- Pricing changes often and third-party sources conflict. Confirm prices with vendors.
- Fitnessbooking (NO) and SATS internal systems could not be documented.
- Reddit was only reachable through secondary roundups (vibefam), not original threads.
- Most review data comes from aggregators with small sample sizes for European products.
- No hands-on trials or demos were done.

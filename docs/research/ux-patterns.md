# UX/UI Patterns per User Surface: Research Report

**Status:** Research only. Nothing here is a decision. It presents evidence, options and questions for the project owner.
**Date:** 2026-11-12
**Method:** Web research on 2026-11-12, plus cross-reading of `similar-projects.md`, `fitness-domain-operations.md`, `integrations-and-architecture.md` and `compliance-legal-security.md` in this folder. Sources are cited inline as [U#] and listed in [Sources](#sources).

**How to read the evidence**

- **Primary:** law and court rulings, regulators (FTC, Danish Agency for Digital Government, Datatilsynet), standards bodies (W3C), platform documentation (Apple, Google/Android, WebKit), Danmarks Statistik.
- **Research/expert:** Nielsen Norman Group (NN/g) and Baymard Institute.
- **Vendor:** gym-software and scheduling-software help centres. These are good for *what exists* and weak for *how well it works*.
- **Anecdote:** blogs, single reviews, developer write-ups. Labelled as such.
- **UNVERIFIED:** a claim from one weak source, conflicting sources, or my own inference.
- Review-site pages (Capterra, G2) mostly blocked automated fetching (HTTP 403). Where I quote them, the text comes from search-result snippets and is marked UNVERIFIED unless another source confirms it.

---

## 1. Executive summary

1. **The cancellation flow is a legal UX artefact, not just a design choice.**
   - **Germany:** On 16 July 2026 the German Federal Court of Justice (BGH, I ZR 200/25, *vzbv v. FitX*) held that a gym's online cancellation confirmation page may contain **only** the cancellation fields and the submit button. Showing a "pause your contract" button there was unlawful [U1].
   - **Denmark:** Gyms may not require cancellation in person or by physical letter, per `fitness-domain-operations.md` [S11 there].
   - **EU:** A digital **withdrawal function** (the 14-day right of withdrawal) has been required for online contracts since 19 June 2026. Denmark implements it in forbrugeraftaleloven § 20 a, and it requires an immediate digital receipt [U2][U3].
   - **US:** The FTC rule was vacated in July 2025 [U5]. The FTC still sued LA Fitness over hard cancellation under ROSCA and the FTC Act [U4].
   - **Implication (option, not decision):** design "freeze/pause" as its own entry point, not as an interstitial on the cancel path. Whether that is required for a Danish-only gym is an open legal question (§9).
2. **Check-in speed is the member's most frequent interaction.** A developer's anecdote measured ~47 s to get from a gym app (PureGym) to a scannable QR, against ~3 s with a self-built Apple Wallet pass [U6, anecdote]. Gym vendors use rotating QR codes against screenshot sharing (expiry from ~25 s to 90 s) [U7][U8]. Wallet passes can be updated by push, but a static pass defeats rotation [U6].
3. **Mobile is the default device in Denmark.** 94% of men and 96% of women aged 16–74 used the internet via mobile/smartphone in 2025 [U9]. Ability to install an app falls with age: 92% of the population, 69% of 75–79-year-olds [U10, secondary summary of DST data]. I found **no reliable source** for the share of gym members who book via app. The widely repeated "82% prefer booking via app" figure could not be traced and should not be used (§2.1).
4. **Accessibility has a legal dimension, but scope is unclear for a single gym.**
   - WCAG 2.2 adds nine criteria [U11]. Three hit our surfaces directly:
     - **2.5.7 Dragging Movements (AA):** drag-and-drop rosters need a single-pointer alternative.
     - **2.5.8 Target Size (AA):** 24×24 CSS px minimum.
     - **3.3.8 Accessible Authentication (AA):** no cognitive tests at login.
   - The EAA covers e-commerce services and self-service terminals such as check-in machines. However, **microenterprises providing services** (<10 staff and ≤€2M turnover/balance sheet) are exempt from the service requirements [U12][U13]. Whether this gym qualifies is an open question.
5. **Staff-side UX evidence is thinner than member-side.**
   - **Planday** is Danish workforce software. Its documented patterns are the reference set: open shifts, swap/handover/sell with colleague and then manager confirmation, and drag-and-drop with Alt-to-copy [U14][U15].
   - Danish law has required an "objective, reliable and accessible" working-time registration system since 1 July 2024 [U16]. Datatilsynet says **fingerprints for ordinary time registration are not lawful** [U17]. That constrains time-clock UX.
6. **Design systems for an AI-built UI.**
   - The strongest documented signals point to **shadcn/ui (Tailwind + Radix/Base UI)**:
     - It markets itself as "AI-Ready" and ships open code [U18].
     - It has an official MCP server that supports Claude Code [U19].
     - It is v0's default stack [U20].
   - **Caveats:**
     - I found no controlled study of agent output quality per design system. Comparative claims are anecdote or vendor marketing [U21][U22].
     - shadcn changed its default primitive library from Radix to Base UI in July 2026 [U23].
     - MUI publishes an `llms.txt` [U24], so it is not agent-hostile.
7. **Known UX complaints about existing gym software:** separate logins and booking glitches (Mindbody, UNVERIFIED), a slow mobile app (Planday, UNVERIFIED snippet), "easy to get lost" (Daxko), and inaccurate reports (Glofox) [U25][U26]. Also see `similar-projects.md` §5.

---

## 2. Cross-cutting findings

### 2.1 Mobile-first: what the numbers do and do not say

- **Denmark:** 94% (men) and 96% (women) of 16–74-year-olds go online via mobile or smartphone (DST SDG indicator 5.b.1, 2025). DST notes that its measure differs from the UN "phone ownership" definition [U9].
- **Older members:** the share who know how to install an app falls from 92% overall to 69% at ages 75–79 (Ældre Sagen's summary of DST *It-anvendelse i befolkningen 2025*) [U10]. This matters if the gym has senior members. A web or PWA fallback and front-desk-assisted flows reduce dependence on app installs (inference).
- **Gym-specific booking-channel data:**
  - "82% of members prefer booking classes via a mobile app" (attributed to Glofox) could not be traced to a primary survey. One source says the 82% figure is not about app preference at all. **Do not use** [U27].
  - ABC Fitness vendor data claims app bookers churn ~30% less than front-desk bookers. **UNVERIFIED** (vendor, methodology unknown, correlation not causation) [U27].
  - Global Payments: 31% of UK adults book fitness classes or gym visits via a platform. This is a general population figure, not gym members [U27].

### 2.2 Accessibility (WCAG 2.2, EAA)

**New WCAG 2.2 criteria and where they bite** [U11]:

| SC | Level | Relevance here |
|---|---|---|
| 2.4.11 Focus Not Obscured (Min) | AA | Sticky headers/footers in the booking app and back office must not hide the focused element. |
| 2.5.7 Dragging Movements | AA | Roster drag-and-drop and calendar drag-to-move need a click/tap alternative (e.g. "Move to…" menu). |
| 2.5.8 Target Size (Min) | AA | ≥24×24 CSS px. NN/g recommends ≥1 cm × 1 cm physical targets on touch [U28], which is stricter. Relevant for kiosks and the trainer's attendance list. |
| 3.2.6 Consistent Help | A | Help/contact link in the same place across the member portal. |
| 3.3.7 Redundant Entry | A | Don't make members re-type data in the sign-up funnel (e.g. billing address = home address). |
| 3.3.8 Accessible Authentication (Min) | AA | No puzzle CAPTCHA or memorise-and-type steps at login. Allow password managers and paste, and passkeys/magic links. |

**EAA / Danish law:**
- EAA services requirements have applied since 28 June 2025, and Denmark transposed them in Lov nr. 801 of 7 June 2022 [U13]. Coverage includes e-commerce services (websites and apps that sell services) [U12].
- The Irish regulator's guidance lists **check-in machines** among the covered self-service terminals. It also says EAA obligations **do not apply to microenterprises providing services** [U12].
- Sources disagree on whether the microenterprise test is "fewer than 10 staff **and** ≤€2M" or "**or**" [U12]. Check against Directive (EU) 2019/882 Art. 3. **UNVERIFIED** until the primary text is read (EUR-Lex returned empty pages to my fetch).
- `compliance-legal-security.md` (§1 item 10) already covers EAA. This section adds only the UX consequences.

### 2.3 Danish/English localisation

- **Formats:**
  - Danish dates are traditionally DD.MM.YYYY. Leading zeros are common.
  - Written time is almost always 24-hour.
  - Weeks start on Monday.
  - Week numbers (ISO 8601) are "very common" in business writing and speech, less so in private life [U29]. The source is Wikipedia, so treat this as **UNVERIFIED**.
- **Candidate implication:** week numbers in staff rosters and the manager schedule view. Monday-first calendars everywhere.
- **Legal strings that need exact wording:** cancellation and withdrawal buttons. DI's examples are "Fortryd køb" and "Bekræft fortrydelse" [U3]. The German case law shows that small or grey cancel labels can fail legibility tests (LG München I, 12 O 4127/23) [U30].
- **Not researched:** Danish text-expansion ratios versus English, and terminology (e.g. *binding*, *opsigelse*, *pause/frys*). These are glossary items for the owner. See the glossary in `fitness-domain-operations.md`.

### 2.4 Deceptive patterns: definitions used by regulators and researchers

- NN/g defines a deceptive pattern as one that benefits the company "by deceiving, misdirecting, shaming, or obstructing the user's ability to make another (less profitable) choice" [U31]. Related NN/g concepts:
  - "sludge" (the opposite of a nudge)
  - "sneaking": forced continuity, hidden costs, sneak into basket [U31]
- The FTC's 2022 staff report *Bringing Dark Patterns to Light* puts cancellation obstacles and "false hierarchy" in its taxonomy. False hierarchy means large "keep" against small grey "cancel". Its guidance is that cancelling should be as easy as signing up [U32]. Secondary summaries only; I did not read the report itself.
- **Denmark:**
  - Forbrugerombudsmanden enforces against subscription traps and pre-ticked boxes under markedsføringsloven [U33].
  - Recent guidance requires the price, billing frequency, binding period and how to cancel to be visible **before purchase** [U33].
  - I found no dedicated Danish *dark patterns* guidance document (UNVERIFIED absence).
- **EU Digital Fairness Act:** the proposal was expected around Q4 2026. Sources conflict on whether it has been published. Its expected themes are "cancel as easily as you subscribed" and a possible general dark-pattern ban [U34]. **UNVERIFIED status.**

---

## 3. Surface 1: Member self-service

### 3.1 Sign-up / onboarding funnel

- **Don't wall value behind registration.** NN/g: login walls "stop users in their tracks". Apps that demand registration before proving their worth cause users to abandon. Guest flows and optional registration increase completion [U35]. For a gym this suggests letting prospects browse the timetable and prices **before** creating an account (candidate).
- **Baymard checkout data** (e-commerce, US shoppers; transferable only by analogy, UNVERIFIED for gyms) [U36]:
  - 18% of abandoners left because the site required an account.
  - 17% left because checkout was too long or complicated.
  - 12% left because the total cost was not visible up front.
  - Average cart abandonment is 70.22% across 50 studies.
- **Price transparency is a legal requirement in Denmark**, not just UX. Price, frequency, binding period and how to cancel must be visible before purchase [U33]. "Ingen binding" may only be advertised if cancellation is effective from one day to the next (`fitness-domain-operations.md` S11).
- **Mobile form basics (NN/g):**
  - Labels above fields, no placeholder-as-label.
  - Autofill working for name, address, email, phone, password and card.
  - Correct input types. Radio buttons instead of drop-downs for 2–3 options [U37].
  - Camera scan for cards/IDs as an option [U37].
- **Health questionnaire (PAR-Q+) and waiver:** this is GDPR special-category data. See `compliance-legal-security.md`. UX implication (inference): keep it a separate, explained step with explicit consent, not a buried checkbox.
- **14-day withdrawal function:** for contracts concluded online, there must be a clearly findable digital withdrawal function and an immediate digital receipt (DK § 20 a, from 19 June 2026) [U2][U3]. Applying this to gym memberships with immediate service start (partial use, proportional payment) is a legal question (§9).

### 3.2 Class booking and waitlists

**Vendor patterns** [U38][U39]:
- **Auto-add:** waitlisted members are promoted in join order as spots open.
- **First-to-claim:** an SMS goes out and the first responder gets the spot (Mindbody, needs two-way SMS).
- **Waitlist lock window:** auto-promotion stops close to class time. The promoted member gets a notification that includes the cancellation policy.
- **Planned at Mindbody:** member priority over non-members, early booking windows, and "waitlisted but must buy a session first".
- Glofox documents auto-promotion together with late-cancel and no-show policies [U39].

**Design rule from a practitioner source:** stop auto-promotion *before* the late-cancel window opens, so a promoted member can still cancel without penalty [U39]. This is a fairness rule, and arguably avoids a trap (UNVERIFIED as a legal requirement).

**Candidate screen states** (inference):
- full
- waitlist position N
- promoted (confirm / auto-booked)
- late-cancel warning showing the fee before confirming
- no-show penalty explanation

### 3.3 Payments and invoices

- Payment-method options for Denmark (cards, Betalingsservice, Vipps MobilePay Recurring) are covered in `integrations-and-architecture.md`.
- UX evidence on member-facing failed-payment ("dunning") screens was **not found** in primary/expert sources. This is a research gap.
- Billing glitches are a top complaint category in existing gym software. Glofox has wrong billing dates and a confusing credit system. PerfectGym has invoice bugs (`similar-projects.md` §5). Candidate implication: an invoice history listing every charge with its reason, and a "why was I charged" detail view (inference).

### 3.4 Freeze and cancel flows

**Legal evidence:**
- **Germany, BGH I ZR 200/25 (16 July 2026), FitX** [U1]:
  - § 312k(2) sentence 3 BGB *exhaustively* governs the confirmation page.
  - No further "offers, information or hints" may appear there.
  - An orange "Vertrag im Selfservice pausieren" button was unlawful.
- **Germany, LG München I (2023):** a "Kündigen" button smaller than body text, in grey on white, was not sufficiently legible [U30]. LG Koblenz (2023) took a more lenient view on alternatives [U30]. The BGH has now settled the confirmation-page point.
- **Denmark:** no in-person or physical-letter-only cancellation (`fitness-domain-operations.md` S11).
- **US:** FTC v. LA Fitness operators (complaint filed 20 Aug 2025, allegations not findings) [U4]. The FTC alleges:
  - cancellation only in person with one specific employee at limited hours, or by mail
  - login required to print the form
  - online cancellation "buried" and impossible in the mobile app
- **US:** the FTC Negative Option ("click-to-cancel") Rule was vacated by the 8th Circuit on 8 July 2025 on procedural grounds. ROSCA, Section 5 and state laws still apply [U5].

**Pattern options (not decisions):**
- **A. "Cancel" as a direct, labelled link** in account settings. A one-page form with only the required fields and submit, then an immediate confirmation with the effective date and a receipt by email. Freeze/pause is offered **elsewhere** (its own menu item) and **not** on the cancel path. This is the safest reading of BGH and the FTC. *Trade-off:* no "save" offer at the moment of intent.
- **B. Retention offer before the cancel form**, as one dismissible screen. This is common in subscription UX. It is risky under the BGH logic if the user has already clicked "cancel", and is "obstruction" under the NN/g/FTC taxonomy if it is repeated or confusing [U1][U31][U32]. *Whether Danish law prohibits this is unknown (§9).*
- **C. Exit survey after cancellation is confirmed** (optional). It does not block the cancellation (inference).

**Freeze UX:** freeze rules (minimum/maximum length, fee, medical reasons) belong to the domain. Candidate screen: "Pause membership" with date pickers, a fee preview and the resulting billing schedule.

### 3.5 Digital membership card, QR, wallet passes

**Options:**

| Option | Evidence | Trade-offs |
|---|---|---|
| In-app QR, rotating | PerfectGym and Magicline document dynamic QR. Clubwise expires the code ~25 s after tap. ClubRight caps it at 90 s [U7][U8] | Defeats screenshot sharing. Does **not** stop photographing a live screen [U8]. Needs app launch time to be fast [U6] |
| Static QR / member number | — | Shareable. Vendors call screenshot sharing a known leak [U7] |
| Apple Wallet pass | Apple documents "generic" pass style for "a gym membership card", or storeCard for membership/loyalty [U40] | Fastest at the turnstile (~3 s from Watch in the anecdote) [U6]. Dynamic codes need a backend to push pass updates. Update rate limits **UNVERIFIED** [U6] |
| Google Wallet generic pass | Google Wallet API supports generic passes with barcode/QR [U41] | Same pattern as Apple. Separate integration |
| NFC / phone-as-key | Not researched | Hardware-dependent |

- **Check-in vs identity:** a scan records that *someone* scanned. It doesn't verify identity [U8]. Front-desk photo display at check-in is a common complement (§4).
- **Hardware:** some access-control hardware supports rotating codes and some does not [U8]. That makes the door hardware a constraint on card UX.

### 3.6 PWA vs native (UX angle)

- **iOS web push** works only for web apps **added to the Home Screen** (iOS 16.4+), and permission must follow a direct user gesture [U42].
- Apple announced removal of Home Screen web apps in the EU for iOS 17.4 (DMA), then **reversed** the decision before release [U43].
- `integrations-and-architecture.md` notes some 2026 sources still report EU PWA push problems (**conflicting**).
- **UX consequences** (inference):
  - A PWA needs an "Add to Home Screen" onboarding step on iOS before push can work, which is extra friction.
  - Wallet passes give a fast card without either an app or a PWA.
  - Native apps get reliable push, wallet integration and widgets, at the cost of app-store distribution.

---

## 4. Surface 2: Front desk / reception

**Findings:**
- **Fast lookup and identity:**
  - Gym Assistant shows the **member photo** on every check-in and puts alerts at the top of the record [U44].
  - GymMaster blocks entry above a set unpaid-balance threshold [U44].
  - Check-in software generally flags expired, frozen or unpaid memberships at the door [U44].
  - Vendor sources; I found no NN/g/Baymard study of gym front desks.
- **Self check-in kiosk:**
  - Mindbody's Check-In app turns a tablet into a self check-in kiosk. Settings are password-protected, and a **chime alerts staff when a client has insufficient funds** [U45].
  - Clients can scan barcodes or enter a client ID [U45].
  - Wodify's kiosk shows attendance and who hasn't shown up [U46].
- **Kiosk lock-down is OS-level:**
  - **iPad:** Guided Access (passcode to exit) or Single App Mode / Autonomous Single App Mode on supervised devices via MDM [U47].
  - **Android:** lock task mode. Only apps allowlisted by a device policy controller (DPC) can run. Status bar, Home and Overview are hidden [U48].
  - Screen pinning on Android can be exited by the user, so it is not a real kiosk [U48].
- **Public touchscreen guidance:**
  - NN/g's large-touchscreen interview: uncluttered pages, natural gestures, signage that the screen is touch-enabled [U49].
  - Kiosk users are often first-timers and distracted [U49, vendor sources cited there].
  - WCAG 2.5.8 and NN/g's 1 cm target guidance apply [U11][U28].
  - If the kiosk is an EAA "self-service terminal", product-side requirements may apply even if the service is exempt (UNVERIFIED for our case) [U12].
- **POS:** Not researched in depth beyond `fitness-domain-operations.md` §6. A gap: no expert UX sources found for gym POS specifically.

**Candidate patterns (inference):**
- one global search box: name, phone, member no., or scan
- result card with photo, status badge and alerts (unpaid, frozen, waiver missing, birthday)
- primary action "Check in"
- secondary actions: sell, book, take payment

---

## 5. Surface 3: Trainer / instructor

**Findings:**
- **Wodify Coach View** (mobile) [U46]:
  - sign athletes into class
  - see booking history and basic profile
  - change coach, adjust class limit
  - message attendees (members can reply but not start a chat)
  - Coach View needs coach permissions [U46]
- **Wodify Kiosk Coachboard** shows attendance, no-shows and the final count [U46]. Its Whiteboard is a daily results leaderboard (Perform add-on) [U46].
- **Coach clock-in:** Wodify has "Timecard" functions to sign in or clock in a coach [U46]. This links attendance-taking to pay calculation (per-class pay, `fitness-domain-operations.md` §7.2).
- **Client notes:** these may contain health data (injuries, PAR-Q answers), which is GDPR special category. The UX implications are role-based visibility and an explicit "health note" type separate from general notes (inference). Legal basis is in `compliance-legal-security.md`.

**Candidate patterns (inference):**
- "Today" list of my classes and sessions
- class roster with large tap targets, one tap per attendee: present / no-show / late
- walk-in add
- offline tolerance (gyms often have poor signal; the PureGym anecdote mentions poor cellular signal [U6])

---

## 6. Surface 4: Manager / admin back office

### 6.1 Dashboards and KPI reporting

- **NN/g** distinguishes *operational* dashboards (time-sensitive, e.g. today's check-ins and class fill) from *analytical* ones (deeper investigation, e.g. monthly churn) [U50]. Its recommendations:
  - encode quantity with **length/2D position** (bars, lines)
  - avoid pies, donuts, gauges and 3D
  - don't encode magnitude with colour [U50]
- **Data tables:** NN/g frames table design around four tasks: find records, compare, view/edit one row, act on records [U51]. Most back-office screens (members, invoices, staff) are tables.
- **KPI list:** see `fitness-domain-operations.md` §10 (retention, churn, attendance, etc.).
- **Known complaint:** Glofox "reports aren't always accurate" [U26, single Capterra review, UNVERIFIED]. Trust in numbers is a UX issue. A candidate pattern is to show the definition and data freshness on every KPI tile (inference).

### 6.2 Staff scheduling UI (drag-and-drop rosters)

- **Planday (Danish) patterns** [U14][U15]:
  - Managers drag shifts onto days or employees. Alt/Option+drag duplicates a shift.
  - Dragging an open shift onto an employee assigns it.
  - Pending requests (swaps, shift requests) sit in a separate queue with department filter.
  - Planday's conflict detection on drag is claimed by a third party only (**UNVERIFIED**) [U15].
- **NN/g drag-and-drop guidance** [U52]:
  - visible grab handles and cursor change
  - a distinct "lifted" item and a preview of the result before drop
  - magnetised drop zones
  - **keyboard** support (Tab to the handle, Space to grab, arrows to move)
  - screen-reader announcements
  - on touch: ~1 cm free space, haptics, and a delay to distinguish tap from drag
  - on touch, prefer a menu if no evidence shows users expect drag
- **WCAG 2.2 SC 2.5.7** requires a single-pointer alternative to dragging [U11]. A candidate pattern is a "Move/Assign to…" action on every shift.
- **Danish specifics:** week numbers in roster headers [U29, UNVERIFIED], Monday-first weeks, and working-time registration obligations (§7).

---

## 7. Surface 5: Employee self-service

**Findings:**
- **Shifts, open shifts, swaps (Planday)** [U14][U53]:
  - Open shifts appear in an "Open shifts" widget. They are visible only to staff in the right department/group with the right skills and no conflict.
  - Swap, handover or sell to colleagues in the same group/department. The shift stays with the original employee until the colleague **and** a manager confirm.
  - The admin may need to enable the feature.
- **Reported complaints (Planday, Capterra snippets, UNVERIFIED)** [U25]:
  - The app sometimes takes 30–45 s to open.
  - Some tasks, e.g. booking holidays, work better on web than in the app.
  - Candidate requirement: launch-to-schedule speed, and feature parity between web and mobile.
- **Time clock:**
  - **Danish law:** working-time registration has been mandatory for all employers since 1 July 2024. The system must be **objective, reliable and accessible**, and accessible to each employee during employment. Data is stored for 5 years. There is freedom of method, and some employees ("self-organisers") are exempt [U16]. Details are in `compliance-legal-security.md`.
  - **Datatilsynet (2019 guidance):** fingerprints for ordinary time registration are **not** compliant, and employee consent does not fix this because of the power imbalance [U17]. A 2022 decision reportedly accepted explicit consent for facial recognition (secondary summary, UNVERIFIED) [U17].
  - **Vendor patterns:**
    - Deputy kiosk photo on "Start Shift", shown on the timesheet
    - opt-in Face Unlock after a PIN on first use
    - geofencing for mobile punches (third-party claim) [U54]
    - Clockify advises informing staff about photo capture [U54]
  - **Candidate options:**
    - shared tablet with PIN
    - PIN + photo
    - personal phone with geofence
    - auto-derive hours from roster + manager approval
    - Biometrics look legally fragile in Denmark [U17]
- **Availability and payslip inputs:** availability entry and pay-input review (hours, per-class counts, commission) were not covered by expert UX sources. `similar-projects.md` §3.2 notes that most gym suites produce a *pay calculation report* and do not run payroll. Danløn integration is in `integrations-and-architecture.md`. Candidate screen: "My hours this period", with classes taught, hours clocked, and flags for discrepancies before payroll export (inference).

---

## 8. Key screens per persona (candidates, not decided)

**Prospect / member** (mobile-first)
1. Public timetable + prices (no login) [U35]
2. Membership picker with full price, frequency, binding period and how to cancel shown up front [U33]
3. Sign-up (account, personal data, payment mandate), with progress indicator and autofill [U37]
4. Health questionnaire / waiver (separate consent step)
5. Home: next booking, membership card shortcut
6. Membership card (rotating QR and/or "Add to Wallet") [U6][U7][U40]
7. Timetable → class detail → book / join waitlist / cancel (with late-cancel fee preview) [U38]
8. My bookings and waitlist positions
9. Invoices and payments (history, receipts, update payment method, failed-payment fix)
10. Membership: pause/freeze (own entry point)
11. Membership: cancel (direct, minimal form, confirmation + receipt) [U1]
12. Withdrawal (14-day) function for online contracts [U3]
13. Profile, consents, notification settings, language (DA/EN)

**Front desk**
1. Global search / scan with result card (photo, status, alerts) [U44]
2. Check-in confirmation / denial with reason
3. Member profile (tabs: membership, bookings, invoices, notes, documents)
4. New member / walk-in sale
5. POS / cart / payment
6. Class roster for today (manual check-in)
7. Self check-in kiosk mode (locked device) [U45][U47][U48]

**Trainer / instructor**
1. My schedule (today / week)
2. Class roster → attendance taking [U46]
3. PT client list → client profile with notes (health notes separated, role-restricted)
4. Clock in/out (if hourly) [U46]
5. Message attendees [U46]

**Manager / admin**
1. Operational dashboard (today: check-ins, class fill, failed payments, staff on shift) [U50]
2. Analytical reports (retention, churn, revenue, attendance) [U50]
3. Members table (filter, bulk actions) [U51]
4. Memberships/products and pricing setup
5. Class timetable builder
6. Staff roster (drag-and-drop with "Move to…" alternative) [U11][U14][U52]
7. Requests queue (swaps, time-off, open-shift claims) [U14]
8. Timesheets approval and payroll export [U16]
9. Settings: roles/permissions, policies (late cancel, freeze rules), kiosk devices, legal texts

**Employee (non-trainer)**
1. My shifts
2. Open shifts → request [U53]
3. Swap / hand over shift [U14]
4. Availability and time-off requests
5. Clock in/out [U16][U54]
6. My hours / pay inputs for the period
7. Documents (contract, certifications with expiry)

---

## 9. Anti-patterns (with evidence)

| Anti-pattern | Evidence |
|---|---|
| Retention/pause offer on the cancellation confirmation page | BGH I ZR 200/25 [U1] |
| Cancel button smaller/greyer than body text; "false hierarchy" | LG München I [U30]; FTC 2022 report [U32] |
| Cancellation only in person, by mail, via one staff member, or impossible in the app | FTC v. LA Fitness (allegations) [U4]; Danish rule (`fitness-domain-operations.md` S11) |
| Hidden subscription terms; pre-ticked boxes | Forbrugerombudsmanden enforcement [U33]; NN/g "sneaking" [U31] |
| Forced registration before seeing timetable/prices | NN/g login walls [U35]; Baymard 18% [U36] |
| Slow, cluttered path to the entry QR (feeds, pop-ups before the code) | PureGym anecdote, 47 s [U6] |
| Static QR shareable by screenshot | Vendor docs [U7][U8] |
| Drag-only interactions | WCAG 2.5.7 [U11]; NN/g [U52] |
| CAPTCHA puzzles / no paste in password fields | WCAG 3.3.8 [U11] |
| Pie/donut/gauge KPI tiles; colour-encoded magnitudes | NN/g [U50] |
| Fingerprint time clock | Datatilsynet 2019 [U17] |
| Mobile app slower than web, or missing web features | Planday reviews (UNVERIFIED) [U25] |
| Waitlist auto-promotion inside the late-cancel window | Practitioner guidance (UNVERIFIED) [U39] |

---

## 10. Design-system options for an AI-agent-built UI

| Option | Evidence for agent-friendliness | Caveats |
|---|---|---|
| **shadcn/ui** (Tailwind; Radix or Base UI primitives) | Docs: "AI-Ready … open code and a consistent API let models read, understand, and generate components"; "not a component library" (code is copied into the repo) [U18]. Official MCP server; documented clients include **Claude Code** (`shadcn mcp init --client claude`), Cursor, VS Code, Codex [U19]. v0's default stack is Next.js, React, TS, Tailwind, shadcn/ui [U20]. Has RTL support and registries [U23] | Since **July 2026** new projects default to **Base UI**, and Radix is still supported [U23][U20]. v0 docs: the model is "trained on the default implementations … and may struggle with any customizations" [U20]. "Generic AI look" is a common critique [U21, anecdote] |
| **Radix Primitives** (headless) | Underlies many shadcn installs. Maintained by WorkOS. Changelog active into 2026 [U23] | Reports that original maintainers left (secondary) [U23]. Headless only, so you style everything |
| **Base UI** (headless, from the MUI team) | v1 in late 2025. "First-class shadcn/ui integration" [U23] | Newer. Less training-data exposure (inference, UNVERIFIED) |
| **MUI / Material UI** | Publishes `llms.txt` docs index [U24]. Community MCP servers exist. No official one found [U24]. Mature component set (date pickers, data grid) | Agent-quality comparisons vs shadcn are vendor/anecdotal. One team's migration case study blames inconsistency on lack of a documented design system more than on MUI itself [U22] |

**Evidence strength:**
- I found **no controlled benchmark** comparing agent output quality by design system.
- The "LLMs default to Tailwind/shadcn because of training-data frequency" argument is plausible but **untested** [U21].
- The best-documented facts are capability facts: MCP server, llms.txt, v0 default.

---

## 11. Open questions for the project owner

1. **Jurisdiction:** Is the gym Danish-only? Will members sign up online (triggering the § 20 a withdrawal function and online-cancellation rules)? Is there any German or US exposure, where the BGH and FTC findings apply directly?
2. **Retention offers:** Do you want any "save" step (pause/discount) in the cancel journey, given the BGH ruling against offers on the confirmation page? Or should pause be a separate menu item only? Should a Danish consumer lawyer review this?
3. **EAA scope:** Does the business meet the EAA microenterprise exemption (<10 staff and ≤€2M turnover/balance)? Will you target WCAG 2.2 AA anyway?
4. **Member app form:** native app, PWA, web only, or wallet pass only? Are push notifications (waitlist promotion) a must-have, given that iOS PWA push requires Add-to-Home-Screen?
5. **Door/check-in hardware:** What scanner/turnstile exists or is planned? Does it support rotating QR codes? Is a staffed desk always present (photo check), or are there unstaffed hours?
6. **Membership card:** Apple/Google Wallet pass, in-app rotating QR, or both? Is screenshot sharing a concern for this gym?
7. **Waitlist policy:** Auto-add or first-to-claim? What lock window? Must it end before the late-cancel window?
8. **Languages:** Danish and English at launch for all surfaces, or English only for staff? Who owns legal wording (cancel/withdrawal button texts)?
9. **Senior/less-digital members:** Should every member action also be doable at the front desk on the member's behalf?
10. **Front desk hardware:** desktop, tablet, or both? A self check-in kiosk? Locked down by MDM?
11. **Time clock method:** PIN, photo, geofenced phone, or roster-derived with approval? Biometrics excluded?
12. **Roster tooling:** Build scheduling, or integrate an existing tool (e.g. Planday) that staff may already use?
13. **Trainer notes:** Will trainers record health information? Who may see it?
14. **Dashboards:** Which 5–10 KPIs does the manager look at daily versus monthly?
15. **Design system:** Is there an existing brand/visual identity to apply? Any preference between shadcn/ui (Base UI or Radix), MUI, or other? Is a "looks like every AI app" aesthetic acceptable for v1?

---

## 12. Research gaps

- Primary text of EAA Art. 3/4(5) (EUR-Lex fetch returned empty). Danish § 20 a statutory text. Whether DK has an equivalent of the German confirmation-page rule.
- Status and text of the EU Digital Fairness Act proposal.
- Gym-member booking-channel statistics from a primary survey.
- Expert (NN/g/Baymard) research on gym front desks, POS and dunning screens.
- Apple Wallet pass update rate limits for rotating codes.
- G2/Capterra full review text (blocked). App Store review mining for Danish gym apps (Fitness World, PureGym DK, SATS).
- A hands-on benchmark of agent-generated UI per design system.

---

## Sources

- [U1] BGH I ZR 200/25 (16 Jul 2026), vzbv v. FitX, beck-aktuell: https://www.beck-aktuell.de/heute-im-recht/rechtsprechung/bgh-IZR200/25-kuendigung-online-button-fitx-2026-07-16 ; WBS Legal: https://www.wbs.legal/allgemein/bgh-urteil-zum-kuendigungsbutton-keine-ablenkungsmanoever-auf-der-bestaetigungsseite-87491/ ; Noerr: https://www.noerr.com/de/insights/kuendigungsbutton-im-onlinevertrieb-neue-rechtsprechung-des-bgh
- [U2] EU Directive 2023/2673 withdrawal function, Greenberg Traurig: https://www.gtlaw.com/en/insights/2026/5/eu-consumer-law-new-withdrawal-button-requirements-for-online-contracts ; iubenda: https://www.iubenda.com/en/blog/the-new-online-withdrawal-function-what-eu-directive-2023-2673-means-for-your-business/ ; Van Bael & Bellis: https://vbb.com/wp-content/uploads/2026/06/8-06-26-The-online-withdrawal-button.pdf
- [U3] Dansk Industri, forbrugeraftaleloven § 20 a from 19 Jun 2026: https://www.danskindustri.dk/vi-radgiver-dig/ecommerce/nyhedsarkiv/nyheder/2026/06/ny-regel-rammer-webshops-fortrydelsesretten-skal-vare-synlig-og-digital-fra-19.-juni-2026
- [U4] FTC press release, LA Fitness suit (Aug 2025): https://ftc.gov/news-events/news/press-releases/2025/08/ftc-sues-la-fitness-making-it-difficult-consumers-cancel-gym-memberships ; Spectrum News: https://spectrumnews1.com/ca/la/business/2025/08/20/ftc-sues-la-fitness-operators-for--exceedingly-difficult--gym-cancellation-policies
- [U5] Eighth Circuit vacates Negative Option Rule, Covington: https://cov.com/en/news-and-insights/insights/2025/07/eighth-circuit-vacates-ftc-negative-option-rule ; Kelley Drye: https://www.kelleydrye.com/viewpoints/blogs/ad-law-access/eighth-circuit-vacates-ftcs-negative-option-rule ; Womble Bond Dickinson: https://www.womblebonddickinson.com/us/insights/alerts/ftcs-click-cancel-rule-vacated-remember-it-was-not-alone
- [U6] D. Robinin, "How I accidentally became PureGym's unofficial Apple Wallet developer" (anecdote): https://drobinin.com/posts/how-i-accidentally-became-puregyms-unofficial-apple-wallet-developer/
- [U7] PerfectGym dynamic QR help: https://support.perfectgym.com/hc/en-001/articles/28069566715793-How-to-configure-dynamic-QR-codes-for-member-check-in ; Magicline dynamic QR API: https://developer.sportalliance.com/apis/magicline/usecases/dynamic-qr-access-code ; Clubwise: https://www.clubwise.com/?p=135 ; IssuePass (vendor): https://issuepass-marketing.pages.dev/en-GB/articles/dynamic-barcodes-wallet-passes
- [U8] ClubRight, credential sharing / access abuse: https://help.clubright.co.uk/en/articles/13619607-membership-credential-login-sharing-access-control-abuse ; Linkbreakers: https://linkbreakers.com/help/article/how-gyms-and-fitness-studios-use-qr-codes-for-member-check-in-and-engagement
- [U9] Danmarks Statistik, SDG 5.b.1: https://www.dst.dk/da/Statistik/temaer/SDG/globale-verdensmaal/05-ligestilling-mellem-koennene/delmaal-b
- [U10] Ældre Sagen, IT-anvendelse 2025 (summary of DST): https://billeder.aeldresagen.dk/cdn/d9DRWXKb4Zdu/2026-it-anvendelse-2025.pdf
- [U11] W3C, What's New in WCAG 2.2: https://www.w3.org/WAI/standards-guidelines/wcag/new-in-22/
- [U12] CCPC (Ireland), EAA guidelines for microenterprises: https://www.ccpc.ie/business/enforcement/accessibility/european-accessibility-act-guidelines-for-microenterprises/ ; Level Access: https://www.levelaccess.com/compliance-overview/european-accessibility-act/ ; Taylor Wessing: https://www.taylorwessing.com/en/interface/2025/accessibility/the-european-accessibility-act-a-win-win-piece-of-regulation ; Disability World (conflicting and/or): https://www.disabilityworld.org/articles/european-accessibility-act-guide/
- [U13] Danish implementation: DI Handel: https://www.danskindustri.dk/brancher/di-handel/nyhedsarkiv/nyheder/2025/1/er-din-virksomhed-klar-til-de-nye-tilgangelighedskrav-for-produkter-og-tjenester/ ; Dansk Erhverv: https://www.danskerhverv.dk/presse-og-nyheder/nyheder/2022/maj/lovforslag-om-tilgangelighedskrav-for-produkter-og-tjenester-er-vedtaget/ ; Erhvervsstyrelsen PDF: https://digst.dk/media/qw3oxj4a/erhvervsstyrelsen-tilgaengelighedsloven.pdf
- [U14] Planday help, open shifts / requests / swaps: https://help.planday.com/en/articles/30399-open-shifts-shift-requests-and-shift-swaps ; swap/handover/sell in app: https://help.planday.com/en/articles/30314-how-to-swap-handover-or-sell-shifts-in-the-planday-app
- [U15] Planday help, move/copy/delete/approve shifts: https://help.planday.com/en-au/articles/6677722-move-copy-delete-and-approve-shifts ; FitSmallBusiness review (conflict-detection claim): https://fitsmallbusiness.com/planday-review
- [U16] Danish working-time registration: Bird & Bird: https://twobirds.com/en/insights/2024/denmark/nye-krav-om-registrering-af-arbejdstid-i-danmark ; PwC: https://www.pwc.dk/en/articles-publications/2024/danish-act-working-hours.html ; Lundgrens: https://lundgrens.dk/en/news/new-requirements-employers-registration-working-time ; Deloitte: https://www.deloitte.com/dk/en/services/legal/perspectives/amendment-of-danish-act-on-working-hours.html
- [U17] Datatilsynet 2019 opinion on fingerprints for time registration (via Lovguiden): https://www.lovguiden.dk/dokument/datatilsynet/29-05-2019-vejledende-udtalelse-om-anvendelse-af-fingeraftryk-til-tidsregistrering-i-ansaettelsesforhold ; Dansk Erhverv: https://www.danskerhverv.dk/presse-og-nyheder/nyheder/2019/august/ma-du-bruge-fingeraftryk-til-tidsregistrering/ ; facial recognition 2022: https://www.danskerhverv.dk/presse-og-nyheder/nyheder/2022/maj/ny-afgorelse-om-brug-af-ansigtsgenkendelse-pa-medarbejdere/
- [U18] shadcn/ui docs, introduction: https://ui.shadcn.com/docs
- [U19] shadcn/ui MCP server docs: https://ui.shadcn.com/docs/mcp ; CLI 3.0 + MCP changelog: https://ui.shadcn.com/docs/changelog/2025-08-cli-3-mcp
- [U20] v0 FAQ: https://v0.app/docs/faqs ; v0 design systems (legacy): https://v0.app/docs/design-systems-legacy ; Vercel, What is shadcn/ui: https://vercel.com/i/what-is-shadcn
- [U21] Anecdote, "Why Your AI Keeps Building the Same Purple Gradient Website": https://prg.sh/ramblings/Why-Your-AI-Keeps-Building-the-Same-Purple-Gradient-Website ; Superdesign blog: https://superdesign.dev/blog/why-ai-design-looks-generic
- [U22] MUI→shadcn migration case study (vendor): https://metadesignsolutions.com/blog/migrating-react-app-from-mui-to-shadcn-ui-design-system-case-study ; Vercel, shadcn vs Material UI (vendor): https://vercel.com/i/shadcn-vs-material-ui
- [U23] shadcn/ui changelog (Base UI as the Default, Jul 2026; RTL Jan 2026): https://ui.shadcn.com/docs/changelog ; Base UI community page: https://base-ui.com/react/overview/community ; Radix releases: https://radix-ui.com/primitives/docs/overview/releases ; DEV, Radix future (secondary): https://dev.to/mashuktamim/is-your-shadcn-ui-project-at-risk-a-deep-dive-into-radixs-future-45ei
- [U24] MUI llms.txt: https://mui.com/material-ui/llms.txt ; community MUI MCP server: https://lobehub.com/en/mcp/jgentes-mui-mcp-cloudflare ; DEV comparison (AGENTS.md in repo, UNVERIFIED): https://dev.to/mr_manushukla/astryx-vs-shadcnui-vs-mui-3-react-design-systems-compared-for-2026-2lhk
- [U25] Planday Capterra reviews (snippets; page 403): https://www.capterra.com/p/145648/Planday/reviews/ ; Mindbody GetApp reviews: https://www.getapp.com/customer-management-software/a/mindbody/reviews/ ; Vibefam Mindbody Reddit summary (competitor, UNVERIFIED): https://vibefam.com/mindbody-reviews-reddit-2026/
- [U26] Glofox Capterra reviews: https://capterra.com/p/136861/Glofox/reviews/ ; see also `similar-projects.md` §5 (Daxko "easy to get lost" [S36 there])
- [U27] Booking-channel stats: SchedulingKit fitness booking statistics: https://schedulingkit.com/hub/industry-guides/fitness-booking-statistics ; Athletech/Global Payments: https://athletechnews.com/some-fitness-consumers-use-booking-apps-to-support-local-operators-global-payments-data/ ; Virtuagym study: https://business.virtuagym.com/news/virtuagym-study/
- [U28] NN/g, Touch Targets on Touchscreens: https://www.nngroup.com/articles/touch-target-size/
- [U29] Wikipedia, Date and time notation in Denmark: https://en.wikipedia.org/wiki/Date_and_time_notation_in_Denmark
- [U30] CMS, Kündigungsbutton rulings: https://cms.law/en/deu/legal-updates/kuendigungsbutton-gerichte-praezisieren-anforderungen ; LG München I 12 O 4127/23: https://www.gesetze-bayern.de/Content/Rtf/Y-300-Z-GRURRS-B-2023-N-32459?all=False ; Luther (LG Koblenz): https://www.luther-lawfirm.com/en/newsroom/blog/detail/requirements-for-companies-for-the-design-of-a-cancellation-button-in-view-of-the-decision-of-the-regional-court-of-koblenz-of-07032023-11-o-21-22
- [U31] NN/g, Deceptive Patterns in UX: https://www.nngroup.com/articles/deceptive-patterns/ ; NN/g, Sneaking: https://www.nngroup.com/articles/sneaking/
- [U32] FTC 2022 "Bringing Dark Patterns to Light" (secondary summaries): Venable: https://www.venable.com/insights/blogs/2022/09/the-ftc-brings-more-light-to-dark-patterns-in-new ; Hunton: https://www.huntonprivacyblog.com/2022/09/27/ftc-releases-report-on-dark-patterns/
- [U33] Dansk Erhverv, subscription rules / KlubKlub police report (May 2026): https://www.danskerhverv.dk/presse-og-nyheder/nyheder/2026/maj/webshop-politianmeldt-har-du-styr-reglerne-for-abonnementer/ ; Lovguiden, misleading online marketing: https://www.lovguiden.dk/praksisoversigt/vildledende-markedsfoering-online ; Dansk Erhverv 2020 loyalty clubs: https://www.danskerhverv.dk/presse-og-nyheder/nyheder/2020/oktober/forbrugerombudsmanden-politianmelder-fordelsklubber/
- [U34] Digital Fairness Act: AKD: https://www.akd.eu/insights/the-digital-fairness-act-what-to-expect-from-the-forthcoming-eu-legislative-proposal-on-strengthening-consumer-protection-rules-for-the-digital-age ; Heuking: https://www.heuking.de/en/news-events/newsletter-articles/detail/digital-fairness-act-implementation-obligations-companies-need-to-prepare-for.html ; CMS: https://cms.law/en/aut/legal-updates/inside-the-eu-s-planned-digital-fairness-act-digital-design-subscription-traps-and-influencer-marketing
- [U35] NN/g, Login Walls: https://www.nngroup.com/articles/login-walls/ ; NN/g, Optional Registration: https://www.nngroup.com/articles/optional-registration/ ; NN/g, Mobile apps initial use: https://www.nngroup.com/articles/mobile-apps-initial-use/
- [U36] Baymard, cart abandonment rate statistics: https://baymard.com/lists/cart-abandonment-rate
- [U37] NN/g, Mobile Input Checklist: https://www.nngroup.com/articles/mobile-input-checklist/ ; NN/g, EAS framework: https://www.nngroup.com/articles/eas-framework-simplify-forms/ ; NN/g, Web form design: https://www.nngroup.com/articles/web-form-design/
- [U38] Mindbody, Waitlist improvements: https://www.mindbodyonline.com/business/education/product-waitlist-improvements
- [U39] Glofox waitlist blog (vendor): https://www.glofox.com/blog/fitness-class-waitlist-software/ ; Vibefam waitlist guide (competitor; cutoff-before-late-cancel rule): https://vibefam.com/how-to-manage-class-waitlists-with-vibefam/ ; PushPress waitlist help: https://help.pushpress.com/en/articles/2950397-using-the-waitlist-in-core-by-pushpress
- [U40] Apple, Creating a generic pass: https://developer.apple.com/documentation/walletpasses/creating-a-generic-pass ; Creating a store card pass: https://developer.apple.com/documentation/walletpasses/creating-a-store-card-pass
- [U41] Google Developers Blog, Introducing the Google Wallet API: https://developers.googleblog.com/en/introducing-the-google-wallet-api/
- [U42] WebKit, Web Push for Web Apps on iOS and iPadOS: https://webkit.org/blog/13878/web-push-for-web-apps-on-ios-and-ipados/
- [U43] 9to5Mac, iOS 17.4 web apps EU: https://9to5mac.com/2024/02/08/ios-17-4-web-app-eu/ ; The Register: https://www.theregister.com/2024/02/08/apple_web_apps_eu/ ; see also `integrations-and-architecture.md` [S50]
- [U44] Gym Assistant PhotoCapture: https://gymassistant.pages.dev/products/photocapture/ ; GymMaster features: https://www.gymmaster.com/gymmaster-features/ ; Zenoti check-in guide: https://www.zenoti.com/thecheckin/gym-check-in-software-guide
- [U45] Mindbody Check-In app (App Store): https://apps.apple.com/us/app/id978183901 ; Mindbody class check-in app: https://www.mindbodyonline.com/business/class-check-in-app
- [U46] Wodify, Navigate and Use Coach View: https://help.wodify.com/hc/en-us/articles/9966132457623 ; Manage Classes in Coach View: https://help.wodify.com/hc/en-us/articles/24826329226007-Manage-Classes-in-Coach-View ; Coachboard in Kiosk+: https://help.wodify.com/hc/en-us/articles/12277777089175-Understand-Coachboard-Functions-in-Kiosk ; Timecard: https://help.wodify.com/hc/en-us/articles/360060876254-Timecard-Functions-to-Sign-in-Clock-in-a-Coach
- [U47] Apple WWDC22 session (Guided Access / Autonomous Single App Mode): https://developer.apple.com/videos/play/wwdc2022/10152/ ; Esper guide (vendor): https://www.esper.io/blog/ipad-kiosk-mode-a-guide-to-ipados-guided-access-and-beyond
- [U48] Android Developers, Lock task mode: https://developer.android.com/work/dpc/dedicated-devices/lock-task-mode
- [U49] NN/g, Very large touchscreen UX design: https://www.nngroup.com/articles/very-large-touchscreen-ux-design/ ; Advanced Kiosks (vendor): https://advancedkiosks.com/designing-custom-kiosk-interfaces-helping-the-beginning-user/
- [U50] NN/g, Dashboards: Making Charts and Graphs Easier to Understand: https://www.nngroup.com/articles/dashboards-preattentive/
- [U51] NN/g, Data Tables: https://nngroup.com/articles/data-tables ; NN/g tables topic: https://www.nngroup.com/topic/tables/
- [U52] NN/g, Drag–and–Drop: How to Design for Ease of Use: https://www.nngroup.com/articles/drag-drop/
- [U53] Planday, How to apply for open shifts: https://help.planday.com/en/articles/30312-how-to-apply-for-open-shifts ; Planday app for employees: https://help.planday.com/en/articles/30304-introduction-to-planday-app-for-employees
- [U54] Deputy help, kiosk photo verification: https://help.deputy.com/hc/en-au/articles/4754348479631 ; Deputy Face Unlock: https://help.deputy.com/hc/en-au/articles/4754361296527 ; Truein (third-party, geofence claim): https://truein.com/blogs/geofencing-time-clock-app-for-restaurants ; Clockify kiosk photo: https://cake.com/updates/clockify-kiosk-photo-capture

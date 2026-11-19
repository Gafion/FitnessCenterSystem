# Fitness Center Domain and Operations Research

**Purpose:** Background on how fitness centers work, as input for requirements. The project owner has no fitness-industry background.
**Status:** Research only. It makes no project decisions. Options and open questions are listed for the owner to decide.
**Date:** 2026-11-12
**Method:** Web research, done 2026-11-12. Sources are cited inline as [S#] and listed in the [Sources](#sources) section.

**How much to trust the sources:**

- **Primary or authoritative.** Industry associations (HFA, formerly IHRSA; EuropeActive), regulators and law (ICO, HSE, legislation.gov.uk, Citizens Advice, Danish Forbrugerombudsmanden, US state statutes), and standards bodies (PAR-Q+ Collaboration, CIMSPA).
- **Secondary.** Most operational detail comes from gym-software vendor blogs, help centres and trade press: Glofox, Gymdesk, GymMaster, Kisi, Health Club Management and others. Vendors sell to gyms, so their pages describe real features and practices well. Their **numbers** are often marketing claims.
- **UNVERIFIED.** Any figure or claim that could not be traced to a primary source carries this label.

---

## 1. Executive summary

1. **The type of center drives almost every requirement.** One useful split is **access gyms** and **coaching gyms** [S30]:
   - **Access gyms** sell entry to equipment. They are budget or 24/7 and run high volume at a low price.
   - **Coaching gyms** sell guided service. Boutiques, CrossFit boxes and PT studios are in this group, at a high price with few members.
   - Big-box clubs (2,000–10,000+ members per site) and boutiques (100–300 active members) differ by an order of magnitude [S29, UNVERIFIED vendor figure].
   - What the system must do changes with the type. Access gyms need billing and door control. Coaching gyms need scheduling, capacity and coach pay.
2. **The market is large and growing.**
   - Europe had 75.5M members, €39.1bn revenue and 67,515 clubs at year-end 2025. Membership grew 5.8% [S2].
   - HFA's 2025 benchmarking puts median member retention at **66.4%** [S4].
   - Losing roughly a third of members each year is the central business problem. Retention tooling is not a nice-to-have.
3. **The member lifecycle is contract-heavy and law-sensitive.** Notice periods, binding (minimum) terms, freezes, cancellation channels and failed payments are all regulated differently by country.
   - **UK:** a minimum term over 12 months may be an unfair term [S9].
   - **Denmark:** gyms may not require cancellation in person or by physical letter [S11]. "No binding" (*ingen binding*) may only be advertised if the member can cancel from one day to the next [S11].
   - **US:** the FTC's click-to-cancel rule was vacated in July 2025, but the FTC is still enforcing against hard-to-cancel gyms (the LA Fitness suit) [S8].
   - **Country is a critical scoping question.**
4. **Access control is tied to payment status.** At unstaffed or 24/7 sites, the door system must lock out cancelled or unpaid members automatically [S14, S15]. Sharing a card and following someone through the door (tailgating) are known revenue leaks [S16].
5. **Booking rules are a real sub-domain:**
   - class capacity
   - waitlists that promote members automatically
   - late-cancel and no-show penalties
   - PT appointments
   - pay-per-visit check-ins from aggregators (Wellhub/ClassPass) [S17–S20]
6. **The employee side is varied:**
   - Pay models are mixed: salaried, hourly, **per-class**, per-session, commission, and freelance trainers who pay the gym **rent** [S21–S24].
   - Qualifications and first aid/CPR/AED expire and need tracking. Requirements differ by jurisdiction [S25, S26, S27].
7. **Health and safety data is sensitive.** Health questionnaires (PAR-Q+) are "special category" personal data under GDPR. That needs explicit consent or another Article 9 condition [S31, S32]. Incident reporting may be legally required, as under UK RIDDOR [S34].

---

## 2. Business types and how their needs differ

| Type | Typical model (sourced) | Implication for a system (analysis, not decision) |
|---|---|---|
| **Big-box / full-service chain**<br>(e.g. David Lloyd, Life Time) | 2,000–10,000+ members per site; capital of $1M minimum, realistically $2–4M [S29, UNVERIFIED]. Mid-tier price ~$40–70/month [S29, UNVERIFIED]. Non-dues revenue may be a large share of revenue (Life Time ~35% claimed) [S5, UNVERIFIED]. | Many departments (spa, pool, kids' club, PT, retail, F&B). Multi-site and roles/permissions are complex. Secondary-spend POS matters. |
| **Budget / HVLP** ("high value, low price"), often 24/7 and unmanned for some hours<br>(Basic-Fit, PureGym, Planet Fitness) | $10–30/month [S29, UNVERIFIED]. Members are price-sensitive and cancel over money [S30]. Access via app, fob or card, linked to payment status [S14, S15]. | Self-service online sign-up, automated billing and dunning, door integration, remote management. Very few staff. |
| **Boutique studio**<br>(yoga, cycling, pilates, HIIT) | Caps at ~100–300 active members [S29, UNVERIFIED]. ~$90–300/month or ~$30–40/class [S29, UNVERIFIED]. | Class-centric: scheduling, capacity, waitlists, class packs, no-show fees, per-class instructor pay. |
| **CrossFit box** (affiliate) | Pays CrossFit an annual affiliate fee ($2,500–4,500, depending on location) plus an application fee [S28]. One owner or licensee must hold a CF Level 1 certificate [S28]. Affiliates set their own pricing and programming; no franchise fee [S28]. ~100–200 members at $150–250/month [S28-vendor, UNVERIFIED]. | Coaching gym: class booking, workout/WOD tracking, coach certifications, per-class coach pay. |
| **Climbing / swim / multi-purpose** | No dedicated sources found in this research. **UNVERIFIED / gap.** These likely add: day passes, lane/court/wall booking, mandatory inductions (e.g. belay checks), lessons and courses. | Needs more research if in scope. |
| **Municipal / community leisure centre** | In the UK often run by charitable leisure trusts or outsourced operators on ~10-year contracts. Facilities are leased from the council [S36, S37]. VAT "agency model" arrangements are spreading [S37]. | Pay-and-play, concessions, courses (swim lessons), public-sector reporting. Pricing specifics not found: **gap**. |
| **Hotel / corporate gym** | Corporate centres are often outsourced to specialist management firms (Premise Health, Exos, Strive and others) [S38]. Michigan's AED law excludes hotel/motel gyms from its "health club" definition [S27]. | Access by guest or employee status rather than paid membership; possibly no billing. Integration with an HR or hotel system. Little hotel-specific data found: **gap**. |
| **Aggregator-fed capacity** (cross-cutting) | Wellhub pays partners per check-in, tied to the gym's retail prices, monthly, with caps per member [S17, S18]. ClassPass sells "excess inventory" [S19]. | Check-ins must be recorded for the gym to get paid [S20]. Integration matters. |

**Market context:**

- **Europe:** 75.5M members (9.3% of the population); top operators Basic-Fit (5.8M members), PureGym (2.1M) and RSG Group (1.8M) [S2].
- **US:** penetration cited at 24.9% [S2].
- **HFA Global Report 2025:** released Aug 2025, covers ~30 markets, paywalled [S1].
- **US membership** is reported as ~77M [S3]. A vendor cites 81M (conflicting, UNVERIFIED).

---

## 3. Member lifecycle

### 3.1 Stages (synthesis of the cited sources)

```
Lead/Prospect → (Tour / Trial / Guest pass) → Sign-up (contract, health declaration, waiver, payment mandate)
 → Onboarding/induction → Active (attend, book, buy) ⇄ Frozen/Paused
 → Change (upgrade/downgrade/transfer) → Payment failure → Dunning → (Recovered | Suspended | Collections)
 → Cancellation request → Notice period → Ended → Win-back / Rejoin
```

### 3.2 Lead and trial

- Trials are common: free trials, paid intro offers and day passes [S39].
- **Trial conversion figures (all vendor, UNVERIFIED):**
  - 19% without structured follow-up and 38% with it.
  - Paid intros ~50–60%; free trials often below 20%.
  - Prospects who visit 3+ times convert at 2–3x the rate of one-visit triers [S39].
- Glofox's metric definition: memberships signed within 7–14 days of the trial ÷ trials completed [S39].
- Sales roles mentioned include greeter, tour guide and membership consultant [S39].

### 3.3 Membership types

Most of these exist in vendor products and operator terms. Sources noted where specific.

| Type | Description |
|---|---|
| Recurring monthly (rolling) | Debited automatically each month. "No binding" in Denmark means cancellable from one day to the next [S11]. |
| Fixed-term / binding (minimum term) | E.g. 12 months. In the UK, a term over 12 months may be unfair [S9]. The UK chain Gymbox says the maximum commitment is 12 months [S10, operator statement]. |
| Paid-in-full (annual, etc.) | Prepaid term. |
| Class pack / punch card / credits | N sessions, often with an expiry. Late cancels can be "counted as used" [S19-Gymdesk via S22]. |
| Pay-as-you-go / drop-in / day pass | Single visit [S39]. |
| Off-peak / tiered access | Access limited by time windows; door systems enforce this [S14]. |
| Multi-club / single-club | Multi-club access is handled as a contract change in some operators [S40]. |
| Family / couple, student, senior, concession | Not specifically researched. Common, but **UNVERIFIED** here. |
| Corporate (employer-paid) and aggregator | Wellhub, ClassPass and similar pay per visit [S17–S19]. |
| PT packages | Sessions sold separately from membership [S21–S23]. |

### 3.4 Contract mechanics

- **Joining/admin fee.**
  - One guide mentions avoiding paying a joining fee again as a reason to freeze rather than cancel [S10].
- **Freezes / pauses.**
  - Some gyms charge, e.g. £10 per month [S10].
  - Medical freezes are often free with proof [S10].
  - A freeze may extend the minimum term by the frozen months [S10].
  - Some gyms disallow freezing during the notice period [S10].
- **Cancellation & notice.**
  - US contracts often require 30 days' written notice; one example adds a $50 fee [S10].
  - UK examples require up to 90 days' paid notice [S10].
  - Notice often runs to the end of a calendar month [S10].
  - NZ guidance: notice starts when the member notifies, not from the paid-up date [S10].
  - **UK (Citizens Advice):**
    - The member usually pays the remainder of the fixed term.
    - Gyms should let members cancel for serious illness or injury, or for a significant financial hardship, with evidence.
    - Unfair terms may include auto-renewal without permission, a minimum term over 1 year, and big price or facility changes mid-contract [S9].
  - **Denmark:**
    - In-person-only cancellation was found unfair. Cancellation by e-mail was valid (Forbrugerklagenævnet 18-11554).
    - Gyms cannot require a physical letter or phone-only cancellation [S11].
    - The relevant law is Forbrugeraftaleloven § 28 [S11].
  - **US:**
    - The click-to-cancel rule was vacated on 8 July 2025.
    - The FTC sued LA Fitness operators in Aug 2025 over hard cancellation [S8].
- **Transfers.**
  - *To another person:* some clubs allow it as goodwill, with an admin fee and eligibility checks (e.g. not an indebted former member) [S40].
  - *To another club:* some franchises allow free transfer on relocation; others charge a fee, e.g. $25 [S40].
  - Some US states regulate transfers on club closure, e.g. Georgia: to a similar gym within 10 miles [S40].
  - Sharing a membership is usually prohibited [S40].
- **Upgrades / downgrades.** Little sourced detail. Handled as contract changes [S40]. **Gap.**

### 3.5 Payments, dunning and collections

- **Dunning** means retrying a failed charge and reminding the member before suspending them [S12].
  - "Smart" retries are spaced out over time [S12].
  - Vendors claim 30–40% recovery before suspension [S12, UNVERIFIED].
- **Direct debit vs card handling differs.** One platform moves a failed direct-debit membership straight to "abandoned", but lets card failures be fixed by the member [S12].
- **Auto-billing switch-off.** Some systems stop automatic billing after N failures to avoid dishonour fees [S12].
- **Collections.**
  - Collections agencies come last [S12].
  - Small debts are unattractive to agencies [S12].
- **No rulebook found.** No jurisdiction-specific rulebook for gym debt collection was found. **Gap.**

### 3.6 Retention, churn and win-back

- HFA 2025 median retention: **66.4%** [S4].
- Annual attrition of ~28.6% is attributed to IHRSA [S5, UNVERIFIED].
- **Early churn.** "Nearly 50% of new members quit within 6 months" appears widely in vendor blogs [S13, UNVERIFIED primary].
- **Attendance predicts churn.** Members visiting ≥2×/week are said to be about half as likely to cancel [S13, UNVERIFIED].
- **Onboarding effect.** Members who completed full onboarding: 87% still active at 6 months, versus 60% for those with little or none (attributed to Dr. Paul Bedford) [S13, UNVERIFIED primary].
- **Win-back.** No primary sources found. Vendors describe re-engagement campaigns to lapsed members. **Gap.**

---

## 4. Check-in and access control

- **Credentials:** phone app/QR, key fob, key card, PIN, biometrics and face recognition [S14, S15, S16].
  - Apps are recommended as the default for staffless sites [S14].
  - Physical tags are kept for members without smartphones [S15].
- **Access rules:**
  - by membership status and time windows (e.g. 24/7 vs staffed hours only) [S14]
  - live payment status [S15]
  - remote grant and revoke [S14]
- **Unstaffed hours** need preconditions. Example failure: a cancelled member's fob still works [S41]. Concerns include security, safety, equipment oversight and cleanliness. Many operators still staff peak times [S42].
- **Fraud and leakage controls:**
  - member photo shown at reception or turnstile on swipe [S16]
  - anti-passback, which blocks two entries without an exit [S16]
  - turnstiles
  - biometrics [S16]
  - Vendor leakage figures (5% sharing, 3–8% revenue loss) are **UNVERIFIED** [S16].
- **Biometrics and GDPR (UK).** The ICO says consent is not freely given if face recognition is the only way in. An alternative such as a card must be offered [S32].
- **Guest passes / day passes / aggregator check-ins** must be recorded to be paid [S20].

## 5. Class and resource booking

- **Capacity and waitlists.** Members on the waitlist are promoted automatically when a spot frees up. Some systems promote without asking the member to confirm, which causes no-shows the member did not intend. Mitigations [S17-Gymflow, GymMaster, Walla; see S19]:
  - reduced or waived fees for last-minute promotions
  - confirmation steps
- **Late-cancel windows:** 12–24 h suggested [S19-Virtuagym]; 4–24 h "standard" [S19-Glofox].
- **Fees:** ~$5–25, or one session deducted [S19, UNVERIFIED as norms].
- **Escalation:** e.g. booking blocked for 7 days after 3 no-shows in 30 days [S19].
- **PT sessions:** booked as appointments; trainer pay is linked per session [S22, S24].
- **Equipment, court, lane and pool booking:** no dedicated sources found. **Gap.**

## 6. POS and retail (secondary spend)

- **Secondary spend** covers PT, retail, F&B, supplements and spa.
- One mid-market chain found members spending up to an extra 50% of their monthly fee within the club after 7 months [S5-HCM, UNVERIFIED].
- **Non-dues revenue as % of revenue** is an HFA benchmark KPI. Values are paywalled [S4].
- **Gap:** no detailed POS process sources were found (stock, tills, member account charging). Standard retail POS concepts are assumed. **UNVERIFIED.**

---

## 7. Employee side

### 7.1 Roles (from job ads and vendor docs; titles vary)

| Role | Notes |
|---|---|
| General / Club Manager | Owns the operating budget, sales process, hiring, training, discipline, staff schedules for peak and quiet times, membership growth and retention offers, facility cleanliness, equipment safety inspection, and vendor scheduling [S43, S44]. |
| Assistant GM / Sales Manager / Membership Consultant | Sales is delegated via a sales manager [S43]. Consultants follow up on tours and trials [S39]. |
| Front desk / reception | Check-in, sales, POS. Photo verification on swipe [S16]. *(Duties not deeply sourced: UNVERIFIED.)* |
| Personal trainer (PT) | Employed, self-employed (rents space), or hybrid [S21–S23]. UK: Level 3 qualification typical; gym instructor Level 2 [S25]. |
| Group exercise instructor | ~65% paid per class (IDEA 2025, via PunchPass) [S24]. Les Mills: typical ~US$27/class [S24]. Brand-licensed programme certifications become inactive after 12 months without teaching [S24]. |
| Coach (CrossFit / boutique) | Paid per class, $25–55 [S28, UNVERIFIED]. |
| Cleaner / maintenance | Not specifically sourced. GMs schedule vendors for cleaning and repairs [S44]. Possibly outsourced. **UNVERIFIED.** |
| Lifeguard / swim teacher (pool sites) | Not researched. **Gap.** |

### 7.2 Pay models and payroll inputs

- **Employed PT:** paid regardless of sessions delivered [S21].
- **Self-employed PT:** bills clients directly and pays rent to the gym [S21].
- **Revenue split:**
  - 40–60% to the trainer at commercial gyms [S22, UNVERIFIED]
  - other sources: 30–50% split or $300–800/month rent [S23, UNVERIFIED]
  - UK budget chains: £200–400/month rent [S23, UNVERIFIED]
- **Hybrid:** salaried floor hours plus self-employed PT time [S21].
- **Software patterns:**
  - hourly pay from a time clock
  - per-session pay from class/PT check-ins
  - per-role rates
  - commissions and overtime
  - a CSV export to the payroll provider. "Payroll-ready" often means an export only, not running payroll itself [S45].
- **Employment status (US).** Classifying trainers as 1099 contractors vs employees is a legal question [S23-NFPT]. Details not verified. **UNVERIFIED.**

### 7.3 Certifications and compliance

- **UK register.** CPR is no longer a requirement for REPs/register access. It is now each instructor's own judgement or the employer's policy [S25].
- **UK first aid.** "Every gym legally needs a first aider on duty at all times" appears in a training-provider source only. **UNVERIFIED.**
- **US AED laws vary by state:**
  - California requires an AED programme for health clubs (HSC §104113) [S27].
  - Illinois requires an AED plus a medical emergency plan for liability protection [S27].
  - Suffolk County, NY requires an AED-trained staff member at all open hours [S27].
- **Tracking need.** Certification type, issuer, expiry, and renewal reminders. This follows from the above as analysis, not a decision.

### 7.4 Onboarding and permissions

- No dedicated sources were found on staff onboarding.
- Permission needs are implied by role separation: GM vs desk vs trainer (analysis). **Gap.**

---

## 8. Club manager workflows (daily / weekly / monthly)

Synthesised from GM job ads and vendor KPI guides [S43, S44, S4, S5]. The cadence split is **UNVERIFIED**: it is a reasonable reading, not a sourced schedule.

- **Daily:**
  - Open or close, or check remotely for unstaffed sites.
  - Facility and equipment safety walk [S44].
  - Review check-ins and failed payments [S12].
  - Review leads, tours and trials [S39].
  - Staffing cover for peaks [S43].
  - Handle incidents and complaints.
- **Weekly:**
  - Staff rota [S43].
  - Class timetable and instructor cover.
  - Sales pipeline review [S43].
  - Contact at-risk members with low attendance [S13].
- **Monthly:**
  - KPI review: joins, cancels, net growth, retention, revenue, payroll % [S4].
  - Payroll export [S45].
  - Aggregator reconciliation [S18].
  - Budget review [S43].
  - Equipment maintenance and vendor scheduling [S44].
  - Certification expiry check.

## 9. Safety, incidents, health declarations and waivers

- **PAR-Q+:**
  - A self-administered pre-participation screen. It flags people who need more screening or medical clearance before increasing activity [S31].
  - The 2021 version is current [S31].
  - Pregnant people use the ePARmed-X+ instead [S31].
  - Should be done before assessments and contracts (BCRPA) [S31].
- **Health data under GDPR (UK).**
  - Health data is special category data.
  - It needs an Article 6 lawful basis **and** an Article 9 condition, typically explicit consent [S32].
  - Vendors advise a separate consent box, not bundled into membership terms [S32, secondary].
- **Incident reporting (UK RIDDOR):**
  - The operator reports, not the member [S34].
  - Injuries to members of the public are reportable mainly when the person is taken from the scene to hospital [S34].
  - A 10-day timescale is cited in 2016 leisure guidance; check against current HSE guidance [S34].
  - Keep internal accident records even when an incident is not reportable [S34].
- **Waivers:**
  - **US:** enforceability varies by state. Gross negligence generally cannot be waived [S35].
  - **UK:** a trader cannot exclude or restrict liability for death or personal injury resulting from negligence (Consumer Rights Act 2015 s65(1)) [S33].
  - **Australia:** recreational-services exclusions are allowed except for reckless conduct [S35].

---

## 10. KPI table

| KPI | Definition | Benchmark (source) |
|---|---|---|
| Member retention rate | Share of members retained over a year | 66.4% median (HFA 2025 benchmarking) [S4] |
| Attrition / churn | 1 − retention, or cancels ÷ average members | ~28.6% attributed to IHRSA [S5, UNVERIFIED] |
| Net membership growth | Joins − drops | 5.5% (HFA 2025) [S4] |
| Monthly accounts added / dropped | Gross joins and cancels per month | Tracked by HFA [S4]; values paywalled |
| Revenue growth | YoY revenue change | 9.9% median 2024 (HFA) [S4]; Europe +9.1% in 2025 [S2] |
| MRR / dues revenue | Recurring monthly membership revenue | No benchmark found |
| Revenue per member | Total revenue ÷ members | HFA tracks it [S4]; values paywalled |
| Non-dues revenue % (secondary spend) | Non-membership revenue ÷ total | HFA tracks it [S4]; Life Time ~35% and Planet Fitness ~15% [S5, UNVERIFIED] |
| Payroll % of revenue | Total payroll ÷ revenue | HFA tracks it [S4]; CrossFit coaching costs 30–42% [S28, UNVERIFIED] |
| Revenue per FTE, per sq ft | Productivity measures | HFA tracks them [S4] |
| EBITDA % | Profitability | HFA tracks it [S4] |
| Attendance / visit frequency | Visits per member per week or month | ≥2/week is linked to lower churn [S13, UNVERIFIED] |
| Class utilisation | Booked (or attended) ÷ capacity | No benchmark found |
| No-show / late-cancel rate | No-shows ÷ bookings | No benchmark found [S19] |
| Trial / lead conversion | Joins within 7–14 days ÷ trials | 19–38%, or 50–60% for paid intros [S39, UNVERIFIED] |
| LTV | Average monthly revenue × average tenure | No industry benchmark found |
| NPS | % promoters − % detractors (0–10 scale) | North America benchmark 39 (IHRSA/TRP 2018); UK 37 [S46] |
| Failed-payment recovery rate | Recovered ÷ failed | 30–40% claimed [S12, UNVERIFIED] |

---

## 11. Glossary

Working definitions, drawn from the sections above.

- **Access gym / coaching gym.** A gym selling equipment access vs one selling guided coaching [S30].
- **Aggregator.** Third-party multi-gym pass. Pays the gym per visit (Wellhub, ClassPass) [S17–S19].
- **AED.** Automated external defibrillator [S27].
- **Anti-passback.** Rule that blocks reusing a credential for entry without an exit [S16].
- **Binding period / minimum term (DK: *binding*).** Contract period before the member may cancel [S9, S11].
- **Big-box.** Large full-service club [S29].
- **Box.** A CrossFit gym [S28].
- **Chair / rent model.** A freelance trainer pays the gym rent and keeps their own session revenue [S21, S23].
- **Class pack / punch card / credits.** A prepaid bundle of sessions.
- **Concession.** Discounted price for eligible groups, e.g. student or senior. Also: a contract to operate a public facility [S37].
- **Day pass / drop-in / PAYG.** A single paid visit.
- **Direct debit / ACH.** Bank-account collection, as opposed to card [S12].
- **Dunning.** The process of retrying failed payments and notifying the member [S12].
- **EHFMR.** EuropeActive/Deloitte European Health & Fitness Market Report [S2].
- **Freeze / pause / hold.** Temporary suspension of a membership [S10].
- **HFA.** Health & Fitness Association, formerly IHRSA [S1].
- **Home club.** The club a membership is attached to [S40].
- **HVLP.** High-value, low-price budget gym [S29].
- **Induction / onboarding.** Introduction session for new members [S13].
- **Late cancel / no-show.** Cancelling inside the window, or not attending a booking [S19].
- **Lead / prospect.** A potential member.
- **MRR.** Monthly recurring revenue.
- **Non-dues revenue.** Revenue other than membership fees [S4].
- **Notice period.** Time between a cancellation request and the end of the membership [S10].
- **NPS.** Net Promoter Score [S46].
- **PAR-Q+.** Physical Activity Readiness Questionnaire for Everyone [S31].
- **PT.** Personal trainer, or personal training.
- **REPs / CIMSPA.** UK exercise-professional register and professional body [S25].
- **RIDDOR.** UK reporting of injuries, diseases and dangerous occurrences regulations [S34].
- **Secondary spend.** In-club spending beyond dues [S5].
- **Tailgating.** Entering behind someone without your own credential [S16].
- **Trial / intro offer.** Time-limited free or paid trial [S39].
- **Waitlist auto-promotion.** Automatic booking of the next person on the waitlist when a spot frees [S19].
- **WOD.** "Workout of the day" (CrossFit). Not specifically sourced here.

---

## 12. Candidate actors / personas

A starting point only. The owner decides scope.

1. **Prospect / lead.** Browses, books a tour or trial, gets follow-up.
2. **Member.** Joins online or at the desk, pays, checks in, books classes and PT, freezes, cancels.
3. **Guest / day-pass visitor / aggregator user** (Wellhub, ClassPass).
4. **Minor / dependant member** under a family plan. Implies a guardian (UNVERIFIED need).
5. **Corporate client / employer account manager.**
6. **Front-desk staff.**
7. **Membership consultant / sales.**
8. **Personal trainer:** employed or freelance (renting space).
9. **Group instructor / coach.**
10. **Cleaner / maintenance staff**, or external vendor.
11. **Club / general manager.**
12. **Area / regional manager or owner** for multi-site. Head-office finance/HR.
13. **Payroll/accountant**: an external consumer of exports [S45].
14. **External systems:**
    - payment provider / direct-debit bureau
    - door/access hardware [S14]
    - aggregators [S17]
    - payroll provider [S45]
    - collections agency [S12]
    - regulator reporting, e.g. HSE RIDDOR [S34]

## 13. Candidate core domain entities

A starting point, not a data model.

- **Organisation / Site (Club) / Area (studio, pool, court, gym floor, door)**
- **Person**. A possible base for Member, Prospect, Employee, Guardian.
- **Lead** (source, status, follow-up tasks) and **Trial / Guest pass**
- **Membership Plan / Product**: type, price, term, binding period, notice period, access windows, sites, freeze rules.
- **Membership (Contract / Subscription)**: status lifecycle.
- **Freeze**, **Cancellation request**, **Transfer**, **Plan change**
- **Payment method / Mandate**, **Invoice / Charge**, **Payment attempt**, **Dunning case**, **Debt / Collection**
- **Credential**: fob, card, app QR, PIN, biometric template.
- **Access rule**, **Check-in / Access event**
- **Class type**, **Class session** (scheduled instance, capacity, instructor), **Booking**, **Waitlist entry**, **Attendance**, **Penalty / Fee**
- **Credit pack / Punch card balance**
- **Appointment** (PT session), **PT package**
- **Resource** (court, lane, equipment) and **Resource booking**
- **POS Sale / Order**, **Product / Stock item**
- **Employee**, **Role**, **Permission**, **Employment / Engagement terms** (employed, freelance, rent, revenue split)
- **Shift / Rota**, **Time entry**, **Pay rate**, **Commission rule**, **Payroll export**
- **Certification / Qualification** (type, issuer, expiry)
- **Health declaration** (PAR-Q+), **Consent record** (GDPR), **Waiver / Terms acceptance**
- **Incident / Accident report** (RIDDOR flag)
- **Aggregator visit / Partner payout reconciliation**
- **Communication / Campaign** (retention, win-back), **Survey response** (NPS)
- **KPI snapshot / Report**

---

## 14. Open questions for the project owner

**A. Business type and scale**

1. What kind of center is this for? Big-box, budget/24-7, boutique, CrossFit, climbing, swim, multi-purpose, municipal, hotel/corporate, or a mix?
2. Is this for one specific real center, a hypothetical one, or a product for many centers (multi-tenant SaaS)?
3. Single site or multi-site? If multi-site: can members use all sites, and is there a "home club"?
4. Roughly how many members per site: under 300, ~1,000, 5,000+?
5. How many employees, and of which roles?
6. Which facilities exist: gym floor, studios, pool, courts, climbing walls, sauna/spa, kids' area, café/retail?
7. Is it a franchise? If so, what does the franchisor dictate (systems, pricing, reporting)?

**B. Country and law**

8. Which country, or countries? This governs:
   - cancellation law (e.g. DK Forbrugeraftaleloven § 28, UK CRA 2015, US state laws)
   - GDPR
   - incident reporting
   - payroll/tax
   - AED and first-aid rules
9. Which currency/currencies and languages?
10. Which VAT/sales-tax rules apply to memberships, PT and retail?
11. Is a lawyer or an existing contract template available for the membership T&Cs?

**C. Opening hours and access**

12. Is the center staffed at all opening hours, or unstaffed for some hours (24/7)?
13. What access hardware exists or is planned (turnstiles, door controllers, fobs, cards, QR/app, biometrics)? Which vendor or protocol?
14. Must access be blocked automatically on non-payment or cancellation?
15. How should card sharing and tailgating be handled (photo on check-in, anti-passback)?
16. Are guests or bring-a-friend passes allowed, and how are they controlled?

**D. Memberships and pricing**

17. Which membership types should be supported?
    - monthly rolling, fixed-term/binding, paid-in-full
    - class packs/punch cards, PAYG/day pass
    - off-peak, family, student, senior, corporate
    - PT packages
18. Are there joining or admin fees?
19. What are the binding periods and notice periods? Do notice periods end at month-end?
20. Freeze rules:
    - allowed? paid or free?
    - medical freezes with evidence?
    - does a freeze extend the binding period?
    - max duration?
21. Which channels may members cancel through (online, e-mail, desk, app)? Is self-service cancellation required?
22. Should membership transfers to another person or club be supported? Upgrades/downgrades mid-term — how is the cost prorated?
23. How are price increases for existing members handled?
24. Is membership tied to one person (photo ID), or can it cover dependants/minors?

**E. Payments**

25. Which payment methods: card, direct debit (e.g. Betalingsservice, SEPA, BACS, ACH), MobilePay/Vipps, cash, invoice?
26. Which payment provider(s)? Is there an existing one?
27. Billing date: fixed day or anniversary? Prorating on join?
28. Dunning policy: number of retries, notifications, when to suspend access, late fees, handover to collections?
29. Do corporate clients get invoiced centrally?

**F. Sales and marketing**

30. Should the system manage leads, tours and trials, with follow-up tasks?
31. Should it run online sign-up with e-signature, health declaration and payment mandate?
32. Are retention/win-back campaigns (e-mail/SMS) in scope, or handled by another tool?
33. Is NPS or other member feedback collection in scope?

**G. Classes and booking**

34. Does the center run group classes? How many per week?
35. Booking needed? Capacity limits? Waitlists — auto-promote or confirm?
36. Late-cancel window? No-show fees, a session deducted, or booking bans?
37. Should PT sessions be bookable by members? What about courts, lanes, equipment or rooms?
38. Are aggregator partners (Wellhub, ClassPass, Urban Sports Club, etc.) used? Is integration needed for check-ins and payouts?
39. Do members need a mobile app, or is web enough?

**H. POS and retail**

40. Is there retail, a café, or supplements sales? Stock management needed?
41. Can members charge purchases to their account?
42. Is there existing POS or till hardware?

**I. Employees**

43. Which roles exist? Which are employed vs freelance?
44. How are trainers and instructors paid? Options: hourly, per class, per session, commission, revenue split, rent.
45. Is shift scheduling/rota needed? Shift swaps? Cover requests?
46. Is a time clock needed? How (app, desk terminal, fob)?
47. Should the system run payroll, or only export payroll inputs? To which payroll system (e.g. Danløn, Visma, DataLøn, Gusto)?
48. Which certifications must be tracked (PT qualification, first aid/CPR, AED, lifeguard, programme licences such as Les Mills)? Should there be expiry alerts?
49. Is employee onboarding (contracts, documents, training checklist) in scope?
50. What should each role be allowed to see and do? Health data is especially sensitive.
51. Are union or collective agreements relevant to scheduling/pay rules?

**J. Health, safety and compliance**

52. Is a health declaration (PAR-Q+ or own form) required at sign-up? Who reviews flagged answers?
53. Are waivers or liability acceptance needed? Has a lawyer validated them for the jurisdiction?
54. Is incident/accident reporting in scope? Does it need regulator reporting (e.g. RIDDOR-like)?
55. Should facility checklists (cleaning, equipment inspection, pool tests) be supported?
56. Data retention: how long are member, health, CCTV and access logs kept? Is a DPO involved?
57. Is any biometric access planned? GDPR requires a non-biometric alternative.

**K. Reporting and management**

58. Which KPIs matter most to the owner/manager? Candidates: retention, churn, MRR, attendance, utilisation, LTV, NPS, secondary spend, payroll %.
59. Is multi-site or head-office consolidated reporting needed?
60. Must data be exported to accounting/bookkeeping (e.g. e-conomic, Dinero, Xero)?

**L. Project scope and constraints**

61. Is the goal a learning/portfolio project or a production system for real users?
62. Is there an existing system to replace or migrate from (Mindbody, Glofox, ABC, Virtuagym, Exerp, etc.)?
63. What is the MVP? Which of members, billing, access, booking, staff and POS come first?
64. Is there a budget for third-party services (payments, SMS, door hardware APIs)?
65. Who are the real stakeholders to interview (a gym manager, front-desk staff, trainers)? Domain interviews are recommended to validate this research (suggestion, not a decision).

---

## 15. Research gaps (recommended follow-up)

- Climbing, swim and multi-purpose operations: courses, lane/court booking, belay and induction certification.
- Municipal pricing and concessions; hotel gym access models.
- Upgrade/downgrade proration practice.
- Win-back practices.
- POS process detail.
- Staff onboarding.
- Primary sources for the 50%/6-month churn figure and for the Bedford onboarding research.
- HFA benchmark values (paywalled) and the full EuropeActive report (paid).
- Country-specific law once the country is chosen. Danish rules were only partly covered: no fitness-specific Forbrugerombudsmanden guideline text was retrieved.
- Urban Sports Club payment model.

---

## Sources

- [S1] HFA – Fitness Industry's Global Momentum Continued in 2024 (HFA Global Report 2025): https://www.healthandfitness.org/fitness-industrys-global-momentum-continued-in-2024-new-report-shows
- [S2] EuropeActive – Strong growth in members and revenues for European health & fitness market in 2025 (EHFMR, presented 15 Apr 2026): https://www.europeactive.eu/blog/press-corner-4/strong-growth-in-members-and-revenues-for-european-health-fitness-market-in-2025-140
- [S3] HFA – New HFA Data Shows How 77 Million US Fitness Facility Members Work Out: https://www.healthandfitness.org/?p=4303 ; Gymdesk gym owner statistics: https://gymdesk.com/blog/gym-owner-statistics
- [S4] HFA – 2025 Fitness Industry Benchmarking Report: https://www.healthandfitness.org/2025-fitness-industry-benchmarking-report/
- [S5] KPI and secondary-spend sources: HFA "4 Financial KPIs": https://healthandfitness.org/improve-your-club/4-financial-kpis-to-watch-at-your-health-club ; Health Club Management "Grow stronger": https://www.healthclubmanagement.co.uk/health-club-management-features/Insight-Grow-stronger/35073 ; Rework gym key metrics: https://resources.rework.com/libraries/gym-fitness-growth/gym-key-metrics ; American Spa on retention: https://www.americanspa.com/sponsored/calculating-retention-and-understanding-impact-your-health-club-s-performance
- [S8] FTC click-to-cancel status: https://athletechnews.com/us-court-voids-click-to-cancel-rule-fitness-industry/ ; https://www.alston.com/en/insights/publications/2025/08/ftc-enforcement-cancellation-practices ; https://www.dlapiper.com/en-om/insights/publications/2025/09/click-to-cancel-lives-on-under-rosca-and-section-5
- [S9] Citizens Advice – Cancelling a gym membership (England): https://www.citizensadvice.org.uk/consumer/changed-your-mind/cancelling-a-gym-membership/
- [S10] Contract terms examples: YouFit policy https://youfit.com/wp-content/uploads/2023/06/YouFit-Membership-Cancellation-Freeze-Policy.pdf ; Village Gym FAQ https://villagegym.co.uk/frequently-asked-questions/membership/ ; Gymbox FAQ https://gymbox.co.uk/faqs/membership ; Consumer NZ https://consumer.org.nz/consumer-rights-and-campaigns/your-rights/cancelling-a-gym-contract ; Ashbourne T&Cs https://ashbourne-memberships.com/en-IE/terms-and-conditions/
- [S11] Denmark: Forbrugerombudsmanden "Fitness og lign." https://forbrugerombudsmanden.dk/alle-emner/abonnementer/fitness-og-lign ; Lovguiden praksis fitnesscenter https://www.lovguiden.dk/praksisoversigt/branche/fitnesscenter ; Dansk Erhverv (cancellation without physical attendance) https://www.danskerhverv.dk/presse-og-nyheder/nyheder/2019/september/opsigelse-af-abonnement-kravede-ikke-fysisk-fremmode/ ; Dansk Erhverv ("ingen binding") https://www.danskerhverv.dk/presse-og-nyheder/nyheder/2020/september/forbrugerombudsmanden-slar-fast-sadan-ma-du-bruge-udtrykket-ingen-binding-i-din-markedsforing/ ; Lovguiden unfair terms https://www.lovguiden.dk/praksisoversigt/forbrugerklage-urimelige-aftalevilkaar
- [S12] Dunning/failed payments: Glofox https://www.glofox.com/blog/gym-software-failed-payments/ ; ABC Fitness https://abcfitness.com/?p=29798 ; WodBoard https://help.wodboard.com/article/39-failed-membership-payments ; GymMaster https://www.gymmaster.com/help/help_understanding_failed_billing/ ; Gymdesk https://gymdesk.com/blog/gym-failed-payment-recovery ; Athletech News https://athletechnews.com/the-hidden-revenue-leaks-costing-fitness-clubs/
- [S13] Retention/attendance (vendor, secondary): ABC Fitness https://abcfitness.com/abc-articles/gym-member-retention-strategy/ ; Gymdesk https://gymdesk.com/blog/5th-class-cliff-gym-member-retention ; Uptivo https://uptivo.fit/blog/gym-member-retention-statistics ; Perspective AI https://getperspective.ai/blog/gym-member-retention-2026-why-members-quit-hear-it-first
- [S14] Kisi – extending gym hours: https://www.getkisi.com/blog/extending-gym-hours
- [S15] Glofox access control https://www.glofox.com/features/access-control-check-in/ ; GymMaster 24/7 https://www.gymmaster.com/business/24-7-gym-software/ ; GymFlow https://gymflow.io/solutions/gym-access-control
- [S16] Fraud/tailgating: Health Club Management "The fraud squad" https://www.healthclubmanagement.co.uk/health-club-management-features/Tech-series-The-fraud-squad/28462 ; Kisi tailgating https://www.getkisi.com/blog/how-to-prevent-gym-tailgating ; Kisi sharing https://www.getkisi.com/blog/membership-sharing
- [S17] Wellhub – How payments work: https://digital.gympass.com/partnerbase/uk/how-payments-work-at-wellhub
- [S18] Wellhub Partner Academy – payments: https://wellhub.com/en-us/partner-academy/understand-wellhub-payments/
- [S19] Booking policies: ClassPass partner FAQ https://classpass.com/partners/faqs ; Gymdesk ClassPass costs https://gymdesk.com/blog/classpass-for-business ; Gymflow feedback https://feedback.gymflow.io/p/no-show-late-cancellation-and-waitlist-rules ; GymMaster waitlist https://www.gymmaster.com/blog/more-targeted-waitlist-system/ ; Walla support https://support.hellowalla.com/en/articles/9073737-class-late-cancellations-no-shows-and-waitlists-settings ; Virtuagym no-shows https://business.virtuagym.com/blog/-how-to-reduce-no-shows-at-fitness-classes/ ; Glofox no-show rate https://www.glofox.com/blog/gym-no-show-rate/
- [S20] LegitFit – Wellhub check-in: https://legitfit.com/blog/introducing-wellhub-check-in-members-get-paid-automatically
- [S21] train.fitness – employed vs self-employed PT: https://train.fitness/personal-trainer-blogs/which-gym-1 ; Health Club Management PT employment: https://www.healthclubmanagement.co.uk/health-club-management-features/Ask-an-expert-Is-it-time-for-gyms-to-change-PT-employment-practices/32506 ; 1040.com https://1040.com/blog/2019/9/16/taxes-for-personal-trainers
- [S22] Gymdesk – How to pay personal trainers: https://gymdesk.com/blog/how-to-pay-personal-trainers
- [S23] Freenance PT finances https://freenance.io/professions/personal-trainer-salary-and-finances/ ; salarytax.uk PT pay https://salarytax.uk/professions/personal-trainer ; NFPT 1099 vs employee https://archive25.nfpt.com/?p=29863
- [S24] Group instructor pay: PunchPass https://punchpass.com/instructor-pay-calculator/ ; Les Mills https://www.lesmills.com/clubs-and-facilities/research-insights/instructor-insights/group-workouts-how-to-raise-revenues-and-reward-instructors ; Les Mills instructor training https://www.lesmills.com/instructors/initial-training/born-to-move
- [S25] UK qualifications/CPR: Leisure Opportunities "Changes to CPR requirements" https://leisureopportunities.co.uk/news/Changes-to-CPR-requirements/47883 ; Protectivity https://www.protectivity.com/knowledge-centre/what-qualifications-do-you-need-as-a-personal-trainer ; Origym https://www.origym.co.uk/blog/first-aid-personal-trainer/
- [S26] CIMSPA user induction considerations: https://memberonly.cimspa.co.uk/wp-content/uploads/2025/08/dry-fitness_user-induction-considerations_march-2023-2.pdf
- [S27] AED laws: San Diego County Health Club AED guidelines https://www.sandiegocounty.gov/hhsa/programs/phs/documents/EMS-2_HealthClubAEDRequirementGuidelines.pdf ; Illinois 210 ILCS 74 https://codes.findlaw.com/il/chapter-210-health-facilities-and-regulation/il-st-sect-210-74-15/ ; Michigan PA 23 of 2006 https://archive.legislature.mi.gov/documents/2005-2006/publicact/htm/2006-PA-0023.htm ; Suffolk County code https://ecode360.com/14945007 ; Connecticut OLR report https://prdext3.cga.ct.gov/2020/rpt/pdf/2020-R-0351.pdf
- [S28] CrossFit – Open a CrossFit gym: https://crossfit.com/open-crossfit-gym ; Gymdesk CrossFit costs https://gymdesk.com/blog/how-much-does-it-cost-to-open-a-crossfit-box ; PulseRevOps CrossFit KPIs https://pulserevops.com/knowledge/ik0413
- [S29] Business-model comparisons: Rework https://resources.rework.com/libraries/gym-fitness-growth/gym-business-models ; Glofox membership prices https://www.glofox.com/blog/gym-membership-prices ; Glofox profitability https://www.glofox.com/blog/is-owning-a-gym-profitable/ ; HFA mid-market squeeze https://healthandfitness.org/improve-your-club/is-the-mid-market-squeeze-threatening-your-health-club ; Athletech K-shaped economy https://athletechnews.com/gym-studio-outlook-inside-the-k-shaped-fitness-economy/
- [S30] Two-Brain Business – Access vs Coaching gyms: https://twobrainbusiness.com/access-vs-coaching-2/
- [S31] PAR-Q+: Health & Fitness Journal of Canada https://hfjc.library.ubc.ca/index.php/HFJC/article/view/103 ; BCRPA exercise screening tools https://healthyliving.bcrpa.bc.ca/fitness-essentials-exercise-screening-tools/ ; preprint on public perceptions https://preprints.org/manuscript/202405.0306/v1/download
- [S32] ICO – special category data https://ico.org.uk/for-organisations/uk-gdpr-guidance-and-resources/lawful-basis/special-category-data/what-is-special-category-data ; conditions for processing https://ico.org.uk/for-organisations/uk-gdpr-guidance-and-resources/lawful-basis/special-category-data/what-are-the-conditions-for-processing/ ; ICO training module 4 https://ICO.ORG.UK/media/for-organisations/training/4022202/module-4-processing-special-category-data.pdf
- [S33] Consumer Rights Act 2015 s65: https://www.legislation.gov.uk/ukpga/2015/15/section/65
- [S34] RIDDOR: RoSPA leisure guidance https://rospa.com/leisure-safety/risk-to-the-public ; LEP guidance PDF https://www.Rospa.com/rospaweb/docs/advice-services/leisure-safety/lep-guidance-concerning-compliance-with-riddor.pdf ; HSE key definitions https://www.hse.gov.uk/riddoR/key-definitions.htm ; Arun DC https://www.arun.gov.uk/accident-reporting-riddor ; Express Solicitors https://www.expresssolicitors.co.uk/blog/how-to-report-a-gym-injury-in-the-uk
- [S35] Waivers: HFA "Will your waiver protect your health club?" https://healthandfitness.org/improve-your-club/will-your-waiver-protect-your-health-club ; Zenoti waivers https://www.zenoti.com/thecheckin/what-fitness-centers-need-to-know-about-waivers-liability-and-legal-compliance-2025-update ; Enjuris https://enjuris.com/blog/questions/liability-waivers ; LegalVision AU https://legalvision.com.au/gym-disclaimer-include
- [S36] European Services Strategy – Leisure and library trusts: https://www.european-services-strategy.org.uk/wp-content/uploads/2008/03/leisure-and-library-trusts.pdf ; LGA Northampton Leisure Trust https://www.local.gov.uk/case-studies/northampton-leisure-trust
- [S37] CMS – Leisure outsourcing https://cms.law/en/gbr/legal-updates/a-bigger-splash-getting-the-most-out-of-leisure-outsourcing ; Local Government Lawyer – agency model https://www.localgovernmentlawyer.co.uk/projects-and-regeneration/317-projects-features/62186-local-authority-leisure-services-and-the-agency-model-what-is-the-impact-two-years-on
- [S38] Corporate fitness management: Premise Health https://www.premisehealth.com/types-of-care/fitness/fitness-center-management/ ; Strive https://strive2bfit.com/fitness-center-solutions/fitness-center-staffing-and-management ; IFMA FMJ https://fmj.ifma.org/insourcing-vs.-outsourcing
- [S39] Trials/leads: Glofox trial conversion https://www.glofox.com/blog/gym-trial-conversion-rates/ ; Virtuagym https://business.virtuagym.com/blog/how-to-improve-your-gym-trial-conversion-rates/ ; Rework free trial strategies https://resources.rework.com/libraries/gym-fitness-growth/free-trial-day-pass-strategies ; Rework open houses https://resources.rework.com/vi/libraries/gym-fitness-growth/community-events-open-houses ; Vibefam https://vibefam.com/what-studio-owners-get-wrong-about-free-trials-and-what-converts-better/
- [S40] Transfers: Bannatyne https://bannatyne.co.uk/health-club/membership ; Ashbourne T&Cs https://ashbourne-memberships.com/terms-and-conditions/ ; Anytime Fitness UK FAQ https://www.anytimefitness.com/en-gb/faqs ; Fitness First T&Cs https://fitnessfirst.co.uk/policies-and-legal/membership-terms-and-conditions ; Fit4Less https://info.fit4less.ca/en/faq/transfer-club-4less ; Georgia Consumer Protection https://consumer.georgia.gov/node/3256 ; Club4 Fitness https://www.club4fitness.com/?p=13045 ; Healthworks https://healthworksfitness.com/membership-transfer-request/
- [S41] Gymdesk – Unstaffed gym hours: https://gymdesk.com/blog/unstaffed-gym-hours
- [S42] Clubwise – 24/7 gyms: https://www.clubwise.com/?p=16370
- [S43] Chron – Duties of a gym general manager: https://work.chron.com/duties-gym-general-manager-26332.html
- [S44] GM job postings / Club Solutions: https://clubsolutionsmagazine.com/2023/06/gym-manager-job-description-salary-and-how-to-become-one/ ; https://goldscareers.careerplug.com/jobs/865327 ; https://PF-FittonClubs.careerplug.com/jobs/851935
- [S45] Payroll features: Gymdesk staff payroll https://docs.gymdesk.com/en/help/docs/staff-payroll ; Glofox instructors pay https://www.glofox.com/blog/managing-multiple-instructors-and-pay-rates-studio-software/ ; Wellyx https://wellyx.com/features/payroll-management/ ; Gym Assistant TimeClock https://gymassistant.pages.dev/products/timeclock/ ; Jackrabbit case study https://www.casestudies.com/company/jackrabbit-class/case-study/integrated-solution-makes-sense-for-gyms-that-take-their-payroll-processing-seriously
- [S46] NPS: HFA "Health clubs use Net Promoter Score" https://healthandfitness.org/improve-your-club/health-clubs-use-net-promoter-score-to-improve-member-experience ; HFA "Over 1 million health club members give valuable feedback" https://www.healthandfitness.org/over-million-health-club-members-give-valuable-feedback/ ; Health Club Management "What do one million members say" https://www.healthclubmanagement.co.uk/health-club-management-features/What-do-one-million-members-say/30010

*(Source numbers S6–S7 are intentionally unused.)*

# Verification Report — Wave 1

**Date:** 2026-11-12
**Status:** Independent verification pass. No decisions are made here, and the original files were **not** edited.
**Files checked:**
- `compliance-legal-security.md` (prefix **C**)
- `integrations-and-architecture.md` (prefix **I**)

**Method:** This pass follows the checklist in `agent-research-methodology.md` §4, "Citation support":
- **Claim selection.** I chose the claims most likely to feed decisions: legal requirements, deadlines, numbers, fines, prices, and API availability or deprecation.
- **Checking.** For each claim I re-fetched the cited URL and looked for a supporting passage. Where a page was unreachable, or did not support the claim, I searched for a primary or better source.
  - PDFs were downloaded and searched as text. This covered the CJEU press release, the EDPB CCTV guidelines and the arXiv papers.
- **Limits.**
  - EUR-Lex returned empty pages again. For GDPR I used a mirror of the official text (gdpr-info.eu); for PSD2 I used the EPC rulebook.
  - A few sites blocked automated fetches: ft.dk, pwc.dk, twobirds.com, taylorwessing.com and classpass.com.
  - Some evidence comes only from search-result summaries. Those cases are labelled.

**Verdicts**
- **SUPPORTED**: the source says what the claim says.
- **PARTIALLY SUPPORTED**: the core is right, but a detail is wrong, missing or overstated.
- **NOT SUPPORTED**: the source does not support the claim, or contradicts it.
- **SOURCE UNREACHABLE**: the source could not be fetched and no substitute was found.

---

## 1. Summary stats

| File | Claims checked | Supported | Partially | Not supported | Unreachable |
|---|---|---|---|---|---|
| compliance-legal-security.md | 33 | 24 (73%) | 6 | 2 | 1 |
| integrations-and-architecture.md | 39 | 24 (62%) | 12 | 1 | 2 |
| **Total** | **72** | **48 (67%)** | **18** | **3** | **3** |

**Overall picture**
- The legal core of the compliance file is solid and well sourced. This includes the Datatilsynet biometric decision, GDPR, forbrugeraftaleloven § 28, BEK 205/2024, TV-overvågningsloven and the CJEU ruling.
- The errors sit in the edges:
  - a wrongly attributed US bill
  - a cited EBA page that contains no EBA answer
  - over-generalised Danish working-time and børneattest statements
- In the integrations file, the main problem is **staleness**, not fabrication. Several vendor facts changed in 2026 and the file does not reflect them:
  - Garmin has paused approvals.
  - Fitbit's API is already turned off.
  - Criipto has been renamed Idura and now prices in EUR.
- There is also one mis-stated benchmark comparison.

---

## 2. compliance-legal-security.md

| ID | Claim (as written, condensed) | Cited | Verdict | Note / better source |
|---|---|---|---|---|
| C-01 | The Art. 30(5) <250-staff exemption from records of processing does not apply if processing is not occasional or includes Art. 9/10 data | S1 | SUPPORTED | EUR-Lex was unreachable. Checked against the official text mirrored at https://gdpr-info.eu/art-30-gdpr/ |
| C-02 | Datatilsynet 2023-31-0028, 3 July 2024 (Sporting Health Club): explicit consent under 9(2)(a) with 6(1)(a) | S10 | SUPPORTED | Date, journal number and company confirmed. |
| C-03 | Reception-only check-in during staffed hours is not an adequate alternative; a code or 24/7 phone support is adequate | S10 | SUPPORTED | Quoted: "ikke i sig selv kan anses for et tilsvarende alternativ". |
| C-04 | Check-in statistics derived from face data cannot rely on legitimate interests | S10 | SUPPORTED | |
| C-05 | The decision does not discuss a DPIA, and it references FysioDanmark 2021-431-0145 | S10 | SUPPORTED | |
| C-06 | AI Act Annex III 1(a): remote biometric identification is high-risk; 1:1 verification is excluded | S22 | SUPPORTED | The text matches. **Nuance missing from the file:** Annex III covers *remote* biometric identification. AI Act Art. 3(41) defines "remote" as identification "without their active involvement". A member who actively walks up to a face reader may fall outside the definition even when matching is 1:N. Lawyer question. |
| C-07 | The Annex III deadline was moved to 2 Dec 2027 (marked UNVERIFIED in the file) | secondary | SUPPORTED (upgrade) | Several sources report that the AI Omnibus was adopted (EP 16 Jun 2026, Council 29 Jun 2026) and published in the OJ in July 2026. The Annex III date is now 2 Dec 2027. Example: https://fpf.org/blog/the-ai-act-implementation-timeline-what-changes-under-the-ai-omnibus. Confirm against the OJ text. |
| C-08 | Norway's DPA fined SATS NOK 10 million | S28 | SUPPORTED | The IAPP URL is dead (404); it moved to `/news/b/`. Better source: EDPB news "Norwegian SA imposes fine on SATS…" (2023) and the decision PDF https://www.edpb.europa.eu/system/files/2023-07/no_2023-02_decisionpublic.pdf |
| C-09 | Retention of former members for 60 months was called "extraordinarily long" | S28 | PARTIALLY | The 5-year retention (justified by 2-year expulsions) was found too long, which is supported. The quoted phrase "extraordinarily long" was **not found** in any source checked. Remove the quotation marks or verify against the decision PDF. |
| C-10 | DK working-time recording applies from 1 Jul 2024; records kept 5 years after the reference period; employees can access their own records | S13/S14 | SUPPORTED | Deloitte (S13) confirms all three. Bird & Bird blocked (402); Lovguiden rate-limited (429). |
| C-11 | Self-organiser category and a 48-hour opt-out exist; whether self-organisers are fully exempt is UNVERIFIED | S13 | PARTIALLY | Deloitte states self-organisers **are** exempt from registration, and their contract must say so, which resolves the UNVERIFIED item. **Overstated:** the >48h individual agreement is limited to certain employees on collective agreements for on-call work in "societally critical functions". It is almost certainly not available to a gym, so NFR-02's "opt-out agreement records" is likely unnecessary. |
| C-12 | CJEU C-55/18, 14 May 2019: an "objective, reliable and accessible" system is required; Member States choose the method | S12 | SUPPORTED | Exact wording found in press release 61/19. |
| C-13 | SAQ A revision published 30 Jan 2025, effective 31 Mar 2025; 6.4.3 and 11.6.1 removed; script-attack eligibility criterion added | S18 | PARTIALLY | The date is **31 Jan 2025** per report-uri, not 30 Jan. Requirement **12.3.1** was also removed. The 31 Mar 2025 effective date is consistent across secondary sources only. The "provider written confirmation" route comes from **FAQ 1588** (28 Feb 2025), not from the SAQ itself. The primary PCI SSC document was not read. |
| C-14 | Payee-initiated payments (MIT) under a standing agreement are out of SCA scope | S19 | SUPPORTED | With a condition the file should state: the customer must have mandated the merchant to initiate payments, and no payer action may trigger each payment (EBA Q&A 2018_4131). |
| C-15 | SEPA Core: 8-week unconditional refund and 13 months for unauthorised debits | S20 (EBA 2019_4573) | NOT SUPPORTED by the cited source | The EBA Q&A was **rejected unanswered**. The 8-week and 13-month figures appear only in the submitter's question. The fact itself is correct per the EPC SDD Core Rulebook (§ "Refund", citing PSD2 Art. 71): https://www.europeanpaymentscouncil.eu/sites/default/files/kb/file/2025-10/EPC016-06%202025%20SDD%20Core%20Rulebook%20version%201.1.pdf, and PSD2 Art. 76/71. Replace the citation. |
| C-16 | § 28(1): cancel with 1 month's notice to month-end once 5 months have passed | S16 | SUPPORTED | |
| C-17 | § 28(4): 11 months only if the price exceeds DKK 20,000/yr **and** start-up costs are high | S16 | SUPPORTED | "High" means one-off costs above the average 6-month price. Also § 28(5): a longer notice period cannot be agreed. § 28(2) also excludes agreements ≤ DKK 2,000 paid within 14 days. |
| C-18 | Forbrugerombudsmanden cites § 25 for the 1-month starting point | S17 | SUPPORTED, but a **CONFLICT** | The FO page cites § 25, while danskelove.dk shows the rule in § 28. Possibly renumbering in LBK 1184/2025, or the FO page is stale. Confirm the section number on retsinformation.dk before citing it in contract terms. |
| C-19 | 2023 case on cancelling via the same medium; 2025 fitness chain fined for misleading offers | S17 | SUPPORTED | Dates: 5 Oct 2023 and 7 Jan 2025. The chain and amount are not on the overview page. |
| C-20 | BEK 205/2024 (route B): weekly full backup to an unrelated party in the EU/EEA kept 5 years; recognised IT-security standards; OIOUBL/Peppol; SAF-T; bank reconciliation | S15 | SUPPORTED | Examples of IT-security standards also include DORA. Certification is not required if compliance is otherwise documented. |
| C-21 | Fines up to DKK 1.5 million | S15 | SUPPORTED | ERST publishes indicative fine bands by turnover. |
| C-22 | Phase-in: annual-report companies from FY starting 1 Jan 2025; personally owned businesses ≥ DKK 300k from 1 Jan 2026 at the earliest | S15 | SUPPORTED | The threshold applies "in each of the two prior years". |
| C-23 | New requirements for registered systems from 1 Mar 2027, including MitID validation and Nemhandel auto-registration | S15 | PARTIALLY | MitID validation by 1 Mar 2027 is confirmed. Requirements 4–8 apply from **1 Jan 2028**. Nemhandel auto-enrolment is opt-out with 4 weeks' notice. Legal basis: BEK 811 of 22 Sep 2026. |
| C-24 | DK digital consent age is 15; below 15, parental consent | S9 | SUPPORTED | § 6(2)–(3) quoted. |
| C-25 | The age was raised from 13 to 15 by L 79 (2023-24) | S9 | SOURCE UNREACHABLE | ft.dk returned 403. Not decision-relevant. |
| C-26 | "Businesses and associations must obtain a børneattest" for staff with direct contact with under-15s | S27 | PARTIALLY | borger.dk lists the **mandatory** duty for associations and institutions. Businesses "may" request one. A business duty depends on sector executive orders (e.g. Erhvervsministeriet BEK 195/2015). Lovguiden's fitness overview reportedly says it is a duty for fitness centres with children's activities (seen via search summary only). Keep as a lawyer question. |
| C-27 | EAA applies from 28 Jun 2025; services by microenterprises (<10 staff, ≤ €2m) are exempt; transition to 28 Jun 2030 | S21 | SUPPORTED | accessible.org quotes Art. 3(23), 4(5), 31(2) and 32(1). Taylor Wessing blocked (403). EN 301 549 is still unverified. |
| C-28 | OWASP ASVS 5.0.0 (May 2025); ~350 requirements in 17 chapters | S23 | PARTIALLY | May 2025 is confirmed (OWASP GitHub). The "~350 / 17 chapters" figure appears in **neither** cited source checked. Verify against the ASVS 5.0 PDF before quoting it. |
| C-29 | CCTV deleted within 30 days, with exceptions for disputes (with notification) or police reports; POLCAM registration | S11 | SUPPORTED | TV-overvågningsloven § 4c(4)–(5) and § 2e. |
| C-30 | EDPB 3/2019: storage beyond 72 hours needs more justification | S4 | SUPPORTED | The quote was found. **The cited URL is 404.** Working URL: https://www.edpb.europa.eu/sites/default/files/files/file1/edpb_guidelines_201903_video_devices_en_0.pdf |
| C-31 | Private parties may process CPR only where law requires, with consent, etc. (§ 11(2)) | S8 | SUPPORTED | |
| C-32 | California: AB 2810 raised the cancellation window from 3 to 5 days | S30 | **NOT SUPPORTED** | The cited Senate analysis says the 5-day window came from **SB 581 (2005)**. AB 2810 (2016) changed **how** to cancel: in person, by email from an address on file, or by first-class mail. |
| C-33 | FTC click-to-cancel rule vacated by the 8th Circuit on 8 Jul 2025 | S31 | SUPPORTED | Fenwick does not mention ROSCA. That part of the claim is uncited. |

**Important claims without a working citation (compliance)**
- GDPR article claims (Art. 12(3) one month, Art. 33 72 hours, etc.) are cited only to EUR-Lex, which is unreachable by agents. They are correct per standard text, but a reachable mirror or the PDF would let future checks succeed.
- "NIS2 does not cover fitness centres", SEC-17 (fail-safe egress) and EN 301 549 → WCAG are already marked UNVERIFIED in the file. This is still true.

---

## 3. integrations-and-architecture.md

| ID | Claim (condensed) | Cited | Verdict | Note / better source |
|---|---|---|---|---|
| I-01 | The legacy MobilePay Subscriptions API is closed; Recurring is the replacement | S1/S2 | SUPPORTED | The changelog says it closed in **April 2026** (test and prod). The changelog itself does not name Recurring as the replacement. Frisbii docs confirm Recurring is "the new version of MobilePay Subscription". |
| I-02 | Stripe MobilePay: recurring not supported; no Billing, Setup Intents or off-session payments; single-use | S5 | SUPPORTED | Verbatim. |
| I-03 | Frisbii advertises "Vipps MobilePay Recurring" | S10 | PARTIALLY | The rebrand page lists only "Vipps MobilePay", and the Adobe listing was unreadable. Better source: https://docs.frisbii.com/docs/vipps-recurring (seen via search summary). |
| I-04 | Paxton Net2 API is limited to commercial integrators since 1 Jul 2024; lifetime licence after approval | S15 | PARTIALLY | Commercial-only and the lifetime licence are confirmed. The **1 Jul 2024 date is not on the cited pages** as fetched today; it appears only in a Paxton page search snippet. **Missing:** Net2 v7.0 (Jan 2026) discontinues the SDK and adds MFA. |
| I-05 | OSDP = IEC 60839-11-5, AES-128 Secure Channel; Wiegand is plaintext | S14 | PARTIALLY | Axis confirms AES-128, RS-485 and plaintext Wiegand. The IEC number is **not on the Axis page**, and the Honeywell URL is 404. The number is likely correct but uncited. |
| I-06 | Salto KS Connect: OpenAPI and OIDC; credentials from the local Salto business unit | S16 | SUPPORTED | |
| I-07 | Stripe DK: 1.5% + 1.80 kr standard EEA; 2.8% + 1.80 kr premium; Billing 0.7%; SEPA DD 2.60 kr | S3 | SUPPORTED | Snapshot as of 2026-11-12. |
| I-08 | BS Creditor API: create/search mandates only; OAuth 1.0a; sandbox | S11 | PARTIALLY | Mandates-only is confirmed. **OAuth 1.0a and sandbox are not stated** on either page fetched. They may be on "API Basics" (not checked). |
| I-09 | GoCardless: create the payment 8 working days before the month; paid out 1 business day after collection | S13 | PARTIALLY | 8 working days (5 pm CET) is confirmed. The payout page is internally inconsistent: D+1 versus "a further 2 working days". |
| I-10 | GoCardless DK domestic fee 1% + €0.20, cap €2 (flagged UNVERIFIED) | S13b | SOURCE UNREACHABLE | 404. Still unverified. |
| I-11 | MobilePay historic price 0.99% + 1 kr per subscription transaction (Feb 2024) | S6 | SUPPORTED, **misleading context** | The price is for the legacy **Subscriptions** product, which is now closed (I-01). It says nothing about Recurring pricing. |
| I-12 | Nexi: unscheduled card-on-file, bulk charge, 0-amount verification | S9 | SUPPORTED | |
| I-13 | e-conomic: app secret token plus agreement grant token | S21 | SUPPORTED | The "API free" claim is still unverified; the page offers a free developer agreement only. |
| I-14 | Dinero: OAuth 2.0 via Visma Connect; approval by Dinero | S22 | SUPPORTED | The URL redirects to https://developer.dinero.dk/documentation/getting-started/ |
| I-15 | "Since 1 Jan 2026 … digital bookkeeping for more business types, including sole proprietorships" | S24 | PARTIALLY | Only personally owned businesses with net turnover ≥ DKK 300k **in each of the two prior years**. ERST's page still says "at the earliest" (tidligst). PwC was blocked (403). The integrations file is less precise than the compliance file (C-22) and should match it. |
| I-16 | ERST issued new guidance for standard-system providers on 5 Oct 2026, with an e-invoicing focus | S24 | PARTIALLY | The page header says 9 Oct and the body says 5 Oct. Scope is **SAF-T and e-invoicing** plus an automation annex. |
| I-17 | Xero tiered pricing since Mar 2026; Starter free, 5 connections, 1,000 calls/day/org | S25 | SUPPORTED | The date comes from Accounting Today (migration from 2 Mar 2026). |
| I-18 | Wellhub acquired Urban Sports Club | S30 | SUPPORTED | Announced 26 Mar 2025; completed by 10 Sep 2025 (USC newsroom); brands "operate independently for now". |
| I-19 | Wellhub requires check-in validation within an expiry window | S27 | SUPPORTED | The partner guide says "within 20 minutes", or the user loses access for the day. The Access Control API getting-started page did not show this. |
| I-20 | USC integrates through booking-software vendors | S28 | SUPPORTED | Partners include Eversports, Magicline, Virtuagym and others. QR check-in is not on this page. |
| I-21 | ClassPass Inventory API is for scheduling-software vendors | S29 | SOURCE UNREACHABLE | 403. |
| I-22 | Legacy Fitbit Web API turn-off on 30 Oct or 2 Nov 2026 | S36 | SUPPORTED, **tense outdated** | Fitbit blog: 30 Oct. Google Health docs: **2 Nov 2026** (HTTP 410). As of today, 12 Nov 2026, **it is already off**. The file says "being turned off". |
| I-23 | Google "not onboarding new projects" to the Google Health API (Oct 2026) | S37 | PARTIALLY | Sahha's 13 Oct update supports it, but Sahha's **29 Oct update says the notice was removed** and the status is unknown. |
| I-24 | Garmin Health API is free but needs approval | S38 | PARTIALLY / **outdated** | The cited Garmin URL is 404. Garmin's page (via search) says it is free for approved business developers. **Uncited omission:** new approvals have been **paused since spring 2026** for a programme "redesign" (https://the5krunner.com/2026/09/14/garmin-developer-api-access-paused/ and Garmin developer forum threads). |
| I-25 | Fowler: "almost all the successful microservice stories have started with a monolith"; premium "often gets projects into serious trouble" | S41/S42 | SUPPORTED | Both quotes are verbatim. |
| I-26 | 94% of LLM compile errors in TypeScript were type-check failures; type-constrained decoding cut compile errors by more than half | S46 | SUPPORTED | 94% is in §1 of the paper body, not the abstract: "on average 94% of compilation errors result from failing type checks". |
| I-27 | TypeScript became #1 on GitHub in Aug 2025 by monthly contributors, overtaking Python and JS; partly attributed to AI | S43 | SUPPORTED | GitHub calls the AI link "observational signals rather than causal claims". C# is #6 for new repos, which is confirmed. |
| I-28 | AutoCodeBench upper bounds: Elixir ~97.5, C# ~88, TS and Python ~61–63; per-language problem sets differ | S45 | SUPPORTED | Table 4: Elixir 97.5, C# 88.4, Python 63.3, TS 61.3. The paper classes TS as a "low-resource" language (TIOBE). |
| I-29 | Stack table: "AutoCodeBench Python lowest upper bound (~63%)" | S45 | **NOT SUPPORTED** | Python (63.3) is **higher** than TS (61.3), and also higher than JS (59.2), Rust (61.3) and Racket (52.8). Among the stacks in the table, TS is the lowest, not Python. This reverses the comparison the table implies. |
| I-30 | SWE-bench Multilingual: 9 languages, Rust highest, C/C++ lowest; "no per-language numbers extracted" | S47 | SUPPORTED | The numbers are on the page: Rust 58.14%, C/C++ 28.57%. |
| I-31 | METR RCT: experienced developers were 19% slower while believing they were faster | S49 | SUPPORTED | 16 developers, 246 issues, Feb–Jun 2025. They believed AI had sped them up by 20%. |
| I-32 | Claude Code guidance: typecheck/test commands in CLAUDE.md; a Stop hook to block until typecheck passes | S48 | SUPPORTED | |
| I-33 | Clerk free tier: 50,000 monthly retained users since Feb 2026 | S52 | SUPPORTED | Changed 5 Feb 2026 (Clerk's own article). The Auth0 25k and Supabase 50k figures were **not checked**. |
| I-34 | Criipto: DKK 1,200 setup + DKK 0.28 per MitID authentication + subscription | S40 | PARTIALLY / **outdated** | **Criipto is now Idura** (criipto.com redirects to idura.eu). Current pricing is in EUR: €0.037 per MitID authentication (≈ DKK 0.28), subscriptions €67/€220/€400 per month. The setup fee is not stated ("onboarding fees may apply"). Source: https://idura.eu/pricing/verify |
| I-35 | Hetzner: Apr 2026 +30–37%; Jun CPX +144–175%; CX23 €5.49 | S51 | PARTIALLY | Only "up to 37% from April 1" was confirmed (Tom's Hardware headline). The June figures and CX23 price were not verified. The file already flags them UNVERIFIED. |
| I-36 | iOS Web Push only for Home-Screen apps (16.4+); Apple's 2024 EU removal was reversed; some sources still disagree | S50 | PARTIALLY | 16.4+ and Home Screen are confirmed (MagicBell). **None of the three cited sources supports the reversal.** Two predate it and MagicBell contradicts it. The reversal is real (Apple, 1 Mar 2024): https://9to5mac.com/2024/03/01/apple-home-screen-web-apps-ios-17-eu/ ; https://www.macrumors.com/2024/03/01/apple-walks-back-decision-to-disable-eu-web-apps/. Whether Web Push works on EU iOS today is still not primary-sourced. |
| I-37 | Kisi API access depends on plan tier (which tier UNVERIFIED) | S19 | SUPPORTED (resolves the gap) | Pro ($199 per location) and Elite include "API and SDK access"; Base and Standard do not. |
| I-38 | Planday Open API (OAuth2): HR, shifts, punch clock, timesheets | S32 | SUPPORTED | The page also lists a Payroll API. "REST" is not stated explicitly. |
| I-39 | Stripe does not process Dankort; co-branded Visa/Dankort goes through Visa | S5 | SUPPORTED | |

**Important claims without citation, or omissions (integrations)**
- The Garmin approval pause (I-24), Fitbit already being off (I-22) and the Criipto → Idura rebrand (I-34) are all missing. Each changes the wearables and MitID options.
- Paxton Net2 v7.0 (Jan 2026) discontinues the SDK; only the local API remains (I-04).
- The Auth0 and Supabase free-tier numbers rest only on a secondary blog (S52) and were not checked against vendor pages.

---

## 4. Corrections that should be applied

These are listed in priority order. The original files were not edited; the owner or orchestrator should apply them.

**compliance-legal-security.md**
1. **§12 (US):** Replace "raised from 3 days by AB 2810" with "extended to 5 days by SB 581 (2005); AB 2810 (2016) allowed cancellation in person, by email or by first-class mail" (C-32).
2. **§5.3 / S20:** Re-cite SEPA refunds to the EPC SDD Core Rulebook and PSD2 Art. 71/76. EBA Q&A 2019_4573 was rejected unanswered (C-15).
3. **§4 Denmark:**
   - Self-organisers are exempt from recording, and the contract must state it. Remove the UNVERIFIED tag.
   - The 48h opt-out is limited to certain on-call employees in critical functions under collective agreements, so it is likely not applicable.
   - Narrow NFR-02 accordingly (C-11).
4. **Exec summary §9 / §8:** Reword the børneattest duty. The mandatory list covers associations and institutions; the duty for businesses depends on sector executive orders. Keep the lawyer flag (C-26).
5. **§3.1 AI Act:**
   - The 2 Dec 2027 Annex III date is now adopted law (AI Omnibus, OJ July 2026). Upgrade it from UNVERIFIED after checking the OJ.
   - Add the Art. 3(41) "without their active involvement" point to the 1:1 versus 1:N analysis (C-06, C-07).
6. **§7 bookkeeping:** For registered systems, MitID validation is due by 1 Mar 2027; other requirements apply from 1 Jan 2028 (BEK 811/2026) (C-23).
7. **§5.1 PCI:**
   - Publication date is 31 Jan 2025.
   - 12.3.1 was also removed.
   - The "written confirmation" route comes from FAQ 1588 (C-13).
8. **Exec summary §4 / S28:**
   - Drop the quotation marks on "extraordinarily long", or verify against the decision PDF.
   - Replace the dead IAPP URL (C-09, C-08).
9. **S4:** Fix the EDPB PDF URL (add `_0`) (C-30).
10. **§10.1:** Mark the "~350 requirements in 17 chapters" figure UNVERIFIED (C-28).
11. **§6.2:** Note the § 25 versus § 28 numbering conflict between Forbrugerombudsmanden and danskelove.dk (C-18).
12. **§5.2:** Add the EBA condition for MIT: there must be a mandate, and no payer action may trigger each payment (C-14).

**integrations-and-architecture.md**
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

## 5. Questions for the project owner

1. **Wearables:** Fitbit is off, Garmin approvals are paused, and Google Health onboarding status is unknown. Should wearables be dropped from v1 scope now, rather than kept as an open decision (D12)?
2. **Applying corrections:** Should the orchestrator apply the corrections in §4 directly to the two research files, or record them as an errata section so the originals stay as written?
3. **Legal form:** The børneattest duty for a business, as opposed to an association, depends on legal form and the services offered to under-15s. Is the centre an ApS/A/S, a sole proprietorship or a forening? This was already asked in compliance Q3, but the answer now matters more.
4. **Face entry:** If face-recognition entry is wanted, would members actively present themselves at a reader, or would they be recognised passively at a distance? This affects whether the AI Act's "remote" biometric identification category applies at all.
5. **Remaining unverified items:** Some sites block automated fetching (ft.dk, pwc.dk, classpass.com, Bird & Bird, Taylor Wessing). Should a human spot-check these, per methodology open question 5?

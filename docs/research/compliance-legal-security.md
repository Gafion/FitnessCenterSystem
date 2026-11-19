# Compliance, Legal, Privacy & Security Research

> **Errata (2026-11-12).** An independent fact-check ([verification-report-wave1.md](verification-report-wave1.md)) found the corrections below. They take precedence over the text further down, which is left as originally written. Claim IDs (e.g. C-32, I-29) refer to the verification report.

## Errata

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

---

**Project:** Fitness Center administration system (members + employees)
**Date:** 2026-11-12
**Status:** Research input. **This is not legal advice.** Nothing here is a decision. It lists requirements, options and open questions to feed `security.md`, `privacy.md` and `nfr.md`.
**Jurisdiction assumption:** None. EU/GDPR is covered in depth, with **Denmark as a worked example**. US differences appear briefly in section 12.

**Source conventions**
- `[S#]` is a numbered source in the **Sources** list at the end.
- **UNVERIFIED** means the claim comes only from secondary sources, or I could not open the primary text during this research. Confirm it before relying on it.
- EUR-Lex pages could not be fetched automatically during this research. GDPR and directive article references are cited to the official EUR-Lex texts by article number. Check the exact wording there.

---

## 1. Executive summary

1. **GDPR applies in full.** A single gym will almost certainly need records of processing. The Art. 30(5) exemption for organisations under 250 staff does not apply when processing is regular or includes special-category data. It will also need privacy notices, a retention schedule, workflows for data-subject requests (DSARs), data processing agreements (DPAs) with every processor, and breach handling within 72 hours [S1].
2. **Biometric entry (face or fingerprint) is the highest-risk feature.**
   - Datatilsynet (the Danish DPA) has accepted face recognition at a gym chain on the basis of **explicit consent (Art. 9(2)(a))**, but only with an equivalent non-biometric alternative. A code or remote door opening counted as equivalent; reception-only access during staffed hours did not [S10].
   - Statistics derived from face data also needed consent. Legitimate interest was rejected [S10].
   - A data protection impact assessment (DPIA) is very likely needed.
   - The EU AI Act may classify 1:N biometric identification as high-risk. 1:1 verification is excluded (UNVERIFIED application to gyms) [S22].
3. **Health data** (health declarations, injuries, medical notes from personal training) is special-category data under Art. 9. It needs its own lawful exception, minimisation, stricter access control and likely a DPIA.
4. **Retention is enforced in practice.** Norway's DPA fined the gym chain SATS NOK 10 million, partly because it kept former-member data for 60 months, which the DPA called "extraordinarily long" (secondary source) [S28].
5. **Employee time recording is mandatory in Denmark from 1 July 2024.**
   - This implements the CJEU *CCOO v Deutsche Bank* ruling (C-55/18) [S12][S13].
   - The system must be "objective, reliable and accessible".
   - Records are kept for 5 years (arbejdstidsloven § 4b, secondary source) [S14].
   - Employees must be able to see their own records.
6. **Payments.** Using a hosted payment page or redirect from a PCI-compliant provider can cut PCI DSS scope to SAQ A.
   - Iframe integrations now carry extra script-integrity eligibility criteria (SAQ A update, effective 31 March 2025) [S18].
   - Recurring card charges can run as merchant-initiated transactions (MIT) outside strong customer authentication (SCA) after the first payment has been authenticated [S19].
   - Direct debit (Betalingsservice/SEPA) has refund rights. SEPA Core has an 8-week no-questions refund [S20].
7. **Danish consumer law limits binding periods.**
   - Under forbrugeraftaleloven § 28, a consumer can cancel an ongoing service contract with 1 month's notice to month-end once 5 months have passed. This caps binding at about 6 months in practice [S16].
   - The cancellation clause must be stated clearly [S16].
   - Cancellation must be possible on the same medium where the contract was made [S17].
   - Online sign-ups trigger the EU 14-day withdrawal right (Directive 2011/83/EU) [S3].
8. **Danish Bookkeeping Act.** If the system itself does the bookkeeping, a **non-registered/custom bookkeeping system** must meet strict technical requirements (BEK 205/2024) [S15]:
   - immutable postings
   - automatic sequential IDs
   - change logs
   - weekly third-party backup in the EU/EEA kept for 5 years
   - recognised IT-security standards
   - e-invoicing in OIOUBL and Peppol BIS
   - SAF-T export
   - bank reconciliation

   Integrating with a **registered** bookkeeping system instead is a significant option to evaluate.
9. **Minors.**
   - In Denmark, children can consent to information-society services from age **15**. Below that, parental consent is required (databeskyttelsesloven § 6) [S9].
   - Staff with direct contact with children under 15 may need a **børneattest** (child-protection record check) (UNVERIFIED for gyms) [S27].
10. **Accessibility.**
    - The European Accessibility Act (EAA) has applied to e-commerce services since 28 June 2025 [S21].
    - **Microenterprises** (fewer than 10 staff and at most €2m turnover or balance sheet) are exempt for services (secondary sources) [S21].
    - Whether online membership sign-up counts as an "e-commerce service" needs legal confirmation.
11. **Security baseline.**
    - OWASP ASVS 5.0 (May 2025) offers three levels [S23]. L2 is a plausible target given the health and biometric data, but that is for the owner to decide.
    - GDPR Art. 32 requires "appropriate" technical and organisational measures [S1].

---

## 2. GDPR core requirements (EU-wide)

Source unless stated otherwise: Regulation (EU) 2016/679 [S1].

| Topic | Requirement | Article | Design implication |
|---|---|---|---|
| Principles | Lawfulness, fairness, transparency, purpose limitation, data minimisation, accuracy, storage limitation, integrity/confidentiality, accountability | Art. 5 | Every field needs a purpose and a retention rule. Accountability means the system must produce evidence. |
| Lawful bases | One of: consent, contract, legal obligation, vital interests, public task, legitimate interests | Art. 6(1) | Likely mapping (options for lawyer review): membership administration is **contract**; bookkeeping and time records are **legal obligation**; CCTV and security logs are **legitimate interests**; marketing is **consent**. |
| Consent conditions | Demonstrable, distinguishable, withdrawable as easily as given, freely given | Art. 7 | Store consent records with timestamp, version of text, channel and withdrawal. Separate checkbox per purpose. Datatilsynet required this granularity in [S10]. |
| Child consent (ISS) | Parental consent below the national age, which is 13 to 16 | Art. 8 | Denmark sets it at 15 [S9]. Age capture and parental-consent flow needed. |
| Special categories | Health, biometric (for unique identification) and others are prohibited unless an Art. 9(2) exception applies | Art. 9 | Separate data store or flags, stricter RBAC, explicit consent flows. |
| Criminal data | Only under official authority or as Member State law allows | Art. 10 | Relevant for børneattest records (section 8). |
| Transparency | Privacy notice at collection | Art. 13/14 | Notices for members, employees, CCTV and website. Must state retention periods. SATS was criticised for not doing so [S28]. |
| Access (DSAR) | Copy of data within 1 month, extendable by 2 months | Art. 12(3), 15 | Export function per data subject across all modules, including logs where they are personal data. |
| Rectification / erasure | Correct inaccurate data; erase where no longer necessary or consent withdrawn, unless retention is legally required | Art. 16, 17 | Erasure must respect legal holds such as bookkeeping (5 years) and time records (5 years). Design per-field anonymise/erase with a reason code. |
| Restriction / portability / objection | Restriction flag; machine-readable export for consent- or contract-based data; objection to legitimate interests | Art. 18, 20, 21 | Restriction state on member and employee records; JSON/CSV export. |
| Privacy by design/default | Minimisation by default | Art. 25 | Optional fields off by default; least-privilege roles. |
| Processors | Written DPA with mandatory clauses | Art. 28 | DPAs needed with hosting, payment provider, email/SMS, access-control vendor, CCTV vendor, payroll, and the software vendor if it hosts the system. |
| Records of processing | Required; the <250-employee exemption does not apply if processing is not occasional or includes Art. 9/10 data | Art. 30(5) | Maintain a RoPA. The system could hold or generate parts of it. |
| Security | Appropriate TOMs: pseudonymisation, encryption, resilience, restore ability, regular testing | Art. 32 | See section 10. |
| Breach | Notify the DPA within 72 h; notify data subjects when risk is high | Art. 33, 34 | Breach register, audit logs to scope incidents, contact export. |
| DPIA | Required for likely high risk, including large-scale special-category processing and systematic monitoring of public areas | Art. 35 | DPIA likely triggered by biometrics, CCTV and health data. The EDPB-endorsed WP248 criteria say two or more criteria usually means a DPIA is needed [S5]. Datatilsynet's national DPIA list was not checked (UNVERIFIED). |
| DPO | Mandatory when core activities involve large-scale special-category data or large-scale systematic monitoring | Art. 37 | Probably not for a single gym, but **lawyer review**, especially for a chain using biometrics. |
| Transfers | Third-country transfers need an adequacy decision or SCCs | Ch. V | Hosting and SaaS vendor locations matter. |

### 2.1 Retention: known anchors

| Data | Retention anchor | Source |
|---|---|---|
| Bookkeeping material (invoices, payments) | 5 years from end of the financial year | Erhvervsstyrelsen [S15] |
| Employee working-time records (DK) | 5 years after the end of the reference period | arbejdstidsloven § 4b, secondary [S14] (UNVERIFIED against official text) |
| CCTV footage (DK) | Delete **no later than 30 days**, with exceptions for a specific dispute or a police report | TV-overvågningsloven, via Datatilsynet guidance and secondary sources [S11] |
| Former-member profile data | No fixed rule. The SATS case shows 60 months was considered too long. | [S28] (secondary) |
| Consent records | As long as needed to demonstrate consent (Art. 7(1)). No fixed period found. | [S1] |

---

## 3. Special-category and high-risk data

### 3.1 Biometric access control (face or fingerprint)
- Biometric data used for unique identification is special-category data under Art. 9(1) [S1][S10].
- **Datatilsynet decision 2023-31-0028 (3 July 2024, Sporting Health Club)** [S10]:
  - Explicit consent under Art. 9(2)(a) is the suitable exception, with Art. 6(1)(a) as the general basis.
  - Consent must be freely given. Members must have a reasonable alternative without significant restrictions or costs.
    - Reception check-in during staffed hours only was **not** adequate.
    - An access code, or phone support that opens the door remotely or issues a code, **was** adequate. No unreasonable waiting times are expected.
  - Consent must be granular. Access and usage statistics need separate checkboxes.
  - The consent form itself must tell members that alternatives exist.
  - Withdrawal must be as easy as giving consent.
  - Check-in statistics derived from facial recognition could **not** rely on legitimate interests.
  - Using face recognition for gym access did not in itself breach Art. 5 proportionality. The decision referred to the FysioDanmark case 2021-431-0145.
  - The decision does not discuss a DPIA. Employee consent was outside the scope of the decision.
- Lovguiden's sector summary says a DPIA is typically required and that the alternative must be free of extra cost (secondary, UNVERIFIED) [S26].
- **EU AI Act:**
  - Annex III point 1 lists remote biometric identification systems as high-risk.
  - It excludes biometric verification whose sole purpose is to confirm that a person is who they claim to be (1:1) [S22].
  - A gym system that matches a face against the whole member database (1:N) may fall in scope; one that verifies after a card swipe may not. This is an interpretation from secondary sources and is **UNVERIFIED**.
  - One secondary source reports that the Annex III deadline was moved to 2 December 2027 (UNVERIFIED).
  - **Lawyer review needed.**
- **Employees:** Using staff fingerprints or face for time or access raises consent-validity issues because of the power imbalance. Datatilsynet has approved fingerprint access control for employees in a specific context (a slaughterhouse, on food-safety grounds), via secondary sources [S26].

### 3.2 Health data
- **Examples:** health declarations or PAR-Q forms at sign-up, injury and accident reports, personal-trainer notes, pregnancy information, doctor's notes for membership freezes, and body-composition measurements (classification as health data is a lawyer question).
- **Art. 9 exception options:**
  - explicit consent (9(2)(a))
  - legal claims (9(2)(f)), for accident reports
  - vital interests (9(2)(c)), for emergencies
  - possibly Danish national provisions
  - Which exception applies to each use is a **lawyer question**.
- **Requirements:** minimise (ask whether a health declaration is necessary at all), separate access, field-level encryption (an option), and short retention.

### 3.3 CCTV
- **EDPB Guidelines 3/2019 on video devices** [S4]:
  - consider less intrusive alternatives first
  - storage beyond 72 hours becomes harder to justify
  - layered signage
- **Denmark** [S11]:
  - TV-overvågningsloven requires signage.
  - Footage must be deleted within 30 days at the latest, with dispute and police-report exceptions; the person must be notified if footage is kept for a dispute.
  - Disclosure is limited (e.g. to the police).
  - Private parties may not film public streets.
  - Cameras covering public roads must be registered in POLCAM.
- Changing rooms and toilets: no source was checked for this research, but the general proportionality principles in Art. 5 [S1][S4] make filming there extremely unlikely to be lawful (UNVERIFIED specific rule).
- **Scope question:** Will the system store, index or link CCTV to member check-ins? Linking raises the risk significantly.
- **Comparable enforcement:** Norway's DPA required SATS to let members object to check-in photos (secondary) [S28].

### 3.4 CPR numbers (Denmark)
- Private businesses may process CPR numbers only where law requires it or with consent, among other conditions (databeskyttelsesloven § 11(2)) [S8].
- A secondary source states that internal convenience, such as deduplication, is not enough (UNVERIFIED) [S8].
- **Implication:** Do not collect members' CPR numbers by default. For employees, CPR is needed for payroll and tax under a legal obligation (UNVERIFIED specific provision).

---

## 4. Employee data and working time

- **GDPR Art. 88** allows national rules for employment data [S1]. Denmark has one in databeskyttelsesloven § 12 (exact text not verified) [S8].
- **Working Time Directive 2003/88/EC:**
  - maximum average 48-hour week (Art. 6)
  - 11 hours' daily rest (Art. 3)
  - weekly rest (Art. 5)
  - breaks (Art. 4) [S2]
- **CJEU C-55/18 CCOO v Deutsche Bank (14 May 2019)** [S12]:
  - Member States must require employers to set up an "objective, reliable and accessible" system to measure daily working time.
  - Member States choose the method.
- **Denmark** (secondary sources; the official BM/AT guidance was not found) [S13][S14]:
  - Applies from 1 July 2024 to all employers regardless of size.
  - Daily working time must be recorded.
  - Employers choose the method; a deviation-based approach is reported to be possible (UNVERIFIED).
  - Records are kept 5 years after the reference period (§ 4b).
  - Employees must have access to their own records.
  - A "self-organiser" (selvtilrettelægger) category and a 48-hour opt-out exist; the opt-out can be withdrawn by the employee.
  - Whether self-organisers are fully exempt from recording is **UNVERIFIED**.
- **Requirements this creates:**
  - shift or time recording per employee per day
  - immutability or audit trail for corrections
  - employee self-view
  - 5-year retention
  - rest-period and 48-hour average warnings (optional feature)
  - opt-out agreement records
- **Other HR data:**
  - contracts, payroll, sickness absence (health data, Art. 9)
  - trade union membership if recorded (Art. 9)
  - børneattest results (Art. 10)
  - certifications
  - Payroll is probably delegated to an external payroll provider, which needs a DPA. That is an open question.

---

## 5. Payments

### 5.1 PCI DSS scope reduction
- **SAQ A** applies to e-commerce merchants that fully outsource card-data capture to a PCI DSS-compliant third party (redirect or iframe).
- The PCI SSC SAQ A revision (published 30 January 2025, effective 31 March 2025) [S18]:
  - Removed requirements 6.4.3 and 11.6.1 from the SAQ A list.
  - Added an **eligibility criterion**: the merchant's site must be protected against script attacks that could affect its payment page, or the merchant must obtain the provider's written confirmation.
  - Several secondary sources say a **full redirect** largely avoids the script criterion, whereas **iframe** merchants must meet it (UNVERIFIED; check PCI SSC FAQ 1588).
- **Option set:**
  - (a) Hosted redirect: smallest scope.
  - (b) Iframe or hosted fields: small scope, plus script-integrity controls.
  - (c) Direct API card handling (SAQ D): large scope, almost certainly not proportionate.
- **Never** store the PAN, CVV or magnetic-stripe data. Store only provider tokens.
- Card-present terminals at reception have their own SAQ types (B, B-IP, P2PE). These were not researched in depth (UNVERIFIED).

### 5.2 PSD2 / SCA
- PSD2 (Directive (EU) 2015/2366) Art. 97 requires strong customer authentication (SCA). The detailed rules are in Delegated Regulation (EU) 2018/389 [S6].
- **Recurring payments** [S19]:
  - SCA is required when the payer creates, amends or first starts a series of payments.
  - Later payments of the same amount to the same payee are exempt under RTS Art. 14.
  - Payee-initiated payments (MIT) under a standing agreement are out of SCA scope.
  - **Variable amounts**, such as add-ons or price changes, may need the MIT route or new SCA. This is a provider-specific question.
- **Implication:** The membership flow must capture a mandate or agreement at sign-up, with authentication by the provider. Price changes may need re-authentication.

### 5.3 Direct debit
- **SEPA Core Direct Debit:**
  - Payers can get an unconditional refund within 8 weeks.
  - Unauthorised collections can be refunded for up to 13 months.
  - The creditor stores the mandate [S20].
- **Betalingsservice (DK):**
  - The creditor needs an agreement with its bank.
  - Debtors have refund rights under the scheme rules (bank rule documents) [S20].
- **Requirements:** mandate storage, failed-payment and refund handling, dunning, and reconciliation.

### 5.4 Danish surcharge rules
- Rules on surcharging consumers for card fees exist in the Danish Payments Act. They were **not researched** in this pass (UNVERIFIED).

---

## 6. Consumer law for memberships

### 6.1 EU: Consumer Rights Directive 2011/83/EU [S3]
- **Information:** Distance and off-premises contracts require pre-contractual information (Art. 6), including total price per billing period, duration, and conditions for terminating.
- **Withdrawal:** 14 days from contract conclusion for services (Art. 9).
- **Early start:** If the member asks to start during the withdrawal period, the trader needs an express request (Art. 7(3)/8(8)). If the member then withdraws, they pay a proportionate amount (Art. 14(3)). The right to withdraw is lost once the service has been fully performed with acknowledgment (Art. 16(a)).
- **Withdrawal form:** A model form is in Annex I(B).
- **Implication:** The system needs:
  - a channel flag (online, phone, in person, off-premises)
  - a withdrawal-deadline calculation
  - an "express request to start now" capture
  - pro-rata refund calculation
- The Danish implementation is in forbrugeraftaleloven. The exact section numbers were not verified (UNVERIFIED).

### 6.2 Denmark
- **Forbrugeraftaleloven § 28** (ongoing supply) [S16]:
  - **(1)** The consumer can cancel with 1 month's notice to the end of a month once 5 months have passed since conclusion.
  - **(3)** Standard terms must clearly state the notice period and the date from which cancellation is possible.
  - **(4)** An exception allowing 11 months applies only where the price exceeds DKK 20,000 per year **and** start-up costs are high. This is unlikely for gyms, but it is a lawyer question.
  - **(2)** Excludes certain low-value agreements.
  - **Effect:** Binding cannot effectively exceed about 5 months plus 1 month's notice (secondary interpretation) [S16].
- **Forbrugerombudsmanden** [S17]:
  - It has handled several fitness-centre cancellation cases.
  - It cites § 25 for the general starting point of 1 month's notice to month-end.
  - 2023 case: it must be possible to cancel on the same medium or platform where the contract was made.
  - 2025: a fitness chain was fined for misleading offers.
- **Forbrugerklagenævnet** (case 18-11554 via a secondary source) [S29]:
  - A clause allowing cancellation only in person was held unfair.
  - Cancellation by email was held valid.
  - Unclear terms are interpreted against the trader.
- **Implications for the system:**
  - online self-service cancellation
  - email cancellation recorded with timestamp
  - automatic calculation of the earliest termination date
  - contract-terms versioning per member
  - no exit fees in breach of § 28
  - price-change notification workflow (rules on unilateral price increases **not researched**, UNVERIFIED)
- **Marketing:** Email and SMS marketing needs prior consent under markedsføringsloven § 10 (referenced in [S8]; rules not researched in detail, UNVERIFIED). Cookie consent under the Danish cookie order was not researched (UNVERIFIED).

---

## 7. Bookkeeping (Denmark: Lov om bogføring, LOV 700 of 24/05/2022)

Sources: Erhvervsstyrelsen [S15].

- **Retention:** Accounting material is kept 5 years from the end of the financial year. This applies even after a system change or bankruptcy.
- **Phase-in of the digital requirement:**
  - Companies required to file annual reports: financial years starting from 1 January 2025.
  - Personally owned businesses with turnover of at least DKK 300k: income years from 1 January 2026 at the earliest.
- **Two compliance routes:**
  - **A. Use a registered standard bookkeeping system.** The provider carries most system obligations, including daily backups and 5-year retention. Businesses do not notify Erhvervsstyrelsen which system they use. The fitness system would integrate with it by exporting invoices, payments and settlements.
  - **B. The fitness system is or contains a non-registered (custom) bookkeeping system.** The business itself is responsible for compliance with BEK nr. 205 of 29/02/2024:
    - Each transaction has date, amount, unique voucher number, text, and currency rate if not DKK.
    - The system auto-assigns registration date, a sequential transaction ID, and the user or program that posted it.
    - Posted transactions cannot be deleted, changed or backdated; corrections are new postings.
    - Voucher changes are logged with the original retrievable.
    - Full backup at least weekly to an unrelated third party on a server in the EU/EEA, kept for 5 years.
    - Recognised IT-security standards (examples given: ISO 27001, NIS2, D-mærket), including user and access management.
    - Send and receive e-invoices in OIOUBL and Peppol BIS, with Nemhandel registration.
    - SAF-T export.
    - Bank reconciliation.
    - Fines of up to DKK 1.5 million.
- **If the system is ever sold to other businesses:** It becomes a "standard" system and must be registered before it is marketed in Denmark. New requirements for registered systems apply from 1 March 2027, including MitID validation and Nemhandel auto-registration [S15].

---

## 8. Minors as members

- **Danish digital consent age:**
  - Under databeskyttelsesloven § 6(2)-(3), consolidated as LBK 289 of 08/03/2024, a child's consent is valid from age **15**. Below 15, a holder of parental responsibility must give or approve consent [S9].
  - The age was raised from 13 to 15 by bill L 79 (2023-24) [S9].
  - § 6 applies only where consent is the basis for information-society services offered directly to children.
- **Contract capacity:** A minor's capacity to enter a paid membership contract is a matter of Danish guardianship law (værgemålsloven). It was **not researched** (UNVERIFIED). **Lawyer review needed.**
- **Børneattest:**
  - Businesses and associations must obtain a child-protection certificate before employing people who have direct contact with children under 15.
  - A secondary source says fitness centres are covered when they run classes or training for under-15s (UNVERIFIED against the executive order) [S27].
  - Businesses order certificates via virk.dk [S27].
  - A pending reform was reported; its status is unknown [S27].
- **Recital 38 GDPR** gives children's data specific protection [S1]. Profiling and marketing aimed at minors needs extra care. EDPB launched a children's-privacy hub in November 2026 (secondary) [S9b].
- **Requirements:**
  - date of birth or age capture
  - guardian linkage with guardian contact and consent
  - age-based restrictions (e.g. equipment areas, times; this is a business-rule question)
  - no biometrics for minors unless the lawyer approves it

---

## 9. Accessibility (European Accessibility Act)

- **Directive (EU) 2019/882** has applied to covered services since 28 June 2025 [S21].
- **Scope:**
  - Covered services include e-commerce services: services provided at a distance through websites or apps at the consumer's request with a view to concluding a consumer contract.
  - Online membership sign-up and payment plausibly fall in scope. This is an **interpretation**; **lawyer review needed**.
- **Microenterprise exemption** (Art. 4(5)), per secondary sources [S21]:
  - Microenterprises providing **services** are exempt.
  - A microenterprise has fewer than 10 employees **and** turnover or balance sheet of at most €2m.
  - It does **not** exempt products.
- **Transition:** Services under contracts concluded before 28 June 2025 may continue until 28 June 2030 (secondary) [S21].
- **Technical standard:**
  - Conformance is usually demonstrated through the harmonised standard EN 301 549, which references WCAG 2.1 AA (UNVERIFIED; not confirmed in this research).
  - WCAG 2.1/2.2 AA is a reasonable engineering target even if the business is exempt. That is a decision for the owner.
- **Danish implementing law:** Not researched (UNVERIFIED).
- **Employee-facing tools:** Not covered by the EAA consumer-service scope (UNVERIFIED). EU employment-equality law may still require reasonable accommodation.

---

## 10. Security baseline

### 10.1 Standards and frameworks
- **GDPR Art. 32:** appropriate measures, including pseudonymisation and encryption, confidentiality, integrity, availability and resilience, timely restore, and regular testing [S1].
- **OWASP ASVS 5.0.0 (May 2025)** [S23]:
  - three cumulative levels (L1 to L3)
  - about 350 requirements in 17 chapters (secondary figure)
  - each chapter now starts with documentation requirements
  - **Options:**
    - L1 as the minimum
    - L2 for apps handling sensitive data (OWASP's general positioning; UNVERIFIED wording)
    - L3 for the most critical apps
  - The biometric and health data argue for at least L2 on those modules. Owner decision.
- **Bookkeeping BEK 205/2024** (if route B): recognised IT-security standard, with ISO 27001 and D-mærket among the examples [S15].
- **NIS2** (Directive (EU) 2022/2555): Fitness centres do not appear to be among the listed sectors (UNVERIFIED; annexes not checked).

### 10.2 Requirements checklist (security)

| ID | Requirement | Basis |
|---|---|---|
| SEC-01 | Role-based access control (RBAC) with least privilege. Candidate roles: owner/admin, manager, reception, trainer, bookkeeper, employee self-service, member self-service. | Art. 5(1)(f), 25, 32 [S1]; BEK 205 user/access management [S15] |
| SEC-02 | Separate permission for special-category data (health, biometrics) and HR data; by default reception should not see health notes. | Art. 9, 25 [S1] |
| SEC-03 | MFA for all staff and admin accounts (option: also for members). | ASVS authentication chapter [S23] (specific level mapping UNVERIFIED) |
| SEC-04 | Password storage with a modern adaptive hash; breached-password checks. | ASVS [S23] |
| SEC-05 | Session management: timeouts, secure cookies, re-authentication for sensitive actions (exports, erasure, payroll). | ASVS [S23] |
| SEC-06 | TLS for all traffic; encryption at rest for databases and backups; consider field-level encryption for health and biometric templates. | Art. 32(1)(a) [S1] |
| SEC-07 | Biometric templates kept separate from the identity store (or on-device), never raw images if avoidable; deleted on consent withdrawal. | Art. 5(1)(c), 9 [S1]; [S10] (design detail UNVERIFIED as a legal requirement) |
| SEC-08 | Append-only audit log covering logins, reads of sensitive records, exports, permission changes, erasures, financial postings and time-record edits. | Art. 5(2), 30, 33 [S1]; BEK 205 logging [S15]; time records "reliable" [S12] |
| SEC-09 | Immutable financial postings; corrections only via reversing entries. | BEK 205 [S15] |
| SEC-10 | Backups: at least weekly (daily recommended); encrypted; held off-site with a third party in the EU/EEA; restore tested; bookkeeping backups kept 5 years. | BEK 205 [S15]; Art. 32(1)(c) [S1] |
| SEC-11 | Erasure must propagate to backups, or backups must expire within a documented window (open question). | Art. 17 [S1] |
| SEC-12 | No card data stored; payments only via tokens from a PCI DSS-compliant provider; script-integrity controls if an iframe is used. | PCI DSS SAQ A [S18] |
| SEC-13 | Breach detection and response runbook; 72-hour DPA notification; breach register. | Art. 33-34 [S1] |
| SEC-14 | Vulnerability management: dependency scanning, patch SLAs, periodic penetration test. | Art. 32(1)(d) [S1]; ASVS [S23] |
| SEC-15 | Secrets management; no secrets in the repository. | ASVS [S23] |
| SEC-16 | Rate limiting and lockout on login and door-access endpoints. | ASVS [S23] |
| SEC-17 | Physical access-control integration fails safe (fire-code compliance): the door controller must not depend on cloud availability for egress (UNVERIFIED; local building code). | Lawyer or fire-safety review |
| SEC-18 | Admin actions on access control (granting door access, disabling biometrics) are logged and attributable. | Art. 5(2) [S1] |

### 10.3 Privacy requirements checklist

| ID | Requirement | Basis |
|---|---|---|
| PRIV-01 | Purpose and lawful basis recorded for every data category (feeds the RoPA). | Art. 6, 30 [S1] |
| PRIV-02 | Configurable retention schedule per category, with automated deletion or anonymisation jobs and legal-hold override. | Art. 5(1)(e) [S1]; [S28] |
| PRIV-03 | Consent ledger: purpose, text version, timestamp, channel, withdrawal; separate consents per purpose. | Art. 7 [S1]; [S10] |
| PRIV-04 | Biometric enrolment only with explicit consent, showing the alternative in the consent form; an equivalent free alternative (code or app); one-click withdrawal. | [S10] |
| PRIV-05 | DSAR tooling: search a person across modules; export (JSON/CSV/PDF); track the 1-month deadline. | Art. 12, 15, 20 [S1] |
| PRIV-06 | Erasure workflow honouring retention holds (bookkeeping 5 years, time records 5 years, CCTV 30 days or less). | Art. 17 [S1]; [S14][S15][S11] |
| PRIV-07 | Restriction and objection flags. | Art. 18, 21 [S1] |
| PRIV-08 | Age capture; guardian consent below 15 (DK) for consent-based ISS processing; guardian linkage. | Art. 8 [S1]; [S9] |
| PRIV-09 | Privacy notices versioned and linked from sign-up, app and employee portal; must include retention periods. | Art. 13 [S1]; [S28] |
| PRIV-10 | CPR number not collected for members unless the lawyer confirms a basis. | databeskyttelsesloven § 11 [S8] |
| PRIV-11 | Processor register with DPA status per vendor; data-location field. | Art. 28, Ch. V [S1] |
| PRIV-12 | DPIA template and records stored for biometrics, CCTV and health modules. | Art. 35 [S1]; [S5] |
| PRIV-13 | Marketing opt-in kept separate from membership terms. | Art. 7 [S1]; markedsføringsloven § 10 (UNVERIFIED) |
| PRIV-14 | CCTV (if integrated): 30-day maximum retention, legal-hold for disputes with notification, no linking to member identity without DPIA. | [S11][S4] |

### 10.4 NFR / legal-functional checklist

| ID | Requirement | Basis |
|---|---|---|
| NFR-01 | Daily working-time recording per employee; employee self-view; corrections audited; 5-year retention; exportable for the authorities. | [S12][S13][S14] |
| NFR-02 | Rest and 48-hour rule warnings; opt-out agreement records with withdrawal. | 2003/88/EC [S2]; [S13] |
| NFR-03 | Earliest-termination-date calculator (DK § 28: 5 + 1 months); cancellation possible online and by email; timestamped receipts. | [S16][S17][S29] |
| NFR-04 | Withdrawal-period tracking (14 days) for distance and off-premises sales; express request to start; pro-rata refunds. | 2011/83/EU [S3] |
| NFR-05 | Contract and terms versioning with acceptance evidence per member. | Art. 7 [S1]; 2011/83/EU [S3] |
| NFR-06 | Payment-mandate storage; SCA at set-up via the provider; MIT for renewals; failed-payment and refund handling for SEPA and Betalingsservice. | [S19][S20] |
| NFR-07 | Bookkeeping route A (integration with a registered system) **or** route B (meet BEK 205 in full). Decision needed. | [S15] |
| NFR-08 | E-invoicing (OIOUBL and Peppol BIS) and SAF-T export **if** route B. | [S15] |
| NFR-09 | Accessibility target for member-facing web and app (e.g. WCAG 2.1 AA) pending the EAA scope decision. | [S21] |
| NFR-10 | Data residency: primary data and backups in the EU/EEA (required for bookkeeping backups under route B; preferable for GDPR transfer simplicity). | [S15]; GDPR Ch. V [S1] |
| NFR-11 | Availability and offline mode for door access (members cannot be locked out by an outage; alternative entry path). | [S10] (alternative access); business continuity |
| NFR-12 | Børneattest tracking per employee who works with under-15s (if applicable). | [S27] |

---

## 11. Data categories and sensitivity

Sensitivity scale (proposed): **4** = special category or criminal (Art. 9/10) or biometric; **3** = high-risk ordinary data (national ID, financial, location/presence patterns, minors); **2** = ordinary personal data; **1** = low / pseudonymous.

| # | Category | Subjects | Examples | Sensitivity | Likely lawful basis (for review) | Retention anchor |
|---|---|---|---|---|---|---|
| 1 | Identity & contact | Members, guardians, staff | name, address, email, phone, date of birth | 2 (3 for minors) | Contract (6(1)(b)) | Membership + TBD; SATS 60-month warning [S28] |
| 2 | Membership & contract | Members | plan, start date, binding, cancellations, terms version, withdrawal | 2 | Contract; legal obligation | Limitation period (UNVERIFIED) / bookkeeping 5 years |
| 3 | Payment tokens & mandates | Members | PSP token, last 4 digits, mandate ID, IBAN (direct debit) | 3 | Contract | Bookkeeping 5 years [S15] |
| 4 | Financial postings & invoices | Members, business | invoices, payments, refunds | 2-3 | Legal obligation (6(1)(c)) | 5 years from financial-year end [S15] |
| 5 | Check-in / attendance logs | Members, staff | timestamps, door ID | 3 (behavioural pattern) | Contract / legitimate interests | TBD; statistics from biometrics need consent [S10] |
| 6 | Biometric templates | Members, (staff) | face or fingerprint template | **4** | Explicit consent 9(2)(a) [S10] | Until consent withdrawn or membership ends |
| 7 | Health data | Members, staff | health declaration, injuries, sick leave, trainer notes | **4** | 9(2)(a)/(f)/(b) (lawyer) | TBD (short) |
| 8 | CCTV footage | Members, staff, visitors | video | 3-4 (4 if used for biometric ID) | Legitimate interests + TV-overvågningsloven | Max 30 days (DK) [S11] |
| 9 | National ID (CPR) | Staff (members: avoid) | CPR number | 3 | Legal obligation (payroll); § 11 DBL [S8] | Per payroll/tax rules (UNVERIFIED) |
| 10 | Employment records | Staff | contract, salary, bank account, appraisals | 3 | Contract; legal obligation; Art. 88 | TBD (lawyer) |
| 11 | Working-time records | Staff | daily hours, breaks, opt-out | 2-3 | Legal obligation | 5 years after reference period [S14] |
| 12 | Criminal record (børneattest) | Staff | attestation result | **4** (Art. 10) | Legal obligation (if applicable) [S27] | TBD (lawyer) |
| 13 | Minors' data | Members < 18 | all of the above + guardian link | 3-4 | Contract via guardian; § 6 DBL [S9] | TBD |
| 14 | Consent & DSAR records | All | consent ledger, request logs | 2 | Legal obligation (accountability) | TBD |
| 15 | Audit & security logs | Staff, members | user, action, IP | 2-3 | Legitimate interests / legal obligation | TBD; BEK 205 change logs follow bookkeeping retention [S15] |
| 16 | Marketing preferences | Members, leads | opt-ins, campaign data | 2 | Consent | Until withdrawal |

---

## 12. US differences (brief; mostly UNVERIFIED)

- **No federal omnibus privacy law.** State laws apply instead, e.g. California CCPA/CPRA, which applies above revenue and volume thresholds (UNVERIFIED thresholds).
- **Biometrics:** Illinois BIPA (740 ILCS 14) requires written consent and a public retention policy, and gives a private right of action. It has driven litigation against gyms and employers (UNVERIFIED; search returned no results).
- **Health:** HIPAA generally does not cover gyms, because they are not covered entities (UNVERIFIED).
- **Gym contracts:**
  - Many states have "health studio" statutes. California Civil Code § 1812.80 et seq. gives a 5-day cancellation window after signing (raised from 3 days by AB 2810), longer windows for large prepayments, and a maximum term (secondary) [S30].
  - The FTC "click-to-cancel" rule was **vacated** by the Eighth Circuit on 8 July 2025. ROSCA and state auto-renewal laws (e.g. California) still apply [S31].
- **Working time:** The FLSA requires recordkeeping of hours for non-exempt employees (29 CFR Part 516; UNVERIFIED in this research). There is no CJEU-style daily-recording mandate beyond that.
- **Accessibility:** ADA Title III applies to places of public accommodation. There is no codified web standard for private businesses (UNVERIFIED).
- **Payments:** PCI DSS applies identically. SCA/PSD2 does not apply in the US.

---

## 13. Needs lawyer review

1. Lawful basis and Art. 9 exception for each health-data use (declaration, injury reports, trainer notes, body measurements).
2. Biometric access:
   - validity of consent, adequacy of the alternative
   - DPIA
   - applicability of the **EU AI Act** (1:1 vs 1:N) and its deadlines
   - use for employees
3. Whether a DPO is required (chain size, biometrics at scale).
4. Retention periods for former members, employment records, consent logs and DSAR logs. Applicable limitation periods.
5. Forbrugeraftaleloven § 28 application to the chosen plan types (prepaid annual, student, corporate). Whether § 28(4) could ever apply. Price-increase clauses.
6. EU 14-day withdrawal mechanics, and whether in-person sign-ups count as off-premises in some scenarios.
7. Contract capacity of minors (værgemålsloven); guardian consent beyond GDPR § 6.
8. Børneattest obligation for the specific services offered to under-15s.
9. EAA scope (is membership sign-up an "e-commerce service"?) and microenterprise status.
10. Bookkeeping route A vs B; whether a self-built system needs an auditor's declaration; implications if the software is later sold to other gyms (registration requirement).
11. CCTV placement, signage, POLCAM registration, and any linkage with access logs.
12. CPR-number processing for members (if requested) and employees.
13. Employee monitoring through access logs, CCTV and time tracking (Danish employment practice; information duties to employees).
14. Cross-border transfers if any vendor (hosting, email, payment) is non-EU.
15. Danish card-surcharge rules and Betalingsservice creditor agreement terms.
16. US equivalents, if the country of operation turns out to be the US.

---

## 14. Open questions for the project owner

1. **Country of operation:** Where will the centre operate (Denmark, another EU state, the US)? How many sites?
2. **Business size:** Number of employees and approximate annual turnover. This determines EAA microenterprise status, DPO likelihood and Danish bookkeeping phase-in.
3. **Legal form:** ApS/A/S, sole proprietorship or association (forening)? This affects bookkeeping obligations and børneattest rules.
4. **Biometrics:** Do you want face or fingerprint entry? If yes, for members, employees or both, and what alternative entry method would you offer at no extra cost?
5. **Health data:** Do you need health declarations, injury records or trainer notes in the system at all? Who must see them?
6. **CCTV:** Is it in scope for this system, or a separate vendor system? Should it link to check-ins?
7. **Minors:** Will you accept members under 18? Under 15? Will staff run classes for under-15s?
8. **Bookkeeping:** Should the system integrate with an existing registered bookkeeping system (e.g. your accountant's), or should it perform bookkeeping itself?
9. **Payments:** Which payment methods (card, MobilePay, Betalingsservice, SEPA, cash at reception)? Do you already have a payment provider?
10. **Sales channels:** Online, in-app, in person, phone, corporate deals? This drives withdrawal-right handling.
11. **Membership terms:** Binding periods, prepaid plans, freezes and price-change practices you intend to use.
12. **Payroll:** Is payroll done in this system or by an external provider or accountant? Should time tracking feed it?
13. **Hosting:** Is EU/EEA-only hosting acceptable or required? Self-hosted or cloud?
14. **Future scope:** Is this only for your own gym, or might it be sold or licensed to other gyms? That changes bookkeeping registration and processor/controller roles.
15. **Security target:** Which OWASP ASVS level do you want to target (L1, L2, or L2 for sensitive modules only)?
16. **Accessibility target:** Will you aim for WCAG AA even if exempt under the EAA?
17. **Existing vendors:** Do you already have access-control hardware or software, a CCTV vendor or an email/SMS provider (each needs a DPA)?

---

## Sources

Primary or official sources are marked **(official)**. All others are secondary and should be confirmed.

- [S1] Regulation (EU) 2016/679 (GDPR) **(official)**: https://eur-lex.europa.eu/eli/reg/2016/679/oj
- [S2] Directive 2003/88/EC (Working Time Directive) **(official)**: https://eur-lex.europa.eu/eli/dir/2003/88/oj
- [S3] Directive 2011/83/EU (Consumer Rights Directive) **(official)**: https://eur-lex.europa.eu/eli/dir/2011/83/oj
- [S4] EDPB Guidelines 3/2019 on processing of personal data through video devices, v2.0, adopted 29 January 2020 **(official)**: https://www.edpb.europa.eu/sites/default/files/files/file1/edpb_guidelines_201903_video_devices_en.pdf
- [S5] Article 29 WP, Guidelines on DPIA (WP248 rev.01), endorsed by the EDPB **(official)**: https://ec.europa.eu/newsroom/article29/items/611236 (not re-fetched in this research)
- [S6] Directive (EU) 2015/2366 (PSD2) and Delegated Regulation (EU) 2018/389 (RTS on SCA) **(official)**: https://eur-lex.europa.eu/eli/dir/2015/2366/oj ; https://eur-lex.europa.eu/eli/reg_del/2018/389/oj
- [S8] Databeskyttelsesloven § 11 (CPR numbers) text: https://danskelove.dk/databeskyttelsesloven/11 ; Legiscope commentary: https://www.legiscope.com/blog/cpr-nummer-indsamling-videregivelse.html ; Datatilsynet overview: https://www.datatilsynet.dk:443/Media/637647832459647592/Personoplysninger%20-%20f%C3%A5%20et%20hurtigt%20overblik.pdf **(official)**
- [S9] Databeskyttelsesloven § 6 (age 15): https://danskelove.dk/databeskyttelsesloven/6 ; Lovguiden LBK 289 of 08/03/2024: https://www.lovguiden.dk/loven/databeskyttelsesloven/6 ; Bill L 79 (2023-24) **(official)**: https://www.ft.dk/ripdf/samling/20231/lovforslag/l79/20231_l79_som_fremsat.pdf
- [S9b] Lovguiden on the EDPB children's privacy hub (Nov 2026): https://www.lovguiden.dk/en/det-offentlige/det-europaeiske-databeskyttelsesraad/2026-11-03-eu-data-protection-board-launches-hub-on-childrens-privacy-online
- [S10] Datatilsynet, "Samtykke til ansigtsgenkendelse i fitnesscenter var gyldigt", j.nr. 2023-31-0028, 3 July 2024 **(official)**: https://www.datatilsynet.dk/afgoerelser/afgoerelser/2024/jul/samtykke-til-ansigtsgenkendelse-i-fitnesscenter-var-gyldigt
- [S11] Datatilsynet, "Vejledning om tv-overvågning – private virksomheder" (June 2023) **(official)**: https://www.Datatilsynet.dk/Media/638234600620556007/Vejledning%20om%20tv-overv%C3%A5gning%20-%20private%20virksomheder.pdf ; TV-overvågningsloven: https://danskelove.dk/tv-overv%C3%A5gningsloven
- [S12] CJEU C-55/18 *CCOO v Deutsche Bank*, 14 May 2019, ECLI:EU:C:2019:402. Press release 61/19 **(official)**: https://curia.europa.eu/jcms/upload/docs/application/pdf/2019-05/cp190061en.pdf
- [S13] Bird & Bird, "Nye krav om registrering af arbejdstid i Danmark" (2024): https://www.twobirds.com/en/insights/2024/denmark/nye-krav-om-registrering-af-arbejdstid-i-danmark ; Deloitte: https://www.deloitte.com/dk/en/services/legal/perspectives/amendment-of-danish-act-on-working-hours.html
- [S14] Arbejdstidsloven § 4b (LBK 982 of 12/08/2024) via Lovguiden: https://www.lovguiden.dk/loven/arbejdstidsloven/4b ; DIF/DGI guide: https://klatreforbund.dk/_files/ugd/c10726_82a9c75fd364495997c6b311a6669478.pdf
- [S15] Erhvervsstyrelsen **(official)**:
  - Non-registered systems: https://erhvervsstyrelsen.dk/ikke-registrerede-digitale-bogfoeringssystemer
  - Bookkeeping guidance: https://erhvervsstyrelsen.dk/vejledning-bogfoering
  - Standard systems: https://erhvervsstyrelsen.dk/krav-til-digitale-standard-bogfoeringssystemer
  - New requirements for registered systems: https://erhvervsstyrelsen.dk/vejledning-nye-krav-til-registrerede-bogfoeringssystemer-effektiv-og-sikker-e-fakturering
- [S16] Forbrugeraftaleloven § 28 text: https://danskelove.dk/forbrugeraftaleloven/28 ; Lovguiden (LBK 1184 of 28/09/2025): https://www.lovguiden.dk/loven/forbrugeraftaleloven/28
- [S17] Forbrugerombudsmanden, "Fitness og lign." **(official)**: https://forbrugerombudsmanden.dk/alle-emner/abonnementer/fitness-og-lign
- [S18] PCI SSC SAQ A update (Jan 2025), via secondary sources: https://chat.securitymetrics.com/blog/guidance-on-saq-a-updates ; https://blog.report-uri.com/pci-dss-4-0-1-saq-a-and-client-side-security-a-plain-english-faq/ ; PCI SSC **(official)**: https://www.pcisecuritystandards.org/
- [S19] EBA Single Rulebook Q&A 2018_4131 (payee-initiated) **(official)**: https://www.eba.europa.eu/single-rule-book-qa/qna/view/publicId/2018_4131 ; Q&A 2018_4031: https://www.eba.europa.eu/single-rule-book-qa/qna/view/publicId/2018_4031 ; Q&A 2018_4048: https://www.eba.europa.eu/single-rule-book-qa/qna/view/publicId/2018_4048
- [S20] EBA Q&A 2019_4573 (SEPA refunds) **(official)**: https://www.eba.europa.eu/single-rule-book-qa/qna/view/publicId/2019_4573 ; Betalingsservice creditor rules (bank PDF): https://midspar.dk/wp-content/uploads/2026/01/Generelle-regler-for-kreditorer-i-Betalingsservice.pdf
- [S21] Directive (EU) 2019/882 (EAA) **(official)**: https://eur-lex.europa.eu/eli/dir/2019/882/oj ; Taylor Wessing on exemptions: https://www.taylorwessing.com/en/interface/2025/accessibility/key-eu-accessibility-act-exemptions-and-the-challenges-they-pose ; accessible.org: https://accessible.org/eaa-ecommerce-services-requirements/
- [S22] EU AI Act Annex III, via the Commission AI Act Service Desk **(official)**: https://ai-act-service-desk.ec.europa.eu/en/ai-act/annex-3
- [S23] OWASP ASVS project **(official)**: https://owasp.org/www-project-application-security-verification-standard/ ; GitHub: https://github.com/OWASP/ASVS ; SoftwareMill on 5.0: https://softwaremill.com/whats-new-in-asvs-5-0/
- [S26] Lovguiden, fitnesscenter practice overview: https://www.lovguiden.dk/praksisoversigt/branche/fitnesscenter ; Dansk Erhverv on fingerprint access approval: https://www.danskerhverv.dk/presse-og-nyheder/nyheder/2020/august/brug-af-fingeraftryk-som-adgangskontrol-godkendt-af-datatilsynet/
- [S27] Børneattest: virk.dk **(official)**: https://virk.dk/myndigheder/stat/POL/selvbetjening/Boerneattest/ ; borger.dk **(official)**: https://www.borger.dk/politi-retsvaesen-forsvar/politi/boerneattest ; Altinget on the reform: https://www.altinget.dk/idraet/artikel/organisationer-jubler-over-regeringens-forslag-om-boerneattesten
- [S28] IAPP, "Norwegian SA fines SATS for violating GDPR": https://iapp.org/news/a/norwegian-sa-fines-sats-for-violating-gdpr ; PPC Land on SATS check-in photos: https://ppc.land/norway-forces-sats-to-let-gym-members-object-to-check-in-photos/
- [S29] Forbrugerklagenævnet case 18-11554 **(official)**: https://fkn.naevneneshus.dk/media/documents/18-11554-25_18-11554_Afg%C3%B8relse_3836225_1_0.pdf ; Lovguiden overview: https://www.lovguiden.dk/praksisoversigt/forbrugerklage-urimelige-aftalevilkaar
- [S30] California AB 2810 Senate floor analysis **(official)**: https://www.leginfo.ca.gov/pub/15-16/bill/asm/ab_2801-2850/ab_2810_cfa_20160808_093403_sen_floor.html ; Nolo: https://www.nolo.com/legal-encyclopedia/do-i-have-to-pay-gym-membership-while-my-gym-is-temporarily-closed.html
- [S31] Fenwick, "Eighth Circuit Vacates FTC's Click-To-Cancel Rule": https://www.fenwick.com/insights/publications/eighth-circuit-vacates-ftcs-click-to-cancel-rule

*(S7, S24 and S25 are intentionally unused.)*

# Stakeholder Interview Guide

> **On hold (D-047, 2026-11-24).** We no longer wait for the meeting with the center; the owner decides these questions instead ("Owner decides" in [open-questions.md](open-questions.md)). The guide is kept in case a center conversation happens later.

For the owner's conversation(s) with the fitness center's management and staff (decision [D-004](decisions.md)).
Each question carries the ID from [open-questions.md](open-questions.md), so answers can be copied straight back into it.

## How to run it

- **Ask about what happens today, not what they want built.** "Walk me through what happens when a member cancels" gives better answers than "Do you want a cancel button?" Feature wishes are useful, but ask what problem each one solves.
- **Ask for the tools and documents they already use**: the current system, spreadsheets, contracts, price list, membership terms, the staff rota, a payslip setup. Ask whether you may get copies or screenshots. Anonymised copies are fine, and no real member data is needed for now.
- **Note pain points literally**, in their words, and how often each one happens.
- **Don't promise features or dates** during the conversation.
- **Talk to more than one role if possible.** A manager, someone at the front desk and one trainer each see different problems.
- Priority marks: ★ = must ask, blocks the PRD. The rest can come later.

---

## Part 1 — Management / owner (30–45 min)

### The business
- ★ What is your current system (or systems) for members, payments, bookings and staff? What do you like about it, and what made you ask for a new one? *(B2, F6)*
- ★ If the new system could fix only three things in the first version, what would they be? *(F6)*
- ★ Roughly how many active members do you have? How many sign up and how many leave per month? *(F4)*
- What legal form is the business: ApS/A/S, sole proprietorship, or association (forening)? Roughly what is the annual turnover? *(F7)* — this affects bookkeeping rules, VAT and the accessibility law.
- Is the center staffed during all opening hours, or are members alone in the building at times? *(F5)*
- Could this ever be used by other centers, or is it only for you? *(F1, multi-tenancy)*

### Memberships and prices
- ★ Which membership types do you sell? For example monthly, punch cards (klippekort), drop-in, family, student/senior, corporate, prepaid. Can we get the price list? *(M1)*
- ★ Are classes and PT included in the membership, or sold separately? *(D1)* — since 1 Jan 2026 this decides the VAT on each invoice line.
- Do you have binding periods? What are the notice periods? Can members pause (freeze)? Are pauses free or paid, and with what limits? *(M2, D7)*
- How do people sign up today: online, in person, by phone, through companies? *(M3)*
- How do members cancel today? Can they take a cancellation back? *(D8, U2)*
- How do you handle price increases for existing members? *(D5)*
- Do you have members under 18, or under 15? *(M4)*
- Do you accept Wellhub, Urban Sports Club or ClassPass members? *(M7)*

### Payments and bookkeeping
- ★ How do members pay: card, Betalingsservice, MobilePay, cash, invoice? Which payment provider do you use, and do you have a PBS creditor number? *(P1, P2)*
- ★ Which bookkeeping system do you or your accountant use (e-conomic, Dinero, Billy, other)? *(P3)*
- What happens when a payment fails: retries, reminders, fees? When is a member's card blocked at the door? *(D11)*
- Do you sell anything at the counter, such as drinks, supplements or merchandise? *(P4)*
- **Ask whether we may send the accountant questions in D2** (VAT on classes by age, splitting mixed prices, punch-card VAT, credit notes).

### The door and the access card
- ★ What reader and door hardware is installed? Make and model, and if possible a photo of the reader and the control box. What card type do members have? *(A1, D-003)*
- ★ Does the card swipe unlock the door, or only register attendance? *(D-003)*
- ★ When a member swipes, how do you know whether they came for a class, a PT session or normal training? Is there one reader or several (for example at the spinning room)? *(D-003)*
- If the internet goes down, should the door stay closed, open, or still let known cards in? *(A2)*
- Is there CCTV? Is it connected to any system? *(A4)*

### Staff
- ★ Which roles do you have (front desk, instructors, PTs, cleaning, managers)? Are they employees or freelancers? *(E1)*
- ★ How is each role paid: hourly, salary, per class, per participant, PT commission, revenue share, rent? *(E2, D14)*
- ★ Who does payroll today (Danløn, accountant, other)? Should the new system calculate pay, or only deliver hours and figures? *(E3)*
- How is the staff rota made today (Planday, spreadsheet, paper)? Are shift swaps and holiday requests part of it? *(E5)*
- How is working time registered today? Danish law has required it since 1 July 2024. *(U8)*
- Do you track certificates such as first aid, PT education or børneattest? What should happen when one expires? *(E4, D15)*
- Who should be allowed to see what? For example, may trainers see member health information? *(D13, M5)*

### Members' health information
- Do members fill out a health declaration? Do you record injuries or trainer notes? Who needs to see them? *(M5)* — this is sensitive data under GDPR, so only collect it if there is a clear need.

### Reporting
- Which numbers do you look at daily, monthly and yearly? For example member count, sign-ups and cancellations, attendance, class fill rate, revenue. *(U9)*

---

## Part 2 — Front desk (15–20 min)

- Walk me through a normal shift. What takes the most time? *(U6)*
- What do members ask you for most often: payments, bookings, pauses, a new card?
- What can you do for a member in the current system, and what can't you do? *(U6)*
- What happens when a member forgets their card, or a card doesn't work?
- Which devices do you use: PC, tablet, a separate screen or kiosk? *(U7)*

## Part 3 — Instructors / personal trainers (15–20 min)

- How do members book your classes or PT sessions today? Is there a waitlist? What happens with no-shows and late cancellations? *(U4, D10)*
- How is attendance recorded for your class or session? *(D-003)*
- How do you see your schedule and register your hours? How are you paid for classes and PT? *(E2, D14)*
- Do you keep notes on clients? Where? *(M5)*
- What is the most annoying part of the current tools?

## Part 4 — Things to collect (if they're willing)

- [ ] Price list and membership terms/contract text
- [ ] Screenshots of the current system (no member data needed)
- [ ] Photo of the card reader, the door controller and a member card
- [ ] Example of the staff rota
- [ ] How pay is calculated for each staff type (no names needed)
- [ ] Name of the payment provider, bookkeeping system and payroll provider
- [ ] Permission to send the accountant the VAT questions (D2)
- [ ] Who our contact person is going forward, and whether we may come back with follow-up questions

---

## After the interview

Write the answers into [open-questions.md](open-questions.md), using the question IDs, or paste your notes to the orchestrator. Answers that settle a medium or large decision go into [decisions.md](decisions.md), and then `vision.md` and `prd.md` can be drafted.

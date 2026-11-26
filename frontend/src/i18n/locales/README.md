# Translations

Danish (`da.json`) is the default language and English (`en.json`) is available (D-065).
Both files must have the same keys; `src/i18n/locales.test.ts` checks it.

## UNVERIFIED Danish wording

These Danish texts are translations by the developer, not checked wording. The owner
should check them before real members use them (FIT-53 covers the real terms texts).

| Keys                                                                                                                                                                 | Why                                                                                                                                                                |
| -------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| `signup.acceptTerms`, `signup.startNow`                                                                                                                              | The terms and the early-start consent are still placeholders (FIT-53).                                                                                             |
| `membership.cancellation.*`, `membership.cancelledOn`, `membership.accessUntil`                                                                                      | Cancellation wording. The research says cancel and withdrawal buttons need exact Danish wording ([ux-patterns.md §2.3](../../../../docs/research/ux-patterns.md)). |
| `classes.cancel`, `classes.cancelUntil`, `classes.cancelClosed`, `classes.errors.cancellation-closed`                                                                | Class booking cancellation ("afmelding").                                                                                                                          |
| `classes.joinWaitlist`, `classes.leaveWaitlist`, `classes.waitlisted`, `classes.waitlistPlace`, `classes.errors.already-waitlisted`, `classes.errors.class-not-full` | Waitlist wording (D-051, FIT-79), translated by the developer.                                                                                                     |
| `classes.offered`, `classes.offerExpires`, `classes.acceptOffer`, `classes.declineOffer`, `classes.errors.offer-expired`, `offers.*`                                 | Waitlist offer wording (D-067, FIT-80), translated by the developer.                                                                                               |
| `loginCode.*`, `authenticator.*`                                                                                                                                     | Login code and authenticator setup (D-059), translated by the developer.                                                                                           |
| `staffRoles.*`, `staffInvite.*`, `account.sections.staff`, `account.inviteStaff`                                                                                     | Staff invite screen (D-058, D-063) and role names, translated by the developer.                                                                                    |
| `myClasses.*`, `account.myClasses`                                                                                                                                   | Instructor's classes screen (D-049, FIT-106), translated by the developer.                                                                                         |
| `staffClasses.*`, `account.staffClasses`                                                                                                                             | Class planning screens for managers and admins (D-062, D-068, FIT-107), translated by the developer.                                                               |

## Texts from the backend

Since FIT-101 the backend sends Danish or English too (D-065 amendment): emails in the
account's language, and the terms, early-start text and membership type name in the
request's `Accept-Language`. These Danish backend texts are also UNVERIFIED translations by
the developer, for the owner to check:

| Where                                                             | What                                                                                   |
| ----------------------------------------------------------------- | -------------------------------------------------------------------------------------- |
| `src/Modules/Members/Members/Domain/ConsentTexts.cs`              | Danish terms and early-start placeholders (FIT-53 brings the real wording).            |
| `src/Modules/Members/Members/Application/MembershipHandler.cs`    | Cancellation and undo emails ("opsigelse", "annullere opsigelsen", "afmeldt").         |
| `src/Modules/Members/Members/Application/PaymentReminders.cs`     | Payment reminder email.                                                                |
| `src/Modules/Identity/Identity/Infrastructure/SmtpEmailSender.cs` | Signup confirmation email.                                                             |
| `src/Modules/Identity/Identity/Application/StaffInvites.cs`       | Staff invite email. The role names in it stay English, as in the app.                  |
| `src/Modules/Booking/Booking/Application/OfferEmails.cs`          | Waitlist offer email ("Der er en plads til dig", D-067).                               |
| `src/Modules/Booking/Booking/Application/ClassChangeEmails.cs`    | Cancelled and moved class emails ("Dit hold er aflyst", "Dit hold er flyttet", D-068). |
| `src/Modules/Booking/Booking/Application/ClassChangeEmails.cs`    | Cancelled class email to the waitlist ("Et hold, du ventede på, er aflyst", D-069).    |
| `src/Modules/Members/Members/Domain/MembershipType.cs`            | "Almindeligt medlemskab" for the placeholder membership type.                          |

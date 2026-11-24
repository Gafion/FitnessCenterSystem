# Translations

Danish (`da.json`) is the default language and English (`en.json`) is available (D-065).
Both files must have the same keys; `src/i18n/locales.test.ts` checks it.

## UNVERIFIED Danish wording

These Danish texts are translations by the developer, not checked wording. The owner
should check them before real members use them (FIT-53 covers the real terms texts).

| Keys                                                                                                  | Why                                                                                                                                                                |
| ----------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| `signup.acceptTerms`, `signup.startNow`                                                               | The terms and the early-start consent are still placeholders (FIT-53).                                                                                             |
| `membership.cancellation.*`, `membership.cancelledOn`, `membership.accessUntil`                       | Cancellation wording. The research says cancel and withdrawal buttons need exact Danish wording ([ux-patterns.md §2.3](../../../../docs/research/ux-patterns.md)). |
| `classes.cancel`, `classes.cancelUntil`, `classes.cancelClosed`, `classes.errors.cancellation-closed` | Class booking cancellation ("afmelding").                                                                                                                          |
| `loginCode.*`, `authenticator.*`                                                                      | Login code and authenticator setup (D-059), translated by the developer.                                                                                           |

Texts from the backend (emails, the terms and early-start texts, the membership type
name) are still English (D-065, out of scope).

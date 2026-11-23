import { type APIRequestContext, expect, type Page } from '@playwright/test';

// Steps that several full-stack flows share. A new flow (e.g. check-in) starts from
// `signUpAndLogIn` and gets a fresh, confirmed, logged-in member.

export type Member = { firstName: string; email: string; password: string };

/** A new synthetic member on the reserved example.com domain (CLAUDE.md). */
export function syntheticMember(): Member {
  const id = `${Date.now()}-${Math.random().toString(36).slice(2, 8)}`;
  return {
    firstName: 'Test',
    email: `e2e-${id}@example.com`,
    password: 'correct-horse-battery',
  };
}

function mailpitUrl(): string {
  const url = process.env.MAILPIT_URL;
  if (!url) {
    throw new Error(
      'Set MAILPIT_URL to Mailpit’s web address, e.g. http://localhost:8025.',
    );
  }
  return url;
}

/**
 * The path and query of the newest confirmation link Mailpit caught for `email`.
 * Only the path is used, so the test opens it on its own base URL whatever host
 * the API put in the link.
 */
async function confirmationPath(
  request: APIRequestContext,
  email: string,
): Promise<string> {
  let link: string | undefined;
  await expect
    .poll(
      async () => {
        const search = await request.get(`${mailpitUrl()}/api/v1/search`, {
          params: { query: `to:"${email}"` },
        });
        const { messages } = (await search.json()) as {
          messages: { ID: string }[];
        };
        if (messages.length === 0) return undefined;
        // Mailpit lists the newest message first.
        const message = await request.get(
          `${mailpitUrl()}/api/v1/message/${messages[0].ID}`,
        );
        const { Text } = (await message.json()) as { Text: string };
        link = Text.match(/\S+\/confirm-email\?\S+/)?.[0];
        return link;
      },
      { message: `a confirmation email to ${email}`, timeout: 15_000 },
    )
    .toBeDefined();
  const url = new URL(link as string);
  return url.pathname + url.search;
}

/** Signs up with email and password and lands on "Check your email" (D-038). */
export async function signUp(page: Page, member: Member) {
  await page.goto('/signup');
  await page.getByLabel('First name').fill(member.firstName);
  await page.getByLabel('Last name').fill('Member');
  await page.getByLabel('Email').fill(member.email);
  await page.getByLabel(/^Password/).fill(member.password);
  await page.getByLabel('I accept the terms').check();
  await page.getByRole('button', { name: 'Sign up' }).click();
  await expect(
    page.getByRole('heading', { name: 'Check your email' }),
  ).toBeVisible();
}

/** Follows the link from the confirmation email. */
export async function confirmEmail(
  page: Page,
  request: APIRequestContext,
  member: Member,
) {
  await page.goto(await confirmationPath(request, member.email));
  await expect(
    page.getByRole('heading', { name: 'Your email is confirmed' }),
  ).toBeVisible();
}

/** Logs in and lands on the account page. */
export async function logIn(page: Page, member: Member) {
  await page.goto('/login');
  await page.getByLabel('Email').fill(member.email);
  await page.getByLabel('Password').fill(member.password);
  await page.getByRole('button', { name: 'Log in', exact: true }).click();
  await expect(
    page.getByRole('heading', { name: `Welcome, ${member.firstName}` }),
  ).toBeVisible();
}

/** A new member, signed up, confirmed and logged in, on the account page. */
export async function signUpAndLogIn(
  page: Page,
  request: APIRequestContext,
): Promise<Member> {
  const member = syntheticMember();
  await signUp(page, member);
  await confirmEmail(page, request, member);
  await logIn(page, member);
  return member;
}

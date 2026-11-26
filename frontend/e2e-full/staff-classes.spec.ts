import { expect, test } from '@playwright/test';
import { mailpitUrl, signUpAndLogIn } from './member';
import { devAdmin, logInAsStaff } from './staff';

// Full stack (D-043, D-068): the development admin schedules a class, a new member
// books it, the admin cancels it, and the member gets the cancellation email. The class
// type gets a name of its own, so the test finds its class among the seeded ones and
// those of earlier runs. Class types can't be removed (D-068), so each run adds one.

/** Tomorrow's date in Copenhagen as YYYY-MM-DD, for the date field. */
function tomorrowInCopenhagen(): string {
  return new Intl.DateTimeFormat('en-CA', {
    timeZone: 'Europe/Copenhagen',
    year: 'numeric',
    month: '2-digit',
    day: '2-digit',
  }).format(new Date(Date.now() + 86_400_000));
}

test('the admin cancels a booked class and the member is emailed', async ({
  page,
  browser,
  request,
  baseURL,
}) => {
  const className = `E2E ${Date.now()}-${Math.random().toString(36).slice(2, 6)}`;

  // The admin, in a browser of its own, adds the class type and schedules a class.
  const adminContext = await browser.newContext({ baseURL });
  const admin = await adminContext.newPage();
  try {
    await logInAsStaff(admin, devAdmin);
    await admin.getByRole('link', { name: 'Plan classes' }).click();
    await expect(
      admin.getByRole('heading', { name: 'Classes', level: 1 }),
    ).toBeVisible();
    await admin.getByLabel('New class type').fill(className);
    await admin.getByRole('button', { name: 'Add', exact: true }).click();
    await expect(
      admin
        .getByRole('list', { name: 'Class types' })
        .getByRole('listitem', { name: className }),
    ).toBeVisible();

    await admin.getByRole('button', { name: 'Schedule a class' }).click();
    const form = admin.getByRole('form', { name: 'Schedule a class' });
    await form.getByRole('combobox', { name: 'Class type' }).click();
    await admin.getByRole('option', { name: className }).click();
    await form.getByRole('combobox', { name: 'Instructor' }).click();
    // The development instructor is the only one seeded (D-060).
    await admin.getByRole('option').first().click();
    await form.getByLabel('Date').fill(tomorrowInCopenhagen());
    await form.getByLabel('Time').fill('12:00');
    await form.getByLabel('Length in minutes').fill('45');
    await form.getByLabel('Spots').fill('5');
    await form.getByRole('button', { name: 'Schedule class' }).click();
    await expect(form).toHaveCount(0);
    const staffItem = admin
      .getByRole('region', { name: 'Upcoming classes' })
      .getByRole('listitem', { name: className });
    await expect(staffItem).toContainText('0 of 5 spots booked');

    // A new member books it.
    const member = await signUpAndLogIn(page, request);
    await page.goto('/classes');
    const card = page.getByRole('article', { name: className });
    await card.getByRole('button', { name: 'Book', exact: true }).click();
    await expect(card.getByText("You're booked.")).toBeVisible();

    // The admin cancels it, after the question that names the email.
    await admin.reload();
    await expect(staffItem).toContainText('1 of 5 spots booked');
    await staffItem.getByRole('button', { name: 'Cancel class' }).click();
    await expect(staffItem).toContainText(
      'The 1 booked member gets an email that the class is cancelled, and the booking is cancelled.',
    );
    await staffItem
      .getByRole('button', { name: 'Yes, cancel the class' })
      .click();
    await expect(
      staffItem.getByText('Cancelled', { exact: true }),
    ).toBeVisible();

    // The cancellation email arrives in Mailpit, in the member's language (D-065).
    await expect
      .poll(
        async () => {
          const search = await request.get(`${mailpitUrl()}/api/v1/search`, {
            params: {
              query: `to:"${member.email}" subject:"Your class is cancelled"`,
            },
          });
          const { messages } = (await search.json()) as {
            messages: { ID: string }[];
          };
          if (messages.length === 0) return '';
          const message = await request.get(
            `${mailpitUrl()}/api/v1/message/${messages[0].ID}`,
          );
          return ((await message.json()) as { Text: string }).Text;
        },
        { message: 'the cancellation email', timeout: 15_000 },
      )
      .toContain(`We have cancelled ${className}`);

    // The member's page no longer offers the class.
    await page.reload();
    await expect(page.getByRole('article', { name: className })).toHaveCount(0);
  } finally {
    await adminContext.close();
  }
});

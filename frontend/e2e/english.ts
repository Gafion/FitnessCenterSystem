// The app defaults to Danish (D-065). The specs were written against the English
// texts, so every browser starts with English chosen, as if the visitor had picked
// it with the language switch (it is kept in localStorage under "language").
// danish.spec.ts clears this to test the default.
export function englishChosen(baseURL: string) {
  return {
    cookies: [],
    origins: [
      {
        origin: new URL(baseURL).origin,
        localStorage: [{ name: 'language', value: 'en' }],
      },
    ],
  };
}

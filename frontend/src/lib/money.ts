// Amounts are Danish kroner, shown the Danish way ("299,00 kr.") in both languages:
// the owner chose `kr.` amounts (D-065), and English Intl would write "DKK 299.00".
const kroner = new Intl.NumberFormat('da-DK', {
  style: 'currency',
  currency: 'DKK',
});

/** Formats an amount in øre (the API's integer money unit) as Danish kroner. */
export function formatOre(ore: number): string {
  return kroner.format(ore / 100);
}

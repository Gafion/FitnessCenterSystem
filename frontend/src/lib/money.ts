const kroner = new Intl.NumberFormat('da-DK', {
  style: 'currency',
  currency: 'DKK',
});

/** Formats an amount in øre (the API's integer money unit) as Danish kroner. */
export function formatOre(ore: number): string {
  return kroner.format(ore / 100);
}

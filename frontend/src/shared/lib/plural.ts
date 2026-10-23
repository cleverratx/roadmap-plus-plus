export function plural(count: number, forms: readonly [one: string, few: string, many: string]): string {
  const lastTwo = Math.abs(count) % 100
  const last = lastTwo % 10

  if (lastTwo >= 11 && lastTwo <= 14) {
    return forms[2]
  }

  if (last === 1) {
    return forms[0]
  }

  return last >= 2 && last <= 4 ? forms[1] : forms[2]
}

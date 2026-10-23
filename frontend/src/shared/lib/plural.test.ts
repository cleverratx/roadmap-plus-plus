import { describe, expect, it } from 'vitest'
import { plural } from './plural'

const forms = ['участник', 'участника', 'участников'] as const

describe('plural', () => {
  it('follows Russian rules', () => {
    expect([1, 2, 5, 11, 12, 14, 21, 22, 25, 101, 111].map((n) => `${n} ${plural(n, forms)}`)).toEqual([
      '1 участник',
      '2 участника',
      '5 участников',
      '11 участников',
      '12 участников',
      '14 участников',
      '21 участник',
      '22 участника',
      '25 участников',
      '101 участник',
      '111 участников',
    ])
  })
})

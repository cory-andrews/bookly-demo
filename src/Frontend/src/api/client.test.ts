import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { getBookings } from './client'
import { formatPeriod } from '../lib/format'

function stubBooking(startsAtUtc: string, endsAtUtc: string) {
  vi.mocked(fetch).mockResolvedValue(
    new Response(
      JSON.stringify({ bookings: [{ id: '1', roomId: 'a', occupantName: 'Maya Chen', startsAtUtc, endsAtUtc }] }),
    ),
  )
}

describe('getBookings', () => {
  beforeEach(() => vi.stubGlobal('fetch', vi.fn()))
  afterEach(() => vi.unstubAllGlobals())

  it('runs the tests away from UTC', () => {
    expect(new Date('2026-11-05T12:00:00Z').getTimezoneOffset()).not.toBe(0)
  })

  it.each([
    ['no designator', '2026-11-05T09:30:00', '2026-11-05T10:30:00'],
    ['a Z designator', '2026-11-05T09:30:00Z', '2026-11-05T10:30:00Z'],
    ['fractional seconds', '2026-11-05T09:30:00.0000000', '2026-11-05T10:30:00.0000000'],
    ['an offset', '2026-11-05T10:30:00+01:00', '2026-11-05T11:30:00+01:00'],
  ])('reads times with %s as UTC', async (_, startsAtUtc, endsAtUtc) => {
    stubBooking(startsAtUtc, endsAtUtc)

    const [booking] = await getBookings('2026-11-05')

    expect(formatPeriod(booking.period)).toBe('09:30-10:30')
    expect(booking.period.start.toISODate()).toBe('2026-11-05')
  })

  it('keeps a late-evening booking on its UTC day', async () => {
    stubBooking('2026-11-05T23:00:00', '2026-11-05T23:30:00')

    const [booking] = await getBookings('2026-11-05')

    expect(booking.period.start.toISODate()).toBe('2026-11-05')
  })
})

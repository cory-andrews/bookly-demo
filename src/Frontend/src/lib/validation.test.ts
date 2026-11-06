import { DateTime, Interval } from 'luxon'
import { describe, expect, it } from 'vitest'
import type { Booking } from '../api/client'
import { validateBooking, type BookingFormValues } from './validation'

const DAY = '2026-11-05'

const form = (overrides: Partial<BookingFormValues> = {}): BookingFormValues => ({
  name: 'Sam Ortiz',
  roomId: 'room-a',
  startDay: DAY,
  startTime: '13:00',
  endDay: DAY,
  endTime: '14:00',
  ...overrides,
})

describe('validateBooking', () => {
  it('accepts a complete booking', () => {
    expect(validateBooking(form(), []).isValid).toBe(true)
  })

  it('reports every missing field', () => {
    const { errors, isValid } = validateBooking(form({ name: '  ', roomId: '', startTime: '', endTime: '' }), [])
    expect(isValid).toBe(false)
    expect(Object.keys(errors).sort()).toEqual(['end', 'name', 'room', 'start'])
  })

  it('rejects a name over the API limit', () => {
    expect(validateBooking(form({ name: 'x'.repeat(101) }), []).errors.name).toBeDefined()
  })

  it('rejects an end time before the start time', () => {
    const { errors } = validateBooking(form({ startTime: '14:00', endTime: '13:00' }), [])
    expect(errors.end).toContain('before it starts at 14:00')
  })

  it('rejects an end time equal to the start time', () => {
    const { errors } = validateBooking(form({ endTime: '13:00' }), [])
    expect(errors.end).toContain('Start and end are both 13:00')
  })

  it('rejects a booking that crosses midnight and offers a fix', () => {
    const { errors } = validateBooking(form({ startTime: '22:00', endTime: '01:00', endDay: '2026-11-06' }), [])
    expect(errors.end).toContain('same day')
    expect(errors.sameDayFix).toBe('End on Thu 5 Nov instead')
  })

  it('reports a conflict with a loaded booking', () => {
    const period = Interval.fromDateTimes(DateTime.fromISO(`${DAY}T13:00`), DateTime.fromISO(`${DAY}T13:20`))
    const existing: Booking = { id: '1', roomId: 'room-a', occupantName: 'Lena', period }
    const result = validateBooking(form(), [existing])
    expect(result.conflicts).toEqual([existing])
    expect(result.isValid).toBe(false)
  })
})

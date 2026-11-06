import { DateTime, Interval } from 'luxon'
import { describe, expect, it } from 'vitest'
import type { Booking, Room } from '../api/client'
import { findConflicts, freeRooms, nextFreePeriod } from './conflicts'
import { formatPeriod } from './format'

const DAY = '2026-11-05'

const rooms: Room[] = [
  { id: 'a', displayName: 'Room A' },
  { id: 'b', displayName: 'Room B' },
]

const period = (start: string, end: string, day = DAY) =>
  Interval.fromDateTimes(DateTime.fromISO(`${day}T${start}`), DateTime.fromISO(`${day}T${end}`))

const booking = (roomId: string, start: string, end: string): Booking => ({
  id: `${roomId}-${start}`,
  roomId,
  occupantName: 'Lena Weiss',
  period: period(start, end),
})

const bookings = [booking('a', '10:00', '11:00')]

describe('findConflicts', () => {
  it('flags an overlapping request', () => {
    expect(findConflicts(bookings, 'a', period('10:30', '11:30'))).toHaveLength(1)
  })

  it('allows adjacent bookings on either side', () => {
    expect(findConflicts(bookings, 'a', period('11:00', '12:00'))).toHaveLength(0)
    expect(findConflicts(bookings, 'a', period('09:00', '10:00'))).toHaveLength(0)
  })

  it('ignores other rooms and other days', () => {
    expect(findConflicts(bookings, 'b', period('10:00', '11:00'))).toHaveLength(0)
    expect(findConflicts(bookings, 'a', period('10:00', '11:00', '2026-11-06'))).toHaveLength(0)
  })
})

describe('nextFreePeriod', () => {
  it('finds the first later period of the same length', () => {
    expect(formatPeriod(nextFreePeriod(bookings, 'a', period('10:30', '11:30'))!)).toBe('11:00-12:00')
  })

  it('returns null when nothing fits before midnight', () => {
    const lateBooking = [booking('a', '22:00', '23:59')]
    expect(nextFreePeriod(lateBooking, 'a', period('22:00', '23:00'))).toBeNull()
  })
})

describe('freeRooms', () => {
  it('lists other rooms that are free for the same time', () => {
    expect(freeRooms(rooms, bookings, 'a', period('10:00', '11:00'))).toEqual([rooms[1]])
  })
})

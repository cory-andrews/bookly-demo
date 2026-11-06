import type { Interval } from 'luxon'
import type { Booking, Room } from '../api/client'

const SEARCH_STEP = { minutes: 15 }

// Matches the API: periods that only touch, like 10:00-11:00 and 11:00-12:00, don't overlap.
export const findConflicts = (bookings: Booking[], roomId: string, period: Interval) =>
  bookings.filter(b => b.roomId === roomId && b.period.overlaps(period))

// Finds the next later period in the same room with the same length. Bookings must end before midnight.
export function nextFreePeriod(bookings: Booking[], roomId: string, period: Interval) {
  const shift = (current: Interval) => current.mapEndpoints(time => time.plus(SEARCH_STEP))

  for (let candidate = shift(period); candidate.end.hasSame(period.start, 'day'); candidate = shift(candidate)) {
    if (!findConflicts(bookings, roomId, candidate).length) return candidate
  }
  return null
}

export const freeRooms = (rooms: Room[], bookings: Booking[], roomId: string, period: Interval) =>
  rooms.filter(r => r.id !== roomId && !findConflicts(bookings, r.id, period).length)

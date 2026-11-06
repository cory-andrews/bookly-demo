import { DateTime, Interval } from 'luxon'

export interface Room {
  id: string
  displayName: string
}

export interface Booking {
  id: string
  roomId: string
  occupantName: string
  period: Interval
}

export interface NewBooking {
  roomId: string
  occupantName: string
  period: Interval
}

interface BookingDto {
  id: string
  roomId: string
  occupantName: string
  startsAtUtc: string
  endsAtUtc: string
}

export interface ServerFieldErrors {
  name?: string
  room?: string
  start?: string
  end?: string
}

export class ApiError extends Error {}

export class ApiValidationError extends ApiError {
  constructor(readonly errors: ServerFieldErrors) {
    super('The API rejected the request.')
  }
}

const FIELD_BY_API_KEY: Record<string, keyof ServerFieldErrors> = {
  occupantname: 'name',
  roomid: 'room',
  startsatutc: 'start',
  endsatutc: 'end',
}

function toFieldErrors(errors: Record<string, string[]>) {
  const fields: ServerFieldErrors = {}
  for (const [key, messages] of Object.entries(errors)) {
    const field = FIELD_BY_API_KEY[key.toLowerCase()]
    if (field) fields[field] = messages[0]
  }
  return fields
}

function toBooking(dto: BookingDto): Booking {
  return {
    id: dto.id,
    roomId: dto.roomId,
    occupantName: dto.occupantName,
    period: Interval.fromDateTimes(DateTime.fromISO(dto.startsAtUtc), DateTime.fromISO(dto.endsAtUtc)),
  }
}

async function request(path: string, init?: RequestInit) {
  let response: Response
  try {
    response = await fetch(path, init)
  } catch {
    throw new ApiError('Could not reach the API.')
  }

  if (response.status === 400) {
    const problem = await response.json().catch(() => null)
    if (problem?.errors) throw new ApiValidationError(toFieldErrors(problem.errors))
  }

  return response
}

async function readJson<T>(response: Response): Promise<T> {
  if (!response.ok) throw new ApiError(`The API responded with ${response.status}.`)
  return response.json()
}

export async function getRooms() {
  const { rooms } = await readJson<{ rooms: Room[] }>(await request('/api/rooms'))
  return rooms.sort((a, b) => a.displayName.localeCompare(b.displayName))
}

export async function getBookings(day: string) {
  const query = new URLSearchParams({ date: day })
  const { bookings } = await readJson<{ bookings: BookingDto[] }>(await request(`/api/bookings?${query}`))
  return bookings.map(toBooking).sort((a, b) => a.period.start.toMillis() - b.period.start.toMillis())
}

export async function createBooking(booking: NewBooking) {
  const response = await request('/api/bookings', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({
      roomId: booking.roomId,
      occupantName: booking.occupantName,
      startsAtUtc: booking.period.start.toISO({ suppressMilliseconds: true }),
      endsAtUtc: booking.period.end.toISO({ suppressMilliseconds: true }),
    }),
  })
  return toBooking(await readJson<BookingDto>(response))
}

// Returns false when the booking no longer exists, e.g. someone else already cancelled it.
export async function cancelBooking(bookingId: string) {
  const response = await request(`/api/bookings/${bookingId}`, { method: 'DELETE' })
  if (response.status === 404) return false
  if (!response.ok) throw new ApiError(`The API responded with ${response.status}.`)
  return true
}

import { DateTime, Interval } from 'luxon'
import type { Booking } from '../api/client'
import { findConflicts } from './conflicts'
import { SHORT_DAY_FORMAT } from './format'

const MAX_NAME_LENGTH = 100

export interface BookingFormValues {
  name: string
  roomId: string
  startDay: string
  startTime: string
  endDay: string
  endTime: string
}

export interface FormErrors {
  name?: string
  room?: string
  start?: string
  end?: string
  sameDayFix?: string
}

const toDateTime = (day: string, time: string) => (day && time ? DateTime.fromISO(`${day}T${time}`) : null)

const shortDay = (day: string) => DateTime.fromISO(day).toFormat(SHORT_DAY_FORMAT)

// Mirrors CreateBookingRequestValidator in the API, which stays the authority on these rules.
export function validateBooking(form: BookingFormValues, bookings: Booking[]) {
  const errors: FormErrors = {}
  const start = toDateTime(form.startDay, form.startTime)
  const end = toDateTime(form.endDay, form.endTime)
  const name = form.name.trim()

  if (!name) errors.name = 'Add your name so others know who has the room.'
  else if (name.length > MAX_NAME_LENGTH) errors.name = `Keep the name to ${MAX_NAME_LENGTH} characters or fewer.`
  if (!form.roomId) errors.room = 'Choose which room you want to book.'
  if (!start) errors.start = 'Pick a start date and time.'
  if (!end) errors.end = 'Pick an end date and time.'

  let period: Interval | null = null
  if (start && end) {
    if (form.endDay !== form.startDay) {
      errors.end =
        form.endDay < form.startDay
          ? `The end date (${shortDay(form.endDay)}) is before the start date (${shortDay(form.startDay)}).`
          : `Bookings must start and end on the same day. This one runs from ${shortDay(form.startDay)} into ${shortDay(form.endDay)}. Book each day separately.`
      errors.sameDayFix = `End on ${shortDay(form.startDay)} instead`
    } else if (end.equals(start)) {
      errors.end = `Start and end are both ${form.startTime}. Choose an end time after ${form.startTime}.`
    } else if (end < start) {
      errors.end = `This ends at ${form.endTime}, before it starts at ${form.startTime}. Choose an end time after ${form.startTime}.`
    } else {
      period = Interval.fromDateTimes(start, end)
    }
  }

  const conflicts = period && form.roomId ? findConflicts(bookings, form.roomId, period) : []

  return {
    errors,
    period,
    conflicts,
    isValid: Object.keys(errors).length === 0 && period !== null && conflicts.length === 0,
  }
}

import userEvent from '@testing-library/user-event'
import { cleanup, fireEvent, render, screen, within } from '@testing-library/vue'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import App from './App.vue'

const DAY = '2026-11-05'
const ROOM_A = 'room-a'
const ROOM_B = 'room-b'

interface BookingDto {
  id: string
  roomId: string
  occupantName: string
  startsAtUtc: string
  endsAtUtc: string
}

const CONFLICT_MESSAGE = 'This room is already booked during the requested period.'

// In-memory stand-in for the API. Times are stored without a zone, as the real GET endpoint returns them.
let serverBookings: BookingDto[]
let requests: { method: string; url: string; body?: Record<string, string> }[]
let rejectNextCreate: (() => void) | null

const json = (body: unknown, status = 200) =>
  new Response(JSON.stringify(body), { status, headers: { 'Content-Type': 'application/json' } })

async function fakeFetch(input: RequestInfo | URL, init?: RequestInit) {
  const url = String(input)
  const method = init?.method ?? 'GET'
  const body = init?.body ? JSON.parse(String(init.body)) : undefined
  requests.push({ method, url, body })

  if (url === '/api/rooms') {
    return json({
      rooms: [
        { id: ROOM_B, displayName: 'Meeting Room B' },
        { id: ROOM_A, displayName: 'Meeting Room A' },
      ],
    })
  }

  if (url.startsWith('/api/bookings?')) {
    const date = new URL(url, 'http://test').searchParams.get('date')
    return json({ bookings: serverBookings.filter(b => b.startsAtUtc.startsWith(date!)) })
  }

  if (method === 'POST') {
    if (rejectNextCreate) {
      rejectNextCreate()
      rejectNextCreate = null
      return json({ errors: { RoomId: [CONFLICT_MESSAGE] } }, 400)
    }
    const created = { id: 'new', ...body }
    serverBookings.push({
      ...created,
      startsAtUtc: body.startsAtUtc.replace('Z', ''),
      endsAtUtc: body.endsAtUtc.replace('Z', ''),
    })
    return json(created)
  }

  if (method === 'DELETE') {
    serverBookings = serverBookings.filter(b => !url.endsWith(b.id))
    return new Response(null, { status: 200 })
  }

  throw new Error(`Unexpected request: ${method} ${url}`)
}

const booking = (id: string, roomId: string, occupantName: string, start: string, end: string): BookingDto => ({
  id,
  roomId,
  occupantName,
  startsAtUtc: `${DAY}T${start}:00`,
  endsAtUtc: `${DAY}T${end}:00`,
})

async function fillForm(user: ReturnType<typeof userEvent.setup>, room: string, start: string, end: string) {
  await user.type(screen.getByLabelText('Your name'), 'Sam Ortiz')
  await user.selectOptions(screen.getByLabelText('Room'), room)
  await fireEvent.update(screen.getByLabelText('Start time (UTC)'), start)
  await fireEvent.update(screen.getByLabelText('End time (UTC)'), end)
}

describe('booking a room', () => {
  beforeEach(() => {
    vi.useFakeTimers({ toFake: ['Date'], now: new Date(`${DAY}T08:00:00Z`) })
    vi.stubGlobal('fetch', vi.fn(fakeFetch))
    serverBookings = []
    requests = []
    rejectNextCreate = null
  })

  afterEach(() => {
    cleanup()
    vi.useRealTimers()
    vi.unstubAllGlobals()
  })

  it('shows existing bookings at their UTC times and labels the UI as UTC', async () => {
    serverBookings = [booking('1', ROOM_A, 'Maya Chen', '09:00', '10:00')]
    render(App)

    const column = await screen.findByTestId('room-Meeting Room A')
    expect(within(column).getByText('Maya Chen')).toBeTruthy()
    expect(within(column).getByText('09:00-10:00')).toBeTruthy()
    expect(screen.getByText('All times UTC')).toBeTruthy()
    expect(screen.getByText('Starts (UTC)')).toBeTruthy()
    expect(screen.getByText('Ends (UTC)')).toBeTruthy()
  })

  it('books a room through the API and shows it on the day view', async () => {
    const user = userEvent.setup()
    render(App)
    await screen.findByTestId('room-Meeting Room A')

    await fillForm(user, 'Meeting Room A', '13:00', '14:00')
    await user.click(screen.getByRole('button', { name: 'Book Meeting Room A, 13:00-14:00 UTC' }))

    const column = await screen.findByTestId('room-Meeting Room A')
    expect(await within(column).findByText('Sam Ortiz')).toBeTruthy()
    expect(screen.getByRole('status').textContent).toContain('Meeting Room A is booked for Sam Ortiz, 13:00-14:00 UTC.')
    expect(requests.find(r => r.method === 'POST')?.body).toEqual({
      roomId: ROOM_A,
      occupantName: 'Sam Ortiz',
      startsAtUtc: `${DAY}T13:00:00Z`,
      endsAtUtc: `${DAY}T14:00:00Z`,
    })
    expect((screen.getByLabelText('Your name') as HTMLInputElement).value).toBe('')
  })

  it('shows every missing field and sends nothing when submitted empty', async () => {
    const user = userEvent.setup()
    render(App)
    await screen.findByTestId('room-Meeting Room A')

    await user.click(screen.getByRole('button', { name: 'Book room' }))

    expect(screen.getByText('Add your name so others know who has the room.')).toBeTruthy()
    expect(screen.getByText('Choose which room you want to book.')).toBeTruthy()
    expect(screen.getByText('Pick a start date and time.')).toBeTruthy()
    expect(screen.getByText('Pick an end date and time.')).toBeTruthy()
    expect(requests.some(r => r.method === 'POST')).toBe(false)
  })

  it('warns about a clash with a loaded booking, offers alternatives, and sends nothing', async () => {
    serverBookings = [booking('1', ROOM_A, 'Lena Weiss', '10:00', '11:00')]
    const user = userEvent.setup()
    render(App)
    await screen.findByTestId('room-Meeting Room A')

    await fillForm(user, 'Meeting Room A', '10:30', '11:30')

    const alert = screen.getByRole('alert')
    expect(alert.textContent).toContain('Meeting Room A is already booked then')
    expect(alert.textContent).toContain('Lena Weiss has Meeting Room A 10:00-11:00 UTC.')
    expect(within(alert).getByRole('button', { name: 'Meeting Room B' })).toBeTruthy()

    await user.click(screen.getByRole('button', { name: 'Book room' }))
    expect(requests.some(r => r.method === 'POST')).toBe(false)

    await user.click(within(alert).getByRole('button', { name: 'Move to 11:00-12:00 UTC' }))
    expect(screen.queryByRole('alert')).toBeNull()
    expect(screen.getByRole('button', { name: 'Book Meeting Room A, 11:00-12:00 UTC' })).toBeTruthy()
  })

  it('explains the clash when someone else takes the slot after the page loaded', async () => {
    const user = userEvent.setup()
    render(App)
    await screen.findByTestId('room-Meeting Room A')
    await fillForm(user, 'Meeting Room A', '10:30', '11:30')
    rejectNextCreate = () => serverBookings.push(booking('2', ROOM_A, 'Lena Weiss', '10:00', '12:00'))

    await user.click(screen.getByRole('button', { name: 'Book Meeting Room A, 10:30-11:30 UTC' }))

    const alert = await screen.findByRole('alert')
    expect(alert.textContent).toContain('Lena Weiss has Meeting Room A 10:00-12:00 UTC.')
    expect(await screen.findByText('Lena Weiss')).toBeTruthy()
  })

  it("shows the server's message when the loaded data can't explain a rejection", async () => {
    const user = userEvent.setup()
    render(App)
    await screen.findByTestId('room-Meeting Room A')
    await fillForm(user, 'Meeting Room A', '10:30', '11:30')
    rejectNextCreate = () => {}

    await user.click(screen.getByRole('button', { name: 'Book Meeting Room A, 10:30-11:30 UTC' }))

    expect((await screen.findByRole('alert')).textContent).toContain(CONFLICT_MESSAGE)
  })

  it('cancels a booking after confirmation', async () => {
    serverBookings = [booking('1', ROOM_A, 'Maya Chen', '09:00', '10:00')]
    const user = userEvent.setup()
    render(App)

    await user.click(await screen.findByRole('button', { name: /Maya Chen/ }))
    expect(screen.getByText('1 hr')).toBeTruthy()
    await user.click(screen.getByRole('button', { name: 'Cancel booking' }))

    const dialog = screen.getByRole('dialog')
    expect(dialog.textContent).toContain('09:00-10:00 UTC')
    await user.click(within(dialog).getByRole('button', { name: 'Cancel booking' }))

    expect((await screen.findByRole('status')).textContent).toContain("Cancelled Maya Chen's booking")
    expect(screen.queryByText('09:00-10:00')).toBeNull()
    expect(requests.some(r => r.method === 'DELETE' && r.url === '/api/bookings/1')).toBe(true)
  })

  it('keeps the booking when cancelling is dismissed', async () => {
    serverBookings = [booking('1', ROOM_A, 'Maya Chen', '09:00', '10:00')]
    const user = userEvent.setup()
    render(App)

    await user.click(await screen.findByRole('button', { name: /Maya Chen/ }))
    await user.click(screen.getByRole('button', { name: 'Cancel booking' }))
    await user.click(screen.getByRole('button', { name: 'Keep booking' }))

    expect(screen.queryByRole('dialog')).toBeNull()
    expect(requests.some(r => r.method === 'DELETE')).toBe(false)
  })

  it('offers a retry when the API is unreachable', async () => {
    vi.mocked(fetch).mockRejectedValueOnce(new TypeError('Failed to fetch'))
    const user = userEvent.setup()
    render(App)

    expect((await screen.findByRole('alert')).textContent).toContain("couldn't load the bookings")

    await user.click(screen.getByRole('button', { name: 'Try again' }))

    expect(await screen.findByTestId('room-Meeting Room A')).toBeTruthy()
  })
})

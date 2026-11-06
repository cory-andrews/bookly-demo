import { ref, shallowRef, watch, type Ref } from 'vue'
import { getBookings, getRooms, type Booking, type Room } from '../api/client'

export function useBookings(day: Ref<string>) {
  const rooms = ref<Room[]>([])
  const bookings = shallowRef<Booking[]>([])
  const isLoading = ref(true)
  const hasLoadError = ref(false)

  // Only the newest request may apply, so a slow response can't overwrite a day picked since.
  let latestRequest = 0

  async function refresh() {
    const request = ++latestRequest
    try {
      if (!rooms.value.length) rooms.value = await getRooms()
      const dayBookings = await getBookings(day.value)
      if (request !== latestRequest) return
      bookings.value = dayBookings
      hasLoadError.value = false
    } catch {
      if (request === latestRequest) hasLoadError.value = true
    } finally {
      if (request === latestRequest) isLoading.value = false
    }
  }

  watch(day, refresh)

  return { rooms, bookings, isLoading, hasLoadError, refresh }
}

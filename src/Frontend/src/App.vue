<script setup lang="ts">
import { DateTime, type Interval } from 'luxon'
import { computed, nextTick, onBeforeUnmount, onMounted, ref, shallowRef } from 'vue'
import { ApiValidationError, cancelBooking, createBooking, type ServerFieldErrors } from './api/client'
import AppHeader from './components/AppHeader.vue'
import BookingDetails from './components/BookingDetails.vue'
import BookingForm from './components/BookingForm.vue'
import type { Notice } from './components/ConflictNotice.vue'
import ConfirmDialog from './components/ConfirmDialog.vue'
import DayGrid, { type BookingPreview } from './components/DayGrid.vue'
import ToastMessage from './components/ToastMessage.vue'
import { useBookings } from './composables/useBookings'
import { freeRooms, nextFreePeriod } from './lib/conflicts'
import { formatPeriod, SHORT_DAY_FORMAT, TIME_FORMAT } from './lib/format'
import { validateBooking, type BookingFormValues } from './lib/validation'

const TOAST_MS = 6000
const FLASH_MS = 2500
const CLOCK_TICK_MS = 30_000
const DEFAULT_SLOT = { hours: 1 }

const emptyForm = (day: string): BookingFormValues => ({
  name: '',
  roomId: '',
  startDay: day,
  startTime: '',
  endDay: day,
  endTime: '',
})

const now = shallowRef(DateTime.now())
const today = computed(() => now.value.toISODate())

const viewDay = ref(today.value)
const { rooms, bookings, isLoading, hasLoadError, refresh } = useBookings(viewDay)

const form = ref(emptyForm(viewDay.value))
const bookingForm = ref<InstanceType<typeof BookingForm>>()
const submitted = ref(false)
const submitting = ref(false)
const serverErrors = ref<ServerFieldErrors>({})
const submitFailed = ref(false)

const selectedId = ref<string | null>(null)
const confirmingCancel = ref(false)
const cancelling = ref(false)
const cancelFailed = ref(false)

const toast = ref<string | null>(null)
const flashId = ref<string | null>(null)

let toastTimer: ReturnType<typeof setTimeout>
let flashTimer: ReturnType<typeof setTimeout>
let clockTimer: ReturnType<typeof setInterval>

onMounted(() => {
  refresh()
  clockTimer = setInterval(() => (now.value = DateTime.now()), CLOCK_TICK_MS)
})

onBeforeUnmount(() => {
  clearTimeout(toastTimer)
  clearTimeout(flashTimer)
  clearInterval(clockTimer)
})

const check = computed(() => validateBooking(form.value, bookings.value))

const errors = computed(() => {
  const { errors: client } = check.value
  const { name, startTime, endTime, startDay } = form.value
  const showRequired = submitted.value
  // Required-field errors wait for a submit; time errors appear as soon as both times are in.
  const showTimes = showRequired || Boolean(startTime && endTime && startDay)
  return {
    name: (showRequired || name ? client.name : undefined) ?? serverErrors.value.name,
    room: showRequired ? client.room : undefined,
    start: (showTimes ? client.start : undefined) ?? serverErrors.value.start,
    end: (showTimes ? client.end : undefined) ?? serverErrors.value.end,
    sameDayFix: showTimes ? client.sameDayFix : undefined,
  }
})

const conflictIds = computed(() => new Set(check.value.conflicts.map(b => b.id)))

const roomName = (roomId: string) => rooms.value.find(r => r.id === roomId)?.displayName ?? 'this room'

const preview = computed<BookingPreview | null>(() => {
  const { period, conflicts } = check.value
  const { roomId, startDay } = form.value
  if (!period || !roomId || startDay !== viewDay.value) return null
  return { roomId, period, clashes: conflicts.length > 0 }
})

const notice = computed<Notice | null>(() => {
  const { conflicts, period } = check.value
  const { roomId } = form.value

  if (period && conflicts.length) {
    const room = roomName(roomId)
    const next = nextFreePeriod(bookings.value, roomId, period)
    return {
      title: `${room} is already booked then`,
      items: conflicts.map(b => `${b.occupantName} has ${room} ${formatPeriod(b.period)} UTC.`),
      next: next ? { label: `Move to ${formatPeriod(next)} UTC`, period: next } : undefined,
      freeRooms: freeRooms(rooms.value, bookings.value, roomId, period),
    }
  }

  // The server rejected a booking the loaded data can't explain, so show its message as-is.
  if (serverErrors.value.room) return { title: "We couldn't book this room", items: [serverErrors.value.room] }
  if (submitFailed.value) {
    return { title: "We couldn't save this booking", items: ['Check that the API is running, then try again.'] }
  }
  return null
})

const submitLabel = computed(() => {
  const { isValid, period } = check.value
  return isValid && period ? `Book ${roomName(form.value.roomId)}, ${formatPeriod(period)} UTC` : 'Book room'
})

const selected = computed(() => bookings.value.find(b => b.id === selectedId.value) ?? null)

const describeSelected = computed(() => {
  const booking = selected.value
  if (!booking) return ''
  const day = booking.period.start.toFormat(SHORT_DAY_FORMAT)
  return `${booking.occupantName} · ${roomName(booking.roomId)} · ${day}, ${formatPeriod(booking.period)} UTC.`
})

function showToast(text: string) {
  clearTimeout(toastTimer)
  toast.value = text
  toastTimer = setTimeout(() => (toast.value = null), TOAST_MS)
}

function clearSubmitErrors() {
  serverErrors.value = {}
  submitFailed.value = false
}

function setDay(day: string) {
  const { startDay, endDay } = form.value
  form.value = { ...form.value, startDay: day, endDay: endDay === startDay ? day : endDay }
  viewDay.value = day
  selectedId.value = null
  clearSubmitErrors()
}

function patchForm(patch: Partial<BookingFormValues>) {
  clearSubmitErrors()
  if (patch.startDay) {
    setDay(patch.startDay)
    return
  }
  form.value = { ...form.value, ...patch }
}

function chooseSlot(roomId: string, minutesIntoDay: number) {
  const start = DateTime.fromISO(viewDay.value).plus({ minutes: minutesIntoDay })
  const end = DateTime.min(start.plus(DEFAULT_SLOT), start.endOf('day'))
  patchForm({ roomId, startTime: start.toFormat(TIME_FORMAT), endTime: end.toFormat(TIME_FORMAT) })
  selectedId.value = null
  submitted.value = false
  nextTick(() => {
    if (!form.value.name) bookingForm.value?.focusName()
  })
}

function moveBooking(period: Interval) {
  patchForm({ startTime: period.start.toFormat(TIME_FORMAT), endTime: period.end.toFormat(TIME_FORMAT) })
}

function clearForm() {
  submitted.value = false
  clearSubmitErrors()
  form.value = emptyForm(viewDay.value)
}

async function submit() {
  submitted.value = true
  clearSubmitErrors()
  const { isValid, period } = check.value
  if (!isValid || !period) return

  const { roomId } = form.value
  const occupantName = form.value.name.trim()

  submitting.value = true
  try {
    const booking = await createBooking({ roomId, occupantName, period })
    await refresh()
    submitted.value = false
    form.value = { ...form.value, name: '', startTime: '', endTime: '' }
    clearTimeout(flashTimer)
    flashId.value = booking.id
    flashTimer = setTimeout(() => (flashId.value = null), FLASH_MS)
    showToast(`${roomName(roomId)} is booked for ${occupantName}, ${formatPeriod(period)} UTC.`)
  } catch (error) {
    if (error instanceof ApiValidationError) {
      serverErrors.value = error.errors
      // Someone else may have just taken the slot. Reloading lets the normal conflict notice explain it.
      if (error.errors.room) await refresh()
    } else {
      submitFailed.value = true
    }
  } finally {
    submitting.value = false
  }
}

async function confirmCancel() {
  const booking = selected.value
  if (!booking) return

  cancelling.value = true
  cancelFailed.value = false
  try {
    const removed = await cancelBooking(booking.id)
    await refresh()
    confirmingCancel.value = false
    selectedId.value = null
    showToast(
      removed
        ? `Cancelled ${booking.occupantName}'s booking in ${roomName(booking.roomId)}, ${formatPeriod(booking.period)} UTC.`
        : 'That booking had already been cancelled.',
    )
  } catch {
    cancelFailed.value = true
  } finally {
    cancelling.value = false
  }
}

function keepBooking() {
  confirmingCancel.value = false
  cancelFailed.value = false
}
</script>

<template>
  <div class="app">
    <AppHeader :day="viewDay" :today="today" @update:day="setDay" />

    <main class="main">
      <section class="panel day-panel">
        <div v-if="hasLoadError" role="alert" class="status">
          <p>We couldn't load the bookings. Check that the API is running.</p>
          <button class="btn btn-secondary" @click="refresh">Try again</button>
        </div>
        <div v-else-if="isLoading" class="status">Loading bookings…</div>
        <DayGrid
          v-else
          :rooms="rooms"
          :bookings="bookings"
          :selected-id="selectedId"
          :flash-id="flashId"
          :conflict-ids="conflictIds"
          :preview="preview"
          :now="viewDay === today ? now : null"
          @select="selectedId = $event"
          @slot="chooseSlot"
        />
      </section>

      <aside class="panel side-panel">
        <BookingDetails
          v-if="selected"
          :booking="selected"
          :room-name="roomName(selected.roomId)"
          @back="selectedId = null"
          @cancel="confirmingCancel = true"
        />
        <BookingForm
          v-else
          ref="bookingForm"
          :form="form"
          :rooms="rooms"
          :errors="errors"
          :room-invalid="check.conflicts.length > 0"
          :notice="notice"
          :submit-label="submitLabel"
          :submitting="submitting"
          @patch="patchForm"
          @submit="submit"
          @clear="clearForm"
          @move="moveBooking"
        />
      </aside>
    </main>

    <ConfirmDialog
      v-if="confirmingCancel && selected"
      :summary="describeSelected"
      :error="cancelFailed ? `We couldn't cancel this booking. Try again.` : null"
      :busy="cancelling"
      @keep="keepBooking"
      @confirm="confirmCancel"
    />

    <ToastMessage v-if="toast" :text="toast" @dismiss="toast = null" />
  </div>
</template>

<style scoped>
.app {
  min-height: 100vh;
  display: flex;
  flex-direction: column;
}

.main {
  flex: 1;
  display: flex;
  flex-wrap: wrap;
  align-items: flex-start;
  gap: var(--space-6);
  padding: 0 var(--space-6) var(--space-6);
}

.panel {
  background: var(--color-surface);
  border-radius: calc(var(--radius-lg) * 1.15);
}

.day-panel {
  flex: 1 1 640px;
  min-width: 0;
  overflow: hidden;
}

.side-panel {
  flex: 0 1 380px;
  min-width: 300px;
  padding: var(--space-6);
  display: flex;
  flex-direction: column;
  gap: var(--space-4);
}

.status {
  padding: var(--space-6);
  font-size: 14px;
  color: var(--color-neutral-800);
}
</style>

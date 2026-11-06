<script setup lang="ts">
import type { Interval } from 'luxon'
import { ref } from 'vue'
import type { Room } from '../api/client'
import type { BookingFormValues, FormErrors } from '../lib/validation'
import ConflictNotice, { type Notice } from './ConflictNotice.vue'
import FieldError from './FieldError.vue'

defineProps<{
  form: BookingFormValues
  rooms: Room[]
  errors: FormErrors
  roomInvalid: boolean
  notice: Notice | null
  submitLabel: string
  submitting: boolean
}>()

const emit = defineEmits<{
  patch: [patch: Partial<BookingFormValues>]
  submit: []
  clear: []
  move: [period: Interval]
}>()

const nameInput = ref<HTMLInputElement>()

function focusName() {
  nameInput.value?.focus()
}

defineExpose({ focusName })

const valueOf = (event: Event) => (event.target as HTMLInputElement | HTMLSelectElement).value
</script>

<template>
  <div>
    <h3 class="heading">Book a room</h3>
    <p class="hint">Fill in the details, or click an empty slot in the day view.</p>
  </div>

  <div class="field">
    <label for="booking-name">Your name</label>
    <input
      id="booking-name"
      ref="nameInput"
      class="input"
      :class="{ invalid: errors.name }"
      placeholder="e.g. Sam Ortiz"
      :value="form.name"
      @input="emit('patch', { name: valueOf($event) })"
    />
    <FieldError :message="errors.name" />
  </div>

  <div class="field">
    <label for="booking-room">Room</label>
    <select
      id="booking-room"
      class="input"
      :class="{ invalid: errors.room || roomInvalid }"
      :value="form.roomId"
      @change="emit('patch', { roomId: valueOf($event) })"
    >
      <option value="">Choose a room</option>
      <option v-for="room in rooms" :key="room.id" :value="room.id">{{ room.displayName }}</option>
    </select>
    <FieldError :message="errors.room" />
  </div>

  <div class="field">
    <label for="booking-start-time">Starts (UTC)</label>
    <div class="date-time">
      <input
        class="input"
        :class="{ invalid: errors.start }"
        type="date"
        max="9999-12-31"
        aria-label="Start date (UTC)"
        :value="form.startDay"
        @change="emit('patch', { startDay: valueOf($event) })"
      />
      <input
        id="booking-start-time"
        class="input"
        :class="{ invalid: errors.start }"
        type="time"
        step="900"
        aria-label="Start time (UTC)"
        :value="form.startTime"
        @input="emit('patch', { startTime: valueOf($event) })"
      />
    </div>
    <FieldError :message="errors.start" />
  </div>

  <div class="field">
    <label for="booking-end-time">Ends (UTC)</label>
    <div class="date-time">
      <input
        class="input"
        :class="{ invalid: errors.sameDayFix }"
        type="date"
        max="9999-12-31"
        aria-label="End date (UTC)"
        :value="form.endDay"
        @change="emit('patch', { endDay: valueOf($event) })"
      />
      <input
        id="booking-end-time"
        class="input"
        :class="{ invalid: errors.end && !errors.sameDayFix }"
        type="time"
        step="900"
        aria-label="End time (UTC)"
        :value="form.endTime"
        @input="emit('patch', { endTime: valueOf($event) })"
      />
    </div>
    <FieldError :message="errors.end" />
    <button v-if="errors.sameDayFix" class="btn btn-ghost same-day-fix" @click="emit('patch', { endDay: form.startDay })">
      {{ errors.sameDayFix }}
    </button>
  </div>

  <ConflictNotice
    v-if="notice"
    :notice="notice"
    @move="period => emit('move', period)"
    @pick-room="roomId => emit('patch', { roomId })"
  />

  <div class="actions">
    <button class="btn btn-primary submit" :disabled="submitting" @click="emit('submit')">{{ submitLabel }}</button>
    <button class="btn btn-ghost clear" @click="emit('clear')">Clear form</button>
  </div>
</template>

<style scoped>
.heading {
  margin: 0 0 4px;
}

.hint {
  margin: 0;
  font-size: 13px;
  color: var(--color-neutral-700);
}

.date-time {
  display: grid;
  grid-template-columns: minmax(0, 1.4fr) minmax(0, 1fr);
  gap: var(--space-2);
}

.date-time .input {
  font-variant-numeric: tabular-nums;
}

.input.invalid {
  border-color: var(--color-accent-600);
}

.same-day-fix {
  margin-top: 4px;
  margin-left: 16px;
}

.actions {
  display: flex;
  flex-direction: column;
  gap: var(--space-2);
}

.submit {
  padding-block: 12px;
  font-size: 15px;
}

.clear {
  align-self: center;
}
</style>

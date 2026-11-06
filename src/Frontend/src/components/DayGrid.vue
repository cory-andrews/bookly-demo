<script setup lang="ts">
import { DateTime, type Interval } from 'luxon'
import { computed, onMounted, ref } from 'vue'
import type { Booking, Room } from '../api/client'
import { formatPeriod, TIME_FORMAT } from '../lib/format'

export interface BookingPreview {
  roomId: string
  period: Interval
  clashes: boolean
}

const HOUR_HEIGHT = 56
const MIN_BLOCK_HEIGHT = 22
const SLOT_MINUTES = 30
const FIRST_VISIBLE_HOUR = 7

const props = defineProps<{
  rooms: Room[]
  bookings: Booking[]
  selectedId: string | null
  flashId: string | null
  conflictIds: Set<string>
  preview: BookingPreview | null
  now: DateTime | null
}>()

const emit = defineEmits<{
  select: [bookingId: string]
  slot: [roomId: string, minutesIntoDay: number]
}>()

const scroller = ref<HTMLElement>()
const hours = Array.from({ length: 24 }, (_, hour) => (hour ? DateTime.fromObject({ hour }).toFormat(TIME_FORMAT) : ''))

const columns = computed(() =>
  props.rooms.map(room => {
    const bookings = props.bookings.filter(b => b.roomId === room.id)
    return {
      room,
      bookings,
      meta: bookings.length ? `${bookings.length} booked` : 'free all day',
      preview: props.preview?.roomId === room.id ? props.preview : null,
    }
  }),
)

const offsetFromMidnight = (time: DateTime) => time.diff(time.startOf('day'), 'hours').hours * HOUR_HEIGHT

const position = (period: Interval) => ({
  top: `${offsetFromMidnight(period.start) + 2}px`,
  height: `${Math.max(period.length('hours') * HOUR_HEIGHT - 4, MIN_BLOCK_HEIGHT)}px`,
})

function onSlotClick(event: MouseEvent, roomId: string) {
  const { top } = (event.currentTarget as HTMLElement).getBoundingClientRect()
  emit('slot', roomId, Math.floor(((event.clientY - top) / HOUR_HEIGHT) * 2) * SLOT_MINUTES)
}

onMounted(() => {
  if (scroller.value) scroller.value.scrollTop = FIRST_VISIBLE_HOUR * HOUR_HEIGHT
})
</script>

<template>
  <div ref="scroller" class="scroller">
    <div class="sheet">
      <div class="room-headers">
        <div class="axis-label">UTC</div>
        <div v-for="column in columns" :key="column.room.id" class="room-header">
          <div class="room-name">{{ column.room.displayName }}</div>
          <div class="room-meta">{{ column.meta }}</div>
        </div>
      </div>

      <div class="body">
        <div class="axis">
          <div v-for="hour in hours" :key="hour" class="axis-hour">
            <span>{{ hour }}</span>
          </div>
        </div>

        <div
          v-for="column in columns"
          :key="column.room.id"
          class="column"
          :data-testid="`room-${column.room.displayName}`"
          title="Click to book this slot"
          @click="onSlotClick($event, column.room.id)"
        >
          <button
            v-for="booking in column.bookings"
            :key="booking.id"
            class="block"
            :class="{
              'block-clash': conflictIds.has(booking.id),
              'block-selected': booking.id === selectedId,
              'block-flash': booking.id === flashId,
            }"
            :style="position(booking.period)"
            @click.stop="emit('select', booking.id)"
          >
            <span class="block-name">{{ booking.occupantName }}</span>
            <span class="block-time">{{ formatPeriod(booking.period) }}</span>
          </button>

          <div
            v-if="column.preview"
            class="preview"
            :class="{ 'preview-clash': column.preview.clashes }"
            :style="position(column.preview.period)"
          >
            {{ column.preview.clashes ? 'Clashes' : 'Your booking' }}
          </div>

          <div v-if="now" class="now" :style="{ top: `${offsetFromMidnight(now)}px` }"></div>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
.scroller {
  overflow: auto;
  max-height: calc(100vh - 120px);
}

.sheet {
  min-width: 620px;
}

.room-headers {
  position: sticky;
  top: 0;
  z-index: 4;
  display: flex;
  background: var(--color-surface);
  padding: var(--space-4) 0 var(--space-2);
  box-shadow: 0 1px 0 var(--color-divider);
}

.axis-label {
  width: 60px;
  flex: none;
  align-self: flex-end;
  padding-right: 10px;
  text-align: right;
  font-size: 11px;
  font-weight: 700;
  color: var(--color-neutral-700);
}

.room-header {
  flex: 1 1 0;
  min-width: 136px;
  padding: 0 var(--space-3);
}

.room-name {
  font-family: var(--font-heading);
  font-size: 18px;
  line-height: 1.2;
}

.room-meta {
  font-size: 12px;
  color: var(--color-neutral-700);
}

.body {
  display: flex;
  padding: 12px 0 16px;
}

.axis {
  width: 60px;
  flex: none;
}

.axis-hour {
  height: 56px;
  position: relative;
}

.axis-hour span {
  position: absolute;
  top: -9px;
  right: 10px;
  font-size: 11px;
  color: var(--color-neutral-700);
  font-variant-numeric: tabular-nums;
}

.column {
  flex: 1 1 0;
  min-width: 136px;
  height: 1344px;
  position: relative;
  cursor: pointer;
  border-left: 1px solid var(--color-divider);
  background-image: repeating-linear-gradient(to bottom, var(--color-divider) 0 1px, transparent 1px 56px);
}

.column:hover {
  background-color: color-mix(in srgb, var(--color-accent-2) 7%, transparent);
}

.block {
  position: absolute;
  left: 6px;
  right: 6px;
  z-index: 2;
  display: flex;
  flex-wrap: wrap;
  align-content: flex-start;
  column-gap: 8px;
  padding: 2px 10px;
  overflow: hidden;
  text-align: left;
  cursor: pointer;
  font: inherit;
  border-radius: 14px;
  background: var(--color-accent-2-200);
  color: var(--color-accent-2-900);
  border: 2px solid transparent;
}

.block-flash {
  background: var(--color-accent-2-400);
}

.block-selected {
  background: var(--color-accent-2-700);
  color: var(--color-neutral-100);
  box-shadow: var(--shadow-md);
}

.block-clash {
  background: var(--color-accent-200);
  color: var(--color-accent-900);
  border-color: var(--color-accent-600);
}

.block-name {
  font-size: 13px;
  font-weight: 700;
  line-height: 1.3;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
  max-width: 100%;
}

.block-time {
  font-size: 12px;
  line-height: 1.35;
  font-variant-numeric: tabular-nums;
}

.preview {
  position: absolute;
  left: 6px;
  right: 6px;
  z-index: 3;
  pointer-events: none;
  padding: 2px 10px;
  border-radius: 14px;
  border: 2px dashed var(--color-accent-2-700);
  background: color-mix(in srgb, var(--color-accent-2-100) 80%, transparent);
  color: var(--color-accent-2-800);
  font-size: 12px;
  font-weight: 700;
  overflow: hidden;
}

.preview-clash {
  border-color: var(--color-accent-600);
  background: repeating-linear-gradient(
    135deg,
    color-mix(in srgb, var(--color-accent-300) 55%, transparent) 0 6px,
    transparent 6px 12px
  );
  color: var(--color-accent-800);
}

.now {
  position: absolute;
  left: 0;
  right: 0;
  height: 2px;
  background: var(--color-accent);
  z-index: 1;
  pointer-events: none;
}
</style>

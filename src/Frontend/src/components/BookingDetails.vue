<script setup lang="ts">
import { ArrowLeft } from 'lucide-vue-next'
import type { Booking } from '../api/client'
import { formatPeriod, LONG_DAY_FORMAT } from '../lib/format'

defineProps<{ booking: Booking; roomName: string }>()
const emit = defineEmits<{ back: []; cancel: [] }>()
</script>

<template>
  <button class="btn btn-ghost back" @click="emit('back')">
    <ArrowLeft :size="16" :stroke-width="2.75" />
    New booking
  </button>
  <div>
    <div class="card-kicker kicker">Booking</div>
    <h3 class="name">{{ booking.occupantName }}</h3>
  </div>
  <div class="details">
    <span class="label">Room</span>
    <span class="value">{{ roomName }}</span>
    <span class="label">Day</span>
    <span class="value">{{ booking.period.start.toFormat(LONG_DAY_FORMAT) }}</span>
    <span class="label">Time</span>
    <span class="value time">{{ formatPeriod(booking.period) }} UTC</span>
    <span class="label">Length</span>
    <span class="value">{{ booking.period.toDuration().rescale().toHuman({ unitDisplay: 'short' }) }}</span>
  </div>
  <button class="btn btn-secondary cancel" @click="emit('cancel')">Cancel booking</button>
</template>

<style scoped>
.back {
  align-self: flex-start;
}

.kicker {
  color: var(--color-accent-2-700);
}

.name {
  margin: 4px 0 0;
}

.details {
  display: grid;
  grid-template-columns: auto 1fr;
  gap: 10px 20px;
  font-size: 14px;
}

.label {
  color: var(--color-neutral-700);
}

.value {
  font-weight: 600;
}

.time {
  font-variant-numeric: tabular-nums;
}

.cancel {
  align-self: flex-start;
  color: var(--color-accent-700);
  border-color: var(--color-accent-300);
}

.cancel:hover {
  background: var(--color-accent-100);
}
</style>

<script setup lang="ts">
import { ChevronLeft, ChevronRight } from 'lucide-vue-next'
import { DateTime } from 'luxon'
import { computed } from 'vue'
import { LONG_DAY_FORMAT } from '../lib/format'

const props = defineProps<{ day: string; today: string }>()
const emit = defineEmits<{ 'update:day': [day: string] }>()

const dayLabel = computed(() => DateTime.fromISO(props.day).toFormat(LONG_DAY_FORMAT))

const shiftDay = (days: number) => emit('update:day', DateTime.fromISO(props.day).plus({ days }).toISODate())

function onPickDay(event: Event) {
  const { value } = event.target as HTMLInputElement
  if (value) emit('update:day', value)
}
</script>

<template>
  <header class="header">
    <div class="brand">
      <span class="brand-mark"></span>
      <span class="brand-name">Bookly</span>
      <span class="brand-note">Meeting rooms</span>
    </div>
    <span class="tag tag-accent-2">All times UTC</span>
    <div class="day-nav">
      <button class="btn btn-secondary btn-icon" aria-label="Previous day" @click="shiftDay(-1)">
        <ChevronLeft :size="18" :stroke-width="2.75" />
      </button>
      <h2 class="day-label">{{ dayLabel }}</h2>
      <button class="btn btn-secondary btn-icon" aria-label="Next day" @click="shiftDay(1)">
        <ChevronRight :size="18" :stroke-width="2.75" />
      </button>
      <button class="btn btn-secondary" :disabled="day === today" @click="emit('update:day', today)">Today</button>
      <input class="input day-picker" type="date" aria-label="Pick a day" max="9999-12-31" :value="day" @change="onPickDay" />
    </div>
  </header>
</template>

<style scoped>
.header {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: var(--space-4);
  padding: var(--space-4) var(--space-6);
}

.brand {
  display: flex;
  align-items: center;
  gap: 10px;
  margin-right: auto;
}

.brand-mark {
  width: 26px;
  height: 26px;
  border-radius: 50%;
  background: var(--color-accent);
  box-shadow: 10px 0 0 -4px var(--color-accent-2);
}

.brand-name {
  font-family: var(--font-heading);
  font-size: 22px;
  margin-left: 8px;
}

.brand-note {
  font-size: 13px;
  color: var(--color-neutral-700);
  margin-left: 6px;
}

.day-nav {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: var(--space-2);
}

.day-label {
  margin: 0;
  font-size: 24px;
  min-width: 250px;
  text-align: center;
}

.day-picker {
  width: auto;
  font-variant-numeric: tabular-nums;
}
</style>

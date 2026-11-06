<script setup lang="ts">
import { CircleAlert } from 'lucide-vue-next'
import type { Interval } from 'luxon'
import type { Room } from '../api/client'

export interface Notice {
  title: string
  items: string[]
  next?: { label: string; period: Interval }
  freeRooms?: Room[]
}

defineProps<{ notice: Notice }>()

const emit = defineEmits<{
  move: [period: Interval]
  pickRoom: [roomId: string]
}>()
</script>

<template>
  <div role="alert" class="notice">
    <div class="title">
      <CircleAlert class="icon" :size="18" :stroke-width="2.75" />
      <span>{{ notice.title }}</span>
    </div>
    <div v-for="item in notice.items" :key="item" class="item">{{ item }}</div>
    <button v-if="notice.next" class="btn btn-secondary move" @click="emit('move', notice.next.period)">
      {{ notice.next.label }}
    </button>
    <template v-if="notice.freeRooms?.length">
      <div class="free-label">Free for the same time</div>
      <div class="free-rooms">
        <button
          v-for="room in notice.freeRooms"
          :key="room.id"
          class="tag tag-accent-2 free-room"
          @click="emit('pickRoom', room.id)"
        >
          {{ room.displayName }}
        </button>
      </div>
    </template>
  </div>
</template>

<style scoped>
.notice {
  display: flex;
  flex-direction: column;
  gap: var(--space-2);
  padding: var(--space-4);
  border-radius: var(--radius-lg);
  background: var(--color-accent-100);
  color: var(--color-accent-900);
}

.title {
  display: flex;
  gap: 8px;
  align-items: center;
  font-family: var(--font-heading);
  font-size: 17px;
}

.icon {
  flex: none;
  color: var(--color-accent-600);
}

.item {
  font-size: 14px;
  line-height: 1.45;
}

.move {
  align-self: flex-start;
  border-color: var(--color-accent-400);
  color: var(--color-accent-800);
}

.free-label {
  font-size: 12px;
  color: var(--color-accent-800);
  margin-top: 4px;
}

.free-rooms {
  display: flex;
  flex-wrap: wrap;
  gap: 6px;
}

.free-room {
  border: 0;
  cursor: pointer;
  font: inherit;
  font-size: 13px;
  padding: 5px 12px;
}
</style>

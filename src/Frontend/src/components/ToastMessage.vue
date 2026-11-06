<script setup lang="ts">
import { Check, X } from 'lucide-vue-next'

defineProps<{ text: string }>()
const emit = defineEmits<{ dismiss: [] }>()
</script>

<template>
  <div role="status" class="toast">
    <Check class="check" :size="16" :stroke-width="2.75" />
    <span>{{ text }}</span>
    <button class="btn btn-icon dismiss" aria-label="Dismiss" @click="emit('dismiss')">
      <X :size="14" :stroke-width="2.75" />
    </button>
  </div>
</template>

<style scoped>
.toast {
  position: fixed;
  left: 50%;
  bottom: 28px;
  transform: translateX(-50%);
  z-index: 30;
  width: max-content;
  max-width: calc(100vw - 32px);
  display: flex;
  align-items: center;
  gap: var(--space-4);
  padding: 10px 12px 10px 20px;
  border-radius: var(--radius-lg);
  background: var(--color-neutral-900);
  color: var(--color-neutral-100);
  box-shadow: var(--shadow-lg);
  font-size: 14px;
}

.check {
  flex: none;
  color: var(--color-accent-2-300);
}

.dismiss {
  width: 30px;
  height: 30px;
  color: var(--color-neutral-300);
}
</style>

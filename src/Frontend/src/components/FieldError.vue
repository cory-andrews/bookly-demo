<script setup lang="ts">
import { CircleAlert } from 'lucide-vue-next'

defineProps<{ message?: string }>()
</script>

<template>
  <div v-if="message" class="field-error">
    <CircleAlert class="icon" :size="16" :stroke-width="2.75" />
    <span>{{ message }}</span>
  </div>
</template>

<style scoped>
.field-error {
  display: flex;
  gap: 6px;
  margin-top: 6px;
  font-size: 13px;
  line-height: 1.4;
  color: var(--color-accent-700);
}

.icon {
  flex: none;
  margin-top: 1px;
}
</style>

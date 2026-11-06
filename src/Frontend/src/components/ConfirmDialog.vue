<script setup lang="ts">
import { onBeforeUnmount, onMounted, ref } from 'vue'

defineProps<{ summary: string; error: string | null; busy: boolean }>()
const emit = defineEmits<{ keep: []; confirm: [] }>()

const keepButton = ref<HTMLButtonElement>()

function onKeydown(event: KeyboardEvent) {
  if (event.key === 'Escape') emit('keep')
}

onMounted(() => {
  window.addEventListener('keydown', onKeydown)
  keepButton.value?.focus()
})

onBeforeUnmount(() => window.removeEventListener('keydown', onKeydown))
</script>

<template>
  <div class="dialog-backdrop backdrop" @click="emit('keep')">
    <div class="dialog" role="dialog" aria-modal="true" aria-labelledby="cancel-title" @click.stop>
      <div id="cancel-title" class="dialog-title">Cancel this booking?</div>
      <div class="dialog-body">{{ summary }} The room becomes free for anyone to book.</div>
      <div v-if="error" role="alert" class="error">{{ error }}</div>
      <div class="dialog-actions">
        <button ref="keepButton" class="btn btn-secondary" @click="emit('keep')">Keep booking</button>
        <button class="btn btn-primary" :disabled="busy" @click="emit('confirm')">Cancel booking</button>
      </div>
    </div>
  </div>
</template>

<style scoped>
.backdrop {
  z-index: 20;
}

.error {
  font-size: 13px;
  color: var(--color-accent-700);
}
</style>

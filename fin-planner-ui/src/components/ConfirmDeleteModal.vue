<template>
  <Transition name="modal">
    <div v-if="isOpen" class="modal-backdrop" @click.self="cancel">
      <div class="modal card danger-modal">
        <!-- Header -->
        <div class="modal-header">
          <div class="danger-icon">
            <svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
              <path d="M10.29 3.86L1.82 18a2 2 0 0 0 1.71 3h16.94a2 2 0 0 0 1.71-3L13.71 3.86a2 2 0 0 0-3.42 0z"/>
              <line x1="12" y1="9" x2="12" y2="13"/>
              <line x1="12" y1="17" x2="12.01" y2="17"/>
            </svg>
          </div>
          <h2>Delete Portfolio</h2>
          <button @click="cancel" class="btn-close" aria-label="Close">
            <svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
              <line x1="18" y1="6" x2="6" y2="18"/>
              <line x1="6" y1="6" x2="18" y2="18"/>
            </svg>
          </button>
        </div>

        <!-- Warning Content -->
        <div class="modal-body">
          <p class="warning-text">
            This action <strong>cannot be undone</strong>. This will permanently delete the 
            <strong>{{ portfolioName }}</strong> portfolio and all associated data.
          </p>

          <div class="deletion-summary">
            <h4>The following will be permanently deleted:</h4>
            <ul>
              <li>All accounts and their settings</li>
              <li>All holdings and position history</li>
              <li>All transaction records</li>
              <li>All asset categories and allocations</li>
              <li>All decisions and recommendations</li>
              <li>All performance snapshots</li>
            </ul>
          </div>

          <!-- Confirmation Input -->
          <div class="confirmation-section">
            <label for="confirmInput">
              To confirm, type <strong class="highlight">{{ portfolioName }}</strong> below:
            </label>
            <input 
              id="confirmInput"
              v-model="confirmationText"
              type="text"
              class="input confirm-input"
              :placeholder="`Type ${portfolioName} to confirm`"
              autocomplete="off"
              @keyup.enter="confirmationMatches && confirmDelete()"
            />
          </div>
        </div>

        <!-- Actions -->
        <div class="modal-footer">
          <button @click="cancel" class="btn btn-secondary">Cancel</button>
          <button 
            @click="confirmDelete" 
            class="btn btn-danger"
            :disabled="!confirmationMatches || deleting"
          >
            <span v-if="deleting" class="spinner"></span>
            {{ deleting ? 'Deleting...' : 'Delete this portfolio' }}
          </button>
        </div>
      </div>
    </div>
  </Transition>
</template>

<script setup>
import { ref, computed, watch } from 'vue';

const props = defineProps({
  isOpen: Boolean,
  portfolioName: {
    type: String,
    required: true
  }
});

const emit = defineEmits(['cancel', 'confirm']);

const confirmationText = ref('');
const deleting = ref(false);

const confirmationMatches = computed(() => {
  return confirmationText.value.trim() === props.portfolioName;
});

// Reset confirmation text when modal opens/closes
watch(() => props.isOpen, (val) => {
  if (!val) {
    confirmationText.value = '';
    deleting.value = false;
  }
});

const cancel = () => {
  emit('cancel');
};

const confirmDelete = () => {
  if (!confirmationMatches.value || deleting.value) return;
  deleting.value = true;
  emit('confirm');
};
</script>

<style scoped>
.modal-backdrop {
  position: fixed;
  inset: 0;
  background: rgba(0, 0, 0, 0.6);
  backdrop-filter: blur(4px);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: var(--z-modal-backdrop, 1000);
}

.danger-modal {
  width: 100%;
  max-width: 480px;
  background: var(--color-bg-primary, #fff);
  border-radius: 12px;
  box-shadow: 0 25px 50px -12px rgba(0, 0, 0, 0.25);
  border: 1px solid var(--color-danger, #ef4444);
  overflow: hidden;
}

.modal-header {
  display: flex;
  align-items: center;
  gap: var(--spacing-md, 12px);
  padding: var(--spacing-lg, 20px) var(--spacing-xl, 24px);
  background: linear-gradient(135deg, rgba(239, 68, 68, 0.1) 0%, rgba(239, 68, 68, 0.05) 100%);
  border-bottom: 1px solid rgba(239, 68, 68, 0.2);
}

.danger-icon {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 40px;
  height: 40px;
  border-radius: 50%;
  background: rgba(239, 68, 68, 0.15);
  color: var(--color-danger, #ef4444);
}

.modal-header h2 {
  flex: 1;
  margin: 0;
  font-size: var(--font-size-lg, 18px);
  font-weight: 600;
  color: var(--color-danger, #ef4444);
}

.btn-close {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 32px;
  height: 32px;
  padding: 0;
  border: none;
  background: transparent;
  color: var(--color-text-muted, #6b7280);
  cursor: pointer;
  border-radius: 6px;
  transition: all 0.2s ease;
}

.btn-close:hover {
  background: rgba(0, 0, 0, 0.05);
  color: var(--color-text-primary, #111827);
}

.modal-body {
  padding: var(--spacing-xl, 24px);
}

.warning-text {
  margin: 0 0 var(--spacing-lg, 20px) 0;
  color: var(--color-text-secondary, #4b5563);
  line-height: 1.6;
}

.warning-text strong {
  color: var(--color-text-primary, #111827);
}

.deletion-summary {
  background: rgba(239, 68, 68, 0.05);
  border: 1px solid rgba(239, 68, 68, 0.15);
  border-radius: 8px;
  padding: var(--spacing-md, 12px) var(--spacing-lg, 16px);
  margin-bottom: var(--spacing-lg, 20px);
}

.deletion-summary h4 {
  margin: 0 0 var(--spacing-sm, 8px) 0;
  font-size: var(--font-size-sm, 14px);
  font-weight: 600;
  color: var(--color-danger, #ef4444);
}

.deletion-summary ul {
  margin: 0;
  padding-left: var(--spacing-lg, 20px);
  color: var(--color-text-secondary, #4b5563);
  font-size: var(--font-size-sm, 14px);
  line-height: 1.8;
}

.confirmation-section {
  margin-top: var(--spacing-lg, 20px);
}

.confirmation-section label {
  display: block;
  margin-bottom: var(--spacing-sm, 8px);
  font-size: var(--font-size-sm, 14px);
  color: var(--color-text-secondary, #4b5563);
}

.highlight {
  color: var(--color-danger, #ef4444);
  font-weight: 600;
}

.confirm-input {
  width: 100%;
  padding: var(--spacing-sm, 10px) var(--spacing-md, 12px);
  border: 1px solid var(--color-border, #e5e7eb);
  border-radius: 8px;
  font-size: var(--font-size-base, 16px);
  transition: all 0.2s ease;
}

.confirm-input:focus {
  outline: none;
  border-color: var(--color-danger, #ef4444);
  box-shadow: 0 0 0 3px rgba(239, 68, 68, 0.15);
}

.modal-footer {
  display: flex;
  justify-content: flex-end;
  gap: var(--spacing-md, 12px);
  padding: var(--spacing-lg, 20px) var(--spacing-xl, 24px);
  background: var(--color-bg-secondary, #f9fafb);
  border-top: 1px solid var(--color-border, #e5e7eb);
}

.btn-danger {
  display: inline-flex;
  align-items: center;
  gap: var(--spacing-sm, 8px);
  padding: var(--spacing-sm, 10px) var(--spacing-lg, 20px);
  background: var(--color-danger, #ef4444);
  color: white;
  border: none;
  border-radius: 8px;
  font-weight: 600;
  cursor: pointer;
  transition: all 0.2s ease;
}

.btn-danger:hover:not(:disabled) {
  background: #dc2626;
  transform: translateY(-1px);
  box-shadow: 0 4px 12px rgba(239, 68, 68, 0.3);
}

.btn-danger:disabled {
  opacity: 0.5;
  cursor: not-allowed;
  transform: none;
  box-shadow: none;
}

.spinner {
  width: 16px;
  height: 16px;
  border: 2px solid rgba(255, 255, 255, 0.3);
  border-top-color: white;
  border-radius: 50%;
  animation: spin 0.8s linear infinite;
}

@keyframes spin {
  to { transform: rotate(360deg); }
}

/* Transitions */
.modal-enter-active,
.modal-leave-active {
  transition: opacity 0.2s ease;
}

.modal-enter-active .danger-modal,
.modal-leave-active .danger-modal {
  transition: transform 0.2s ease;
}

.modal-enter-from,
.modal-leave-to {
  opacity: 0;
}

.modal-enter-from .danger-modal {
  transform: scale(0.95) translateY(-20px);
}

.modal-leave-to .danger-modal {
  transform: scale(0.95) translateY(20px);
}
</style>

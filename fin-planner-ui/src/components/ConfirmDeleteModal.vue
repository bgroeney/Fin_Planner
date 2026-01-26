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



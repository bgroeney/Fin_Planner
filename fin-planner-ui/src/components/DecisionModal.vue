<template>
  <Transition name="modal-fade">
    <div v-if="isOpen" class="modal-backdrop" @click="close" role="dialog" aria-modal="true" :aria-labelledby="titleId">
      <div class="modal-content" @click.stop>
        <div class="modal-header">
          <h3 :id="titleId">{{ type }} {{ assetSymbol }}</h3>
          <button class="btn-close" @click="close" aria-label="Close modal">
            <svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><line x1="18" y1="6" x2="6" y2="18"/><line x1="6" y1="6" x2="18" y2="18"/></svg>
          </button>
        </div>
        
        <div class="modal-body">
          <div class="form-group">
            <label for="units-input">Units</label>
            <input 
              id="units-input"
              v-model.number="units" 
              type="number" 
              class="form-input" 
              placeholder="0"
              min="0"
              step="any"
              autofocus
            />
          </div>

          <div v-if="type === 'Sell'" class="form-group">
            <label for="allocation-method">Tax Optimization Method</label>
            <select id="allocation-method" v-model="allocationMethod" class="form-select">
              <option value="FIFO">First-In-First-Out (Standard)</option>
              <option value="MinTax">Minimize Tax (High Cost/Long Term)</option>
              <option value="MaxGain">Maximize Gain (Low Cost)</option>
            </select>
            <div class="tax-hint" v-if="projectedTax !== null">
              Projected Taxable Gain: {{ formatCurrency(projectedTax) }}
            </div>
          </div>

          <div class="form-group">
            <label for="rationale-input">Rationale</label>
            <textarea 
              id="rationale-input"
              v-model="rationale" 
              class="form-input" 
              rows="3" 
              placeholder="Why make this trade?"
            ></textarea>
          </div>
          
          <div v-if="error" class="error-message" role="alert">
              {{ error }}
          </div>
        </div>

        <div class="modal-footer">
          <button class="btn btn-secondary" @click="close">Cancel</button>
          <button class="btn btn-primary" @click="submit" :disabled="submitting || !isValid">
            <span v-if="submitting" class="spinner-sm"></span>
            {{ submitting ? 'Saving...' : 'Create Decision' }}
          </button>
        </div>
      </div>
    </div>
  </Transition>
</template>

<script setup>
import { ref, computed, watch, nextTick } from 'vue';
import api from '../services/api';
import { formatCurrency } from '../utils/formatters';

const props = defineProps({
  isOpen: Boolean,
  type: { type: String, required: true }, // 'Buy' or 'Sell'
  assetSymbol: { type: String, required: true },
  portfolioId: { type: String, required: true },
  assetId: { type: String, required: true }
});

const emit = defineEmits(['close', 'success']);

const units = ref(0);
const rationale = ref('');
const allocationMethod = ref('FIFO');
const projectedTax = ref(null);
const submitting = ref(false);
const error = ref('');

const titleId = computed(() => `modal-title-${props.assetId}`);

const isValid = computed(() => {
    return units.value > 0;
});

watch(() => props.isOpen, (val) => {
  if (val) {
    units.value = 0;
    rationale.value = '';
    allocationMethod.value = 'FIFO';
    projectedTax.value = null;
    error.value = '';
    
    // Auto focus units input
    nextTick(() => {
        const input = document.getElementById('units-input');
        if (input) input.focus();
    });
  }
});

const close = () => {
  emit('close');
};

const submit = async () => {
  if (!isValid.value) return;
  
  submitting.value = true;
  error.value = '';
  
  try {
    await api.post('/decisions', {
      portfolioId: props.portfolioId,
      title: `${props.type} ${units.value} ${props.assetSymbol}`,
      rationale: rationale.value || `${props.type} decision for ${props.assetSymbol}`,
      allocationMethod: props.type === 'Sell' ? allocationMethod.value : null,
      saveAsDraft: false,
    });
    
    emit('success');
    close();
  } catch (e) {
    console.error(e);
    error.value = 'Failed to create decision. Please try again.';
  } finally {
    submitting.value = false;
  }
};
</script>

<style scoped>
.modal-backdrop {
  position: fixed;
  top: 0;
  left: 0;
  width: 100%;
  height: 100%;
  background: rgba(0, 0, 0, 0.5);
  backdrop-filter: blur(2px); /* Glassmorphism touch */
  display: flex;
  justify-content: center;
  align-items: center;
  z-index: 1000;
  transition: opacity 0.2s ease;
}

.modal-content {
  background: var(--color-bg-secondary);
  border-radius: var(--radius-lg);
  width: 95%; /* Responsive width */
  max-width: 480px; /* Slightly tighter max-width */
  box-shadow: var(--shadow-2xl); /* Deeper shadow */
  display: flex;
  flex-direction: column;
  max-height: 90vh;
  border: 1px solid var(--color-border);
  transform-origin: center;
  transition: transform 0.2s ease, opacity 0.2s ease;
}

/* Transitions */
.modal-fade-enter-from,
.modal-fade-leave-to {
  opacity: 0;
}

.modal-fade-enter-from .modal-content,
.modal-fade-leave-to .modal-content {
  transform: scale(0.95);
  opacity: 0;
}

.modal-header {
  padding: var(--spacing-md) var(--spacing-lg);
  border-bottom: 1px solid var(--color-border);
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.modal-header h3 {
  margin: 0;
  font-size: var(--font-size-lg);
  font-weight: 600;
  color: var(--color-text-primary);
}

.btn-close {
  background: transparent;
  border: none;
  cursor: pointer;
  color: var(--color-text-muted);
  padding: 4px;
  border-radius: var(--radius-md);
  transition: all 0.2s;
  display: flex;
  align-items: center;
  justify-content: center;
}

.btn-close:hover {
  background: var(--color-bg-elevated);
  color: var(--color-text-primary);
}

.modal-body {
  padding: var(--spacing-lg);
  overflow-y: auto;
}

.form-group {
  margin-bottom: var(--spacing-lg); /* More spacing between groups */
}

.form-group label {
  display: block;
  font-size: var(--font-size-sm);
  font-weight: 500;
  color: var(--color-text-secondary);
  margin-bottom: 6px;
}

.form-input, .form-select {
  width: 100%;
  padding: 10px 12px; /* Larger tap targets */
  border: 1px solid var(--color-border);
  border-radius: var(--radius-md);
  background: var(--color-bg-primary);
  color: var(--color-text-primary);
  font-size: var(--font-size-md);
  transition: border-color 0.2s, box-shadow 0.2s;
}

.form-input:focus, .form-select:focus {
  outline: none;
  border-color: var(--color-accent);
  box-shadow: 0 0 0 3px rgba(var(--color-accent-rgb), 0.15); /* More prominent focus ring */
}

.modal-footer {
  padding: var(--spacing-md) var(--spacing-lg);
  border-top: 1px solid var(--color-border);
  display: flex;
  justify-content: flex-end;
  gap: var(--spacing-md);
  background: var(--color-bg-tertiary); /* Subtle contrast for footer */
  border-bottom-left-radius: var(--radius-lg);
  border-bottom-right-radius: var(--radius-lg);
}

.btn {
  padding: 8px 16px;
  border-radius: var(--radius-md);
  font-weight: 500;
  font-size: var(--font-size-sm);
  cursor: pointer;
  border: 1px solid transparent;
  transition: all 0.2s;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  gap: 8px;
}

.btn-secondary {
  background: white;
  border: 1px solid var(--color-border);
  color: var(--color-text-secondary);
}

.btn-secondary:hover {
  background: var(--color-bg-elevated);
  border-color: var(--color-border-hover);
  color: var(--color-text-primary);
}

.btn-primary {
  background: var(--color-accent);
  color: white;
  box-shadow: 0 1px 2px rgba(0, 0, 0, 0.05); /* Subtle shadow */
}

.btn-primary:hover:not(:disabled) {
  background: var(--color-accent-hover);
  transform: translateY(-1px); /* Micro-interaction lift */
}

.btn-primary:disabled {
  opacity: 0.7;
  cursor: not-allowed;
  transform: none;
}

.tax-hint {
  font-size: var(--font-size-xs);
  color: var(--color-info);
  margin-top: 6px;
}

.error-message {
    color: var(--color-danger);
    font-size: var(--font-size-sm);
    margin-top: var(--spacing-sm);
    padding: 8px;
    background: rgba(220, 38, 38, 0.1);
    border-radius: var(--radius-md);
}

/* Spinner */
.spinner-sm {
  width: 14px;
  height: 14px;
  border: 2px solid rgba(255, 255, 255, 0.3);
  border-top-color: white;
  border-radius: 50%;
  animation: spin 0.8s linear infinite;
}

@keyframes spin {
  to { transform: rotate(360deg); }
}
</style>

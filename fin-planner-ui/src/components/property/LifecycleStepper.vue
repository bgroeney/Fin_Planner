<template>
  <div class="lifecycle-stepper">
    <div class="stepper-track">
      <div 
        v-for="(step, index) in steps" 
        :key="step.key"
        class="step-item"
        :class="{ 
          'active': currentStepIndex === index, 
          'completed': currentStepIndex > index,
          'future': currentStepIndex < index
        }"
        @click="handleStepClick(index)"
      >
        <div class="step-indicator">
          <div class="step-circle">
            <svg v-if="currentStepIndex > index" width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="3">
              <polyline points="20 6 9 17 4 12"></polyline>
            </svg>
            <span v-else>{{ index + 1 }}</span>
          </div>
          <div class="step-line" v-if="index < steps.length - 1"></div>
        </div>
        <div class="step-label">
          <span class="step-name">{{ step.label }}</span>
          <span v-if="currentStepIndex === index" class="step-status">Current Stage</span>
        </div>
      </div>
    </div>

    <!-- Actions Area -->
    <div class="lifecycle-actions" v-if="nextAction && !isTerminal">
      <div class="action-info">
        <h4>Move to next stage?</h4>
        <p>Current stage: <strong>{{ currentStep.label }}</strong>. Next: <strong>{{ nextAction.label }}</strong></p>
      </div>
      <button class="btn btn-primary" @click="advanceStage" :disabled="loading">
        <span v-if="loading" class="spinner"></span>
        {{ nextAction.buttonText }}
      </button>
    </div>
  </div>
</template>

<script setup>
import { computed } from 'vue';

const props = defineProps({
  currentStatus: { type: String, required: true },
  loading: { type: Boolean, default: false }
});

const emit = defineEmits(['transition']);

const steps = [
  { key: 'Draft', label: 'Draft' }, // Initial
  { key: 'Under Review', label: 'Under Review' },
  { key: 'Proceed to Acquire', label: 'Proceed' },
  { key: 'Offer Made', label: 'Offer Made' },
  { key: 'Under Due Diligence', label: 'Due Diligence' },
  { key: 'Waiting Settlement', label: 'Settlement' },
  { key: 'Acquired', label: 'Acquired' }
];

const currentStepIndex = computed(() => {
  const idx = steps.findIndex(s => s.key === props.currentStatus);
  return idx === -1 ? 0 : idx;
});

const currentStep = computed(() => steps[currentStepIndex.value]);

const isTerminal = computed(() => {
  return currentStepIndex.value === steps.length - 1;
});

const nextAction = computed(() => {
  if (isTerminal.value) return null;
  const nextStep = steps[currentStepIndex.value + 1];
  
  let buttonText = `Mark as ${nextStep.label}`;
  if (nextStep.key === 'Proceed to Acquire') buttonText = 'Proceed to Acquire';
  if (nextStep.key === 'Offer Made') buttonText = 'Record Offer';
  if (nextStep.key === 'Acquired') buttonText = 'Confirm Acquisition';

  return {
    label: nextStep.label,
    key: nextStep.key,
    buttonText
  };
});

function handleStepClick(index) {
  // Optional: Allow jumping back?
  // For now, strict forward progression via button, maybe click strictly for history view.
}

function advanceStage() {
  if (nextAction.value) {
    emit('transition', nextAction.value.key);
  }
}
</script>

<style scoped>
.lifecycle-stepper {
  background: var(--color-bg-card);
  padding: var(--spacing-lg);
  border-bottom: 1px solid var(--color-border);
}

.stepper-track {
  display: flex;
  justify-content: space-between;
  margin-bottom: var(--spacing-lg);
  overflow-x: auto;
}

.step-item {
  flex: 1;
  display: flex;
  flex-direction: column;
  align-items: center;
  position: relative;
  min-width: 100px;
  cursor: default;
}

.step-indicator {
  display: flex;
  align-items: center;
  width: 100%;
  margin-bottom: var(--spacing-sm);
  position: relative;
}

.step-circle {
  width: 24px;
  height: 24px;
  border-radius: 50%;
  background: var(--color-bg-elevated);
  border: 2px solid var(--color-border);
  color: var(--color-text-muted);
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 10px;
  font-weight: 700;
  z-index: 2;
  transition: all 0.3s;
}

.step-line {
  flex: 1;
  height: 2px;
  background: var(--color-border);
  position: absolute;
  left: 50%;
  right: -50%;
  top: 11px;
  z-index: 1;
}

.step-item:last-child .step-line {
  display: none;
}

/* States */
.step-item.active .step-circle {
  background: var(--color-industrial-copper);
  border-color: var(--color-industrial-copper);
  color: white;
  box-shadow: 0 0 0 4px rgba(180, 83, 9, 0.15);
}

.step-item.completed .step-circle {
  background: var(--color-success);
  border-color: var(--color-success);
  color: white;
}

.step-item.completed .step-line {
  background: var(--color-success);
}

.step-label {
  text-align: center;
}

.step-name {
  font-size: var(--font-size-xs);
  font-weight: 500;
  color: var(--color-text-muted);
}

.step-item.active .step-name {
  color: var(--color-text-primary);
  font-weight: 700;
}

.step-status {
  display: block;
  font-size: 9px;
  text-transform: uppercase;
  color: var(--color-industrial-copper);
  font-weight: 600;
  margin-top: 2px;
}

/* Actions */
.lifecycle-actions {
  display: flex;
  justify-content: space-between;
  align-items: center;
  background: var(--color-bg-elevated);
  padding: var(--spacing-md) var(--spacing-lg);
  border-radius: var(--radius-md);
  border: 1px solid var(--color-border-subtle);
}

.action-info h4 {
  margin: 0 0 4px 0;
  font-size: var(--font-size-sm);
}

.action-info p {
  margin: 0;
  font-size: var(--font-size-xs);
  color: var(--color-text-muted);
}

.spinner {
  width: 14px;
  height: 14px;
  border: 2px solid white;
  border-top-color: transparent;
  border-radius: 50%;
  animation: spin 0.8s linear infinite;
  display: inline-block;
  margin-right: 6px;
}

@keyframes spin { to { transform: rotate(360deg); } }
</style>

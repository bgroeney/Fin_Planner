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
    <div class="lifecycle-actions" v-if="!loading">
      <div class="action-left">
         <button class="btn-text-icon" @click="$emit('toggle-history')">
            <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><circle cx="12" cy="12" r="10"/><polyline points="12 6 12 12 16 14"/></svg>
            History
         </button>
      </div>

      <div class="action-right" v-if="!isTerminal">
         <div class="revert-dropdown-wrapper" v-if="currentStepIndex > 0">
            <button class="btn-text-danger" @click="showRevertMenu = !showRevertMenu">
               Revert Status
            </button>
            <div v-if="showRevertMenu" class="revert-menu glass-card" v-click-outside="() => showRevertMenu = false">
               <div class="menu-label">Revert to:</div>
               <button 
                  v-for="(step, idx) in previousSteps" 
                  :key="step.key" 
                  class="menu-item"
                  @click="handleRevertClick(step.key)"
               >
                  {{ step.label }}
               </button>
            </div>
         </div>
         
         <div class="forward-action" v-if="nextAction">
            <span class="next-label">Next: {{ nextAction.label }}</span>
            <button class="btn btn-primary btn-sm" @click="requestAdvance">
               {{ nextAction.buttonText }}
               <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><polyline points="9 18 15 12 9 6"/></svg>
            </button>
         </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { computed, ref } from 'vue';

// Custom directive for local use
const vClickOutside = {
  mounted(el, binding) {
    el._clickOutsideHandler = (event) => {
      if (!(el === event.target || el.contains(event.target))) {
        binding.value(event);
      }
    };
    document.body.addEventListener('click', el._clickOutsideHandler);
  },
  unmounted(el) {
    document.body.removeEventListener('click', el._clickOutsideHandler);
  }
};

const props = defineProps({
  currentStatus: { type: String, required: true },
  loading: { type: Boolean, default: false }
});

const emit = defineEmits(['request-transition', 'request-revert', 'toggle-history']);

const showRevertMenu = ref(false);

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

const previousSteps = computed(() => {
   return steps.slice(0, currentStepIndex.value).reverse();
});

const isTerminal = computed(() => {
  return currentStepIndex.value === steps.length - 1;
});

const nextAction = computed(() => {
  if (isTerminal.value) return null;
  const nextStep = steps[currentStepIndex.value + 1];
  
  let buttonText = 'Advance';
  if (nextStep.key === 'Proceed to Acquire') buttonText = 'Proceed';
  if (nextStep.key === 'Offer Made') buttonText = 'Record Offer';
  if (nextStep.key === 'Under Due Diligence') buttonText = 'Start DD';
  if (nextStep.key === 'Acquired') buttonText = 'Settled';

  return {
    label: nextStep.label,
    key: nextStep.key,
    buttonText
  };
});

function handleStepClick(index) {
  // Disable click-to-revert in favor of explicit menu, but keep for UX if desired?
  // User asked for "box to confirm revert", keeping explicit menu is safer.
  // We can keep this but it's hidden UI.
}

function handleRevertClick(targetStatus) {
   showRevertMenu.value = false;
   emit('request-revert', targetStatus);
}

function requestAdvance() {
  if (nextAction.value) {
    emit('request-transition', nextAction.value.key);
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
  margin-bottom: var(--spacing-lg); /* More space for actions */
  padding: 0 var(--spacing-md); /* Indent slightly */
  overflow-x: auto;
}

.step-item {
  flex: 1;
  display: flex;
  flex-direction: column;
  align-items: center;
  position: relative;
  min-width: 80px;
}

.step-indicator {
  display: flex;
  align-items: center;
  width: 100%;
  margin-bottom: 8px;
  position: relative;
}

.step-circle {
  width: 28px;
  height: 28px;
  border-radius: 50%;
  background: var(--color-bg-elevated);
  border: 2px solid var(--color-border);
  color: var(--color-text-muted);
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 11px;
  font-weight: 700;
  z-index: 2;
  transition: all 0.3s;
  position: relative;
}

/* Connectors */
.step-line {
  flex: 1;
  height: 2px;
  background: var(--color-border);
  position: absolute;
  left: 50%;
  right: -50%;
  top: 13px; /* Center with 28px circle */
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
  font-size: 11px;
  font-weight: 500;
  color: var(--color-text-muted);
  white-space: nowrap;
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

/* Actions Bar */
.lifecycle-actions {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding-top: var(--spacing-md);
  border-top: 1px solid var(--color-border-subtle);
  margin-top: var(--spacing-md);
}

.action-left {
   display: flex;
   gap: 12px;
}

.action-right {
   display: flex;
   align-items: center;
   gap: 16px;
}

.btn-text-icon {
   display: flex;
   align-items: center;
   gap: 6px;
   background: none;
   border: none;
   font-size: 12px;
   color: var(--color-text-muted);
   cursor: pointer;
   padding: 4px 8px;
   border-radius: 4px;
}

.btn-text-icon:hover {
   background: var(--color-bg-elevated);
   color: var(--color-text-primary);
}

.revert-dropdown-wrapper {
   position: relative;
}

.btn-text-danger {
   background: none;
   border: none;
   font-size: 12px;
   color: var(--color-text-muted);
   cursor: pointer;
   padding: 4px 8px;
}

.btn-text-danger:hover {
   color: var(--color-danger);
   text-decoration: underline;
}

.revert-menu {
   position: absolute;
   bottom: 100%;
   right: 0;
   margin-bottom: 8px;
   width: 160px;
   background: var(--glass-bg);
   backdrop-filter: blur(12px);
   border: 1px solid var(--glass-border);
   border-radius: var(--radius-md);
   box-shadow: var(--shadow-lg);
   padding: 4px;
   z-index: 20;
}

.menu-label {
   font-size: 10px;
   text-transform: uppercase;
   color: var(--color-text-muted);
   padding: 4px 8px;
   border-bottom: 1px solid var(--color-border-subtle);
   margin-bottom: 4px;
}

.menu-item {
   display: block;
   width: 100%;
   text-align: left;
   padding: 6px 8px;
   font-size: 12px;
   color: var(--color-text-secondary);
   background: none;
   border: none;
   cursor: pointer;
   border-radius: 4px;
}

.menu-item:hover {
   background: rgba(239, 68, 68, 0.1);
   color: var(--color-danger);
}

.forward-action {
   display: flex;
   align-items: center;
   gap: 12px;
}

.next-label {
   font-size: 11px;
   color: var(--color-text-muted);
}

.btn-sm {
   padding: 6px 12px;
   font-size: 12px;
   gap: 6px;
}
</style>

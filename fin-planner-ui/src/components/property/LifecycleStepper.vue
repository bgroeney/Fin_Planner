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



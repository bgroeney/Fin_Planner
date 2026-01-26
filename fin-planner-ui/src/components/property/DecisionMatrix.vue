<template>
  <div class="decision-matrix">
    <!-- Recommendation Banner -->
    <div class="recommendation-banner" :class="getRecommendationClass(recommendation)">
      <svg v-if="recommendation === 'Buy'" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
        <polyline points="20 6 9 17 4 12"></polyline>
      </svg>
      <svg v-else-if="recommendation === 'Uneconomic'" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
        <circle cx="12" cy="12" r="10"></circle>
        <line x1="15" y1="9" x2="9" y2="15"></line>
        <line x1="9" y1="9" x2="15" y2="15"></line>
      </svg>
      <svg v-else width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
        <circle cx="12" cy="12" r="10"></circle>
        <line x1="12" y1="16" x2="12" y2="12"></line>
        <line x1="12" y1="8" x2="12.01" y2="8"></line>
      </svg>
      <span class="recommendation-text">
        AI Recommendation: <strong>{{ recommendation }}</strong>
      </span>
    </div>

    <!-- Decision Buttons -->
    <div v-if="currentStatus !== 'Buy' && currentStatus !== 'Pass' && currentStatus !== 'Uneconomic'" class="decision-buttons">
      <button 
        class="decision-btn btn-buy"
        @click="openDecisionModal('Buy')"
        :class="{ suggested: recommendation === 'Buy' }"
      >
        <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
          <polyline points="20 6 9 17 4 12"></polyline>
        </svg>
        Buy
      </button>
      
      <button 
        class="decision-btn btn-pass"
        @click="openDecisionModal('Pass')"
        :class="{ suggested: recommendation === 'Pass' }"
      >
        <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
          <line x1="18" y1="6" x2="6" y2="18"></line>
          <line x1="6" y1="6" x2="18" y2="18"></line>
        </svg>
        Pass
      </button>
      
      <button 
        class="decision-btn btn-uneconomic"
        @click="openDecisionModal('Uneconomic')"
        :class="{ suggested: recommendation === 'Uneconomic' }"
      >
        <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
          <path d="M10.29 3.86L1.82 18a2 2 0 0 0 1.71 3h16.94a2 2 0 0 0 1.71-3L13.71 3.86a2 2 0 0 0-3.42 0z"></path>
          <line x1="12" y1="9" x2="12" y2="13"></line>
          <line x1="12" y1="17" x2="12.01" y2="17"></line>
        </svg>
        Uneconomic
      </button>
    </div>

    <!-- Already Decided Banner -->
    <div v-else class="decision-recorded">
      <span class="decision-badge" :class="getRecommendationClass(currentStatus)">
        Decision Recorded: {{ currentStatus }}
      </span>
    </div>

    <!-- Decision Modal -->
    <div v-if="showModal" class="modal-backdrop" @click.self="showModal = false">
      <div class="modal decision-modal card">
        <div class="modal-header">
          <h3>Record Decision: {{ pendingDecision }}</h3>
          <button class="btn-close" @click="showModal = false">×</button>
        </div>
        <div class="modal-body">
          <div class="form-group">
            <label>Rationale (optional)</label>
            <textarea 
              v-model="rationale" 
              rows="4" 
              placeholder="Document why you're making this decision..."
            ></textarea>
          </div>
        </div>
        <div class="modal-actions">
          <button class="btn btn-secondary" @click="showModal = false">Cancel</button>
          <button 
            class="btn" 
            :class="getButtonClass(pendingDecision)"
            @click="confirmDecision"
          >
            Confirm {{ pendingDecision }}
          </button>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref } from 'vue';

const props = defineProps({
  recommendation: { type: String, required: true },
  currentStatus: { type: String, required: true }
});

const emit = defineEmits(['decide']);

const showModal = ref(false);
const pendingDecision = ref('');
const rationale = ref('');

function openDecisionModal(decision) {
  pendingDecision.value = decision;
  rationale.value = '';
  showModal.value = true;
}

function confirmDecision() {
  emit('decide', pendingDecision.value, rationale.value);
  showModal.value = false;
}

function getRecommendationClass(rec) {
  switch (rec) {
    case 'Buy': return 'recommendation-buy';
    case 'Pass': return 'recommendation-pass';
    case 'Uneconomic': return 'recommendation-uneconomic';
    default: return 'recommendation-analyzing';
  }
}

function getButtonClass(decision) {
  switch (decision) {
    case 'Buy': return 'btn-success';
    case 'Pass': return 'btn-secondary';
    case 'Uneconomic': return 'btn-danger';
    default: return 'btn-primary';
  }
}
</script>



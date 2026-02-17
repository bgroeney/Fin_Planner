<template>
  <Transition name="modal">
    <div v-if="isOpen" class="modal-backdrop" @click.self="handleBackdropClick">
      <div class="portfolio-modal card slide-in">
        <!-- Header -->
        <div class="pm-header">
          <div>
            <h2 class="pm-title">Your Portfolios</h2>
            <p class="pm-subtitle">Select a portfolio to manage</p>
          </div>
          <button v-if="allowClose" @click="close" class="btn-close" aria-label="Close">
            <svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><line x1="18" y1="6" x2="6" y2="18"/><line x1="6" y1="6" x2="18" y2="18"/></svg>
          </button>
        </div>

        <!-- Portfolio List -->
        <div class="pm-body">
          <!-- Loading -->
          <div v-if="portfolioStore.loading" class="pm-loading">
            <div v-for="i in 2" :key="i" class="pm-card-skeleton">
              <div class="skeleton skeleton-title"></div>
              <div class="skeleton skeleton-text"></div>
            </div>
          </div>

          <!-- Empty State -->
          <div v-else-if="portfolioStore.portfolios.length === 0 && !showCreateForm" class="pm-empty">
            <div class="pm-empty-icon">
              <svg xmlns="http://www.w3.org/2000/svg" width="40" height="40" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round"><rect x="2" y="7" width="20" height="14" rx="2" ry="2"/><path d="M16 21V5a2 2 0 0 0-2-2h-4a2 2 0 0 0-2 2v16"/></svg>
            </div>
            <h3>No Portfolios Yet</h3>
            <p class="text-muted">Create your first portfolio to get started.</p>
            <button @click="showCreateForm = true" class="btn btn-primary mt-lg">
              Create Portfolio
            </button>
          </div>

          <!-- Portfolio Cards -->
          <div v-else class="pm-card-list">
            <div
              v-for="p in portfolioStore.portfolios"
              :key="p.id"
              :class="['pm-card', { 'pm-card-active': p.id === portfolioStore.currentPortfolioId }]"
              @click="selectPortfolio(p.id)"
            >
              <div class="pm-card-main">
                <div class="pm-card-info">
                  <div class="pm-card-name">
                    {{ p.name }}
                    <span v-if="p.isShared" class="pm-shared-badge">Shared</span>
                  </div>
                  <div class="pm-card-value">{{ formatCurrency(p.totalValue) }}</div>
                </div>
                <div v-if="p.id === portfolioStore.currentPortfolioId" class="pm-active-indicator">
                  <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round"><polyline points="20 6 9 17 4 12"/></svg>
                </div>
              </div>
            </div>
          </div>

          <!-- Create New Portfolio -->
          <div v-if="showCreateForm" class="pm-create-section">
            <div class="pm-create-header">
              <h4>Create New Portfolio</h4>
              <button v-if="portfolioStore.portfolios.length > 0" @click="showCreateForm = false" class="btn-close-sm">
                <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><line x1="18" y1="6" x2="6" y2="18"/><line x1="6" y1="6" x2="18" y2="18"/></svg>
              </button>
            </div>
            <form @submit.prevent="createPortfolio" class="pm-create-form">
              <div class="form-group">
                <label class="pm-label">Portfolio Name</label>
                <input
                  v-model="newPortfolioName"
                  type="text"
                  class="input w-full"
                  placeholder="e.g. Family Trust"
                  required
                  autofocus
                />
              </div>

              <!-- Demo Data Toggle -->
              <label class="pm-switch-label">
                <input type="checkbox" v-model="shouldLoadDemoData">
                <span class="pm-switch-slider"></span>
                <span class="pm-switch-text">
                  <strong>Load Demo Dataset</strong>
                  <span class="text-muted text-xs">Pre-fill with sample data (ETFs, Stocks, History).</span>
                </span>
              </label>

              <div class="pm-create-actions">
                <button v-if="portfolioStore.portfolios.length > 0" type="button" @click="showCreateForm = false" class="btn btn-secondary">Cancel</button>
                <button type="submit" class="btn btn-primary" :disabled="!newPortfolioName.trim() || creating">
                  {{ creating ? 'Creating...' : 'Create Portfolio' }}
                </button>
              </div>
            </form>
          </div>

          <!-- Add Portfolio Button -->
          <button
            v-if="!showCreateForm && portfolioStore.portfolios.length > 0"
            @click="showCreateForm = true"
            class="pm-add-btn"
          >
            <svg xmlns="http://www.w3.org/2000/svg" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><line x1="12" y1="5" x2="12" y2="19"/><line x1="5" y1="12" x2="19" y2="12"/></svg>
            New Portfolio
          </button>
        </div>
      </div>
    </div>
  </Transition>
</template>

<script setup>
import { ref, watch } from 'vue';
import { usePortfolioStore } from '../stores/portfolio';
import api from '../services/api';

const props = defineProps({
  isOpen: Boolean,
  allowClose: {
    type: Boolean,
    default: true
  }
});

const emit = defineEmits(['close', 'selected']);

const portfolioStore = usePortfolioStore();

// Create form state
const showCreateForm = ref(false);
const newPortfolioName = ref('');
const shouldLoadDemoData = ref(false);
const creating = ref(false);

const formatCurrency = (val) => {
  return new Intl.NumberFormat('en-AU', {
    style: 'currency',
    currency: 'AUD',
    minimumFractionDigits: 0,
    maximumFractionDigits: 0
  }).format(val || 0);
};

// Reset state when modal opens/closes
watch(() => props.isOpen, (val) => {
  if (val) {
    showCreateForm.value = portfolioStore.portfolios.length === 0;
    newPortfolioName.value = '';
    shouldLoadDemoData.value = false;
  }
});

const handleBackdropClick = () => {
  if (props.allowClose) {
    close();
  }
};

const close = () => {
  emit('close');
};

// --- Selection ---
const selectPortfolio = (id) => {
  portfolioStore.setCurrentPortfolio(id);
  emit('selected', id);
  close();
};

// --- Create ---
const createPortfolio = async () => {
  if (!newPortfolioName.value.trim()) return;
  creating.value = true;
  try {
    if (shouldLoadDemoData.value) {
      await api.post('/portfolios/demo', { name: newPortfolioName.value });
    } else {
      await api.post('/portfolios', { name: newPortfolioName.value });
    }
    await portfolioStore.fetchPortfolios();
    showCreateForm.value = false;
    newPortfolioName.value = '';
    shouldLoadDemoData.value = false;

    // Auto-select the newly created portfolio
    const newest = portfolioStore.portfolios[portfolioStore.portfolios.length - 1];
    if (newest) {
      selectPortfolio(newest.id);
    }
  } catch (e) {
    console.error('Failed to create portfolio:', e);
  } finally {
    creating.value = false;
  }
};
</script>

<style scoped>
/* Modal Backdrop */
.modal-backdrop {
  position: fixed;
  inset: 0;
  background: rgba(0, 0, 0, 0.5);
  backdrop-filter: blur(4px);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: var(--z-modal-backdrop, 999);
}

/* Modal Container */
.portfolio-modal {
  width: 100%;
  max-width: 520px;
  max-height: 85vh;
  display: flex;
  flex-direction: column;
  z-index: var(--z-modal, 1000);
  overflow: hidden;
}

/* Header */
.pm-header {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  padding: var(--spacing-xl) var(--spacing-xl) var(--spacing-md);
  flex-shrink: 0;
}

.pm-title {
  font-size: var(--font-size-xl);
  font-weight: 700;
  color: var(--color-text-primary);
  margin: 0 0 var(--spacing-xs);
}

.pm-subtitle {
  font-size: var(--font-size-sm);
  color: var(--color-text-muted);
  margin: 0;
}

.btn-close {
  background: transparent;
  border: none;
  color: var(--color-text-muted);
  cursor: pointer;
  padding: 4px;
  border-radius: var(--radius-sm);
  transition: all 0.15s ease;
}

.btn-close:hover {
  background: var(--color-bg-elevated);
  color: var(--color-text-primary);
}

.btn-close-sm {
  background: transparent;
  border: none;
  color: var(--color-text-muted);
  cursor: pointer;
  padding: 2px;
  border-radius: var(--radius-sm);
  transition: all 0.15s ease;
  line-height: 1;
}

.btn-close-sm:hover {
  color: var(--color-text-primary);
}

/* Body */
.pm-body {
  flex: 1;
  overflow-y: auto;
  padding: 0 var(--spacing-xl) var(--spacing-xl);
}

/* Loading Skeleton */
.pm-loading {
  display: flex;
  flex-direction: column;
  gap: var(--spacing-sm);
}

.pm-card-skeleton {
  padding: var(--spacing-lg);
  background: var(--color-bg-elevated);
  border-radius: var(--radius-md);
}

/* Empty State */
.pm-empty {
  text-align: center;
  padding: var(--spacing-2xl) var(--spacing-lg);
}

.pm-empty-icon {
  color: var(--color-text-muted);
  margin-bottom: var(--spacing-md);
  opacity: 0.5;
}

.pm-empty h3 {
  font-size: var(--font-size-lg);
  margin-bottom: var(--spacing-xs);
}

/* Card List */
.pm-card-list {
  display: flex;
  flex-direction: column;
  gap: var(--spacing-sm);
}

/* Card */
.pm-card {
  border: 1px solid var(--color-border);
  border-radius: var(--radius-md);
  background: var(--color-bg-secondary);
  transition: all 0.2s ease;
  overflow: hidden;
  cursor: pointer;
}

.pm-card:hover {
  border-color: var(--color-accent);
  box-shadow: 0 0 0 1px rgba(30, 64, 175, 0.1);
}

.pm-card-active {
  border-color: var(--color-accent);
  background: rgba(30, 64, 175, 0.04);
}

[data-theme="dark"] .pm-card-active {
  background: rgba(59, 130, 246, 0.08);
}

.pm-card-main {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: var(--spacing-md) var(--spacing-lg);
  gap: var(--spacing-md);
}

.pm-card-info {
  flex: 1;
  min-width: 0;
}

.pm-card-name {
  font-weight: 600;
  font-size: var(--font-size-base);
  color: var(--color-text-primary);
  display: flex;
  align-items: center;
  gap: var(--spacing-sm);
}

.pm-shared-badge {
  font-size: var(--font-size-xs);
  padding: 1px 6px;
  background: var(--color-bg-elevated);
  color: var(--color-text-muted);
  border-radius: var(--radius-full);
  font-weight: 500;
}

.pm-card-value {
  font-size: var(--font-size-sm);
  color: var(--color-text-muted);
  margin-top: 2px;
  font-family: var(--font-mono);
}

.pm-active-indicator {
  color: var(--color-accent);
  display: flex;
  align-items: center;
}

/* Create Section */
.pm-create-section {
  margin-top: var(--spacing-md);
  padding: var(--spacing-lg);
  background: var(--color-bg-elevated);
  border-radius: var(--radius-md);
  border: 1px dashed var(--color-border);
}

.pm-create-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: var(--spacing-md);
}

.pm-create-header h4 {
  font-size: var(--font-size-base);
  font-weight: 600;
  margin: 0;
}

.pm-create-form {
  display: flex;
  flex-direction: column;
  gap: var(--spacing-md);
}

.pm-label {
  display: block;
  font-size: var(--font-size-xs);
  font-weight: 600;
  color: var(--color-text-muted);
  text-transform: uppercase;
  letter-spacing: 0.03em;
  margin-bottom: var(--spacing-xs);
}

.pm-create-actions {
  display: flex;
  justify-content: flex-end;
  gap: var(--spacing-sm);
  margin-top: var(--spacing-xs);
}

/* Switch Label (Demo Data Toggle) */
.pm-switch-label {
  display: flex;
  align-items: flex-start;
  cursor: pointer;
  gap: var(--spacing-md);
  padding: var(--spacing-sm);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-md);
  transition: border-color var(--transition-fast);
}

.pm-switch-label:hover {
  border-color: var(--color-primary, var(--color-accent));
}

.pm-switch-label input {
  display: none;
}

.pm-switch-slider {
  display: inline-block;
  width: 44px;
  height: 24px;
  background-color: #cbd5e1;
  border-radius: 24px;
  position: relative;
  transition: background-color var(--transition-fast);
  flex-shrink: 0;
  margin-top: 2px;
}

.pm-switch-slider::after {
  content: '';
  position: absolute;
  top: 2px;
  left: 2px;
  width: 18px;
  height: 18px;
  background-color: white;
  border-radius: 50%;
  transition: transform var(--transition-fast);
  box-shadow: 0 2px 4px rgba(0,0,0,0.2);
}

input:checked + .pm-switch-slider {
  background-color: var(--color-primary, var(--color-accent));
}

input:checked + .pm-switch-slider::after {
  transform: translateX(20px);
}

.pm-switch-text strong {
  display: block;
  font-size: var(--font-size-sm);
  margin-bottom: 2px;
}

.pm-switch-text .text-xs {
  display: block;
}

/* Add Button */
.pm-add-btn {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: var(--spacing-sm);
  width: 100%;
  padding: var(--spacing-md);
  margin-top: var(--spacing-sm);
  background: transparent;
  border: 1px dashed var(--color-border);
  border-radius: var(--radius-md);
  color: var(--color-text-muted);
  font-size: var(--font-size-sm);
  font-weight: 500;
  cursor: pointer;
  transition: all 0.2s ease;
}

.pm-add-btn:hover {
  border-color: var(--color-accent);
  color: var(--color-accent);
  background: rgba(30, 64, 175, 0.03);
}

/* Modal Transitions */
.modal-enter-active,
.modal-leave-active {
  transition: opacity 0.2s ease;
}

.modal-enter-from,
.modal-leave-to {
  opacity: 0;
}

.slide-in {
  animation: slideUp 0.25s ease-out;
}

@keyframes slideUp {
  from {
    opacity: 0;
    transform: translateY(16px);
  }
  to {
    opacity: 1;
    transform: translateY(0);
  }
}

/* Utility classes */
.text-muted { color: var(--color-text-muted); }
.text-xs { font-size: var(--font-size-xs); }
.mt-lg { margin-top: var(--spacing-lg); }
.w-full { width: 100%; }

@media (max-width: 600px) {
  .portfolio-modal {
    max-width: 100%;
    margin: var(--spacing-md);
    max-height: 90vh;
  }
}
</style>

<template>
  <div class="portfolio-list animate-fade-in">
    <!-- Header -->
    <div class="page-header">
      <div class="header-content">
        <h1>Portfolios</h1>
        <p class="text-muted">Manage your investment strategies</p>
      </div>
      <button @click="showCreate = true" class="btn btn-primary">
        <svg xmlns="http://www.w3.org/2000/svg" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><line x1="12" y1="5" x2="12" y2="19"/><line x1="5" y1="12" x2="19" y2="12"/></svg>
        New Portfolio
      </button>
    </div>

    <!-- Loading State -->
    <div v-if="loading" class="portfolio-grid">
      <div v-for="i in 3" :key="i" class="skeleton-card card">
        <div class="skeleton skeleton-title"></div>
        <div class="skeleton skeleton-text"></div>
        <div class="skeleton skeleton-value"></div>
      </div>
    </div>
    
    <!-- Empty State -->
    <div v-else-if="portfolios.length === 0" class="empty-state card">
      <div class="empty-icon">
        <svg xmlns="http://www.w3.org/2000/svg" width="48" height="48" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1" stroke-linecap="round" stroke-linejoin="round"><rect x="2" y="7" width="20" height="14" rx="2" ry="2"/><path d="M16 21V5a2 2 0 0 0-2-2h-4a2 2 0 0 0-2 2v16"/></svg>
      </div>
      <h2>Create Your First Portfolio</h2>
      <p class="text-muted">Start tracking your investments</p>
      <button @click="showCreate = true" class="btn btn-primary mt-lg">
        Create Portfolio
      </button>
    </div>

    <!-- Portfolio Grid -->
    <div v-else class="portfolio-grid">
      <div 
        v-for="(p, index) in portfolios" 
        :key="p.id" 
        class="portfolio-card card animate-fade-in-up"
        :style="{ animationDelay: `${index * 0.05}s` }"
        @click="selectPortfolio(p.id); $router.push(`/portfolio/${p.id}`)"
      >
        <div class="card-header">
          <h3>{{ p.name }}</h3>
          <span v-if="p.benchmarkSymbol" class="benchmark-badge">{{ p.benchmarkSymbol }}</span>
        </div>
        
        <div class="card-body">
          <span class="value-label">Total Value</span>
          <div class="value-display">{{ formatCurrency(p.totalValue) }}</div>
        </div>

        <div class="card-footer">
          <div class="portfolio-stats">
            <span class="stat-value">{{ p.targetAllocation?.length || 0 }}</span>
            <span class="stat-label">Categories</span>
          </div>
          <svg class="card-arrow" xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><polyline points="9 18 15 12 9 6"/></svg>
        </div>
      </div>
    </div>

    <!-- Create Modal -->
    <Transition name="modal">
      <div v-if="showCreate" class="modal-backdrop" @click.self="showCreate = false">
        <div class="modal card">
          <div class="modal-header">
            <h2>Create New Portfolio</h2>
            <button class="btn-close" @click="showCreate = false">
              <svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><line x1="18" y1="6" x2="6" y2="18"/><line x1="6" y1="6" x2="18" y2="18"/></svg>
            </button>
          </div>
          <p class="text-muted mb-lg">Enter a name for your new investment strategy.</p>
          <form @submit.prevent="createPortfolio">
            <div class="form-group">
              <label>Portfolio Name</label>
              <input 
                v-model="newPortfolioName" 
                required 
                placeholder="e.g. Family Trust" 
                autofocus
              >
            </div>
            <!-- Demo Data Switch -->
            <div class="form-group mt-lg">
              <label class="switch-label">
                <input type="checkbox" v-model="shouldLoadDemoData">
                <span class="switch-slider"></span>
                <span class="switch-text">
                  <strong>Load Demo Dataset</strong>
                  <span class="text-muted block text-xs">Pre-fill with comprehensive sample data (ETFs, Stocks, History).</span>
                </span>
              </label>
            </div>

            <div class="modal-actions mt-xl">
              <button type="button" @click="showCreate = false" class="btn btn-secondary">Cancel</button>
              <button type="submit" class="btn btn-primary" :disabled="!newPortfolioName.trim() || loading">
                {{ loading ? 'Creating...' : 'Create Portfolio' }}
              </button>
            </div>
          </form>
        </div>
      </div>
    </Transition>
  </div>
</template>

<script setup>
import { ref, onMounted, computed } from 'vue';
import { usePortfolioStore } from '../stores/portfolio';
import api from '../services/api';

const portfolioStore = usePortfolioStore();
const portfolios = computed(() => portfolioStore.portfolios);
const loading = computed(() => portfolioStore.loading);
const showCreate = ref(false);
const shouldLoadDemoData = ref(false);
const newPortfolioName = ref('');

const formatCurrency = (val) => {
  return new Intl.NumberFormat('en-AU', { 
    style: 'currency', 
    currency: 'AUD',
    minimumFractionDigits: 0,
    maximumFractionDigits: 0
  }).format(val || 0);
};

const fetchPortfolios = async () => {
  await portfolioStore.fetchPortfolios();
};

const selectPortfolio = (id) => {
  portfolioStore.setCurrentPortfolio(id);
  // Navigation is handled by @click in template
};

const createPortfolio = async () => {
  if (!newPortfolioName.value.trim()) return;
  
  try {
    if (shouldLoadDemoData.value) {
        await api.post('/portfolios/demo', { name: newPortfolioName.value }); 
    } else {
        await api.post('/portfolios', { name: newPortfolioName.value });
    }
    
    showCreate.value = false;
    newPortfolioName.value = '';
    shouldLoadDemoData.value = false;
    await portfolioStore.fetchPortfolios();
  } catch (e) {
    console.error('Failed to create portfolio:', e);
  }
};

onMounted(fetchPortfolios);
</script>

<style scoped>
.portfolio-list {
  max-width: 1200px;
}

/* Header */
.page-header {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  margin-bottom: var(--spacing-xl);
}

.page-header h1 {
  margin-bottom: var(--spacing-xs);
}

/* Portfolio Grid */
.portfolio-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(300px, 1fr));
  gap: var(--spacing-lg);
}

/* Portfolio Card */
.portfolio-card {
  padding: var(--spacing-lg);
  cursor: pointer;
  transition: all var(--transition-fast);
}

.portfolio-card:hover {
  transform: translateY(-2px);
  box-shadow: var(--shadow-lg);
}

.portfolio-card:hover .card-arrow {
  transform: translateX(4px);
  color: var(--color-accent);
}

.card-header {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  margin-bottom: var(--spacing-md);
}

.card-header h3 {
  font-size: var(--font-size-lg);
  font-weight: 600;
}

.benchmark-badge {
  padding: 2px 8px;
  background: var(--color-bg-elevated);
  color: var(--color-text-muted);
  font-size: var(--font-size-xs);
  font-weight: 600;
  border-radius: var(--radius-full);
}

/* Card Body */
.value-label {
  display: block;
  font-size: var(--font-size-xs);
  font-weight: 500;
  color: var(--color-text-muted);
  text-transform: uppercase;
  letter-spacing: 0.025em;
  margin-bottom: var(--spacing-xs);
}

.value-display {
  font-size: var(--font-size-xl);
  font-weight: 700;
  color: var(--color-text-primary);
  margin-bottom: var(--spacing-md);
}

/* Card Footer */
.card-footer {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding-top: var(--spacing-md);
  border-top: 1px solid var(--color-border);
}

.portfolio-stats {
  display: flex;
  align-items: baseline;
  gap: var(--spacing-xs);
}

.stat-value {
  font-weight: 600;
  color: var(--color-accent);
}

.stat-label {
  font-size: var(--font-size-sm);
  color: var(--color-text-muted);
}

.card-arrow {
  color: var(--color-text-muted);
  transition: all var(--transition-fast);
}

/* Empty State */
.empty-state {
  text-align: center;
  padding: var(--spacing-3xl);
}

.empty-icon {
  color: var(--color-text-muted);
  margin-bottom: var(--spacing-lg);
}

.empty-state h2 {
  margin-bottom: var(--spacing-sm);
}

/* Skeleton */
.skeleton-card {
  padding: var(--spacing-lg);
  pointer-events: none;
}

.skeleton-title {
  height: 24px;
  width: 60%;
  margin-bottom: var(--spacing-md);
}

.skeleton-text {
  height: 12px;
  width: 40%;
  margin-bottom: var(--spacing-lg);
}

.skeleton-value {
  height: 32px;
  width: 50%;
}

/* Modal */
.modal-backdrop {
  position: fixed;
  inset: 0;
  background: rgba(0, 0, 0, 0.5);
  backdrop-filter: blur(4px);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: var(--z-modal-backdrop);
}

.modal {
  width: 100%;
  max-width: 420px;
  padding: var(--spacing-xl);
  z-index: var(--z-modal);
}

.modal-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: var(--spacing-md);
}

.modal-header h2 {
  font-size: var(--font-size-lg);
}

.btn-close {
  background: transparent;
  border: none;
  color: var(--color-text-muted);
  cursor: pointer;
  padding: 4px;
  transition: color var(--transition-fast);
}

.btn-close:hover {
  color: var(--color-text-primary);
}

.modal-actions {
  display: flex;
  justify-content: flex-end;
  gap: var(--spacing-sm);
  margin-top: var(--spacing-lg);
}

/* Transitions */
.modal-enter-active, .modal-leave-active {
  transition: opacity 0.2s ease;
}
.modal-enter-from, .modal-leave-to {
  opacity: 0;
}

.mb-lg { margin-bottom: var(--spacing-lg); }
.mt-lg { margin-top: var(--spacing-lg); }

/* Switch Component */
.switch-label {
  display: flex;
  align-items: flex-start;
  cursor: pointer;
  gap: var(--spacing-md);
  padding: var(--spacing-sm);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-md);
  transition: border-color var(--transition-fast);
}
.switch-label:hover {
  border-color: var(--color-primary);
}

.switch-label input {
  display: none;
}

.switch-slider {
  display: inline-block;
  width: 44px;
  height: 24px;
  background-color: #cbd5e1; /* Increased contrast from border color */
  border-radius: 24px;
  position: relative;
  transition: background-color var(--transition-fast);
  flex-shrink: 0;
  margin-top: 4px;
  border: 1px solid transparent; 
}

.switch-slider::after {
  content: '';
  position: absolute;
  top: 2px;
  left: 2px;
  width: 18px; /* Slightly smaller to show more background */
  height: 18px;
  background-color: white;
  border-radius: 50%;
  transition: transform var(--transition-fast);
  box-shadow: 0 2px 4px rgba(0,0,0,0.2); /* Stronger shadow */
}

input:checked + .switch-slider {
  background-color: var(--color-primary);
  border-color: rgba(0,0,0,0.1);
}

input:checked + .switch-slider::after {
  transform: translateX(20px);
}

.switch-text strong {
  display: block;
  font-size: var(--font-size-sm);
  margin-bottom: 2px;
}

.mt-xl { margin-top: var(--spacing-xl); }
.block { display: block; }
.text-xs { font-size: var(--font-size-xs); }

</style>

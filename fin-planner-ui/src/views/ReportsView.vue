<template>
  <div class="reports-view animate-fade-in content-container">
    <!-- Header Section -->
    <div class="page-header mb-lg">
      <div>
        <h1>Reports & Analysis</h1>
        <p class="text-muted">Track performance, tax liabilities, and transaction history.</p>
      </div>
      
      <div class="controls">
        <div class="select-wrapper">
          <select v-model="selectedPortfolioId" class="portfolio-select" v-if="portfolios.length > 0">
            <option v-for="p in portfolios" :key="p.id" :value="p.id">
              {{ p.name }}
            </option>
          </select>
        </div>
      </div>
    </div>

    <div v-if="selectedPortfolioId">
      <!-- Tabs Navigation -->
      <div class="tabs mb-lg">
        <button 
          v-for="tab in tabs" 
          :key="tab.id"
          class="tab-btn"
          :class="{ active: currentTab === tab.id }"
          @click="currentTab = tab.id"
        >
          {{ tab.label }}
        </button>
      </div>

      <!-- Tab Content -->
      <div class="tab-content">
        <!-- Performance Tab -->
        <div v-if="currentTab === 'performance'" class="animate-fade-in">
          <div class="section-header mb-md">
            <h2>Portfolio Performance</h2>
            <p class="text-muted">Visualizing portfolio value and growth over time versus invested capital.</p>
          </div>
          <PerformanceChart :portfolioId="selectedPortfolioId" />
        </div>

        <!-- Tax Tab -->
        <div v-if="currentTab === 'tax'" class="animate-fade-in">
          <div class="section-header mb-md">
            <h2>Tax Impact</h2>
            <p class="text-muted">Estimated tax liabilities, realized gains, and franking credits for the financial year.</p>
          </div>
          <TaxSummary :portfolioId="selectedPortfolioId" />
        </div>

        <!-- Transactions Tab -->
        <div v-if="currentTab === 'transactions'" class="animate-fade-in">
          <div class="section-header mb-md">
            <h2>Transaction Ledger</h2>
            <p class="text-muted">Complete history of all buy, sell, and income events.</p>
          </div>
          <TransactionLedger :portfolioId="selectedPortfolioId" />
        </div>
      </div>
    </div>

    <!-- Loading/Empty State -->
    <div v-else class="loading-state card">
      <div v-if="loading" class="spinner"></div>
      <p v-else class="text-muted">No portfolios found. Please create one to view reports.</p>
    </div>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue';
import api from '../services/api';
import TaxSummary from '../components/reports/TaxSummary.vue';
import PerformanceChart from '../components/reports/PerformanceChart.vue';
import TransactionLedger from '../components/reports/TransactionLedger.vue';

const portfolios = ref([]);
const selectedPortfolioId = ref(null);
const loading = ref(true);
const currentTab = ref('performance');

const tabs = [
  { id: 'performance', label: 'Performance' },
  { id: 'tax', label: 'Tax Center' },
  { id: 'transactions', label: 'Ledger' }
];

const loadPortfolios = async () => {
  try {
    const res = await api.get('/portfolios');
    portfolios.value = res.data;
    if (portfolios.value.length > 0) {
      selectedPortfolioId.value = portfolios.value[0].id;
    }
  } catch (e) {
    console.error('Failed to load portfolios', e);
  } finally {
    loading.value = false;
  }
};

onMounted(loadPortfolios);
</script>

<style scoped>
.content-container {
  max-width: 1200px;
  margin: 0 auto;
  padding-bottom: var(--spacing-2xl);
}

.page-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  flex-wrap: wrap;
  gap: var(--spacing-md);
}

.text-muted {
  color: var(--color-text-muted);
  font-size: var(--font-size-sm);
  margin-top: 4px;
}

.portfolio-select {
  padding: 8px 16px;
  padding-right: 32px;
  border-radius: var(--radius-md);
  border: 1px solid var(--color-border);
  background: var(--color-bg-elevated);
  color: var(--color-text-primary);
  min-width: 240px;
  cursor: pointer;
  appearance: none;
  background-image: url("data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' width='16' height='16' viewBox='0 0 24 24' fill='none' stroke='%2364748b' stroke-width='2' stroke-linecap='round' stroke-linejoin='round'%3E%3Cpolyline points='6 9 12 15 18 9'/%3E%3C/svg%3E");
  background-repeat: no-repeat;
  background-position: right 12px center;
}

/* Tabs */
.tabs {
  display: flex;
  gap: 4px;
  border-bottom: 1px solid var(--color-border);
  overflow-x: auto;
}

.tab-btn {
  padding: var(--spacing-md) var(--spacing-lg);
  background: transparent;
  border: none;
  border-bottom: 2px solid transparent;
  color: var(--color-text-muted);
  font-weight: 500;
  cursor: pointer;
  transition: all 0.2s;
  font-size: var(--font-size-sm);
  white-space: nowrap;
}

.tab-btn:hover {
  color: var(--color-text-primary);
  background: var(--color-bg-elevated);
}

.tab-btn.active {
  color: var(--color-accent);
  border-bottom-color: var(--color-accent);
  background: transparent;
}

.section-header h2 {
  font-size: var(--font-size-lg);
  margin-bottom: 4px;
}

.loading-state {
  display: flex;
  flex-direction: column;
  justify-content: center;
  align-items: center;
  padding: 100px;
  text-align: center;
  gap: var(--spacing-md);
}

.spinner {
  width: 24px;
  height: 24px;
  border: 2px solid var(--color-border);
  border-top-color: var(--color-accent);
  border-radius: 50%;
  animation: spin 0.8s linear infinite;
}

@keyframes spin {
  to { transform: rotate(360deg); }
}

@media (max-width: 640px) {
  .page-header {
    flex-direction: column;
    align-items: flex-start;
  }
  
  .controls {
    width: 100%;
  }

  .portfolio-select {
    width: 100%;
  }

  .tabs {
    overflow-x: auto;
    padding-bottom: 1px; /* Hide scrollbar glitch */
  }
  
  .tab-btn {
    white-space: nowrap;
  }
}
</style>

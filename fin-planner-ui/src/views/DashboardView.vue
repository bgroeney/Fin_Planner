<template>
  <div class="dashboard animate-fade-in">
    <div v-if="loading" class="page-loader-container">
      <MoneyBoxLoader size="lg" text="Loading Financial Overview..." />
    </div>

    <!-- Welcome Section -->
    <section v-else class="welcome-section">
      <div class="welcome-content">
        <h1 class="welcome-title">Welcome back, {{ userName }}</h1>
        <p class="welcome-subtitle">Your portfolio overview for {{ currentDate }}</p>
      </div>
      <router-link to="/portfolios" class="btn btn-primary">
        View Portfolios
      </router-link>
    </section>

    <!-- Performance Section (New) -->
    <section v-if="!loading" class="performance-section animate-fade-in-up">
      <div class="section-header-row">
        <h2 class="section-title">Performance</h2>
        <div class="benchmark-selector">
          <label for="benchmark">Benchmark:</label>
          <select id="benchmark" v-model="selectedBenchmark">
            <option value="">None</option>
            <option value="ASX200">ASX 200</option>
            <option value="S&P500">S&P 500</option>
            <option value="ALLORDS">All Ordinaries</option>
          </select>
        </div>
      </div>
      
      <!-- We need to import PerformanceChart first, assumed available in components/reports -->
      <PerformanceChart 
        v-if="primaryPortfolioId" 
        :portfolio-id="primaryPortfolioId" 
        :benchmark="selectedBenchmark"
      />
      <div v-else class="empty-state-card card">
        <p>Select a portfolio to view performance</p>
      </div>
    </section>

    <!-- Stats Grid -->
    <section v-if="!loading" class="stats-grid">
      <div class="stat-card card animate-fade-in-up stagger-1">
        <div class="stat-header">
          <span class="stat-label">Total Net Worth</span>
        </div>
        <div class="stat-value">{{ formatCurrency(totalValue) }}</div>
        <div class="stat-change" :class="{ 'value-positive': changePercent >= 0, 'value-negative': changePercent < 0 }">
          {{ changePercent >= 0 ? '+' : '' }}{{ changePercent.toFixed(2) }}% this month
        </div>
      </div>

      <div class="stat-card card animate-fade-in-up stagger-2">
        <div class="stat-header">
          <span class="stat-label">Portfolios</span>
        </div>
        <div class="stat-value">{{ portfolioCount }}</div>
        <div class="stat-detail text-muted">Active strategies</div>
      </div>

      <div class="stat-card card animate-fade-in-up stagger-3">
        <div class="stat-header">
          <span class="stat-label">Assets Tracked</span>
        </div>
        <div class="stat-value">{{ assetCount }}</div>
        <div class="stat-detail text-muted">Across all accounts</div>
      </div>

      <!-- Replace Pending Decisions Stat with Goal Widget (spans 1 cell) -->
      <div class="card animate-fade-in-up stagger-4" style="padding: 0; overflow: hidden;">
        <GoalProgressWidget />
      </div>
    </section>

    <!-- Quick Actions -->
    <section v-if="!loading" class="actions-section">
      <h2 class="section-title">Quick Actions</h2>
      <div class="actions-grid">
        <router-link to="/import" class="action-card card">
          <div class="action-icon">
            <svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round"><path d="M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4"/><polyline points="17 8 12 3 7 8"/><line x1="12" y1="3" x2="12" y2="15"/></svg>
          </div>
          <div class="action-content">
            <h3>Import Data</h3>
            <p>Upload portfolio valuations</p>
          </div>
          <svg class="action-arrow" xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><polyline points="9 18 15 12 9 6"/></svg>
        </router-link>

        <router-link to="/decisions" class="action-card card">
          <div class="action-icon">
            <svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round"><path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z"/><polyline points="14 2 14 8 20 8"/><line x1="9" y1="15" x2="15" y2="15"/></svg>
          </div>
          <div class="action-content">
            <h3>Review Decisions</h3>
            <p>AI recommendations</p>
          </div>
          <svg class="action-arrow" xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><polyline points="9 18 15 12 9 6"/></svg>
        </router-link>

        <router-link to="/reports" class="action-card card">
          <div class="action-icon">
            <svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round"><line x1="18" y1="20" x2="18" y2="10"/><line x1="12" y1="20" x2="12" y2="4"/><line x1="6" y1="20" x2="6" y2="14"/></svg>
          </div>
          <div class="action-content">
            <h3>Generate Reports</h3>
            <p>Tax summaries & performance</p>
          </div>
          <svg class="action-arrow" xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><polyline points="9 18 15 12 9 6"/></svg>
        </router-link>
      </div>
    </section>
  </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue';
import { useAuthStore } from '../stores/auth';
import { usePortfolioStore } from '../stores/portfolio';
import api from '../services/api';
import MoneyBoxLoader from '../components/MoneyBoxLoader.vue';
import PerformanceChart from '../components/reports/PerformanceChart.vue';
import GoalProgressWidget from '../components/goals/GoalProgressWidget.vue'; // New Import
import { formatCurrency, formatDate } from '../utils/formatters';

const authStore = useAuthStore();
const portfolioStore = usePortfolioStore();

const loading = computed(() => portfolioStore.loading);
const portfolios = computed(() => portfolioStore.portfolios);
const selectedBenchmark = ref('');

const userName = computed(() => {
  const email = authStore.user?.email || 'User';
  if (!email.includes('@')) return email;
  const namePart = email.split('@')[0];
  return namePart.split('.').map(w => w.charAt(0).toUpperCase() + w.slice(1)).join(' ');
});

const currentDate = computed(() => {
  return formatDate(new Date(), 'long');
});

const totalValue = computed(() => {
  return portfolios.value.reduce((sum, p) => sum + (p.totalValue || 0), 0);
});

const portfolioCount = computed(() => portfolios.value.length);

const primaryPortfolioId = computed(() => {
  return portfolioStore.currentPortfolio?.id || null;
});

const assetCount = computed(() => {
  return portfolios.value.reduce((sum, p) => sum + (p.holdingCount || 0), 0);
});

const pendingDecisions = ref(0);
const changePercent = ref(2.34);

const fetchData = async () => {
  await portfolioStore.fetchPortfolios();
};

onMounted(fetchData);
</script>

<style scoped>
.dashboard {
  max-width: 1200px;
}

/* Performance Section */
.performance-section {
  margin-bottom: var(--spacing-xl);
}

.section-header-row {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: var(--spacing-md);
}

.benchmark-selector {
  display: flex;
  align-items: center;
  gap: var(--spacing-sm);
  font-size: var(--font-size-sm);
  color: var(--color-text-muted);
}

.benchmark-selector select {
  background: var(--color-bg-elevated);
  border: 1px solid var(--color-border);
  color: var(--color-text-primary);
  padding: 4px 8px;
  border-radius: var(--radius-md);
  outline: none;
}

.empty-state-card {
  padding: var(--spacing-2xl);
  text-align: center;
  color: var(--color-text-muted);
  background: var(--color-bg-elevated);
}

/* Welcome Section */
.welcome-section {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  margin-bottom: var(--spacing-xl);
}

.welcome-title {
  font-size: var(--font-size-2xl);
  font-weight: 600;
  margin-bottom: var(--spacing-xs);
}

.welcome-subtitle {
  color: var(--color-text-muted);
}

/* Stats Grid */
.stats-grid {
  display: grid;
  grid-template-columns: repeat(4, 1fr);
  gap: var(--spacing-lg);
  margin-bottom: var(--spacing-2xl);
}

@media (max-width: 1024px) {
  .stats-grid {
    grid-template-columns: repeat(2, 1fr);
  }
}

@media (max-width: 640px) {
  .stats-grid {
    grid-template-columns: 1fr;
  }
}

.stat-card {
  padding: var(--spacing-lg);
}

.stat-label {
  font-size: var(--font-size-sm);
  font-weight: 500;
  color: var(--color-text-muted);
  text-transform: uppercase;
  letter-spacing: 0.025em;
}

.stat-value {
  font-size: var(--font-size-2xl);
  font-weight: 700;
  color: var(--color-text-primary);
  margin: var(--spacing-sm) 0;
}

.stat-change {
  font-size: var(--font-size-sm);
  font-weight: 500;
}

.stat-detail {
  font-size: var(--font-size-sm);
}

/* Section Title */
.section-title {
  font-size: var(--font-size-lg);
  font-weight: 600;
  margin-bottom: var(--spacing-lg);
}

/* Actions Grid */
.actions-grid {
  display: grid;
  grid-template-columns: repeat(3, 1fr);
  gap: var(--spacing-lg);
}

@media (max-width: 900px) {
  .actions-grid {
    grid-template-columns: 1fr;
  }
}

.action-card {
  display: flex;
  align-items: center;
  gap: var(--spacing-md);
  padding: var(--spacing-lg);
  text-decoration: none;
  cursor: pointer;
}

.action-card:hover .action-arrow {
  transform: translateX(4px);
  color: var(--color-accent);
}

.action-icon {
  width: 44px;
  height: 44px;
  display: flex;
  align-items: center;
  justify-content: center;
  background: var(--color-bg-elevated);
  border-radius: var(--radius-md);
  color: var(--color-accent);
  flex-shrink: 0;
}

.action-content {
  flex: 1;
}

.action-content h3 {
  font-size: var(--font-size-base);
  font-weight: 600;
  color: var(--color-text-primary);
  margin-bottom: 2px;
}

.action-content p {
  font-size: var(--font-size-sm);
  color: var(--color-text-muted);
  margin: 0;
}

.action-arrow {
  color: var(--color-text-muted);
  transition: all var(--transition-fast);
  flex-shrink: 0;
}

.page-loader-container {
  display: flex;
  justify-content: center;
  align-items: center;
  height: 400px;
}
</style>

<template>
  <div class="dividends-view animate-fade-in">
    <div class="header-row">
      <div class="header-content">
        <h1>Dividend Calendar</h1>
        <p class="subtitle">Track upcoming dividend payments and historical income</p>
      </div>
    </div>

    <div v-if="loading" class="loader-container">
      <div class="spinner"></div>
    </div>

    <template v-else>
      <!-- Summary Cards -->
      <div class="summary-grid">
        <div class="summary-card card">
          <div class="label-text">Upcoming (12 months)</div>
          <div class="value-large">{{ formatCurrency(upcomingTotal) }}</div>
          <div class="text-muted">{{ upcomingDividends.length }} payments expected</div>
        </div>
        <div class="summary-card card">
          <div class="label-text">FY {{ currentFY }} Received</div>
          <div class="value-large value-positive">{{ formatCurrency(historyTotal) }}</div>
          <div class="text-muted">{{ dividendHistory.length }} payments</div>
        </div>
        <div class="summary-card card">
          <div class="label-text">Franking Credits</div>
          <div class="value-large">{{ formatCurrency(frankingCredits) }}</div>
          <div class="text-muted">Tax offset available</div>
        </div>
      </div>

      <!-- Monthly Chart -->
      <div class="chart-section card" v-if="monthlySummary.length > 0">
        <h3>Monthly Dividend Income</h3>
        <div class="chart-container">
          <apexchart 
            type="bar" 
            height="250" 
            :options="chartOptions" 
            :series="chartSeries"
          />
        </div>
      </div>

      <!-- Tabs -->
      <div class="tabs">
        <button 
          class="tab" 
          :class="{ active: activeTab === 'upcoming' }"
          @click="activeTab = 'upcoming'"
        >
          Upcoming
        </button>
        <button 
          class="tab" 
          :class="{ active: activeTab === 'history' }"
          @click="activeTab = 'history'"
        >
          History
        </button>
      </div>

      <!-- Upcoming Dividends -->
      <div v-if="activeTab === 'upcoming'" class="table-section card">
        <table v-if="upcomingDividends.length > 0" class="data-table">
          <thead>
            <tr>
              <th>Expected Date</th>
              <th>Asset</th>
              <th>Units Held</th>
              <th class="text-right">Est. Amount</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="div in upcomingDividends" :key="`${div.assetId}-${div.paymentDate}`">
              <td>
                <span class="date-badge">{{ formatDate(div.paymentDate) }}</span>
              </td>
              <td>
                <div class="asset-cell">
                  <span class="asset-symbol">{{ div.assetSymbol }}</span>
                  <span class="asset-name">{{ div.assetName }}</span>
                </div>
              </td>
              <td>{{ formatNumber(div.units) }}</td>
              <td class="text-right">
                <span class="value-positive">{{ formatCurrency(div.estimatedAmount) }}</span>
                <span v-if="div.isEstimate" class="estimate-badge">Est.</span>
              </td>
            </tr>
          </tbody>
        </table>
        <div v-else class="empty-state">
          <p>No upcoming dividends estimated. Add dividend-paying holdings to see projections.</p>
        </div>
      </div>

      <!-- History -->
      <div v-if="activeTab === 'history'" class="table-section card">
        <div class="filter-row">
          <label>Financial Year:</label>
          <select v-model="selectedFY" @change="loadHistory">
            <option v-for="fy in fiscalYears" :key="fy" :value="fy">FY {{ fy }}</option>
          </select>
        </div>
        <table v-if="dividendHistory.length > 0" class="data-table">
          <thead>
            <tr>
              <th>Date</th>
              <th>Asset</th>
              <th class="text-right">Amount</th>
              <th class="text-right">Franking Credits</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="div in dividendHistory" :key="`${div.date}-${div.assetSymbol}`">
              <td>{{ formatDate(div.date) }}</td>
              <td>
                <div class="asset-cell">
                  <span class="asset-symbol">{{ div.assetSymbol }}</span>
                  <span class="asset-name">{{ div.assetName }}</span>
                </div>
              </td>
              <td class="text-right value-positive">{{ formatCurrency(div.amount) }}</td>
              <td class="text-right">{{ formatCurrency(div.frankingCredits) }}</td>
            </tr>
          </tbody>
        </table>
        <div v-else class="empty-state">
          <p>No dividend history for this financial year.</p>
        </div>
      </div>
    </template>
  </div>
</template>

<script setup>
import { ref, computed, onMounted, watch } from 'vue';
import { usePortfolioStore } from '../stores/portfolio';
import api from '../services/api';
import { formatCurrency, formatDate, formatNumber } from '../utils/formatters';
import VueApexCharts from 'vue3-apexcharts';

const apexchart = VueApexCharts;
const portfolioStore = usePortfolioStore();

const loading = ref(true);
const activeTab = ref('upcoming');
const upcomingDividends = ref([]);
const dividendHistory = ref([]);
const monthlySummary = ref([]);
const selectedFY = ref(new Date().getMonth() > 5 ? new Date().getFullYear() + 1 : new Date().getFullYear());
const currentFY = computed(() => selectedFY.value);

const currentPortfolioId = computed(() => portfolioStore.currentPortfolioId);

const fiscalYears = computed(() => {
  const current = new Date().getFullYear();
  return [current + 1, current, current - 1, current - 2];
});

const upcomingTotal = computed(() => upcomingDividends.value.reduce((sum, d) => sum + d.estimatedAmount, 0));
const historyTotal = computed(() => dividendHistory.value.reduce((sum, d) => sum + d.amount, 0));
const frankingCredits = computed(() => dividendHistory.value.reduce((sum, d) => sum + d.frankingCredits, 0));

const chartOptions = computed(() => ({
  chart: {
    type: 'bar',
    toolbar: { show: false },
    background: 'transparent'
  },
  colors: ['#10b981'],
  plotOptions: {
    bar: {
      borderRadius: 4,
      columnWidth: '60%'
    }
  },
  dataLabels: { enabled: false },
  xaxis: {
    categories: monthlySummary.value.map(m => `${m.monthName} ${m.year}`),
    labels: { style: { colors: '#94a3b8' } }
  },
  yaxis: {
    labels: {
      style: { colors: '#94a3b8' },
      formatter: (val) => '$' + val.toLocaleString()
    }
  },
  grid: {
    borderColor: 'rgba(255,255,255,0.1)'
  },
  tooltip: {
    theme: 'dark',
    y: { formatter: (val) => formatCurrency(val) }
  }
}));

const chartSeries = computed(() => [{
  name: 'Dividends',
  data: monthlySummary.value.map(m => m.totalAmount)
}]);

const loadData = async () => {
  if (!currentPortfolioId.value) {
    loading.value = false;
    return;
  }
  loading.value = true;
  try {
    await Promise.all([loadUpcoming(), loadHistory(), loadSummary()]);
  } finally {
    loading.value = false;
  }
};

const loadUpcoming = async () => {
  try {
    const res = await api.get(`/dividends/upcoming/${currentPortfolioId.value}`);
    upcomingDividends.value = res.data;
  } catch (e) {
    console.error('Failed to load upcoming dividends', e);
    upcomingDividends.value = [];
  }
};

const loadHistory = async () => {
  try {
    const res = await api.get(`/dividends/history/${currentPortfolioId.value}?fiscalYear=${selectedFY.value}`);
    dividendHistory.value = res.data;
  } catch (e) {
    console.error('Failed to load dividend history', e);
    dividendHistory.value = [];
  }
};

const loadSummary = async () => {
  try {
    const res = await api.get(`/dividends/summary/${currentPortfolioId.value}`);
    monthlySummary.value = res.data;
  } catch (e) {
    console.error('Failed to load monthly summary', e);
    monthlySummary.value = [];
  }
};

// Watch for portfolio changes
watch(currentPortfolioId, () => {
  loadData();
});

onMounted(() => {
  if (currentPortfolioId.value) {
    loadData();
  } else {
    loading.value = false;
  }
});
</script>

<style scoped>
.dividends-view {
  max-width: 1200px;
  margin: 0 auto;
}

.header-row {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: var(--spacing-xl);
  flex-wrap: wrap;
  gap: var(--spacing-md);
}

.portfolio-selector select {
  padding: var(--spacing-sm) var(--spacing-md);
  border-radius: var(--radius-md);
  border: 1px solid var(--color-border);
  background: var(--color-bg-card);
  color: var(--color-text-primary);
  min-width: 200px;
}

.summary-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(220px, 1fr));
  gap: var(--spacing-lg);
  margin-bottom: var(--spacing-xl);
}

.summary-card {
  padding: var(--spacing-lg);
}

.chart-section {
  padding: var(--spacing-lg);
  margin-bottom: var(--spacing-xl);
}

.chart-section h3 {
  margin-bottom: var(--spacing-md);
}

.tabs {
  display: flex;
  gap: var(--spacing-sm);
  margin-bottom: var(--spacing-lg);
}

.tab {
  padding: var(--spacing-sm) var(--spacing-lg);
  border: none;
  background: var(--color-bg-elevated);
  color: var(--color-text-secondary);
  border-radius: var(--radius-md);
  cursor: pointer;
  transition: all var(--transition-fast);
}

.tab:hover {
  background: var(--color-bg-card);
}

.tab.active {
  background: var(--color-accent);
  color: white;
}

.table-section {
  padding: var(--spacing-lg);
}

.filter-row {
  display: flex;
  align-items: center;
  gap: var(--spacing-md);
  margin-bottom: var(--spacing-lg);
}

.filter-row select {
  padding: var(--spacing-sm) var(--spacing-md);
  border-radius: var(--radius-md);
  border: 1px solid var(--color-border);
  background: var(--color-bg-elevated);
  color: var(--color-text-primary);
}

.data-table {
  width: 100%;
  border-collapse: collapse;
}

.data-table th,
.data-table td {
  padding: var(--spacing-md);
  text-align: left;
  border-bottom: 1px solid var(--color-border);
}

.data-table th {
  font-size: var(--font-size-xs);
  font-weight: 600;
  text-transform: uppercase;
  letter-spacing: var(--letter-spacing-wide);
  color: var(--color-text-muted);
}

.text-right {
  text-align: right;
}

.asset-cell {
  display: flex;
  flex-direction: column;
  gap: 2px;
}

.asset-symbol {
  font-weight: 600;
  color: var(--color-text-primary);
}

.asset-name {
  font-size: var(--font-size-sm);
  color: var(--color-text-muted);
}

.date-badge {
  padding: 2px 8px;
  background: var(--color-bg-elevated);
  border-radius: var(--radius-sm);
  font-size: var(--font-size-sm);
}

.estimate-badge {
  margin-left: var(--spacing-xs);
  padding: 2px 6px;
  background: rgba(245, 158, 11, 0.2);
  color: #f59e0b;
  border-radius: var(--radius-sm);
  font-size: var(--font-size-xs);
}

.empty-state {
  text-align: center;
  padding: var(--spacing-xl);
  color: var(--color-text-muted);
}

.loader-container {
  display: flex;
  justify-content: center;
  padding: 60px;
}

.spinner {
  width: 30px;
  height: 30px;
  border: 3px solid rgba(255,255,255,0.1);
  border-radius: 50%;
  border-top-color: var(--color-accent);
  animation: spin 1s ease-in-out infinite;
}

@keyframes spin {
  to { transform: rotate(360deg); }
}
</style>

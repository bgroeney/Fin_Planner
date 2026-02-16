<template>
  <div class="portfolio-detail animate-fade-in" v-if="portfolio">
    <!-- Header -->
    <div class="page-header">
      <div class="header-info">
        <div class="title-row">
          <h1>{{ portfolio.name }}</h1>
          <button @click="showSettingsModal = true" class="btn-icon" title="Portfolio Settings">
            <svg xmlns="http://www.w3.org/2000/svg" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><circle cx="12" cy="12" r="3"/><path d="M19.4 15a1.65 1.65 0 0 0 .33 1.82l.06.06a2 2 0 0 1 0 2.83 2 2 0 0 1-2.83 0l-.06-.06a1.65 1.65 0 0 0-1.82-.33 1.65 1.65 0 0 0-1 1.51V21a2 2 0 0 1-2 2 2 2 0 0 1-2-2v-.09A1.65 1.65 0 0 0 9 19.4a1.65 1.65 0 0 0-1.82.33l-.06.06a2 2 0 0 1-2.83 0 2 2 0 0 1 0-2.83l.06-.06a1.65 1.65 0 0 0 .33-1.82 1.65 1.65 0 0 0-1.51-1H3a2 2 0 0 1-2-2 2 2 0 0 1 2-2h.09A1.65 1.65 0 0 0 4.6 9a1.65 1.65 0 0 0-.33-1.82l-.06-.06a2 2 0 0 1 0-2.83 2 2 0 0 1 2.83 0l.06.06a1.65 1.65 0 0 0 1.82.33H9a1.65 1.65 0 0 0 1-1.51V3a2 2 0 0 1 2-2 2 2 0 0 1 2 2v.09a1.65 1.65 0 0 0 1 1.51 1.65 1.65 0 0 0 1.82-.33l.06-.06a2 2 0 0 1 2.83 0 2 2 0 0 1 0 2.83l-.06.06a1.65 1.65 0 0 0-.33 1.82V9a1.65 1.65 0 0 0 1.51 1H21a2 2 0 0 1 2 2 2 2 0 0 1-2 2h-.09a1.65 1.65 0 0 0-1.51 1z"/></svg>
          </button>
          <span v-if="portfolio.benchmarkSymbol" class="benchmark-badge">vs {{ portfolio.benchmarkSymbol }}</span>
        </div>
        <p class="text-muted">{{ accounts.length }} accounts connected</p>
      </div>
      <router-link to="/import" class="btn btn-secondary">Import Data</router-link>
    </div>

    <!-- Account Tabs & Toggle -->
    <div class="controls-bar">
      <div class="tabs">
        <button 
          :class="['tab', { active: !selectedAccountId }]"
          @click="selectedAccountId = null"
        >All Accounts</button>
        <button 
          v-for="acc in accounts" 
          :key="acc.id"
          :class="['tab', { active: selectedAccountId === acc.id }]"
          @click="selectedAccountId = acc.id"
        >{{ acc.accountName || acc.accountNumber }}</button>
      </div>
      <button 
        class="toggle-btn" 
        :class="{ active: showClosedPositions }"
        @click="showClosedPositions = !showClosedPositions"
      >
        <svg v-if="showClosedPositions" xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M22 11.08V12a10 10 0 1 1-5.93-9.14"/><polyline points="22 4 12 14.01 9 11.01"/></svg>
        <svg v-else xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><rect x="3" y="3" width="18" height="18" rx="2" ry="2"/></svg>
        Show Closed
      </button>
    </div>

    <!-- Allocation Chart (Collapsible) -->
    <div class="allocation-section">
      <button @click="allocationExpanded = !allocationExpanded" class="section-header">
        <span class="chevron" :class="{ expanded: allocationExpanded }">
          <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><polyline points="9 18 15 12 9 6"/></svg>
        </span>
        <span>Allocation</span>
        <span class="total-value">{{ formatCurrency(totalPortfolioValue) }}</span>
      </button>
      <transition name="collapse">
        <div v-show="allocationExpanded" class="allocation-chart">
          <AllocationChart :holdings="filteredHoldings" />
        </div>
      </transition>
    </div>

    <!-- Split Layout -->
    <div :class="['split-layout', { 'panel-open': drawerOpen }]">
      <!-- Table -->
      <div class="holdings-table card">
        <table>
          <thead>
            <tr>
              <th class="col-name">Name</th>
              <th class="col-num">Units</th>
              <th class="col-num">Price</th>
              <th class="col-num">Value</th>
              <th class="col-num">Alloc</th>
            </tr>
          </thead>
          <tbody v-for="group in groupedHoldings" :key="group.name">
            <tr class="group-row">
              <td colspan="5">{{ group.name }} ({{ formatCurrency(group.totalValue) }})</td>
            </tr>
            <tr 
              v-for="h in group.holdings" 
              :key="h.id" 
              :class="['holding-row', { selected: selectedAssetId === h.assetId }]"
              @click="openDrawer(h.assetId)"
            >
              <td class="col-name">
                <div class="holding-name">{{ h.name }}</div>
                <div class="holding-symbol">{{ h.symbol }}</div>
              </td>
              <td class="col-num">{{ h.units.toLocaleString('en-AU', { maximumFractionDigits: 4 }) }}</td>
              <td class="col-num">{{ h.units !== 0 ? formatCurrency(h.currentValue / h.units) : '-' }}</td>
              <td class="col-num font-bold">{{ formatCurrency(h.currentValue) }}</td>
              <td class="col-num">{{ totalPortfolioValue ? ((h.currentValue / totalPortfolioValue) * 100).toFixed(1) : '0' }}%</td>
            </tr>
          </tbody>
        </table>
      </div>

      <!-- Side Panel -->
      <div class="side-panel" v-if="drawerOpen">
        <AssetDetailDrawer 
          :isOpen="drawerOpen" 
          :portfolioId="portfolio?.id"
          :assetId="selectedAssetId"
          :embedded="true"
          @close="closeDrawer"
        />
      </div>
    </div>

    <!-- Portfolio Settings Modal -->
    <PortfolioSettingsModal 
      :isOpen="showSettingsModal"
      :portfolio="portfolio"
      @close="showSettingsModal = false"
      @updated="loadData"
      @deleted="handlePortfolioDeleted"
    />

  </div>
  <div v-else class="loading-state">Loading Portfolio...</div>
</template>

<script setup>
import { ref, computed, onMounted, watch } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { usePortfolioStore } from '../stores/portfolio';
import api from '../services/api';
import AllocationChart from '../components/AllocationChart.vue';
import AssetDetailDrawer from '../components/AssetDetailDrawer.vue';
import PortfolioSettingsModal from '../components/PortfolioSettingsModal.vue';
import { formatCurrency } from '../utils/formatters';

const route = useRoute();
const router = useRouter();
const portfolioStore = usePortfolioStore();

const portfolio = ref(null);
const accounts = ref([]);
const holdings = ref([]);
const selectedAccountId = ref(null);
const showClosedPositions = ref(false);
const drawerOpen = ref(false);
const showSettingsModal = ref(false);
const selectedAssetId = ref(null);
const allocationExpanded = ref(true);

const filteredHoldings = computed(() => {
  let result = [];
  if (selectedAccountId.value) {
    result = holdings.value.filter(h => h.accountId === selectedAccountId.value);
  } else {
    const map = new Map();
    for (const h of holdings.value) {
      if (!map.has(h.assetId)) {
        map.set(h.assetId, { ...h, units: 0, currentValue: 0, totalCost: 0 });
      }
      const item = map.get(h.assetId);
      item.units += h.units;
      item.currentValue += h.currentValue;
      item.totalCost += (h.units * h.avgCost);
    }
    result = Array.from(map.values()).map(h => ({
      ...h,
      avgCost: h.units !== 0 ? h.totalCost / h.units : 0
    }));
  }
  if (!showClosedPositions.value) {
    result = result.filter(h => Math.abs(h.units) > 0.0001);
  }
  return result;
});

const totalPortfolioValue = computed(() => {
  return filteredHoldings.value.reduce((sum, h) => sum + h.currentValue, 0);
});

const groupedHoldings = computed(() => {
  const groups = {};
  for (const h of filteredHoldings.value) {
    const catName = h.categoryName || 'Uncategorized';
    if (!groups[catName]) groups[catName] = { name: catName, holdings: [], totalValue: 0 };
    groups[catName].holdings.push(h);
    groups[catName].totalValue += h.currentValue;
  }
  return Object.values(groups).sort((a, b) => b.totalValue - a.totalValue);
});

const openDrawer = (assetId) => {
  selectedAssetId.value = assetId;
  drawerOpen.value = true;
  allocationExpanded.value = false;
};

const closeDrawer = () => {
  drawerOpen.value = false;
  selectedAssetId.value = null;
};

const handlePortfolioDeleted = () => {
    router.push('/');
};

const loadData = async () => {
  const portfolioId = portfolioStore.currentPortfolioId;
  if (!portfolioId) return;

  try {
    const pRes = await api.get(`/portfolios/${portfolioId}`);
    portfolio.value = pRes.data;
    const accRes = await api.get(`/accounts/portfolio/${portfolioId}`);
    accounts.value = accRes.data;
    const hRes = await api.get(`/holdings/portfolio/${portfolioId}`);
    holdings.value = hRes.data;
  } catch (e) {
    console.error(e);
  }
};

// Watch for global portfolio changes
watch(() => portfolioStore.currentPortfolioId, (newId) => {
  if (newId) loadData();
});

onMounted(() => {
  if (portfolioStore.currentPortfolioId) {
    loadData();
  }
});
</script>

<style scoped>
.portfolio-detail {
  max-width: 100%;
}

.loading-state {
  padding: var(--spacing-xl);
  text-align: center;
  color: var(--color-text-muted);
}

/* Header */
.page-header {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  margin-bottom: var(--spacing-lg);
}

.title-row {
  display: flex;
  align-items: center;
  gap: var(--spacing-sm);
  margin-bottom: var(--spacing-xs);
}

.title-row h1 {
  font-size: var(--font-size-2xl);
  margin: 0;
}

.btn-icon {
  background: transparent;
  border: none;
  color: var(--color-text-muted);
  cursor: pointer;
  padding: 4px;
  border-radius: var(--radius-sm);
}

.btn-icon:hover {
  background: var(--color-bg-elevated);
  color: var(--color-text-primary);
}

.benchmark-badge {
  font-size: var(--font-size-xs);
  padding: 4px 10px;
  background: var(--color-bg-elevated);
  color: var(--color-text-secondary);
  border-radius: var(--radius-full);
  font-weight: 500;
}

/* Controls */
.controls-bar {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: var(--spacing-lg);
}

.tabs {
  display: flex;
  gap: 4px;
}

.tab {
  padding: 8px 16px;
  border: none;
  background: transparent;
  cursor: pointer;
  font-size: var(--font-size-sm);
  color: var(--color-text-muted);
  border-bottom: 2px solid transparent;
  transition: all var(--transition-fast);
}

.tab:hover {
  color: var(--color-text-primary);
}

.tab.active {
  color: var(--color-accent);
  border-bottom-color: var(--color-accent);
  font-weight: 500;
}

.toggle-btn {
  display: flex;
  align-items: center;
  gap: var(--spacing-sm);
  padding: 6px 12px;
  border: 1px solid var(--color-border);
  background: var(--color-bg-secondary);
  border-radius: var(--radius-full);
  cursor: pointer;
  font-size: var(--font-size-sm);
  color: var(--color-text-muted);
  transition: all var(--transition-fast);
}

.toggle-btn:hover {
  background: var(--color-bg-elevated);
}

.toggle-btn.active {
  background: var(--color-accent);
  color: white;
  border-color: var(--color-accent);
}

/* Allocation Section */
.allocation-section {
  margin-bottom: var(--spacing-lg);
}

.section-header {
  display: flex;
  align-items: center;
  gap: var(--spacing-sm);
  width: 100%;
  padding: var(--spacing-md);
  background: var(--color-bg-secondary);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-md);
  cursor: pointer;
  font-size: var(--font-size-sm);
  font-weight: 500;
  color: var(--color-text-primary);
}

.section-header:hover {
  background: var(--color-bg-elevated);
}

.chevron {
  display: flex;
  transition: transform 0.2s ease;
}

.chevron.expanded {
  transform: rotate(90deg);
}

.total-value {
  margin-left: auto;
  color: var(--color-text-muted);
}

.allocation-chart {
  margin-top: var(--spacing-md);
}

/* Split Layout */
.split-layout {
  display: grid;
  grid-template-columns: 1fr 0;
  gap: 0;
  transition: all 0.3s ease;
}

.split-layout.panel-open {
  grid-template-columns: 1fr 420px;
  gap: var(--spacing-lg);
}

/* Table */
.holdings-table {
  padding: 0;
  overflow: hidden;
}

table {
  width: 100%;
  border-collapse: collapse;
}

th {
  padding: var(--spacing-sm) var(--spacing-md);
  text-align: left;
  font-size: var(--font-size-xs);
  font-weight: 600;
  text-transform: uppercase;
  letter-spacing: 0.05em;
  color: var(--color-text-muted);
  background: var(--color-bg-elevated);
  border-bottom: 1px solid var(--color-border);
}

.col-num {
  text-align: right;
}

.group-row td {
  padding: var(--spacing-sm) var(--spacing-md);
  font-size: var(--font-size-xs);
  font-weight: 600;
  text-transform: uppercase;
  letter-spacing: 0.05em;
  color: var(--color-text-muted);
  background: var(--color-bg-primary);
}

.holding-row {
  cursor: pointer;
  transition: background var(--transition-fast);
}

.holding-row:hover {
  background: var(--color-bg-elevated);
}

.holding-row.selected {
  background: rgba(30, 64, 175, 0.05);
  border-left: 3px solid var(--color-accent);
}

.holding-row td {
  padding: var(--spacing-sm) var(--spacing-md);
  font-size: var(--font-size-sm);
  border-bottom: 1px solid var(--color-border-subtle);
}

.holding-name {
  font-weight: 500;
  color: var(--color-text-primary);
}

.holding-symbol {
  font-size: var(--font-size-xs);
  color: var(--color-text-muted);
}

.font-bold {
  font-weight: 600;
}

/* Side Panel */
.side-panel {
  height: calc(100vh - 200px);
  position: sticky;
  top: 20px;
  background: var(--color-bg-secondary);
  border-radius: var(--radius-lg);
  border: 1px solid var(--color-border);
  overflow: hidden;
}

/* Transition */
.collapse-enter-active,
.collapse-leave-active {
  transition: all 0.2s ease;
  overflow: hidden;
}

.collapse-enter-from,
.collapse-leave-to {
  opacity: 0;
  max-height: 0;
}

.collapse-enter-to,
.collapse-leave-from {
  max-height: 500px;
}
</style>

<style scoped>
/* Mobile Responsive Styles */
@media (max-width: 900px) {
  .split-layout {
    display: flex;
    flex-direction: column;
  }

  .split-layout.panel-open {
    grid-template-columns: 1fr;
    gap: 0;
  }

  /* Overlay Side Panel on Mobile */
  .side-panel {
    position: fixed;
    top: 0;
    left: 0;
    right: 0;
    bottom: 0;
    width: 100%;
    height: 100%;
    z-index: 500;
    border-radius: 0;
    border: none;
  }

  /* Header adjustments */
  .page-header {
    flex-direction: column;
    align-items: stretch;
    gap: var(--spacing-md);
  }

  .header-info {
    width: 100%;
  }

  .title-row {
    flex-wrap: wrap;
  }

  .controls-bar {
    flex-direction: column;
    align-items: stretch;
    gap: var(--spacing-md);
  }

  .tabs {
    overflow-x: auto;
    padding-bottom: 4px;
    -webkit-overflow-scrolling: touch;
  }

  .toggle-btn {
    justify-content: center;
  }

  /* Allocation Section */
  .allocation-section {
    padding: var(--spacing-sm);
  }

  /* Table */
  .holdings-table {
    overflow-x: auto;
    -webkit-overflow-scrolling: touch;
  }

  table {
    min-width: 600px; /* Force scroll on small screens */
  }
}
</style>

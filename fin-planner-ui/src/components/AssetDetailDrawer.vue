<template>
  <div :class="['asset-drawer', { 'is-overlay': !embedded }]" v-if="isOpen">
    <!-- Backdrop for overlay mode -->
    <div v-if="!embedded" class="drawer-backdrop" @click="$emit('close')"></div>

    <div :class="['drawer-content', { 'overlay-mode': !embedded }]">
      <!-- Loading State -->
      <div v-if="loading" class="drawer-loading">
        <MoneyBoxLoader size="lg" text="Loading Asset Data..." />
      </div>

      <!-- Error State -->
      <div v-else-if="error" class="drawer-error">
        <p>Failed to load asset details</p>
        <button @click="loadData" class="btn btn-secondary">Retry</button>
      </div>

      <!-- Content -->
      <div v-else-if="asset" class="drawer-body">
        <!-- Header -->
        <div class="drawer-header">
          <div class="header-main">
            <div class="asset-symbol">{{ asset.symbol }}</div>
            <div class="header-badges">
                <!-- Converted to button for better a11y and click handling -->
                <button class="category-selector" @click.stop="toggleCategoryDropdown" :class="{ open: showCategoryDropdown }" type="button">
                    <span class="category-badge">{{ getCategoryName(asset.categoryId) }}</span>
                    <svg class="cat-chevron" xmlns="http://www.w3.org/2000/svg" width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><polyline points="6 9 12 15 18 9"/></svg>
                </button>
                
                <div v-if="showCategoryDropdown" class="category-dropdown" @click.stop>
                    <div
                    v-for="cat in categories"
                    :key="cat.id"
                    class="cat-option"
                    :class="{ selected: asset.categoryId === cat.id }"
                    @click="changeCategory(cat.id)"
                    >
                    {{ cat.name }}
                    <svg v-if="asset.categoryId === cat.id" xmlns="http://www.w3.org/2000/svg" width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><polyline points="20 6 9 17 4 12"/></svg>
                    </div>
                    <div v-if="categories.length === 0" class="cat-option disabled">No categories found</div>
                </div>
            </div>
          </div>
          <div class="asset-name">{{ asset.name }}</div>
          <button @click="$emit('close')" class="btn-close" title="Close">
            <svg xmlns="http://www.w3.org/2000/svg" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><line x1="18" y1="6" x2="6" y2="18"/><line x1="6" y1="6" x2="18" y2="18"/></svg>
          </button>
        </div>

        <!-- Compact Value & Actions Row -->
        <div class="value-actions-row">
            <div class="value-group">
                <div class="current-value">{{ formatCurrency(asset.position.currentValue) }}</div>
                <div class="return-badge" :class="{ positive: asset.position.returnPercent >= 0, negative: asset.position.returnPercent < 0 }">
                    {{ asset.position.returnPercent >= 0 ? '+' : '' }}{{ asset.position.returnPercent.toFixed(2) }}%
                </div>
            </div>
            <div class="actions-group">
                <button @click="openDecisionForm('Buy')" class="btn btn-sm btn-success">Buy</button>
                <button @click="openDecisionForm('Sell')" class="btn btn-sm btn-danger">Sell</button>
            </div>
        </div>

        <!-- Chart with Time Selector -->
        <div class="chart-section">
          <div class="chart-controls">
              <button 
                v-for="range in timeRanges" 
                :key="range" 
                @click="timeRange = range"
                :class="['range-btn', { active: timeRange === range }]"
              >
                {{ range }}
              </button>
          </div>
          <!-- Increased height slightly to accommodate axes -->
          <div class="chart-container" v-if="hasChartData">
              <apexchart 
                :key="timeRange"
                type="area" 
                :options="chartOptions" 
                :series="chartSeries" 
                height="180"
              />
          </div>
          <div v-else class="chart-empty">No data for this range</div>
        </div>

        <!-- Position Stats (Compact) -->
        <div class="stats-section">
          <div class="stats-grid">
            <div class="stat-item">
              <span class="stat-label">Units</span>
              <span class="stat-value">{{ asset.position.units.toLocaleString() }}</span>
            </div>
            <div class="stat-item">
              <span class="stat-label">Avg Cost</span>
              <span class="stat-value" :title="formatCurrencyPrecise(Math.abs(asset.position.avgCost))">{{ formatCurrencyPrecise(Math.abs(asset.position.avgCost)) }}</span>
            </div>
            <div class="stat-item">
              <span class="stat-label">Total Cost</span>
              <span class="stat-value">{{ formatCurrency(Math.abs(asset.position.totalCost)) }}</span>
            </div>
            <div class="stat-item">
              <span class="stat-label">P&L</span>
              <span class="stat-value" :class="{ 'text-success': asset.position.profitLoss >= 0, 'text-danger': asset.position.profitLoss < 0 }">
                {{ formatCurrency(asset.position.profitLoss) }}
              </span>
            </div>
          </div>
        </div>

        <!-- Transactions -->
        <div class="transactions-section">
          <h4 class="section-title">History</h4>
          <div class="transactions-list-container">
            <div v-if="loadingTx && transactions.length === 0" class="transactions-loading">
              <div class="spinner-sm"></div> Loading...
            </div>
            <div v-else-if="transactions.length === 0" class="transactions-empty">
              No transactions found
            </div>
            <div v-else class="transaction-items" @scroll="handleScroll">
              <div v-for="tx in transactions" :key="tx.id" class="transaction-item">
                <div class="tx-date">{{ formatDate(tx.date) }}</div>
                <div class="tx-badge-col">
                  <span :class="['tx-badge', tx.type.toLowerCase()]">{{ tx.type }}</span>
                </div>
                <div class="tx-units">{{ tx.units.toLocaleString() }}</div>
                <div class="tx-amount">{{ formatCurrency(Math.abs(tx.cost || tx.value)) }}</div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
    
    <DecisionModal
      v-if="showDecisionForm"
      :isOpen="showDecisionForm"
      :type="decisionType"
      :assetSymbol="asset?.symbol || ''"
      :portfolioId="portfolioId"
      :assetId="assetId"
      @close="showDecisionForm = false"
      @success="onDecisionSuccess"
    />
  </div>
</template>

<script setup>
import { ref, watch, computed, onMounted, onUnmounted } from 'vue';
import api from '../services/api';
import MoneyBoxLoader from './MoneyBoxLoader.vue';
import DecisionModal from './DecisionModal.vue';
import { formatCurrency, formatCurrencyPrecise, formatDate } from '../utils/formatters';

const props = defineProps({
  isOpen: Boolean,
  portfolioId: String,
  assetId: String,
  embedded: { type: Boolean, default: false }
});

const emit = defineEmits(['close', 'updated']);

const loading = ref(false);
const error = ref(false);
const asset = ref(null);
const categories = ref([]);
const showCategoryDropdown = ref(false);
const categorySaving = ref(false);
const transactions = ref([]);
const loadingTx = ref(false);
const page = ref(1);
const hasMore = ref(true);

// Chart Logic
const timeRange = ref('1Y');
const timeRanges = ['1M', '3M', '6M', '1Y', 'ALL'];

const filteredHistory = computed(() => {
  if (!asset.value?.history || asset.value.history.length === 0) return [];
  if (timeRange.value === 'ALL') return asset.value.history;

  const now = new Date();
  let cutoff = new Date();
  
  switch (timeRange.value) {
    case '1M': cutoff.setMonth(now.getMonth() - 1); break;
    case '3M': cutoff.setMonth(now.getMonth() - 3); break;
    case '6M': cutoff.setMonth(now.getMonth() - 6); break;
    case '1Y': cutoff.setFullYear(now.getFullYear() - 1); break;
  }
  
  const filtered = asset.value.history
    .filter(h => new Date(h.date) >= cutoff)
    .sort((a, b) => new Date(a.date) - new Date(b.date));
    
  return filtered;
});

const hasChartData = computed(() => filteredHistory.value.length > 0);

const chartSeries = computed(() => [{
  name: 'Price', // Changed from Value to Price for clarity
  data: filteredHistory.value.map(h => ({ x: new Date(h.date).getTime(), y: h.price })) // Use Price instead of Value
}]);

const chartOptions = computed(() => ({
  chart: { 
      type: 'area', 
      toolbar: { show: false }, 
      sparkline: { enabled: false }, 
      fontFamily: 'inherit',
      animations: { enabled: false }
  },
  dataLabels: { enabled: false }, // Explicitly disable data labels
  stroke: { curve: 'smooth', width: 2, colors: ['var(--color-accent)'] },
  fill: { type: 'gradient', gradient: { shadeIntensity: 1, opacityFrom: 0.3, opacityTo: 0.05, stops: [0, 100] } },
  tooltip: { 
      x: { format: 'dd MMM yyyy' }, 
      y: { formatter: (val) => formatCurrencyPrecise(val) }, // 2 decimals for price
      theme: 'dark'
  },
  xaxis: { 
      type: 'datetime', 
      tooltip: { enabled: false },
      labels: { 
          show: true,
          style: { fontSize: '10px', colors: '#64748b' }, 
          datetimeFormatter: { year: 'yyyy', month: "MMM 'yy", day: 'dd MMM' },
      },
      axisBorder: { show: false },
      axisTicks: { show: false }
  },
  yaxis: {
      show: true,
      labels: { 
          show: true,
          // Use formatted currency (2 decimals) for unit price
          formatter: (val) => formatCurrencyPrecise(val), 
          style: { fontSize: '10px', colors: '#64748b' }
      }
  },
  grid: {
      show: true,
      borderColor: 'var(--color-border-subtle)',
      strokeDashArray: 4,
      padding: { top: 0, right: 10, bottom: 0, left: 10 }
  },
  colors: ['var(--color-accent)']
}));

const getCategoryName = (id) => {
  const c = categories.value.find(x => x.id === id);
  return c ? c.name : 'Uncategorized';
};

const loadCategories = async () => {
  if (!props.portfolioId) return;
  try {
    const res = await api.get(`/portfolios/${props.portfolioId}`);
    categories.value = res.data.targetAllocation?.map(t => ({ id: t.id, name: t.name })) || [];
  } catch (e) {
      console.error('Failed to load categories', e);
  }
};

const loadData = async () => {
  if (!props.portfolioId || !props.assetId) return;
  loading.value = true;
  error.value = false;
  transactions.value = [];
  page.value = 1;
  hasMore.value = true;
  
  try {
    const res = await api.get(`/portfolios/${props.portfolioId}/assets/${props.assetId}/details`);
    asset.value = res.data;
    loading.value = false;
    // Load categories if not already loaded (or refresh them)
    if (categories.value.length === 0) await loadCategories();
    
    await loadTransactions();
  } catch (e) {
    console.error(e);
    error.value = true;
    loading.value = false;
  }
};

const loadTransactions = async () => {
  if (!props.portfolioId || !props.assetId || loadingTx.value || !hasMore.value) return;
  loadingTx.value = true;
  try {
    const res = await api.get(`/portfolios/${props.portfolioId}/assets/${props.assetId}/transactions?page=${page.value}&pageSize=20`);
    if (res.data.length < 20) hasMore.value = false;
    transactions.value.push(...res.data);
    page.value++;
  } catch (e) {
    console.error('Failed to load transactions', e);
  } finally {
    loadingTx.value = false;
  }
};

const handleScroll = (e) => {
  const { scrollTop, clientHeight, scrollHeight } = e.target;
  if (scrollHeight - scrollTop <= clientHeight + 50) {
    loadTransactions();
  }
};

watch(() => props.isOpen, (val) => {
  if (val) {
    loadData();
    showCategoryDropdown.value = false;
  }
}, { immediate: true });

watch(() => props.assetId, (val) => {
  if (props.isOpen && val) loadData();
});

// Decision Logic
const showDecisionForm = ref(false);
const decisionType = ref('Buy');

const openDecisionForm = (type) => {
  decisionType.value = type;
  showDecisionForm.value = true;
};

const onDecisionSuccess = () => {
    loadData();
    emit('updated');
};

const toggleCategoryDropdown = () => {
  // Always verify we have categories loaded when clicking
  if (categories.value.length === 0) loadCategories();
  showCategoryDropdown.value = !showCategoryDropdown.value;
};

const changeCategory = async (categoryId) => {
  if (!props.portfolioId || !props.assetId || categoryId === asset.value.categoryId) {
    showCategoryDropdown.value = false;
    return;
  }
  categorySaving.value = true;
  try {
    await api.put(`/portfolios/${props.portfolioId}/assets/${props.assetId}/category`, { categoryId });
    asset.value.categoryId = categoryId;
    showCategoryDropdown.value = false;
    emit('updated'); // Signal that asset details (category) changed
  } catch (e) {
    console.error('Failed to update category', e);
  } finally {
    categorySaving.value = false;
  }
};

// Global click listener for closing dropdown
const closeDropdownOnClickOutside = (e) => {
    if (showCategoryDropdown.value) {
        // Check if click target is outside the dropdown logic
        // We use @click.stop on the toggle button and dropdown itself, 
        // so any click reaching here that isn't those elements should close it.
        const el = document.querySelector('.category-selector');
        const dropdown = document.querySelector('.category-dropdown');
        if (el && !el.contains(e.target) && dropdown && !dropdown.contains(e.target)) {
            showCategoryDropdown.value = false;
        }
    }
};

onMounted(() => {
    document.addEventListener('click', closeDropdownOnClickOutside);
});

onUnmounted(() => {
    document.removeEventListener('click', closeDropdownOnClickOutside);
});
</script>

<style scoped>
.asset-drawer { height: 100%; }
.is-overlay { position: fixed; inset: 0; z-index: 200; }
.drawer-backdrop { position: absolute; inset: 0; background: rgba(0, 0, 0, 0.3); backdrop-filter: blur(1px); transition: opacity 0.3s ease; }
.drawer-content {
  height: 100%;
  background: var(--color-bg-secondary);
  display: flex;
  flex-direction: column;
  overflow: hidden;
  box-shadow: var(--shadow-2xl);
  transition: transform 0.3s cubic-bezier(0.16, 1, 0.3, 1);
}
.overlay-mode { position: absolute; right: 0; top: 0; bottom: 0; width: 420px; max-width: 95vw; }

.drawer-loading, .drawer-error {
  display: flex; flex-direction: column; align-items: center; justify-content: center; height: 100%; gap: var(--spacing-md); color: var(--color-text-muted);
}
.drawer-body { display: flex; flex-direction: column; height: 100%; overflow: hidden; }

/* Header */
.drawer-header {
  padding: 16px 20px 4px;
  background: var(--color-bg-primary);
  display: flex; justify-content: space-between; align-items: flex-start;
  flex-shrink: 0;
  overflow: visible; /* Allowing dropdown to overflow header */
  z-index: 10; 
  position: relative;
}
.header-main { display: flex; flex-direction: column; gap: 4px; }
.asset-symbol { font-size: 20px; font-weight: 700; color: var(--color-text-primary); line-height: 1.1; }
.asset-name { font-size: 13px; color: var(--color-text-secondary); line-height: 1.3; } 
.btn-close {
  background: transparent; border: none; color: var(--color-text-muted); padding: 4px; margin: -4px;
  cursor: pointer; border-radius: var(--radius-full); transition: all 0.2s;
}
.btn-close:hover { background: var(--color-bg-tertiary); color: var(--color-text-primary); }

/* Category Selector */
.category-selector { 
    display: inline-flex; align-items: center; gap: 4px; position: relative; cursor: pointer; 
    background: transparent; border: none; padding: 0; /* Reset button styles */
}
.category-badge { 
    font-size: 11px; padding: 2px 8px; background: var(--color-bg-elevated); 
    color: var(--color-text-secondary); border-radius: 99px; transition: all 0.2s; font-weight: 500; 
}
.category-selector:hover .category-badge { background: var(--color-bg-tertiary); color: var(--color-text-primary); }
.header-badges { display: flex; align-items: center; margin-top: 2px; }

/* Dropdown */
.category-dropdown {
  position: absolute; top: 100%; left: 0; margin-top: 4px; min-width: 220px;
  background: var(--color-bg-primary); border: 1px solid var(--color-border); border-radius: 8px;
  box-shadow: var(--shadow-xl); z-index: 100; padding: 4px; max-height: 200px; overflow-y: auto;
}
.cat-option { padding: 8px 12px; font-size: 13px; border-radius: 4px; cursor: pointer; display: flex; justify-content: space-between; align-items: center; color: var(--color-text-primary); }
.cat-option:hover { background: var(--color-bg-tertiary); }
.cat-option.selected { color: var(--color-accent); font-weight: 600; background: var(--color-bg-accent-subtle); }
.cat-option.disabled { font-style: italic; color: var(--color-text-muted); cursor: default; }

/* Value & Actions Row */
.value-actions-row {
  display: flex; justify-content: space-between; align-items: flex-end;
  padding: 4px 20px 12px;
  background: var(--color-bg-primary);
  border-bottom: 1px solid var(--color-border-subtle);
  flex-shrink: 0;
  z-index: 1; /* Lower than header */
}
.value-group { display: flex; align-items: baseline; gap: 8px; }
.current-value { font-size: 24px; font-weight: 700; color: var(--color-text-primary); letter-spacing: -0.02em; }
.return-badge { font-size: 12px; font-weight: 600; padding: 2px 6px; border-radius: 4px; }
.return-badge.positive { color: var(--color-success); background: rgba(var(--color-success-rgb), 0.1); }
.return-badge.negative { color: var(--color-danger); background: rgba(var(--color-danger-rgb), 0.1); }

.actions-group { display: flex; gap: 8px; }
.btn-sm { padding: 6px 12px; font-size: 12px; border-radius: 6px; font-weight: 600; cursor: pointer; border: none; transition: transform 0.1s; }
.btn-sm:hover { transform: translateY(-1px); }
.btn-success { background: var(--color-success); color: white; }
.btn-danger { background: var(--color-danger); color: white; }

/* Chart Section */
.chart-section {
  padding: 12px 20px 4px;
  background: var(--color-bg-primary);
  border-bottom: 1px solid var(--color-border-subtle);
  flex-shrink: 0;
}
.chart-controls { display: flex; justify-content: flex-end; gap: 4px; margin-bottom: 4px; }
.range-btn {
  background: transparent; border: none; font-size: 10px; font-weight: 600; color: var(--color-text-muted);
  cursor: pointer; padding: 2px 6px; border-radius: 4px; transition: all 0.2s;
}
.range-btn:hover { color: var(--color-text-primary); background: var(--color-bg-elevated); }
.range-btn.active { color: var(--color-accent); background: var(--color-bg-accent-subtle); }
.chart-empty { font-size: 12px; color: var(--color-text-muted); text-align: center; padding: 20px; }

/* Stats (Compact) */
.stats-section { padding: 12px 20px; background: var(--color-bg-secondary); border-bottom: 1px solid var(--color-border-subtle); flex-shrink: 0; }
.stats-grid { display: grid; grid-template-columns: repeat(4, 1fr); gap: 12px; }
.stat-item { display: flex; flex-direction: column; gap: 2px; }
.stat-label { font-size: 10px; text-transform: uppercase; color: var(--color-text-muted); font-weight: 600; }
.stat-value { font-size: 13px; font-weight: 600; color: var(--color-text-primary); white-space: nowrap; overflow: hidden; text-overflow: ellipsis; }
.text-success { color: var(--color-success); }
.text-danger { color: var(--color-danger); }

/* Transactions */
.transactions-section {
  flex: 1; min-height: 0;
  display: flex; flex-direction: column;
  background: var(--color-bg-secondary);
  padding: 12px 20px 0;
}
.section-title { font-size: 11px; font-weight: 700; text-transform: uppercase; letter-spacing: 0.05em; color: var(--color-text-muted); margin-bottom: 8px; flex-shrink: 0; }

.transactions-list-container {
  flex: 1; min-height: 0;
  position: relative;
  display: flex; flex-direction: column;
}

.transaction-items {
  flex: 1;
  overflow-y: auto;
  padding-bottom: 20px;
  scrollbar-width: thin;
  scrollbar-color: var(--color-border) transparent;
}
.transaction-items::-webkit-scrollbar { width: 4px; }
.transaction-items::-webkit-scrollbar-thumb { background-color: var(--color-border); border-radius: 4px; }

.transaction-item {
  display: grid; grid-template-columns: 80px auto 1fr auto; align-items: center; gap: 8px;
  padding: 8px 0; border-bottom: 1px solid var(--color-border-subtle); font-size: 12px;
}
.tx-date { color: var(--color-text-secondary); }
.tx-badge { display: inline-block; padding: 1px 6px; border-radius: 3px; font-size: 10px; font-weight: 700; text-transform: uppercase; }
.tx-badge.buy { color: var(--color-success); background: rgba(var(--color-success-rgb), 0.1); }
.tx-badge.sell { color: var(--color-danger); background: rgba(var(--color-danger-rgb), 0.1); }
.tx-badge.dividend { color: var(--color-info); background: rgba(var(--color-info-rgb), 0.1); }
.tx-units { text-align: right; color: var(--color-text-secondary); }
.tx-amount { text-align: right; font-weight: 600; color: var(--color-text-primary); min-width: 60px; }

/* Loading/Empty */
.transactions-loading, .transactions-empty { text-align: center; padding: 20px; font-size: 12px; color: var(--color-text-muted); }
.spinner-sm { display: inline-block; width: 12px; height: 12px; border: 2px solid var(--color-border); border-top-color: var(--color-accent); border-radius: 50%; animation: spin 0.8s linear infinite; margin-right: 6px; }
</style>

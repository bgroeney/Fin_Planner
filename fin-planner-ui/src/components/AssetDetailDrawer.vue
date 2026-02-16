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
          <div class="asset-info">
            <div class="asset-symbol">{{ asset.symbol }}</div>
            <div class="asset-name">{{ asset.name }}</div>
            <div class="asset-category">
              <div class="category-selector" @click.stop="toggleCategoryDropdown" :class="{ open: showCategoryDropdown }">
                <span class="category-badge interactive">{{ getCategoryName(asset.categoryId) }}</span>
                <svg class="cat-chevron" xmlns="http://www.w3.org/2000/svg" width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><polyline points="6 9 12 15 18 9"/></svg>
              </div>
              <div v-if="showCategoryDropdown" class="category-dropdown">
                <div
                  v-for="cat in categories"
                  :key="cat.id"
                  class="cat-option"
                  :class="{ selected: asset.categoryId === cat.id }"
                  @click.stop="changeCategory(cat.id)"
                >
                  {{ cat.name }}
                  <svg v-if="asset.categoryId === cat.id" xmlns="http://www.w3.org/2000/svg" width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><polyline points="20 6 9 17 4 12"/></svg>
                </div>
                <div v-if="categories.length === 0" class="cat-option disabled">No categories defined</div>
              </div>
              <span v-if="categorySaving" class="cat-saving">Saving...</span>
            </div>
          </div>
          <button @click="$emit('close')" class="btn-close" title="Close">
            <svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><line x1="18" y1="6" x2="6" y2="18"/><line x1="6" y1="6" x2="18" y2="18"/></svg>
          </button>
        </div>

        <!-- Value Display -->
        <div class="value-section">
          <div class="current-value">{{ formatCurrency(asset.position.currentValue) }}</div>
          <div class="return-badge" :class="{ positive: asset.position.returnPercent >= 0, negative: asset.position.returnPercent < 0 }">
            {{ asset.position.returnPercent >= 0 ? '+' : '' }}{{ asset.position.returnPercent.toFixed(2) }}%
          </div>
        </div>

        <!-- Chart -->
        <div class="chart-section">
          <apexchart 
            type="area" 
            :options="chartOptions" 
            :series="chartSeries" 
            height="130"
          />
        </div>

        <!-- Decision Actions -->
        <div class="actions-section">
          <div v-if="!showDecisionForm" class="action-buttons">
            <button @click="openDecisionForm('Buy')" class="btn btn-success flex-1">Buy</button>
            <button @click="openDecisionForm('Sell')" class="btn btn-danger flex-1">Sell</button>
          </div>
          
          <div v-else class="decision-form card">
            <h4 class="form-title">{{ decisionType }} {{ asset.symbol }}</h4>
            
            <div class="form-group">
              <label>Units</label>
              <input v-model.number="decisionUnits" type="number" class="form-input" placeholder="0" />
            </div>

            <div v-if="decisionType === 'Sell'" class="form-group">
              <label>Tax Optimization Method</label>
              <select v-model="allocationMethod" class="form-select">
                <option value="FIFO">First-In-First-Out (Standard)</option>
                <option value="MinTax">Minimize Tax (High Cost/Long Term)</option>
                <option value="MaxGain">Maximize Gain (Low Cost)</option>
              </select>
              <div class="tax-hint" v-if="projectedTax !== null">
                Projected Taxable Gain: {{ formatCurrency(projectedTax) }}
              </div>
            </div>

            <div class="form-group">
              <label>Rationale</label>
              <textarea v-model="decisionRationale" class="form-input" rows="2" placeholder="Why make this trade?"></textarea>
            </div>

            <div class="form-actions">
              <button @click="showDecisionForm = false" class="btn btn-secondary">Cancel</button>
              <button @click="submitDecision" class="btn btn-primary" :disabled="submitting">
                {{ submitting ? 'Saving...' : 'Create Decision' }}
              </button>
            </div>
          </div>
        </div>

        <!-- Position Stats -->
        <div class="stats-section">
          <h4 class="section-title">Position Details</h4>
          <div class="stats-grid">
            <div class="stat-item">
              <span class="stat-label">Units</span>
              <span class="stat-value">{{ asset.position.units.toLocaleString() }}</span>
            </div>
            <div class="stat-item">
              <span class="stat-label">Avg Cost</span>
              <span class="stat-value">{{ formatCurrency(Math.abs(asset.position.avgCost)) }}</span>
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
          <h4 class="section-title">Transaction History</h4>
          <div class="transactions-list">
            <div v-if="loadingTx && transactions.length === 0" class="transactions-loading">
              <div class="spinner-sm"></div>
              Loading transactions...
            </div>
            <div v-else-if="transactions.length === 0" class="transactions-empty">
              No transactions found
            </div>
            <div v-else class="transaction-items" @scroll="handleScroll">
              <div v-for="tx in transactions" :key="tx.id" class="transaction-item">
                <div class="tx-date">{{ formatDate(tx.date) }}</div>
                <div class="tx-type">
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
  </div>
</template>

<script setup>
import { ref, watch, computed } from 'vue';
import api from '../services/api';
import MoneyBoxLoader from './MoneyBoxLoader.vue';
import { formatCurrency, formatDate } from '../utils/formatters';

const props = defineProps({
  isOpen: Boolean,
  portfolioId: String,
  assetId: String,
  embedded: { type: Boolean, default: false }
});

const emit = defineEmits(['close']);

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

const getCategoryName = (id) => {
  const c = categories.value.find(x => x.id === id);
  return c ? c.name : 'Uncategorized';
};

const loadCategories = async () => {
  if (!props.portfolioId) return;
  try {
    const res = await api.get(`/portfolios/${props.portfolioId}`);
    categories.value = res.data.targetAllocation?.map(t => ({ id: t.id, name: t.name })) || [];
  } catch (e) { console.error(e); }
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
    loadCategories();
    showCategoryDropdown.value = false;
  }
}, { immediate: true });

watch(() => props.assetId, (val) => {
  if (props.isOpen && val) loadData();
});

// Chart
const chartSeries = computed(() => [{
  name: 'Value',
  data: asset.value?.history?.map(h => ({ x: new Date(h.date).getTime(), y: h.value })) || []
}]);

// Decision Logic
const showDecisionForm = ref(false);
const decisionType = ref('Buy');
const decisionUnits = ref(0);
const decisionRationale = ref('');
const allocationMethod = ref('FIFO');
const projectedTax = ref(null);
const submitting = ref(false);

const openDecisionForm = (type) => {
  decisionType.value = type;
  showDecisionForm.value = true;
  decisionUnits.value = 0;
  decisionRationale.value = '';
  allocationMethod.value = 'FIFO';
  projectedTax.value = null;
};

// Start watching for tax calculation needs
watch([decisionUnits, allocationMethod], async () => {
    if (decisionType.value === 'Sell' && decisionUnits.value > 0) {
        // Debounce or call API to estimate tax
        // This requires a new endpoint in ReportsController or DecisionsController to exposing CalculateTaxImpact
        // For now, we'll leave it as a placeholder or implementing that endpoint next.
    }
});

const submitDecision = async () => {
  if (!props.portfolioId || !decisionUnits.value) return;
  submitting.value = true;
  try {
    await api.post('/decisions', {
      portfolioId: props.portfolioId,
      title: `${decisionType.value} ${decisionUnits.value} ${asset.value.symbol}`,
      rationale: decisionRationale.value || `${decisionType.value} decision for ${asset.value.symbol}`,
      allocationMethod: decisionType.value === 'Sell' ? allocationMethod.value : null,
      saveAsDraft: false
    });
    showDecisionForm.value = false;
    // Refresh
    loadData();
    emit('updated'); 
  } catch (e) {
    console.error(e);
    alert('Failed to create decision');
  } finally {
    submitting.value = false;
  }
};

const chartOptions = computed(() => ({
  chart: { type: 'area', toolbar: { show: false }, sparkline: { enabled: false }, fontFamily: 'inherit' },
  dataLabels: { enabled: false },
  stroke: { curve: 'smooth', width: 2, colors: ['var(--color-accent)'] },
  xaxis: { type: 'datetime', labels: { show: false }, axisBorder: { show: false }, axisTicks: { show: false } },
  yaxis: { show: false },
  grid: { show: false },
  fill: { type: 'gradient', gradient: { shadeIntensity: 1, opacityFrom: 0.3, opacityTo: 0.05, stops: [0, 100] } },
  tooltip: { x: { format: 'dd MMM yyyy' }, y: { formatter: (val) => formatCurrency(val) } },
  colors: ['var(--color-accent)']
}));

const toggleCategoryDropdown = () => {
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
  } catch (e) {
    console.error('Failed to update category', e);
  } finally {
    categorySaving.value = false;
  }
};
</script>

<style scoped>
.asset-drawer {
  height: 100%;
}

.is-overlay {
  position: fixed;
  inset: 0;
  z-index: 200;
}

.drawer-backdrop {
  position: absolute;
  inset: 0;
  background: rgba(0, 0, 0, 0.3);
}

.drawer-content {
  height: 100%;
  background: var(--color-bg-secondary);
  display: flex;
  flex-direction: column;
  overflow: hidden;
}

.overlay-mode {
  position: absolute;
  right: 0;
  top: 0;
  bottom: 0;
  width: 600px;
  max-width: 95vw;
  box-shadow: var(--shadow-xl);
}

.drawer-loading,
.drawer-error {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  height: 100%;
  gap: var(--spacing-md);
  color: var(--color-text-muted);
}

.drawer-body {
  display: flex;
  flex-direction: column;
  height: 100%;
  overflow: hidden;
}

/* Header */
.drawer-header {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  padding: var(--spacing-md) var(--spacing-lg);
  border-bottom: 1px solid var(--color-border);
}

.asset-symbol {
  font-size: var(--font-size-lg);
  font-weight: 700;
  color: var(--color-text-primary);
}

.asset-name {
  font-size: var(--font-size-sm);
  color: var(--color-text-muted);
  margin-bottom: var(--spacing-xs);
}

.category-badge {
  display: inline-block;
  padding: 2px 8px;
  background: var(--color-bg-elevated);
  color: var(--color-text-secondary);
  font-size: var(--font-size-xs);
  border-radius: var(--radius-full);
}

.category-badge.interactive {
  cursor: pointer;
  transition: all 0.15s ease;
}

.category-badge.interactive:hover {
  background: var(--color-accent);
  color: white;
}

.category-selector {
  display: inline-flex;
  align-items: center;
  gap: 4px;
  cursor: pointer;
  position: relative;
}

.cat-chevron {
  transition: transform 0.15s ease;
  color: var(--color-text-muted);
}

.category-selector.open .cat-chevron {
  transform: rotate(180deg);
}

.category-dropdown {
  position: absolute;
  top: calc(100% + 4px);
  left: 0;
  min-width: 200px;
  background: var(--color-bg-primary, white);
  border: 1px solid var(--color-border);
  border-radius: 8px;
  box-shadow: 0 4px 16px rgba(0,0,0,0.12);
  z-index: 50;
  padding: 4px;
  max-height: 200px;
  overflow-y: auto;
}

.cat-option {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 8px 10px;
  border-radius: 6px;
  font-size: var(--font-size-sm);
  cursor: pointer;
  color: var(--color-text-primary);
  transition: background 0.1s;
}

.cat-option:hover { background: var(--color-bg-elevated); }
.cat-option.selected { font-weight: 600; color: var(--color-accent); }
.cat-option.disabled { color: var(--color-text-muted); cursor: default; font-style: italic; }
.cat-option.disabled:hover { background: transparent; }

.cat-saving {
  font-size: var(--font-size-xs);
  color: var(--color-text-muted);
  margin-left: 6px;
  animation: pulse 1s infinite;
}

@keyframes pulse {
  0%, 100% { opacity: 1; }
  50% { opacity: 0.4; }
}

.btn-close {
  background: transparent;
  border: none;
  color: var(--color-text-muted);
  cursor: pointer;
  padding: 4px;
  border-radius: var(--radius-sm);
  transition: all var(--transition-fast);
}

.btn-close:hover {
  background: var(--color-bg-elevated);
  color: var(--color-text-primary);
}

/* Value Section */
.value-section {
  display: flex;
  align-items: baseline;
  gap: var(--spacing-md);
  padding: var(--spacing-sm) var(--spacing-lg);
}

.current-value {
  font-size: var(--font-size-2xl);
  font-weight: 700;
  color: var(--color-text-primary);
}

.return-badge {
  font-size: var(--font-size-sm);
  font-weight: 600;
  padding: 4px 10px;
  border-radius: var(--radius-full);
}

.return-badge.positive {
  background: rgba(5, 150, 105, 0.1);
  color: var(--color-success);
}

.return-badge.negative {
  background: rgba(220, 38, 38, 0.1);
  color: var(--color-danger);
}

/* Chart */
.chart-section {
  padding: 0 var(--spacing-lg);
}

/* Actions */
.actions-section {
  padding: 0 var(--spacing-lg) var(--spacing-md);
}

.action-buttons {
  display: flex;
  gap: var(--spacing-md);
}

.flex-1 { flex: 1; }

.btn {
  padding: 8px 16px;
  border-radius: var(--radius-md);
  font-weight: 500;
  cursor: pointer;
  border: none;
  font-size: var(--font-size-sm);
  transition: all 0.2s;
}

.btn-success { background: var(--color-success); color: white; }
.btn-success:hover { background: #047857; }

.btn-danger { background: var(--color-danger); color: white; }
.btn-danger:hover { background: #b91c1c; }

.decision-form {
  padding: var(--spacing-md);
  background: var(--color-bg-elevated);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-md);
}

.form-title {
  margin: 0 0 var(--spacing-md);
  font-size: var(--font-size-sm);
  text-transform: uppercase;
  color: var(--color-text-muted);
}

.form-group {
  margin-bottom: var(--spacing-sm);
}

.form-group label {
  display: block;
  font-size: var(--font-size-xs);
  color: var(--color-text-secondary);
  margin-bottom: 4px;
}

.form-input, .form-select {
  width: 100%;
  padding: 8px;
  border: 1px solid var(--color-border);
  border-radius: var(--radius-sm);
  background: var(--color-bg-primary);
  color: var(--color-text-primary);
}

.form-actions {
  display: flex;
  justify-content: flex-end;
  gap: var(--spacing-sm);
  margin-top: var(--spacing-md);
}

.tax-hint {
  font-size: var(--font-size-xs);
  color: var(--color-info);
  margin-top: 4px;
}

/* Stats */
.stats-section {
  padding: var(--spacing-md) var(--spacing-lg);
}

.section-title {
  font-size: var(--font-size-xs);
  font-weight: 600;
  text-transform: uppercase;
  letter-spacing: 0.05em;
  color: var(--color-text-muted);
  margin-bottom: var(--spacing-md);
}

.stats-grid {
  display: grid;
  grid-template-columns: repeat(4, 1fr);
  gap: var(--spacing-sm);
}

.stat-item {
  display: flex;
  justify-content: space-between;
  padding: var(--spacing-sm) var(--spacing-md);
  background: var(--color-bg-elevated);
  border-radius: var(--radius-md);
}

.stat-label {
  font-size: var(--font-size-sm);
  color: var(--color-text-muted);
}

.stat-value {
  font-size: var(--font-size-sm);
  font-weight: 600;
  color: var(--color-text-primary);
}

.text-success { color: var(--color-success); }
.text-danger { color: var(--color-danger); }

/* Transactions */
.transactions-section {
  flex: 1;
  display: flex;
  flex-direction: column;
  padding: 0 var(--spacing-lg) var(--spacing-lg);
  overflow: hidden;
}

.transactions-list {
  flex: 1;
  overflow: hidden;
}

.transactions-loading,
.transactions-empty {
  text-align: center;
  padding: var(--spacing-xl);
  color: var(--color-text-muted);
  font-size: var(--font-size-sm);
}

.transaction-items {
  height: 100%;
  overflow-y: auto;
  /* Visual cue for more records - mask fade at bottom */
  mask-image: linear-gradient(to bottom, black 85%, transparent 100%);
  -webkit-mask-image: linear-gradient(to bottom, black 85%, transparent 100%);
}

.transaction-item {
  display: grid;
  grid-template-columns: 1fr auto auto auto;
  gap: var(--spacing-md);
  padding: var(--spacing-sm) 0;
  border-bottom: 1px solid var(--color-border-subtle);
  font-size: var(--font-size-sm);
  align-items: center;
}

.tx-date {
  color: var(--color-text-secondary);
}

.tx-badge {
  display: inline-block;
  padding: 2px 6px;
  border-radius: var(--radius-sm);
  font-size: var(--font-size-xs);
  font-weight: 500;
  text-transform: uppercase;
}

.tx-badge.buy { background: rgba(5, 150, 105, 0.1); color: var(--color-success); }
.tx-badge.sell { background: rgba(220, 38, 38, 0.1); color: var(--color-danger); }
.tx-badge.dividend { background: rgba(2, 132, 199, 0.1); color: var(--color-info); }

.tx-units {
  text-align: right;
  color: var(--color-text-secondary);
}

.tx-amount {
  text-align: right;
  font-weight: 500;
  color: var(--color-text-primary);
  min-width: 80px;
}

/* Spinner */
.spinner {
  width: 24px;
  height: 24px;
  border: 2px solid var(--color-border);
  border-top-color: var(--color-accent);
  border-radius: 50%;
  animation: spin 0.8s linear infinite;
}

.spinner-sm {
  width: 16px;
  height: 16px;
  border: 2px solid var(--color-border);
  border-top-color: var(--color-accent);
  border-radius: 50%;
  animation: spin 0.8s linear infinite;
  display: inline-block;
  margin-right: var(--spacing-sm);
}

@keyframes spin {
  to { transform: rotate(360deg); }
}
</style>

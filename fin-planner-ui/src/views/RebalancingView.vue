<template>
  <div class="rebalancing-view animate-fade-in content-container">
    <div class="header-row mb-xl">
      <div class="header-content">
        <h1>Rebalancing Wizard</h1>
        <p class="subtitle">Align your portfolio with your target asset allocation</p>
      </div>
      <div class="portfolio-selector">
        <select v-model="selectedPortfolioId" @change="loadReport" class="form-select">
          <option v-for="p in portfolios" :key="p.id" :value="p.id">{{ p.name }}</option>
        </select>
      </div>
    </div>

    <div v-if="loading" class="loader-container">
      <div class="spinner"></div>
      <p>Analyzing portfolio allocations...</p>
    </div>

    <template v-else-if="report">
      <!-- Portfolio Health Summary -->
      <div class="health-summary card mb-xl">
        <div class="health-item">
          <span class="label">Total Value</span>
          <span class="value">{{ formatCurrency(report.totalValue) }}</span>
        </div>
        <div class="health-divider"></div>
        <div class="health-item">
          <span class="label">Allocation Drift</span>
          <span :class="['value', driftClass]">{{ totalDrift.toFixed(1) }}%</span>
        </div>
        <div class="health-divider"></div>
        <div class="health-item">
          <span class="label">Status</span>
          <span :class="['badge', statusClass]">{{ statusText }}</span>
        </div>
      </div>

      <!-- Categories Table -->
      <div class="card mb-xl overflow-hidden">
        <table class="data-table">
          <thead>
            <tr>
              <th>Category</th>
              <th class="text-right">Current</th>
              <th class="text-right">Target</th>
              <th class="text-right">Drift</th>
              <th class="text-right">Action</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="cat in report.categories" :key="cat.categoryId">
              <td>
                <div class="cat-name">{{ cat.categoryName }}</div>
                <div class="cat-value">{{ formatCurrency(cat.currentValue) }}</div>
              </td>
              <td class="text-right">
                <div class="percentage-bar-mini">
                  <div class="bar-fill" :style="{ width: cat.currentPercentage + '%' }"></div>
                </div>
                {{ cat.currentPercentage.toFixed(1) }}%
              </td>
              <td class="text-right">{{ cat.targetPercentage.toFixed(1) }}%</td>
              <td :class="['text-right', getDriftClass(cat.currentPercentage - cat.targetPercentage)]">
                {{ (cat.currentPercentage - cat.targetPercentage).toFixed(1) }}%
              </td>
              <td class="text-right">
                <span :class="['action-tag', cat.recommendation.toLowerCase()]">
                  {{ cat.recommendation }}
                </span>
              </td>
            </tr>
          </tbody>
        </table>
      </div>

      <!-- Recommendations Section -->
      <div class="recommendations-section">
        <div class="section-header mb-md">
          <h2>Execution Plan</h2>
          <p class="subtitle">Select the trades you want to execute to realign your portfolio.</p>
        </div>

        <div v-if="proposedActions.length === 0" class="empty-state card">
          <div class="icon">
            <svg xmlns="http://www.w3.org/2000/svg" width="48" height="48" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round"><path d="M22 11.08V12a10 10 0 1 1-5.93-9.14"/><polyline points="22 4 12 14.01 9 11.01"/></svg>
          </div>
          <h3>Portfolio is Balanced</h3>
          <p>Your current allocations are within tolerance of your targets.</p>
        </div>

        <div v-else class="actions-list">
          <div v-for="(action, index) in proposedActions" :key="index" class="action-card card">
            <div class="action-checkbox">
              <input type="checkbox" v-model="action.selected" />
            </div>
            <div class="action-details">
              <div class="action-type" :class="action.type.toLowerCase()">{{ action.type }}</div>
              <div class="asset-info">
                <span class="symbol">{{ action.symbol }}</span>
                <span class="name">{{ action.assetName }}</span>
              </div>
            </div>
            <div class="action-math">
              <div class="units">{{ action.units.toFixed(4) }} units</div>
              <div class="price">@ {{ formatCurrency(action.price) }}</div>
            </div>
            <div class="action-total">
              {{ formatCurrency(action.amount) }}
            </div>
          </div>

          <div class="execution-footer mt-xl">
            <button 
              class="btn btn-primary btn-lg" 
              @click="executeSelected" 
              :disabled="selectedActions.length === 0 || executing"
            >
              {{ executing ? 'Executing Trades...' : `Execute ${selectedActions.length} Selected Trades` }}
            </button>
          </div>
        </div>
      </div>
    </template>

    <div v-else class="empty-state card">
      <p>No rebalancing data available. Ensure your portfolio has target allocations set up.</p>
    </div>

    <!-- Success Modal -->
    <Teleport to="body">
      <div v-if="success" class="modal-overlay">
        <div class="modal-card success-modal">
          <div class="success-icon">
            <svg xmlns="http://www.w3.org/2000/svg" width="64" height="64" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round"><path d="M22 11.08V12a10 10 0 1 1-5.93-9.14"/><polyline points="22 4 12 14.01 9 11.01"/></svg>
          </div>
          <h2>Trades Executed!</h2>
          <p>Your portfolio has been updated with the rebalancing transactions.</p>
          <div class="modal-actions">
            <button class="btn btn-primary" @click="closeSuccess">Great, take me to Ledger</button>
          </div>
        </div>
      </div>
    </Teleport>
  </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue';
import { useRouter } from 'vue-router';
import api from '../services/api';

const router = useRouter();
const portfolios = ref([]);
const selectedPortfolioId = ref(null);
const report = ref(null);
const loading = ref(true);
const executing = ref(false);
const success = ref(false);
const proposedActions = ref([]);

const formatCurrency = (val) => new Intl.NumberFormat('en-AU', { style: 'currency', currency: 'AUD' }).format(val || 0);

const totalDrift = computed(() => {
  if (!report.value) return 0;
  return report.value.categories.reduce((acc, cat) => acc + Math.abs(cat.currentPercentage - cat.targetPercentage), 0) / 2;
});

const driftClass = computed(() => {
  const drift = totalDrift.value;
  if (drift < 2) return 'text-success';
  if (drift < 5) return 'text-warning';
  return 'text-danger';
});

const statusClass = computed(() => {
  const drift = totalDrift.value;
  if (drift < 2) return 'badge-success';
  if (drift < 5) return 'badge-warning';
  return 'badge-danger';
});

const statusText = computed(() => {
  const drift = totalDrift.value;
  if (drift < 2) return 'Balanced';
  if (drift < 5) return 'Slight Drift';
  return 'Rebalance Needed';
});

const getDriftClass = (drift) => {
  if (Math.abs(drift) < 1) return 'text-success';
  if (Math.abs(drift) < 3) return 'text-warning';
  return 'text-danger';
};

const loadPortfolios = async () => {
  try {
    const res = await api.get('/portfolios');
    portfolios.value = res.data;
    if (portfolios.value.length > 0) {
      selectedPortfolioId.value = portfolios.value[0].id;
      await loadReport();
    }
  } catch (e) {
    console.error('Failed to load portfolios', e);
  } finally {
    loading.value = false;
  }
};

const loadReport = async () => {
  if (!selectedPortfolioId.value) return;
  loading.value = true;
  try {
    const res = await api.get(`/rebalancing/${selectedPortfolioId.value}`);
    report.value = res.data;
    generateProposedActions();
  } catch (e) {
    console.error('Failed to load rebalancing report', e);
  } finally {
    loading.value = false;
  }
};

const generateProposedActions = () => {
  proposedActions.value = [];
  if (!report.value) return;

  report.value.categories.forEach(cat => {
    if (cat.recommendation === 'Hold') return;

    // Pick top asset in category to buy/sell for rebalance
    // In a real app, we might split across assets, but for MVP we'll pick the largest one
    const mainAsset = cat.assets.sort((a, b) => b.totalValue - a.totalValue)[0];
    
    if (mainAsset && Math.abs(cat.varianceAmount) > 100) { // $100 threshold
      const amount = Math.abs(cat.varianceAmount);
      const units = amount / mainAsset.currentPrice;

      proposedActions.value.push({
        assetId: mainAsset.assetId,
        symbol: mainAsset.symbol,
        assetName: mainAsset.name,
        type: cat.recommendation === 'Buy' ? 'Buy' : 'Sell',
        units: units,
        price: mainAsset.currentPrice,
        amount: amount,
        selected: true
      });
    }
  });
};

const selectedActions = computed(() => proposedActions.value.filter(a => a.selected));

const executeSelected = async () => {
  executing.value = true;
  try {
    const actions = selectedActions.value.map(a => ({
      assetId: a.assetId,
      type: a.type === 'Buy' ? 0 : 1, // TransactionType enum Buy=0, Sell=1
      units: a.units,
      amount: a.amount
    }));

    await api.post(`/rebalancing/${selectedPortfolioId.value}/execute`, actions);
    success.value = true;
  } catch (e) {
    console.error('Execution failed', e);
    alert('Failed to execute rebalancing trades.');
  } finally {
    executing.value = false;
  }
};

const closeSuccess = () => {
  success.value = false;
  router.push('/reports'); // Navigate to Ledger
};

onMounted(loadPortfolios);
</script>

<style scoped>
.content-container {
  max-width: 1000px;
  margin: 0 auto;
}

.header-row {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.health-summary {
  display: flex;
  justify-content: space-around;
  padding: var(--spacing-xl);
  text-align: center;
}

.health-item {
  display: flex;
  flex-direction: column;
  gap: var(--spacing-xs);
}

.health-item .label {
  font-size: var(--font-size-sm);
  color: var(--color-text-muted);
  text-transform: uppercase;
  letter-spacing: 0.05em;
}

.health-item .value {
  font-size: var(--font-size-2xl);
  font-weight: 700;
}

.health-divider {
  width: 1px;
  background: var(--color-border);
}

.percentage-bar-mini {
  width: 60px;
  height: 4px;
  background: var(--color-bg-elevated);
  border-radius: 2px;
  display: inline-block;
  vertical-align: middle;
  margin-right: var(--spacing-sm);
  overflow: hidden;
}

.bar-fill {
  height: 100%;
  background: var(--color-accent);
}

.action-tag {
  padding: 4px 8px;
  border-radius: 4px;
  font-size: var(--font-size-xs);
  font-weight: 700;
  text-transform: uppercase;
}

.action-tag.buy { background: rgba(16, 185, 129, 0.1); color: #10b981; }
.action-tag.sell { background: rgba(239, 68, 68, 0.1); color: #ef4444; }

.action-card {
  display: flex;
  align-items: center;
  padding: var(--spacing-md);
  margin-bottom: var(--spacing-md);
  gap: var(--spacing-lg);
  border: 1px solid var(--color-border);
  transition: all 0.2s;
}

.action-card:hover { border-color: var(--color-accent); }

.action-type {
  font-weight: 800;
  text-transform: uppercase;
  font-size: var(--font-size-xs);
}
.action-type.buy { color: #10b981; }
.action-type.sell { color: #ef4444; }

.asset-info .symbol { font-weight: 700; margin-right: 8px; }
.asset-info .name { color: var(--color-text-muted); font-size: var(--font-size-sm); }

.action-math { text-align: right; margin-left: auto; }
.action-math .price { font-size: var(--font-size-xs); color: var(--color-text-muted); }

.action-total { font-weight: 700; min-width: 120px; text-align: right; }

.execution-footer {
  display: flex;
  justify-content: flex-end;
}

.success-modal {
  text-align: center;
  padding: var(--spacing-2xl);
}

.success-icon {
  font-size: 4rem;
  margin-bottom: var(--spacing-lg);
}

.badge {
  padding: 4px 12px;
  border-radius: 20px;
  font-size: var(--font-size-sm);
  font-weight: 600;
}
.badge-success { background: #10b981; color: white; }
.badge-warning { background: #f59e0b; color: white; }
.badge-danger { background: #ef4444; color: white; }

.text-success { color: #10b981; }
.text-warning { color: #f59e0b; }
.text-danger { color: #ef4444; }
</style>

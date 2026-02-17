<template>
  <div class="rebalancing-view animate-fade-in content-container">
    <div class="header-row mb-xl">
      <div class="header-content">
        <h1>Rebalancing Wizard</h1>
        <p class="subtitle">Align your portfolio with your target asset allocation</p>
      </div>
    </div>

    <div v-if="loading" class="loader-container">
      <div class="spinner"></div>
      <p>Analyzing portfolio allocations...</p>
    </div>

    <template v-else-if="report">
      <!-- Cash Flow Adjustment Panel -->
      <div class="cash-flow-panel card mb-xl">
        <div class="panel-header">
          <h3 class="panel-title">
            <svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><line x1="12" y1="1" x2="12" y2="23"/><path d="M17 5H9.5a3.5 3.5 0 0 0 0 7h5a3.5 3.5 0 0 1 0 7H6"/></svg>
            Cash Flow Adjustment
          </h3>
        </div>
        <div class="mode-selector">
          <button
            :class="['mode-btn', cashFlowMode === 'none' && 'active']"
            @click="setCashFlowMode('none')"
          >
            <span class="mode-icon">⚖️</span>
            <span class="mode-label">Rebalance Only</span>
            <span class="mode-desc">Realign existing holdings</span>
          </button>
          <button
            :class="['mode-btn add', cashFlowMode === 'add' && 'active']"
            @click="setCashFlowMode('add')"
          >
            <span class="mode-icon">📈</span>
            <span class="mode-label">Add Funds</span>
            <span class="mode-desc">Inject new capital</span>
          </button>
          <button
            :class="['mode-btn remove', cashFlowMode === 'remove' && 'active']"
            @click="setCashFlowMode('remove')"
          >
            <span class="mode-icon">📉</span>
            <span class="mode-label">Remove Funds</span>
            <span class="mode-desc">Withdraw capital</span>
          </button>
        </div>

        <div v-if="cashFlowMode !== 'none'" class="cash-flow-input-row">
          <label class="input-label">{{ cashFlowMode === 'add' ? 'Amount to Add' : 'Amount to Remove' }}</label>
          <div class="currency-input">
            <span class="currency-prefix">$</span>
            <input
              type="number"
              v-model.number="cashFlowInputAmount"
              @input="debouncedPreview"
              min="0"
              step="1000"
              placeholder="e.g. 300,000"
            />
          </div>
          <button class="btn btn-secondary" @click="clearCashFlow" style="white-space: nowrap;">Clear</button>
        </div>
      </div>

      <!-- Portfolio Health Summary -->
      <div class="health-summary card mb-xl">
        <div class="health-item">
          <span class="label">Current Value</span>
          <span class="value">{{ formatCurrency(report.totalValue) }}</span>
        </div>
        <div v-if="effectiveCashFlow !== 0" class="health-divider"></div>
        <div v-if="effectiveCashFlow !== 0" class="health-item">
          <span class="label">{{ effectiveCashFlow > 0 ? 'Adding' : 'Removing' }}</span>
          <span :class="['value', effectiveCashFlow > 0 ? 'text-success' : 'text-danger']">
            {{ effectiveCashFlow > 0 ? '+' : '' }}{{ formatCurrency(effectiveCashFlow) }}
          </span>
        </div>
        <div class="health-divider"></div>
        <div class="health-item">
          <span class="label">{{ effectiveCashFlow !== 0 ? 'New Portfolio Value' : 'Total Value' }}</span>
          <span class="value value-highlight">{{ formatCurrency(report.adjustedTotalValue) }}</span>
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
                <div class="cat-value">{{ formatCurrency(cat.currentValue) }} → {{ formatCurrency(cat.targetValue) }}</div>
              </td>
              <td class="text-right">
                <div class="percentage-bar-mini">
                  <div class="bar-fill" :style="{ width: Math.min(cat.currentPercentage, 100) + '%' }"></div>
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

        <div v-else>
          <div class="actions-list">
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
                <div class="price">@ {{ formatCurrencyPrecise(action.price) }}</div>
              </div>
              <div class="action-total">
                {{ formatCurrency(action.amount) }}
              </div>
            </div>
          </div>

          <!-- Execution Mode Selector -->
          <div class="execution-mode-section card mt-xl">
            <h3 class="panel-title">Execution Strategy</h3>

            <div class="exec-mode-selector">
              <button
                :class="['exec-mode-btn', executionMode === 'LumpSum' && 'active']"
                @click="executionMode = 'LumpSum'"
              >
                <span class="exec-icon">⚡</span>
                <span class="exec-label">Lump Sum</span>
                <span class="exec-desc">Execute all at once</span>
              </button>
              <button
                :class="['exec-mode-btn', executionMode === 'Distributed' && 'active']"
                @click="executionMode = 'Distributed'"
              >
                <span class="exec-icon">📊</span>
                <span class="exec-label">Distributed</span>
                <span class="exec-desc">Spread over multiple periods</span>
              </button>
              <button
                :class="['exec-mode-btn', executionMode === 'Combination' && 'active']"
                @click="executionMode = 'Combination'"
              >
                <span class="exec-icon">🔀</span>
                <span class="exec-label">Combination</span>
                <span class="exec-desc">Partial now, rest scheduled</span>
              </button>
            </div>

            <!-- Distributed / Combination Config -->
            <div v-if="executionMode !== 'LumpSum'" class="schedule-config">
              <!-- Combination: Lump Sum Percentage -->
              <div v-if="executionMode === 'Combination'" class="config-row">
                <label class="config-label">Execute Now</label>
                <div class="slider-group">
                  <input type="range" v-model.number="lumpSumPct" min="10" max="90" step="5" class="slider" />
                  <span class="slider-value">{{ lumpSumPct }}%</span>
                </div>
                <div class="split-preview">
                  <span class="split-now">Now: {{ formatCurrency(selectedTotal * lumpSumPct / 100) }}</span>
                  <span class="split-later">Scheduled: {{ formatCurrency(selectedTotal * (100 - lumpSumPct) / 100) }}</span>
                </div>
              </div>

              <div class="config-row-inline">
                <div class="config-field">
                  <label class="config-label">Periods</label>
                  <select v-model.number="totalPeriods">
                    <option v-for="n in 11" :key="n+1" :value="n+1">{{ n + 1 }} periods</option>
                  </select>
                </div>
                <div class="config-field">
                  <label class="config-label">Interval</label>
                  <select v-model="scheduleInterval">
                    <option value="Weekly">Weekly</option>
                    <option value="Fortnightly">Fortnightly</option>
                    <option value="Monthly">Monthly</option>
                  </select>
                </div>
              </div>

              <!-- Schedule Timeline Preview -->
              <div class="timeline-preview">
                <h4 class="timeline-title">Planned Schedule</h4>
                <div class="timeline">
                  <div
                    v-for="(tranche, i) in schedulePreview"
                    :key="i"
                    :class="['timeline-item', tranche.isLump && 'is-lump']"
                  >
                    <div class="timeline-dot"></div>
                    <div class="timeline-content">
                      <span class="timeline-date">{{ formatDate(tranche.date) }}</span>
                      <span class="timeline-amount">{{ formatCurrency(tranche.amount) }}</span>
                      <span v-if="tranche.isLump" class="timeline-badge">Lump Sum</span>
                      <span v-else class="timeline-badge period-badge">Period {{ tranche.period }}</span>
                    </div>
                  </div>
                </div>
              </div>
            </div>
          </div>

          <div class="execution-footer mt-xl">
            <div class="exec-summary" v-if="selectedActions.length > 0">
              <span class="exec-total-label">Total Selected</span>
              <span class="exec-total-value">{{ formatCurrency(selectedTotal) }}</span>
            </div>
            <button
              class="btn btn-primary btn-lg"
              @click="executeSelected"
              :disabled="selectedActions.length === 0 || executing"
            >
              {{ executionButtonLabel }}
            </button>
          </div>
        </div>
      </div>

      <!-- Active Schedules Panel -->
      <div v-if="activeSchedules.length > 0" class="schedules-section mt-xl">
        <div class="section-header mb-md">
          <h2>Active Schedules</h2>
          <p class="subtitle">Manage your ongoing rebalancing schedules.</p>
        </div>

        <div v-for="sched in activeSchedules" :key="sched.id" class="schedule-card card mb-md">
          <div class="schedule-header">
            <div>
              <span class="schedule-mode-badge">{{ sched.executionMode }}</span>
              <span class="schedule-meta">Created {{ formatDate(sched.createdDate) }}</span>
            </div>
            <div class="schedule-progress">
              <span>{{ sched.completedPeriods }}/{{ sched.totalPeriods }} completed</span>
              <div class="progress-bar">
                <div class="progress-fill" :style="{ width: (sched.completedPeriods / sched.totalPeriods * 100) + '%' }"></div>
              </div>
            </div>
          </div>

          <div class="schedule-items">
            <div
              v-for="item in sched.items"
              :key="item.id"
              :class="['schedule-item', item.status.toLowerCase()]"
            >
              <span class="item-period">
                {{ item.periodNumber === 0 && sched.executionMode === 'Combination' ? 'Lump' : `P${item.periodNumber + 1}` }}
              </span>
              <span class="item-date">{{ formatDate(item.plannedDate) }}</span>
              <span class="item-amount">{{ formatCurrency(item.actions.reduce((s, a) => s + a.amount, 0)) }}</span>
              <span :class="['item-status', item.status.toLowerCase()]">{{ item.status }}</span>
            </div>
          </div>

          <div class="schedule-actions">
            <button
              class="btn btn-primary"
              @click="executeNextTranche(sched.id)"
              :disabled="executingTranche === sched.id"
              v-if="sched.items.some(i => i.status === 'Pending')"
            >
              {{ executingTranche === sched.id ? 'Executing...' : 'Execute Next Tranche' }}
            </button>
            <button
              class="btn btn-secondary btn-danger-outline"
              @click="cancelSchedule(sched.id)"
              v-if="sched.items.some(i => i.status === 'Pending')"
            >
              Cancel Schedule
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
          <h2>{{ successMessage }}</h2>
          <p>{{ successDetail }}</p>
          <div class="modal-actions">
            <button class="btn btn-primary" @click="closeSuccess">Great, take me to Ledger</button>
          </div>
        </div>
      </div>
    </Teleport>
  </div>
</template>

<script setup>
import { ref, computed, onMounted, watch } from 'vue';
import { useRouter } from 'vue-router';
import { usePortfolioStore } from '../stores/portfolio';
import api from '../services/api';
import { formatCurrency, formatCurrencyPrecise } from '../utils/formatters';

const router = useRouter();
const portfolioStore = usePortfolioStore();

const report = ref(null);
const loading = ref(true);
const executing = ref(false);
const success = ref(false);
const successMessage = ref('Trades Executed!');
const successDetail = ref('Your portfolio has been updated with the rebalancing transactions.');
const proposedActions = ref([]);
const activeSchedules = ref([]);
const executingTranche = ref(null);

// Cash flow state
const cashFlowMode = ref('none'); // 'none' | 'add' | 'remove'
const cashFlowInputAmount = ref(0);

// Execution scheduling state
const executionMode = ref('LumpSum'); // 'LumpSum' | 'Distributed' | 'Combination'
const totalPeriods = ref(4);
const scheduleInterval = ref('Monthly');
const lumpSumPct = ref(50);

const currentPortfolioId = computed(() => portfolioStore.currentPortfolioId);

// Local formatters removed in favor of shared ones

const formatDate = (dateStr) => {
  const d = new Date(dateStr);
  return d.toLocaleDateString('en-AU', { day: 'numeric', month: 'short', year: 'numeric' });
};

const effectiveCashFlow = computed(() => {
  if (cashFlowMode.value === 'none' || !cashFlowInputAmount.value) return 0;
  return cashFlowMode.value === 'add' ? cashFlowInputAmount.value : -cashFlowInputAmount.value;
});

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

const selectedActions = computed(() => proposedActions.value.filter(a => a.selected));

const selectedTotal = computed(() => selectedActions.value.reduce((sum, a) => sum + a.amount, 0));

const executionButtonLabel = computed(() => {
  const count = selectedActions.value.length;
  if (executing.value) return 'Executing...';
  if (count === 0) return 'No Trades Selected';
  if (executionMode.value === 'LumpSum') return `Execute ${count} Trades Now`;
  if (executionMode.value === 'Distributed') return `Create Schedule (${totalPeriods.value} periods)`;
  return `Execute ${lumpSumPct.value}% Now + Schedule Rest`;
});

// Schedule timeline preview
const schedulePreview = computed(() => {
  if (executionMode.value === 'LumpSum') return [];
  const total = selectedTotal.value;
  const items = [];
  const now = new Date();

  if (executionMode.value === 'Combination') {
    items.push({ date: now, amount: total * lumpSumPct.value / 100, isLump: true, period: 0 });
    const remaining = total * (100 - lumpSumPct.value) / 100;
    const perPeriod = remaining / totalPeriods.value;
    for (let i = 0; i < totalPeriods.value; i++) {
      items.push({ date: addPeriods(now, i + 1), amount: perPeriod, isLump: false, period: i + 1 });
    }
  } else {
    const perPeriod = total / totalPeriods.value;
    for (let i = 0; i < totalPeriods.value; i++) {
      items.push({ date: addPeriods(now, i), amount: perPeriod, isLump: false, period: i + 1 });
    }
  }

  return items;
});

function addPeriods(from, count) {
  const d = new Date(from);
  if (scheduleInterval.value === 'Weekly') d.setDate(d.getDate() + 7 * count);
  else if (scheduleInterval.value === 'Fortnightly') d.setDate(d.getDate() + 14 * count);
  else d.setMonth(d.getMonth() + count);
  return d;
}

let debounceTimer = null;
const debouncedPreview = () => {
  clearTimeout(debounceTimer);
  debounceTimer = setTimeout(() => loadReport(), 400);
};

const setCashFlowMode = (mode) => {
  cashFlowMode.value = mode;
  if (mode === 'none') {
    cashFlowInputAmount.value = 0;
    loadReport();
  }
};

const clearCashFlow = () => {
  cashFlowInputAmount.value = 0;
  loadReport();
};

const loadReport = async () => {
  if (!currentPortfolioId.value) {
    loading.value = false;
    return;
  }
  loading.value = true;
  try {
    const params = effectiveCashFlow.value !== 0 ? `?cashFlow=${effectiveCashFlow.value}` : '';
    const res = await api.get(`/rebalancing/${currentPortfolioId.value}${params}`);
    report.value = res.data;
    generateProposedActions();
  } catch (e) {
    console.error('Failed to load rebalancing report', e);
  } finally {
    loading.value = false;
  }
};

const loadSchedules = async () => {
  if (!currentPortfolioId.value) return;
  try {
    const res = await api.get(`/rebalancing/${currentPortfolioId.value}/schedules`);
    activeSchedules.value = res.data;
  } catch (e) {
    console.error('Failed to load schedules', e);
  }
};

const generateProposedActions = () => {
  proposedActions.value = [];
  if (!report.value) return;

  report.value.categories.forEach(cat => {
    if (cat.recommendation === 'Hold') return;

    const mainAsset = cat.assets.sort((a, b) => b.totalValue - a.totalValue)[0];

    if (mainAsset && Math.abs(cat.varianceAmount) > 100) {
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

const executeSelected = async () => {
  executing.value = true;
  try {
    const actions = selectedActions.value.map(a => ({
      assetId: a.assetId,
      type: a.type === 'Buy' ? 0 : 1,
      units: a.units,
      amount: a.amount
    }));

    if (executionMode.value === 'LumpSum') {
      await api.post(`/rebalancing/${currentPortfolioId.value}/execute`, actions);
      successMessage.value = 'Trades Executed!';
      successDetail.value = 'Your portfolio has been updated with the rebalancing transactions.';
    } else {
      const payload = {
        actions,
        executionMode: executionMode.value === 'Distributed' ? 1 : 2,
        totalPeriods: totalPeriods.value,
        interval: scheduleInterval.value === 'Weekly' ? 0 : scheduleInterval.value === 'Fortnightly' ? 1 : 2,
        lumpSumPercentage: lumpSumPct.value,
        cashFlowAmount: effectiveCashFlow.value
      };
      const res = await api.post(`/rebalancing/${currentPortfolioId.value}/execute-scheduled`, payload);
      successMessage.value = executionMode.value === 'Combination'
        ? `${lumpSumPct.value}% Executed, Rest Scheduled!`
        : 'Schedule Created!';
      successDetail.value = res.data.message;
      await loadSchedules();
    }

    success.value = true;
  } catch (e) {
    console.error('Execution failed', e);
    alert('Failed to execute rebalancing trades.');
  } finally {
    executing.value = false;
  }
};

const executeNextTranche = async (scheduleId) => {
  executingTranche.value = scheduleId;
  try {
    await api.post(`/rebalancing/schedules/${scheduleId}/execute-next`);
    await loadSchedules();
    await loadReport();
  } catch (e) {
    console.error('Tranche execution failed', e);
    alert(e.response?.data?.message || 'Failed to execute tranche.');
  } finally {
    executingTranche.value = null;
  }
};

const cancelSchedule = async (scheduleId) => {
  if (!confirm('Cancel this rebalancing schedule? Remaining tranches will not be executed.')) return;
  try {
    await api.post(`/rebalancing/schedules/${scheduleId}/cancel`);
    await loadSchedules();
  } catch (e) {
    console.error('Cancel failed', e);
  }
};

const closeSuccess = () => {
  success.value = false;
  router.push('/reports');
};

// Watch for portfolio changes
watch(currentPortfolioId, () => {
  loadReport();
  loadSchedules();
});

onMounted(() => {
  if (currentPortfolioId.value) {
    loadReport();
    loadSchedules();
  } else {
    loading.value = false;
  }
});
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

/* Cash Flow Panel */
.panel-header {
  margin-bottom: var(--spacing-md);
}

.panel-title {
  display: flex;
  align-items: center;
  gap: var(--spacing-sm);
  margin: 0;
  font-size: 1.1rem;
  color: hsl(var(--color-text-main));
}

.mode-selector {
  display: grid;
  grid-template-columns: repeat(3, 1fr);
  gap: var(--spacing-sm);
}

.mode-btn {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 4px;
  padding: var(--spacing-md);
  border: 2px solid hsl(var(--color-text-muted) / 0.15);
  border-radius: var(--radius-md);
  background: hsl(var(--color-surface));
  cursor: pointer;
  transition: all var(--transition-speed);
}

.mode-btn:hover {
  border-color: hsl(var(--color-primary) / 0.3);
  background: hsl(var(--color-primary-light));
}

.mode-btn.active {
  border-color: hsl(var(--color-primary));
  background: hsl(var(--color-primary-light));
  box-shadow: 0 0 0 3px hsl(var(--color-primary) / 0.1);
}

.mode-btn.add.active {
  border-color: hsl(var(--color-success));
  background: hsl(var(--color-success) / 0.08);
  box-shadow: 0 0 0 3px hsl(var(--color-success) / 0.1);
}

.mode-btn.remove.active {
  border-color: hsl(var(--color-danger));
  background: hsl(var(--color-danger) / 0.08);
  box-shadow: 0 0 0 3px hsl(var(--color-danger) / 0.1);
}

.mode-icon { font-size: 1.5rem; }
.mode-label { font-weight: 600; font-size: 0.9rem; }
.mode-desc { font-size: 0.75rem; color: hsl(var(--color-text-muted)); }

.cash-flow-input-row {
  display: flex;
  align-items: end;
  gap: var(--spacing-md);
  margin-top: var(--spacing-lg);
}

.input-label {
  display: block;
  font-size: 0.85rem;
  font-weight: 600;
  margin-bottom: 4px;
  color: hsl(var(--color-text-muted));
}

.currency-input {
  position: relative;
  flex: 1;
}

.currency-prefix {
  position: absolute;
  left: 12px;
  top: 50%;
  transform: translateY(-50%);
  color: hsl(var(--color-text-muted));
  font-weight: 600;
}

.currency-input input {
  padding-left: 28px;
  font-size: 1.1rem;
  font-weight: 600;
}

/* Health Summary */
.health-summary {
  display: flex;
  justify-content: space-around;
  padding: var(--spacing-xl);
  text-align: center;
  flex-wrap: wrap;
  gap: var(--spacing-md);
}

.health-item {
  display: flex;
  flex-direction: column;
  gap: var(--spacing-xs);
}

.health-item .label {
  font-size: var(--font-size-sm, 0.875rem);
  color: hsl(var(--color-text-muted));
  text-transform: uppercase;
  letter-spacing: 0.05em;
}

.health-item .value {
  font-size: var(--font-size-2xl, 1.5rem);
  font-weight: 700;
}

.value-highlight {
  color: hsl(var(--color-primary));
}

.health-divider {
  width: 1px;
  background: hsl(var(--color-text-muted) / 0.15);
  align-self: stretch;
}

/* Categories Table */
.percentage-bar-mini {
  width: 60px;
  height: 4px;
  background: hsl(var(--color-text-muted) / 0.1);
  border-radius: 2px;
  display: inline-block;
  vertical-align: middle;
  margin-right: var(--spacing-sm);
  overflow: hidden;
}

.bar-fill {
  height: 100%;
  background: hsl(var(--color-primary));
  border-radius: 2px;
}

.cat-value {
  font-size: 0.8rem;
  color: hsl(var(--color-text-muted));
}

.action-tag {
  padding: 4px 8px;
  border-radius: 4px;
  font-size: 0.75rem;
  font-weight: 700;
  text-transform: uppercase;
}

.action-tag.buy { background: hsl(var(--color-success) / 0.1); color: hsl(var(--color-success)); }
.action-tag.sell { background: hsl(var(--color-danger) / 0.1); color: hsl(var(--color-danger)); }
.action-tag.hold { background: hsl(var(--color-text-muted) / 0.1); color: hsl(var(--color-text-muted)); }

/* Action Cards */
.action-card {
  display: flex;
  align-items: center;
  padding: var(--spacing-md);
  margin-bottom: var(--spacing-md);
  gap: var(--spacing-lg);
  border: 1px solid hsl(var(--color-text-muted) / 0.1);
  transition: all 0.2s;
}

.action-card:hover { border-color: hsl(var(--color-primary) / 0.3); transform: translateY(-1px); }

.action-type {
  font-weight: 800;
  text-transform: uppercase;
  font-size: 0.75rem;
}
.action-type.buy { color: hsl(var(--color-success)); }
.action-type.sell { color: hsl(var(--color-danger)); }

.asset-info .symbol { font-weight: 700; margin-right: 8px; }
.asset-info .name { color: hsl(var(--color-text-muted)); font-size: 0.875rem; }

.action-math { text-align: right; margin-left: auto; }
.action-math .price { font-size: 0.75rem; color: hsl(var(--color-text-muted)); }

.action-total { font-weight: 700; min-width: 120px; text-align: right; }

/* Execution Mode Selector */
.execution-mode-section {
  padding: var(--spacing-lg);
}

.exec-mode-selector {
  display: grid;
  grid-template-columns: repeat(3, 1fr);
  gap: var(--spacing-sm);
  margin-top: var(--spacing-md);
}

.exec-mode-btn {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 4px;
  padding: var(--spacing-md) var(--spacing-sm);
  border: 2px solid hsl(var(--color-text-muted) / 0.15);
  border-radius: var(--radius-md);
  background: hsl(var(--color-surface));
  cursor: pointer;
  transition: all var(--transition-speed);
}

.exec-mode-btn:hover {
  border-color: hsl(var(--color-primary) / 0.3);
}

.exec-mode-btn.active {
  border-color: hsl(var(--color-primary));
  background: hsl(var(--color-primary-light));
  box-shadow: 0 0 0 3px hsl(var(--color-primary) / 0.1);
}

.exec-icon { font-size: 1.3rem; }
.exec-label { font-weight: 600; font-size: 0.9rem; }
.exec-desc { font-size: 0.72rem; color: hsl(var(--color-text-muted)); text-align: center; }

/* Schedule Config */
.schedule-config {
  margin-top: var(--spacing-lg);
  padding-top: var(--spacing-lg);
  border-top: 1px solid hsl(var(--color-text-muted) / 0.1);
}

.config-row {
  margin-bottom: var(--spacing-lg);
}

.config-label {
  display: block;
  font-size: 0.85rem;
  font-weight: 600;
  margin-bottom: 6px;
  color: hsl(var(--color-text-muted));
}

.slider-group {
  display: flex;
  align-items: center;
  gap: var(--spacing-md);
}

.slider {
  flex: 1;
  height: 6px;
  appearance: none;
  background: hsl(var(--color-text-muted) / 0.15);
  border-radius: 3px;
  outline: none;
}

.slider::-webkit-slider-thumb {
  appearance: none;
  width: 22px;
  height: 22px;
  border-radius: 50%;
  background: hsl(var(--color-primary));
  cursor: pointer;
  box-shadow: 0 2px 6px hsl(var(--color-primary) / 0.3);
}

.slider-value {
  font-weight: 700;
  font-size: 1.2rem;
  min-width: 50px;
  text-align: center;
  color: hsl(var(--color-primary));
}

.split-preview {
  display: flex;
  justify-content: space-between;
  margin-top: var(--spacing-sm);
  font-size: 0.85rem;
}

.split-now { color: hsl(var(--color-primary)); font-weight: 600; }
.split-later { color: hsl(var(--color-text-muted)); }

.config-row-inline {
  display: flex;
  gap: var(--spacing-md);
}

.config-field {
  flex: 1;
}

.config-field select {
  width: 100%;
}

/* Timeline Preview */
.timeline-preview {
  margin-top: var(--spacing-xl);
}

.timeline-title {
  font-size: 0.9rem;
  color: hsl(var(--color-text-muted));
  margin-bottom: var(--spacing-md);
}

.timeline {
  position: relative;
  padding-left: 20px;
}

.timeline::before {
  content: '';
  position: absolute;
  left: 6px;
  top: 4px;
  bottom: 4px;
  width: 2px;
  background: hsl(var(--color-text-muted) / 0.15);
}

.timeline-item {
  position: relative;
  padding-bottom: var(--spacing-md);
  padding-left: var(--spacing-md);
}

.timeline-dot {
  position: absolute;
  left: -17px;
  top: 4px;
  width: 10px;
  height: 10px;
  border-radius: 50%;
  background: hsl(var(--color-text-muted) / 0.3);
  border: 2px solid hsl(var(--color-surface));
}

.timeline-item.is-lump .timeline-dot {
  background: hsl(var(--color-primary));
  width: 12px;
  height: 12px;
  left: -18px;
  top: 3px;
}

.timeline-content {
  display: flex;
  align-items: center;
  gap: var(--spacing-sm);
  font-size: 0.85rem;
}

.timeline-date { color: hsl(var(--color-text-muted)); min-width: 100px; }
.timeline-amount { font-weight: 600; }
.timeline-badge {
  font-size: 0.7rem;
  padding: 2px 8px;
  border-radius: 10px;
  background: hsl(var(--color-primary) / 0.1);
  color: hsl(var(--color-primary));
  font-weight: 600;
}

.period-badge {
  background: hsl(var(--color-text-muted) / 0.1);
  color: hsl(var(--color-text-muted));
}

/* Execution Footer */
.execution-footer {
  display: flex;
  justify-content: flex-end;
  align-items: center;
  gap: var(--spacing-xl);
}

.exec-summary {
  display: flex;
  flex-direction: column;
  align-items: flex-end;
}

.exec-total-label {
  font-size: 0.8rem;
  color: hsl(var(--color-text-muted));
  text-transform: uppercase;
}

.exec-total-value {
  font-size: 1.3rem;
  font-weight: 700;
  color: hsl(var(--color-primary));
}

/* Active Schedules */
.schedule-card {
  cursor: default;
}

.schedule-card:hover {
  transform: none;
}

.schedule-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: var(--spacing-md);
  flex-wrap: wrap;
  gap: var(--spacing-sm);
}

.schedule-mode-badge {
  padding: 4px 10px;
  border-radius: 4px;
  font-size: 0.75rem;
  font-weight: 700;
  text-transform: uppercase;
  background: hsl(var(--color-primary) / 0.1);
  color: hsl(var(--color-primary));
}

.schedule-meta {
  font-size: 0.8rem;
  color: hsl(var(--color-text-muted));
  margin-left: var(--spacing-sm);
}

.schedule-progress {
  display: flex;
  align-items: center;
  gap: var(--spacing-sm);
  font-size: 0.85rem;
}

.progress-bar {
  width: 120px;
  height: 6px;
  background: hsl(var(--color-text-muted) / 0.1);
  border-radius: 3px;
  overflow: hidden;
}

.progress-fill {
  height: 100%;
  background: hsl(var(--color-success));
  border-radius: 3px;
  transition: width 0.3s;
}

.schedule-items {
  display: flex;
  flex-direction: column;
  gap: 4px;
  margin-bottom: var(--spacing-md);
}

.schedule-item {
  display: flex;
  align-items: center;
  gap: var(--spacing-md);
  padding: var(--spacing-sm) var(--spacing-md);
  border-radius: var(--radius-sm);
  font-size: 0.85rem;
  background: hsl(var(--color-text-muted) / 0.03);
}

.schedule-item.executed {
  opacity: 0.6;
}

.item-period {
  font-weight: 700;
  min-width: 40px;
}

.item-date {
  color: hsl(var(--color-text-muted));
  min-width: 100px;
}

.item-amount {
  font-weight: 600;
  flex: 1;
}

.item-status {
  font-size: 0.75rem;
  font-weight: 600;
  text-transform: uppercase;
  padding: 2px 8px;
  border-radius: 4px;
}

.item-status.executed { background: hsl(var(--color-success) / 0.1); color: hsl(var(--color-success)); }
.item-status.pending { background: hsl(var(--color-warning) / 0.1); color: hsl(var(--color-warning)); }
.item-status.skipped { background: hsl(var(--color-text-muted) / 0.1); color: hsl(var(--color-text-muted)); }

.schedule-actions {
  display: flex;
  gap: var(--spacing-sm);
}

.btn-danger-outline {
  color: hsl(var(--color-danger)) !important;
  border-color: hsl(var(--color-danger) / 0.3) !important;
}

.btn-danger-outline:hover {
  background: hsl(var(--color-danger) / 0.05) !important;
}

/* Success Modal */
.success-modal {
  text-align: center;
  padding: var(--spacing-2xl, 2.5rem);
}

.success-icon {
  font-size: 4rem;
  margin-bottom: var(--spacing-lg);
  color: hsl(var(--color-success));
}

/* Status Colors */
.badge {
  padding: 4px 12px;
  border-radius: 20px;
  font-size: 0.875rem;
  font-weight: 600;
}
.badge-success { background: hsl(var(--color-success)); color: white; }
.badge-warning { background: hsl(var(--color-warning)); color: white; }
.badge-danger { background: hsl(var(--color-danger)); color: white; }

.text-success { color: hsl(var(--color-success)); }
.text-warning { color: hsl(var(--color-warning)); }
.text-danger { color: hsl(var(--color-danger)); }

.mt-xl { margin-top: var(--spacing-xl); }
.mb-xl { margin-bottom: var(--spacing-xl); }
.mb-md { margin-bottom: var(--spacing-md); }

/* Responsive */
@media (max-width: 640px) {
  .mode-selector, .exec-mode-selector { grid-template-columns: 1fr; }
  .health-summary { flex-direction: column; }
  .health-divider { width: 100%; height: 1px; }
  .config-row-inline { flex-direction: column; }
  .action-card { flex-wrap: wrap; }
  .cash-flow-input-row { flex-direction: column; }
}
</style>

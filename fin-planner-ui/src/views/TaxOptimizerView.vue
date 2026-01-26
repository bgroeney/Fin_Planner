<template>
  <div class="tax-optimizer-view animate-fade-in">
    <div class="header-row">
      <div class="header-content">
        <h1>Tax Distribution Optimizer</h1>
        <p class="subtitle">Optimize trust income distribution to minimize family tax liability</p>
      </div>
    </div>

    <div class="controls-row card">
      <div class="control-group">
        <label>Trust</label>
        <select v-model="selectedTrustId" @change="resetOptimization">
          <option :value="null" disabled>Select a trust...</option>
          <option v-for="t in trusts" :key="t.id" :value="t.id">{{ t.trustName }}</option>
        </select>
      </div>
      <div class="control-group">
        <label>Financial Year</label>
        <select v-model="fiscalYear">
          <option v-for="fy in fiscalYears" :key="fy" :value="fy">FY {{ fy }}</option>
        </select>
      </div>
      <button 
        class="btn btn-primary" 
        :disabled="!selectedTrustId || loading"
        @click="runOptimization"
      >
        {{ loading ? 'Optimizing...' : '🎯 Optimize Distribution' }}
      </button>
    </div>

    <div v-if="loading" class="loader-container">
      <div class="spinner"></div>
    </div>

    <div v-else-if="result" class="optimization-results">
      <!-- Summary Cards -->
      <div class="summary-grid">
        <div class="summary-card card">
          <div class="label-text">Total Distributable Income</div>
          <div class="value-large">{{ formatCurrency(result.totalDistributableIncome) }}</div>
        </div>
        <div class="summary-card card">
          <div class="label-text">Franking Credits Available</div>
          <div class="value-large">{{ formatCurrency(result.totalFrankingCredits) }}</div>
          <div class="text-muted">Used: {{ formatCurrency(result.totalFrankingCreditsUsed) }}</div>
        </div>
        <div class="summary-card card highlight-success">
          <div class="label-text">Estimated Total Tax</div>
          <div class="value-large value-positive">{{ formatCurrency(result.totalEstimatedTax) }}</div>
          <div class="text-muted">After franking credit offsets</div>
        </div>
      </div>

      <!-- Recommendations -->
      <div class="recommendations-section card">
        <h2>Recommended Distributions</h2>
        <p class="section-description">{{ result.message }}</p>

        <table class="data-table" v-if="result.recommendations?.length > 0">
          <thead>
            <tr>
              <th>Beneficiary</th>
              <th>Type</th>
              <th class="text-right">Franked Dividends</th>
              <th class="text-right">Other Income</th>
              <th class="text-right">Capital Gains</th>
              <th class="text-right">Total</th>
              <th class="text-right">Tax Rate</th>
              <th class="text-right">Est. Tax</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="rec in result.recommendations" :key="rec.beneficiaryId">
              <td>
                <div class="beneficiary-cell">
                  <span class="beneficiary-name">{{ rec.beneficiaryName }}</span>
                </div>
              </td>
              <td>
                <span class="type-badge" :class="rec.isCompany ? 'type-company' : 'type-person'">
                  {{ rec.isCompany ? 'Company' : 'Individual' }}
                </span>
              </td>
              <td class="text-right">
                <div>{{ formatCurrency(rec.frankedDividends) }}</div>
                <div class="text-muted text-small" v-if="rec.frankingCredits > 0">
                  + {{ formatCurrency(rec.frankingCredits) }} FC
                </div>
              </td>
              <td class="text-right">{{ formatCurrency(rec.unfrankedIncome) }}</td>
              <td class="text-right">
                <div v-if="rec.discountCapitalGains > 0">
                  {{ formatCurrency(rec.discountCapitalGains) }}
                  <span class="text-muted text-small">(50% disc.)</span>
                </div>
                <div v-if="rec.nonDiscountCapitalGains > 0">
                  {{ formatCurrency(rec.nonDiscountCapitalGains) }}
                </div>
                <span v-if="rec.discountCapitalGains === 0 && rec.nonDiscountCapitalGains === 0">-</span>
              </td>
              <td class="text-right font-semibold">{{ formatCurrency(rec.totalDistribution) }}</td>
              <td class="text-right">
                <div class="rate-change">
                  <span class="rate-before">{{ formatPercent(rec.marginalTaxRateBefore) }}</span>
                  <span class="rate-arrow">→</span>
                  <span class="rate-after">{{ formatPercent(rec.marginalTaxRateAfter) }}</span>
                </div>
              </td>
              <td class="text-right" :class="rec.estimatedTaxOnDistribution < 0 ? 'value-positive' : ''">
                {{ formatCurrency(rec.estimatedTaxOnDistribution) }}
                <span v-if="rec.estimatedTaxOnDistribution < 0" class="text-small">(refund)</span>
              </td>
            </tr>
          </tbody>
        </table>

        <div v-else class="empty-state">
          <p>No distribution recommendations available. Make sure you have beneficiaries added to this trust.</p>
        </div>
      </div>

      <!-- Action Buttons -->
      <div class="action-row" v-if="result.recommendations?.length > 0">
        <button class="btn btn-secondary" @click="resetOptimization">
          Reset
        </button>
        <button class="btn btn-primary" @click="applyOptimization" :disabled="applying">
          {{ applying ? 'Applying...' : 'Apply as Draft Distributions' }}
        </button>
      </div>
    </div>

    <div v-else-if="!selectedTrustId" class="empty-state card">
      <div class="empty-icon">🎯</div>
      <h3>Select a Trust to Optimize</h3>
      <p>Choose a trust from the dropdown above to run the tax distribution optimizer.</p>
      <router-link to="/entities" class="btn btn-secondary" v-if="trusts.length === 0">
        Create a Trust First
      </router-link>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue';
import api from '../services/api';

const loading = ref(false);
const applying = ref(false);
const trusts = ref([]);
const selectedTrustId = ref(null);
const fiscalYear = ref(new Date().getMonth() > 5 ? new Date().getFullYear() + 1 : new Date().getFullYear());
const result = ref(null);

const fiscalYears = computed(() => {
  const current = new Date().getFullYear();
  return [current + 1, current, current - 1];
});

const formatCurrency = (value) => {
  return new Intl.NumberFormat('en-AU', { style: 'currency', currency: 'AUD', maximumFractionDigits: 0 }).format(value || 0);
};

const formatPercent = (value) => {
  return ((value || 0) * 100).toFixed(1) + '%';
};

const loadTrusts = async () => {
  try {
    const res = await api.get('/trusts');
    trusts.value = res.data;
  } catch (e) {
    console.error('Failed to load trusts', e);
  }
};

const runOptimization = async () => {
  if (!selectedTrustId.value) return;
  
  loading.value = true;
  result.value = null;
  
  try {
    const res = await api.get(`/trusts/${selectedTrustId.value}/optimize-distribution?fiscalYear=${fiscalYear.value}`);
    result.value = res.data;
  } catch (e) {
    console.error('Failed to run optimization', e);
    alert('Failed to run optimization. Make sure you have trust income recorded for this fiscal year.');
  } finally {
    loading.value = false;
  }
};

const applyOptimization = async () => {
  if (!selectedTrustId.value || !result.value) return;
  
  applying.value = true;
  
  try {
    const res = await api.post(`/trusts/${selectedTrustId.value}/apply-optimization?fiscalYear=${fiscalYear.value}`);
    alert(res.data.message || 'Distributions created successfully');
  } catch (e) {
    console.error('Failed to apply optimization', e);
    alert('Failed to apply optimization');
  } finally {
    applying.value = false;
  }
};

const resetOptimization = () => {
  result.value = null;
};

onMounted(loadTrusts);
</script>

<style scoped>
.tax-optimizer-view {
  max-width: 1200px;
  margin: 0 auto;
}

.header-row {
  margin-bottom: var(--spacing-xl);
}

.controls-row {
  display: flex;
  align-items: flex-end;
  gap: var(--spacing-lg);
  padding: var(--spacing-lg);
  margin-bottom: var(--spacing-xl);
  flex-wrap: wrap;
}

.control-group {
  display: flex;
  flex-direction: column;
  gap: var(--spacing-xs);
}

.control-group label {
  font-size: var(--font-size-sm);
  color: var(--color-text-muted);
  font-weight: 500;
}

.control-group select {
  padding: var(--spacing-sm) var(--spacing-md);
  border-radius: var(--radius-md);
  border: 1px solid var(--color-border);
  background: var(--color-bg-elevated);
  color: var(--color-text-primary);
  min-width: 200px;
}

.summary-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(250px, 1fr));
  gap: var(--spacing-lg);
  margin-bottom: var(--spacing-xl);
}

.summary-card {
  padding: var(--spacing-lg);
}

.highlight-success {
  border-color: rgba(16, 185, 129, 0.3);
  background: linear-gradient(135deg, rgba(16, 185, 129, 0.05), transparent);
}

.recommendations-section {
  padding: var(--spacing-xl);
  margin-bottom: var(--spacing-xl);
}

.recommendations-section h2 {
  margin-bottom: var(--spacing-xs);
}

.section-description {
  color: var(--color-text-muted);
  margin-bottom: var(--spacing-xl);
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

.text-small {
  font-size: var(--font-size-xs);
}

.font-semibold {
  font-weight: 600;
}

.beneficiary-cell {
  display: flex;
  flex-direction: column;
  gap: 2px;
}

.beneficiary-name {
  font-weight: 500;
}

.type-badge {
  padding: 2px 8px;
  border-radius: var(--radius-sm);
  font-size: var(--font-size-xs);
  font-weight: 600;
}

.type-person {
  background: rgba(59, 130, 246, 0.15);
  color: #3b82f6;
}

.type-company {
  background: rgba(16, 185, 129, 0.15);
  color: #10b981;
}

.rate-change {
  display: flex;
  align-items: center;
  justify-content: flex-end;
  gap: var(--spacing-xs);
  font-size: var(--font-size-sm);
}

.rate-before {
  color: var(--color-text-muted);
}

.rate-arrow {
  color: var(--color-text-muted);
}

.rate-after {
  font-weight: 600;
}

.action-row {
  display: flex;
  justify-content: flex-end;
  gap: var(--spacing-md);
}

.empty-state {
  text-align: center;
  padding: 60px;
}

.empty-icon {
  font-size: 48px;
  margin-bottom: var(--spacing-md);
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

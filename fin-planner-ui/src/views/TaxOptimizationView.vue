<template>
  <div class="tax-optimization-view animate-fade-in content-container">
    <div class="header-row mb-xl">
      <div class="header-content">
        <h1>Tax Distribution Optimizer</h1>
        <p class="subtitle">Mathematically optimize trust distributions to minimize family group tax.</p>
      </div>
      <div class="portfolio-selector">
        <select v-model="selectedPortfolioId" @change="loadData" class="form-select">
          <option v-for="p in portfolios" :key="p.id" :value="p.id">{{ p.name }}</option>
        </select>
      </div>
    </div>

    <div v-if="loading" class="loader-container">
      <div class="spinner"></div>
      <p>Analyzing tax positions and calculating optimal distributions...</p>
    </div>

    <template v-else-if="portfolios.length > 0">
      <div class="grid-2-1">
        <!-- Main Analysis Section -->
        <div class="left-col">
          <!-- Setup Summary -->
          <div class="card mb-xl">
            <div class="card-header-flex">
              <h3>Configuration</h3>
              <div class="fy-badge">FY {{ fiscalYear }}</div>
            </div>
            <div class="setup-grid">
              <div class="setup-item">
                <span class="l">Trusts Found</span>
                <span class="v">{{ results?.trusts?.length || 0 }}</span>
              </div>
              <div class="setup-item">
                <span class="l">Beneficiaries</span>
                <span class="v">{{ results?.beneficiaries?.length || 0 }}</span>
              </div>
              <div class="setup-item">
                <span class="l">Total Trust Income</span>
                <span class="v text-primary">{{ formatCurrency(totalTrustIncome) }}</span>
              </div>
            </div>
          </div>

          <!-- Optimization Results -->
          <div v-if="results" class="results-section">
            <div class="section-header mb-md">
              <h2>Distribution Plan</h2>
              <div class="savings-tag" v-if="results.estimatedSavings > 0">
                💰 Potential Savings: {{ formatCurrency(results.estimatedSavings) }}
              </div>
            </div>

            <div v-for="trustRes in results.trusts" :key="trustRes.trustId" class="card mb-xl">
              <div class="card-header-flex">
                <h4>{{ trustRes.trustName }}</h4>
                <div class="income-pills">
                  <span class="pill">Franked: {{ formatCurrency(trustRes.frankedDividends) }}</span>
                  <span class="pill">Other: {{ formatCurrency(trustRes.otherIncome) }}</span>
                </div>
              </div>

              <table class="data-table mt-md">
                <thead>
                  <tr>
                    <th>Beneficiary</th>
                    <th class="text-right">Allocation</th>
                    <th class="text-right">FC Credits</th>
                    <th class="text-right">Final Taxable</th>
                    <th class="text-right">Action</th>
                  </tr>
                </thead>
                <tbody>
                  <tr v-for="dist in trustRes.proposedDistributions" :key="dist.beneficiaryId">
                    <td>{{ dist.beneficiaryName }}</td>
                    <td class="text-right font-semibold">{{ formatCurrency(dist.totalAmount) }}</td>
                    <td class="text-right text-success">{{ formatCurrency(dist.frankingCredits) }}</td>
                    <td class="text-right">{{ formatCurrency(dist.taxableIncomeAfterThis) }}</td>
                    <td class="text-right">
                      <button class="btn btn-secondary btn-xs" @click="applyDistribution(trustRes, dist)">Apply</button>
                    </td>
                  </tr>
                </tbody>
              </table>
            </div>
          </div>

          <div v-else class="empty-state card">
            <p>No trusts or eligible beneficiaries found in this portfolio.</p>
            <router-link to="/entities" class="btn btn-primary mt-md">Manage Entities</router-link>
          </div>
        </div>

        <!-- Side Panel: Impact Analysis -->
        <div class="right-col">
          <div class="card impact-card sticky" v-if="results">
            <h3>Tax Impact Analysis</h3>
            
            <div class="impact-item mt-lg">
              <div class="label">Total Family Tax (Current)</div>
              <div class="value">{{ formatCurrency(results.totalTaxCurrent) }}</div>
            </div>
            
            <div class="impact-item">
              <div class="label">Total Family Tax (Optimized)</div>
              <div class="value text-success">{{ formatCurrency(results.totalTaxOptimized) }}</div>
            </div>

            <div class="savings-large mt-xl">
              <div class="savings-label">Net Saving</div>
              <div class="savings-value">{{ formatCurrency(results.estimatedSavings) }}</div>
            </div>

            <div class="explanation mt-xl">
              <p>The optimizer allocates income to beneficiaries with the lowest marginal tax rates first, while prioritizing franking credit utilization to avoid wastage.</p>
            </div>
            
            <button class="btn btn-primary btn-block mt-xl" @click="applyAll" :disabled="applying">
              {{ applying ? 'Applying...' : 'Apply All Resolutions' }}
            </button>
          </div>
        </div>
      </div>
    </template>

    <div v-else class="empty-state card">
      <p>Please create a portfolio first.</p>
    </div>

    <!-- Success Modal -->
    <Teleport to="body">
      <div v-if="success" class="modal-overlay">
        <div class="modal-card success-modal">
          <div class="success-icon">📜</div>
          <h2>Resolutions Recorded</h2>
          <p>The distribution resolutions have been saved to the respective trusts for FY {{ fiscalYear }}.</p>
          <div class="modal-actions">
            <button class="btn btn-primary" @click="success = false">Close</button>
          </div>
        </div>
      </div>
    </Teleport>
  </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue';
import api from '../services/api';

const portfolios = ref([]);
const selectedPortfolioId = ref(null);
const results = ref(null);
const loading = ref(true);
const applying = ref(false);
const success = ref(false);
const fiscalYear = ref(2025);

const formatCurrency = (val) => new Intl.NumberFormat('en-AU', { style: 'currency', currency: 'AUD', maximumFractionDigits: 0 }).format(val || 0);

const totalTrustIncome = computed(() => {
  if (!results.value) return 0;
  return results.value.trusts.reduce((sum, t) => sum + t.frankedDividends + t.otherIncome, 0);
});

const loadData = async () => {
  loading.value = true;
  try {
    const pRes = await api.get('/portfolios');
    portfolios.value = pRes.data;
    if (portfolios.value.length > 0) {
      if (!selectedPortfolioId.value) selectedPortfolioId.value = portfolios.value[0].id;
      
      const optRes = await api.get(`/tax-optimization/trust-distributions/${selectedPortfolioId.value}?fiscalYear=${fiscalYear.value}`);
      results.value = optRes.data;
    }
  } catch (e) {
    console.error('Failed to load tax optimization data', e);
    results.value = null;
  } finally {
    loading.value = false;
  }
};

const applyDistribution = async (trust, dist) => {
    try {
        await api.post(`/trusts/${trust.trustId}/distributions`, {
            beneficiaryId: dist.beneficiaryId,
            fiscalYear: fiscalYear.value,
            frankedDividends: dist.frankedDividends,
            unfrankedIncome: dist.otherIncome,
            frankingCredits: dist.frankingCredits,
            status: 'Resolved'
        });
        alert(`Resolution recorded for ${dist.beneficiaryName}`);
    } catch (e) {
        alert('Failed to apply redistribution');
    }
};

const applyAll = async () => {
    applying.value = true;
    try {
        for (const trust of results.value.trusts) {
            for (const dist of trust.proposedDistributions) {
                if (dist.totalAmount > 0) {
                    await api.post(`/trusts/${trust.trustId}/distributions`, {
                        beneficiaryId: dist.beneficiaryId,
                        fiscalYear: fiscalYear.value,
                        frankedDividends: dist.frankedDividends,
                        unfrankedIncome: dist.otherIncome,
                        frankingCredits: dist.frankingCredits,
                        status: 'Resolved'
                    });
                }
            }
        }
        success.value = true;
    } catch (e) {
        alert('Error applying one or more resolutions.');
    } finally {
        applying.value = false;
    }
};

onMounted(loadData);
</script>

<style scoped>
.content-container { max-width: 1200px; margin: 0 auto; }
.header-row { display: flex; justify-content: space-between; align-items: center; }

.grid-2-1 { display: grid; grid-template-columns: 2fr 1fr; gap: var(--spacing-xl); }

.setup-grid { display: grid; grid-template-columns: repeat(3, 1fr); gap: var(--spacing-md); margin-top: var(--spacing-md); }
.setup-item { display: flex; flex-direction: column; }
.setup-item .l { font-size: 10px; text-transform: uppercase; color: var(--color-text-muted); }
.setup-item .v { font-weight: 700; font-size: var(--font-size-lg); }

.savings-tag { background: rgba(16, 185, 129, 0.1); color: #10b981; padding: 4px 12px; border-radius: 20px; font-weight: 700; font-size: var(--font-size-sm); }

.income-pills { display: flex; gap: var(--spacing-xs); }
.pill { font-size: 10px; background: var(--color-bg-elevated); padding: 2px 8px; border-radius: 4px; border: 1px solid var(--color-border); }

.impact-card { padding: var(--spacing-xl); background: var(--color-bg-card); border: 1px solid var(--color-border); }
.impact-item { display: flex; justify-content: space-between; margin-bottom: var(--spacing-md); }
.impact-item .label { color: var(--color-text-muted); font-size: var(--font-size-sm); }
.impact-item .value { font-weight: 700; }

.savings-large { text-align: center; padding: var(--spacing-lg); background: rgba(16, 185, 129, 0.05); border-radius: var(--radius-lg); border: 1px dashed #10b981; }
.savings-label { font-size: var(--font-size-sm); color: #10b981; text-transform: uppercase; letter-spacing: 0.1em; }
.savings-value { font-size: var(--font-size-3xl); font-weight: 800; color: #10b981; }

.explanation { font-size: var(--font-size-sm); color: var(--color-text-muted); line-height: 1.6; font-style: italic; }

.success-modal { text-align: center; padding: var(--spacing-2xl); }
.success-icon { font-size: 4rem; margin-bottom: var(--spacing-lg); }

.sticky { position: sticky; top: var(--spacing-xl); }
</style>

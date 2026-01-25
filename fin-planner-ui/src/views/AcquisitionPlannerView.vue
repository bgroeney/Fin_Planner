<template>
  <div class="acquisition-planner" :class="{ 'drill-down-mode': isDrillDownMode }">
    <!-- Header -->
    <header class="planner-header">
      <div class="header-content">
        <div class="breadcrumb">
          <router-link to="/properties" class="breadcrumb-link">Properties</router-link>
          <span class="breadcrumb-sep">/</span>
          <template v-if="isDrillDownMode">
            <a href="#" @click.prevent="exitDrillDown" class="breadcrumb-link">Acquisitions</a>
            <span class="breadcrumb-sep">/</span>
            <span class="breadcrumb-current">{{ selectedDeal?.name }}</span>
          </template>
          <span v-else class="breadcrumb-current">Acquisitions</span>
        </div>
        <h1 class="page-title">{{ isDrillDownMode ? selectedDeal?.name : 'Acquisition Planner' }}</h1>
        <p class="page-subtitle">{{ isDrillDownMode ? selectedDeal?.address || 'Cashflow Analysis' : 'Analyze potential deals with uncertainty modeling' }}</p>
      </div>
      <div class="header-actions">
        <button v-if="isDrillDownMode" class="btn btn-secondary" @click="exitDrillDown">
          <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <line x1="19" y1="12" x2="5" y2="12"></line>
            <polyline points="12 19 5 12 12 5"></polyline>
          </svg>
          Back to List
        </button>
        <button v-if="!isDrillDownMode" class="btn btn-primary" @click="openNewDealModal">
          <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <line x1="12" y1="5" x2="12" y2="19"></line>
            <line x1="5" y1="12" x2="19" y2="12"></line>
          </svg>
          New Deal
        </button>
      </div>
    </header>

    <!-- Main Layout - List Mode -->
    <div v-if="!isDrillDownMode" class="planner-layout">
      <!-- Deals Sidebar -->
      <aside class="deals-sidebar card">
        <div class="sidebar-header">
          <h3>Scenarios</h3>
          <select v-model="statusFilter" class="status-filter">
            <option value="">All</option>
            <option value="Draft">Drafts</option>
            <option value="Analyzing">Analyzing</option>
            <option value="Buy">Buy</option>
            <option value="Pass">Pass</option>
            <option value="Uneconomic">Uneconomic</option>
          </select>
        </div>
        <div v-if="loading" class="sidebar-loading">
          <MoneyBoxLoader size="sm" />
        </div>
        <div v-else-if="filteredDeals.length === 0" class="empty-sidebar">
          <p>No deals yet</p>
          <button class="btn btn-secondary btn-sm" @click="openNewDealModal">Create First</button>
        </div>
        <ul v-else class="deals-list">
          <li
            v-for="deal in filteredDeals"
            :key="deal.id"
            class="deal-item"
            @click="enterDrillDown(deal.id)"
          >
            <div class="deal-item-header">
              <span class="deal-name">{{ deal.name }}</span>
              <span class="deal-status" :class="getStatusClass(deal.status)">{{ deal.status }}</span>
            </div>
            <div class="deal-item-meta">
              <span class="deal-price">{{ formatCurrency(deal.askingPrice) }}</span>
              <span class="deal-cap">{{ deal.capRate.toFixed(1) }}% cap</span>
            </div>
          </li>
        </ul>
      </aside>

      <!-- Summary Preview -->
      <main class="preview-area">
        <div class="empty-workbench card">
          <svg width="64" height="64" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5">
            <path d="M3 9l9-7 9 7v11a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2z"></path>
            <polyline points="9 22 9 12 15 12 15 22"></polyline>
          </svg>
          <h3>Select a Deal to Analyze</h3>
          <p>Click a scenario from the list to view detailed cashflow analysis</p>
        </div>
      </main>
    </div>

    <!-- Drill-Down Mode - Full Workbench -->
    <div v-else class="drill-down-workbench">
      <DealWorkbench
        :deal="selectedDeal"
        :simulation-results="simulationResults"
        :running-simulation="runningSimulation"
        @update="handleDealUpdate"
        @run-simulation="handleRunSimulation"
        @record-decision="handleRecordDecision"
      />
    </div>

    <!-- New Deal Modal -->
    <div v-if="showNewDealModal" class="modal-backdrop" @click.self="showNewDealModal = false">
      <div class="modal deal-modal card">
        <div class="modal-header">
          <h2>New Property Deal</h2>
          <button class="btn-close" @click="showNewDealModal = false">×</button>
        </div>
        <form @submit.prevent="createDeal" class="modal-body">
          <div class="form-group">
            <label>Deal Name *</label>
            <input v-model="newDeal.name" type="text" required placeholder="e.g., 123 Industrial Ave - Warehouse" />
          </div>
          <div class="form-row">
            <div class="form-group">
              <label>Address</label>
              <input v-model="newDeal.address" type="text" placeholder="123 Main St, Sydney NSW" />
            </div>
            <div class="form-group">
              <label>Building Type</label>
              <select v-model="newDeal.buildingType">
                <option value="">Select type</option>
                <option value="Office">Office</option>
                <option value="Retail">Retail</option>
                <option value="Industrial">Industrial</option>
                <option value="Mixed">Mixed Use</option>
              </select>
            </div>
          </div>
          <div class="form-row">
            <div class="form-group">
              <label>Asking Price *</label>
              <input v-model.number="newDeal.askingPrice" type="number" min="0" step="any" required placeholder="1500000" />
            </div>
            <div class="form-group">
              <label>Estimated Gross Rent (p.a.) *</label>
              <input v-model.number="newDeal.estimatedGrossRent" type="number" min="0" step="any" required placeholder="120000" />
            </div>
          </div>
          <div class="modal-actions">
            <button type="button" class="btn btn-secondary" @click="showNewDealModal = false">Cancel</button>
            <button type="submit" class="btn btn-primary" :disabled="creatingDeal">
              {{ creatingDeal ? 'Creating...' : 'Create & Analyze' }}
            </button>
          </div>
        </form>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, onMounted, watch } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import api from '../services/api';
import { runSimulation, formatCurrency as formatCurrencyUtil } from '../services/monteCarloEngine';
import MoneyBoxLoader from '../components/MoneyBoxLoader.vue';
import DealWorkbench from '../components/property/DealWorkbench.vue';

const route = useRoute();
const router = useRouter();

// State
const loading = ref(true);
const deals = ref([]);
const selectedDeal = ref(null);
const simulationResults = ref(null);
const statusFilter = ref('');
const showNewDealModal = ref(false);
const creatingDeal = ref(false);
const runningSimulation = ref(false);
const isDrillDownMode = ref(false);

const newDeal = ref({
  name: '',
  address: '',
  buildingType: '',
  askingPrice: 0,
  estimatedGrossRent: 0
});

// Computed
const filteredDeals = computed(() => {
  if (!statusFilter.value) return deals.value;
  return deals.value.filter(d => d.status === statusFilter.value);
});

// Lifecycle
onMounted(async () => {
  await fetchDeals();
  
  // Auto-select deal from URL if present
  const dealId = route.query.deal;
  if (dealId) {
    await selectDeal(dealId);
  }
});

// Methods
async function fetchDeals() {
  try {
    loading.value = true;
    const response = await api.get('/propertydeals');
    deals.value = response.data;
  } catch (error) {
    console.error('Failed to fetch deals:', error);
  } finally {
    loading.value = false;
  }
}

async function selectDeal(dealId) {
  try {
    const response = await api.get(`/propertydeals/${dealId}`);
    selectedDeal.value = response.data;
    simulationResults.value = response.data.simulationResults?.[0] || null;
    
    // Update URL
    router.replace({ query: { deal: dealId } });
  } catch (error) {
    console.error('Failed to load deal:', error);
  }
}

async function enterDrillDown(dealId) {
  await selectDeal(dealId);
  isDrillDownMode.value = true;
}

function exitDrillDown() {
  isDrillDownMode.value = false;
  selectedDeal.value = null;
  router.replace({ query: {} });
}

function openNewDealModal() {
  newDeal.value = {
    name: '',
    address: '',
    buildingType: '',
    askingPrice: 0,
    estimatedGrossRent: 0
  };
  showNewDealModal.value = true;
}

async function createDeal() {
  try {
    creatingDeal.value = true;
    const response = await api.post('/propertydeals', newDeal.value);
    showNewDealModal.value = false;
    await fetchDeals();
    await selectDeal(response.data.id);
  } catch (error) {
    console.error('Failed to create deal:', error);
    alert('Failed to create deal. Please try again.');
  } finally {
    creatingDeal.value = false;
  }
}

async function handleDealUpdate(updates) {
  if (!selectedDeal.value) return;
  
  try {
    const response = await api.put(`/propertydeals/${selectedDeal.value.id}`, updates);
    selectedDeal.value = response.data;
    
    // Refresh deals list for sidebar
    await fetchDeals();
  } catch (error) {
    console.error('Failed to update deal:', error);
  }
}

async function handleRunSimulation() {
  if (!selectedDeal.value) return;
  
  try {
    runningSimulation.value = true;
    
    // Run client-side simulation
    const inputs = {
      askingPrice: selectedDeal.value.askingPrice,
      stampDutyRate: selectedDeal.value.stampDutyRate,
      legalCosts: selectedDeal.value.legalCosts,
      capExReserve: selectedDeal.value.capExReserve,
      estimatedGrossRent: selectedDeal.value.estimatedGrossRent,
      vacancyRatePercent: selectedDeal.value.vacancyRatePercent,
      managementFeePercent: selectedDeal.value.managementFeePercent,
      outgoingsEstimate: selectedDeal.value.outgoingsEstimate,
      loanAmount: selectedDeal.value.loanAmount,
      interestRatePercent: selectedDeal.value.interestRatePercent,
      capitalGrowthPercent: selectedDeal.value.capitalGrowthPercent,
      holdingPeriodYears: selectedDeal.value.holdingPeriodYears,
      rentVariancePercent: selectedDeal.value.rentVariancePercent,
      vacancyVariancePercent: selectedDeal.value.vacancyVariancePercent,
      interestVariancePercent: selectedDeal.value.interestVariancePercent,
      capitalGrowthVariancePercent: selectedDeal.value.capitalGrowthVariancePercent,
      discountRate: selectedDeal.value.discountRate,
      leaseDetails: selectedDeal.value.leaseDetailsJson ? JSON.parse(selectedDeal.value.leaseDetailsJson) : null,
      loanDetails: selectedDeal.value.loanDetailsJson ? JSON.parse(selectedDeal.value.loanDetailsJson) : null
    };
    
    const results = runSimulation(inputs, 5000);
    
    // Update local state immediately for responsiveness
    simulationResults.value = results;
    
    // Save results to backend
    await api.post(`/propertydeals/${selectedDeal.value.id}/simulations`, {
      iterations: results.iterations,
      medianNPV: results.medianNPV,
      p10NPV: results.p10NPV,
      p90NPV: results.p90NPV,
      medianIRR: results.medianIRR,
      p10IRR: results.p10IRR,
      p90IRR: results.p90IRR,
      calculatedCapRate: results.calculatedCapRate,
      recommendedDecision: results.recommendedDecision,
      npvHistogramJson: JSON.stringify(results.npvHistogram),
      irrHistogramJson: JSON.stringify(results.irrHistogram),
      yearlyDCFJson: JSON.stringify(results.yearlyDCF),
      inputsSnapshotJson: JSON.stringify(inputs)
    });
    
    // Refresh deal to get updated status and persistent results
    await selectDeal(selectedDeal.value.id);
    await fetchDeals();
    
  } catch (error) {
    console.error('Simulation failed:', error);
    alert('Simulation failed. Please try again.');
  } finally {
    runningSimulation.value = false;
  }
}

async function handleRecordDecision(decision, rationale) {
  if (!selectedDeal.value) return;
  
  try {
    await api.put(`/propertydeals/${selectedDeal.value.id}/decision`, {
      decision,
      rationale
    });
    
    await selectDeal(selectedDeal.value.id);
    await fetchDeals();
  } catch (error) {
    console.error('Failed to record decision:', error);
    alert('Failed to record decision. Please try again.');
  }
}

function getStatusClass(status) {
  switch (status) {
    case 'Buy': return 'status-buy';
    case 'Pass': return 'status-pass';
    case 'Uneconomic': return 'status-uneconomic';
    case 'Analyzing': return 'status-analyzing';
    default: return 'status-draft';
  }
}

function formatCurrency(value) {
  return formatCurrencyUtil(value);
}
</script>

<style scoped>
.acquisition-planner {
  max-width: 1600px;
  padding: 0 var(--spacing-md);
}

/* Header */
.planner-header {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  margin-bottom: var(--spacing-xl);
  padding-bottom: var(--spacing-lg);
  border-bottom: 3px solid var(--color-industrial-copper);
}

.breadcrumb {
  display: flex;
  align-items: center;
  gap: var(--spacing-xs);
  font-size: var(--font-size-sm);
  margin-bottom: var(--spacing-sm);
}

.breadcrumb-link {
  color: var(--color-text-muted);
  text-decoration: none;
}

.breadcrumb-link:hover {
  color: var(--color-industrial-copper);
}

.breadcrumb-sep {
  color: var(--color-text-muted);
}

.breadcrumb-current {
  color: var(--color-text-primary);
  font-weight: 500;
}

.page-title {
  font-size: var(--font-size-2xl);
  font-weight: 700;
  letter-spacing: -0.02em;
  margin: 0 0 var(--spacing-xs) 0;
  color: var(--color-text-primary);
}

.page-subtitle {
  color: var(--color-text-muted);
  margin: 0;
}

/* Layout */
.planner-layout {
  display: grid;
  grid-template-columns: 280px 1fr;
  gap: var(--spacing-lg);
  min-height: calc(100vh - 200px);
}

@media (max-width: 1000px) {
  .planner-layout {
    grid-template-columns: 1fr;
  }
  
  .deals-sidebar {
    max-height: 300px;
  }
}

/* Sidebar */
.deals-sidebar {
  padding: 0;
  overflow: hidden;
  display: flex;
  flex-direction: column;
}

.sidebar-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: var(--spacing-md);
  border-bottom: 1px solid var(--color-border);
}

.sidebar-header h3 {
  margin: 0;
  font-size: var(--font-size-base);
  font-weight: 600;
}

.status-filter {
  padding: var(--spacing-xs) var(--spacing-sm);
  font-size: var(--font-size-xs);
  border-radius: var(--radius-md);
  border: 1px solid var(--color-border);
  background: var(--color-bg-elevated);
}

.sidebar-loading,
.empty-sidebar {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  padding: var(--spacing-xl);
  text-align: center;
  color: var(--color-text-muted);
  gap: var(--spacing-md);
}

.deals-list {
  list-style: none;
  margin: 0;
  padding: 0;
  overflow-y: auto;
  flex: 1;
}

.deal-item {
  padding: var(--spacing-md);
  border-bottom: 1px solid var(--color-border-subtle);
  cursor: pointer;
  transition: background var(--transition-fast);
}

.deal-item:hover {
  background: var(--color-bg-elevated);
}

.deal-item.active {
  background: rgba(180, 83, 9, 0.08);
  border-left: 3px solid var(--color-industrial-copper);
}

.deal-item-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: var(--spacing-xs);
}

.deal-name {
  font-weight: 500;
  font-size: var(--font-size-sm);
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
  max-width: 150px;
}

.deal-status {
  font-size: 10px;
  font-weight: 600;
  text-transform: uppercase;
  padding: 2px 6px;
  border-radius: var(--radius-full);
}

.status-draft { background: var(--color-bg-elevated); color: var(--color-text-muted); }
.status-analyzing { background: rgba(59, 130, 246, 0.1); color: var(--color-info); }
.status-buy { background: rgba(16, 185, 129, 0.1); color: var(--color-success); }
.status-pass { background: var(--color-bg-elevated); color: var(--color-text-secondary); }
.status-uneconomic { background: rgba(239, 68, 68, 0.1); color: var(--color-danger); }

.deal-item-meta {
  display: flex;
  justify-content: space-between;
  font-size: var(--font-size-xs);
  color: var(--color-text-muted);
}

/* Workbench */
.workbench-area {
  min-height: 500px;
}

.empty-workbench {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  height: 100%;
  text-align: center;
  color: var(--color-text-muted);
  gap: var(--spacing-md);
  padding: var(--spacing-2xl);
}

.empty-workbench svg {
  opacity: 0.4;
}

.empty-workbench h3 {
  margin: 0;
  color: var(--color-text-primary);
}

/* Modal */
.modal-backdrop {
  position: fixed;
  inset: 0;
  background: rgba(0, 0, 0, 0.6);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: var(--z-modal-backdrop);
  backdrop-filter: blur(4px);
}

.deal-modal {
  width: 520px;
  max-width: 90%;
  border: 2px solid var(--color-industrial-copper);
}

.modal-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: var(--spacing-lg);
  border-bottom: 1px solid var(--color-border);
}

.modal-header h2 {
  margin: 0;
  font-size: var(--font-size-lg);
}

.btn-close {
  background: none;
  border: none;
  font-size: 1.5rem;
  cursor: pointer;
  color: var(--color-text-muted);
}

.modal-body {
  padding: var(--spacing-lg);
}

.form-row {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: var(--spacing-md);
}

.modal-actions {
  display: flex;
  justify-content: flex-end;
  gap: var(--spacing-md);
  margin-top: var(--spacing-lg);
  padding-top: var(--spacing-lg);
  border-top: 1px solid var(--color-border);
}

/* Header Actions */
.header-actions {
  display: flex;
  gap: var(--spacing-md);
}

/* Drill-Down Mode */
.drill-down-mode .planner-header {
  margin-bottom: var(--spacing-lg);
}

.drill-down-workbench {
  min-height: calc(100vh - 200px);
}

/* Preview Area (same as workbench area) */
.preview-area {
  min-height: 500px;
}
</style>

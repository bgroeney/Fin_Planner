<template>
  <div class="acquisition-planner">
    <!-- Header -->
    <header class="planner-header">
      <div class="header-content">
        <!-- Breadcrumbs & Nav -->
        <div class="nav-area">
          <div class="breadcrumb">
             <router-link to="/properties" class="breadcrumb-link">Properties</router-link>
             <span class="breadcrumb-sep">/</span>
             <a href="#" v-if="selectedDeal" @click.prevent="clearSelection" class="breadcrumb-link">Acquisitions</a>
             <span class="breadcrumb-current" v-else>Acquisitions</span>
          </div>
          
          <!-- Title / Deal Selector -->
          <div v-if="!selectedDeal" class="page-title-wrapper">
             <h1 class="page-title">Acquisition Planner</h1>
             <p class="page-subtitle">Analyze potential deals with uncertainty modeling</p>
          </div>
          <div v-else class="deal-selector-wrapper" v-click-outside="closeDealDropdown">
             <button class="deal-selector-btn" @click="toggleDealDropdown">
                <span class="deal-selector-name">{{ selectedDeal.name }}</span>
                <svg class="chevron-icon" :class="{ rotated: showDealDropdown }" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><polyline points="6 9 12 15 18 9"></polyline></svg>
             </button>
             
             <!-- Deal Dropdown Menu -->
             <transition name="dropdown-fade">
               <div v-if="showDealDropdown" class="deal-dropdown-menu glass-card">
                  <div class="dropdown-search">
                     <input type="text" v-model="dealSearch" placeholder="Search deals..." class="search-input" ref="searchInput" />
                  </div>
                  <div class="dropdown-list">
                     <div v-if="filteredDropdownDeals.length === 0" class="dropdown-empty">No deals found</div>
                     <button 
                        v-for="deal in filteredDropdownDeals" 
                        :key="deal.id"
                        class="dropdown-item"
                        :class="{ active: deal.id === selectedDeal.id }"
                        @click="switchDeal(deal.id)"
                     >
                        <span class="dropdown-item-name">{{ deal.name }}</span>
                        <span class="dropdown-item-status" :class="getStatusClass(deal.status)">{{ deal.status }}</span>
                     </button>
                  </div>
                  <div class="dropdown-footer">
                     <button class="btn-create-dropdown" @click="openNewDealModalFromDropdown">
                        + New Deal
                     </button>
                  </div>
               </div>
             </transition>
          </div>
        </div>
      </div>

      <div class="header-actions">
        <!-- Actions vary by mode -->
        <template v-if="!selectedDeal">
           <div class="view-toggles">
              <select v-model="statusFilter" class="status-filter-large">
                <option value="">All Statuses</option>
                <option value="Draft">Drafts</option>
                <option value="Analyzing">Analyzing</option>
                <option value="Buy">Buy</option>
                <option value="Pass">Pass</option>
              </select>
           </div>
           <button class="btn btn-primary" @click="openNewDealModal">
            <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <line x1="12" y1="5" x2="12" y2="19"></line>
              <line x1="5" y1="12" x2="19" y2="12"></line>
            </svg>
            New Deal
          </button>
        </template>
        <template v-else>
           <button class="btn btn-secondary" @click="clearSelection">
              Close Deal
           </button>
        </template>
      </div>
    </header>

    <!-- IMMERSIVE LAYOUT -->
    <div class="immersive-content">
      
      <!-- Dashboard Grid (Index View) -->
      <transition name="fade-slide" mode="out-in">
        <div v-if="!selectedDeal" class="dashboard-grid-view">
           <div v-if="loading" class="loading-state">
              <MoneyBoxLoader />
           </div>
           <div v-else-if="filteredDeals.length === 0" class="empty-state-large">
              <div class="empty-content">
                 <div class="empty-icon-circle">
                    <svg width="32" height="32" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5"><path d="M3 9l9-7 9 7v11a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2z"></path><polyline points="9 22 9 12 15 12 15 22"></polyline></svg>
                 </div>
                 <h3>No Deals Found</h3>
                 <p>Start your acquisition pipeline by adding a new property deal.</p>
                 <button class="btn btn-primary mt-4" @click="openNewDealModal">Create First Deal</button>
              </div>
           </div>
           <div v-else class="deals-grid">
              <div 
                v-for="deal in filteredDeals" 
                :key="deal.id" 
                class="deal-card glass-card hover-lift"
                @click="selectDeal(deal.id)"
              >
                  <div class="deal-card-header">
                     <span class="deal-status-badge" :class="getStatusClass(deal.status)">{{ deal.status }}</span>
                     <button class="btn-icon-more" @click.stop>
                        <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><circle cx="12" cy="12" r="1"/><circle cx="19" cy="12" r="1"/><circle cx="5" cy="12" r="1"/></svg>
                     </button>
                  </div>
                  <div class="deal-card-body">
                     <h3 class="deal-card-title">{{ deal.name }}</h3>
                     <p class="deal-card-address">{{ deal.address || 'No address provided' }}</p>
                     
                     <div class="deal-metrics">
                        <div class="metric">
                           <span class="label">Price</span>
                           <span class="value">{{ formatCurrency(deal.askingPrice) }}</span>
                        </div>
                        <div class="metric">
                           <span class="label">Cap Rate</span>
                           <span class="value">{{ deal.calculatedCapRate ? (deal.calculatedCapRate * 100).toFixed(1) + '%' : '-' }}</span>
                        </div>
                     </div>
                  </div>
                  <div class="deal-card-footer">
                     <div v-if="deal.simulationResults && deal.simulationResults.length > 0" class="sim-badge success">
                        Simulated
                     </div>
                     <div v-else class="sim-badge empty">
                        No Analysis
                     </div>
                     <span class="last-updated">Updated today</span>
                  </div>
              </div>
           </div>
        </div>

        <!-- Full Screen Workbench (Detail View) -->
        <div v-else class="workbench-container" key="workbench">
          <DealWorkbench
            :deal="selectedDeal"
            :simulation-results="simulationResults"
            :running-simulation="runningSimulation"
            @update="handleDealUpdate"
            @run-simulation="handleRunSimulation"
            @record-decision="handleRecordDecision"
            @refresh="handleRefresh"
          />
        </div>
      </transition>
    </div>

    <!-- New Deal Modal -->
    <div v-if="showNewDealModal" class="modal-backdrop" @click.self="showNewDealModal = false">
      <div class="modal deal-modal glass-card">
        <div class="modal-header">
          <h2>New Property Deal</h2>
          <button class="btn-close" @click="showNewDealModal = false">×</button>
        </div>
        <form @submit.prevent="createDeal" class="modal-body">
          <div class="form-group">
            <label>Deal Name *</label>
            <input v-model="newDeal.name" type="text" required placeholder="e.g., 123 Industrial Ave - Warehouse" class="input-lg" />
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
import { ref, computed, onMounted, nextTick, watch } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { usePortfolioStore } from '../stores/portfolio';
import api from '../services/api';
import { runSimulation, formatCurrency as formatCurrencyUtil } from '../services/monteCarloEngine';
import MoneyBoxLoader from '../components/MoneyBoxLoader.vue';
import DealWorkbench from '../components/property/DealWorkbench.vue';

// Custom v-click-outside directive (simple local version)
const vClickOutside = {
  mounted(el, binding) {
    el._clickOutsideHandler = (event) => {
      if (!(el === event.target || el.contains(event.target))) {
        binding.value(event);
      }
    };
    document.body.addEventListener('click', el._clickOutsideHandler);
  },
  unmounted(el) {
    document.body.removeEventListener('click', el._clickOutsideHandler);
  }
};

const route = useRoute();
const useRouterInstance = useRouter();
const portfolioStore = usePortfolioStore();

// State
const loading = ref(true);
const deals = ref([]);
const selectedDeal = ref(null);
const simulationResults = ref(null);
const statusFilter = ref('');
const showNewDealModal = ref(false);
const creatingDeal = ref(false);
const runningSimulation = ref(false);

const currentPortfolioId = computed(() => portfolioStore.currentPortfolioId);

// Header State
const showDealDropdown = ref(false);
const dealSearch = ref('');
const searchInput = ref(null);

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

const filteredDropdownDeals = computed(() => {
  let list = deals.value;
  if (dealSearch.value) {
    const q = dealSearch.value.toLowerCase();
    list = list.filter(d => d.name.toLowerCase().includes(q) || (d.address && d.address.toLowerCase().includes(q)));
  }
  return list;
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
  if (!currentPortfolioId.value) {
    deals.value = [];
    loading.value = false;
    return;
  }
  try {
    loading.value = true;
    const response = await api.get(`/propertydeals?portfolioId=${currentPortfolioId.value}`);
    deals.value = response.data;
  } catch (error) {
    console.error('Failed to fetch deals:', error);
  } finally {
    loading.value = false;
  }
}

async function selectDeal(dealId, force = false) {
  try {
    // Optimistic active state update if already loaded
    if (deals.value.length > 0 && !force) {
       // Just ensure we don't flash empty if we click the same one
       if (selectedDeal.value?.id === dealId) return; 
    }

    const response = await api.get(`/propertydeals/${dealId}`);
    selectedDeal.value = response.data;
    
    // Check if we have simulation results populated on the read model
    // Assuming backend populates basic simulation results or we fetch them separately
    // If your backend isn't sending simulationResults array, we might need a separate call
    // or rely on what's in the deal object if it's there.
    // For now assuming it is or we tolerate null.
    simulationResults.value = response.data.simulationResults?.[0] || null;
    
    // Update URL
    useRouterInstance.replace({ query: { deal: dealId } });
    showDealDropdown.value = false;
  } catch (error) {
    console.error('Failed to load deal:', error);
  }
}

function clearSelection() {
   selectedDeal.value = null;
   useRouterInstance.replace({ query: {} });
   showDealDropdown.value = false;
   fetchDeals(); // Refresh list to ensure latest data
}

function switchDeal(dealId) {
   selectDeal(dealId);
}

function toggleDealDropdown() {
   showDealDropdown.value = !showDealDropdown.value;
   if (showDealDropdown.value) {
      nextTick(() => {
         searchInput.value?.focus();
      });
   }
}

function closeDealDropdown() {
   showDealDropdown.value = false;
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

function openNewDealModalFromDropdown() {
   showDealDropdown.value = false;
   openNewDealModal();
}

async function createDeal() {
  try {
    creatingDeal.value = true;
    const response = await api.post('/propertydeals', {
      ...newDeal.value,
      portfolioId: currentPortfolioId.value
    });
    showNewDealModal.value = false;
    await fetchDeals(); // Refresh list
    await selectDeal(response.data.id); // Select new deal
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
    
    // Update item in local list list to reflect changes (e.g. name, status)
    const index = deals.value.findIndex(d => d.id === selectedDeal.value.id);
    if (index !== -1) {
      deals.value[index] = { ...deals.value[index], ...response.data };
    }
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
    
    // Refresh deal
    const updated = await api.get(`/propertydeals/${selectedDeal.value.id}`);
     selectedDeal.value = updated.data;
     
     // Update list
    const index = deals.value.findIndex(d => d.id === selectedDeal.value.id);
    if (index !== -1) {
       deals.value[index] = { ...updated.data };
    }
    
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
    
    // Refresh
    const updated = await api.get(`/propertydeals/${selectedDeal.value.id}`);
    selectedDeal.value = updated.data;
     
     // Update list
    const index = deals.value.findIndex(d => d.id === selectedDeal.value.id);
    if (index !== -1) {
       deals.value[index].status = decision;
    }
  } catch (error) {
    console.error('Failed to record decision:', error);
    alert('Failed to record decision. Please try again.');
  }
}

async function handleRefresh() {
  if (selectedDeal.value) {
    await selectDeal(selectedDeal.value.id, true);
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

// Watch for portfolio changes
watch(currentPortfolioId, () => {
  fetchDeals();
});
</script>

<style scoped>
.acquisition-planner {
  max-width: 1800px;
  margin: 0 auto;
  padding: 0 var(--spacing-md);
  height: calc(100vh - 80px); /* Fill remaining height */
  display: flex;
  flex-direction: column;
}

/* Header */
.planner-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: var(--spacing-lg);
  padding: var(--spacing-md) 0;
  border-bottom: 3px solid var(--color-industrial-copper);
  flex-shrink: 0;
}

.nav-area {
   display: flex;
   flex-direction: column;
   gap: 4px;
}

.breadcrumb {
  display: flex;
  align-items: center;
  gap: var(--spacing-xs);
  font-size: var(--font-size-sm);
  color: var(--color-text-muted);
}

.breadcrumb-link {
  color: var(--color-text-muted);
  text-decoration: none;
}

.breadcrumb-link:hover {
  color: var(--color-industrial-copper);
}

.breadcrumb-start {
   font-weight: 500;
}

.page-title {
  font-size: var(--font-size-2xl);
  font-weight: 700;
  letter-spacing: -0.02em;
  margin: 0;
  color: var(--color-text-primary);
  line-height: 1.2;
}

.page-subtitle {
  color: var(--color-text-muted);
  margin: 0;
  font-size: var(--font-size-sm);
}

/* Deal Selector */
.deal-selector-wrapper {
   position: relative;
   display: inline-block;
}

.deal-selector-btn {
   display: flex;
   align-items: center;
   gap: 8px;
   background: none;
   border: none;
   padding: 0;
   cursor: pointer;
   color: var(--color-text-primary);
}

.deal-selector-name {
   font-size: var(--font-size-2xl);
   font-weight: 700;
   letter-spacing: -0.02em;
}

.chevron-icon {
   color: var(--color-text-muted);
   transition: transform 0.2s;
}

.chevron-icon.rotated {
   transform: rotate(180deg);
}

/* Dropdown */
.deal-dropdown-menu {
   position: absolute;
   top: 100%;
   left: 0;
   width: 300px;
   margin-top: 8px;
   background: var(--glass-bg);
   backdrop-filter: blur(12px);
   border: 1px solid var(--glass-border);
   border-radius: var(--radius-lg);
   box-shadow: var(--shadow-xl);
   z-index: 100;
   display: flex;
   flex-direction: column;
   max-height: 400px;
}

.dropdown-search {
   padding: 12px;
   border-bottom: 1px solid var(--glass-border);
}

.search-input {
   width: 100%;
   padding: 8px 12px;
   border: 1px solid var(--color-border);
   border-radius: var(--radius-md);
   background: var(--color-bg-elevated);
   font-size: 13px;
   color: var(--color-text-primary);
}

.dropdown-list {
   overflow-y: auto;
   padding: 4px;
   flex: 1;
}

.dropdown-item {
   width: 100%;
   text-align: left;
   padding: 10px 12px;
   display: flex;
   justify-content: space-between;
   align-items: center;
   background: none;
   border: none;
   border-radius: var(--radius-md);
   cursor: pointer;
   transition: background 0.1s;
}

.dropdown-item:hover, .dropdown-item.active {
   background: var(--color-bg-elevated);
}

.dropdown-item.active {
   color: var(--color-accent);
}

.dropdown-item-name {
   font-weight: 500;
   font-size: 13px;
   white-space: nowrap;
   overflow: hidden;
   text-overflow: ellipsis;
   max-width: 180px;
}

.dropdown-item-status {
   font-size: 10px;
   text-transform: uppercase;
   font-weight: 700;
   padding: 2px 6px;
   border-radius: 99px;
}

.dropdown-footer {
   padding: 8px;
   border-top: 1px solid var(--glass-border);
}

.btn-create-dropdown {
   width: 100%;
   padding: 8px;
   background: var(--color-bg-elevated);
   border: 1px dashed var(--color-border);
   border-radius: var(--radius-md);
   color: var(--color-text-muted);
   font-size: 12px;
   cursor: pointer;
}

.btn-create-dropdown:hover {
   color: var(--color-accent);
   border-color: var(--color-accent);
}

/* Immersive Content */
.immersive-content {
   flex: 1;
   display: flex;
   flex-direction: column;
   overflow: hidden;
   position: relative;
}

.dashboard-grid-view {
   height: 100%;
   overflow-y: auto;
   padding-bottom: var(--spacing-xl);
}

.deals-grid {
   display: grid;
   grid-template-columns: repeat(auto-fill, minmax(300px, 1fr));
   gap: var(--spacing-lg);
}

/* Deal Card */
.deal-card {
   border-radius: var(--radius-lg);
   padding: var(--spacing-lg);
   cursor: pointer;
   transition: all 0.3s cubic-bezier(0.25, 0.8, 0.25, 1);
   border: 1px solid var(--glass-border);
   display: flex;
   flex-direction: column;
   height: 240px;
}

.deal-card:hover {
   transform: translateY(-4px);
   box-shadow: var(--shadow-xl), 0 0 0 1px var(--color-accent);
}

.deal-card-header {
   display: flex;
   justify-content: space-between;
   margin-bottom: var(--spacing-md);
}

.deal-status-badge {
    font-size: 11px;
    font-weight: 700;
    text-transform: uppercase;
    padding: 4px 8px;
    border-radius: 4px;
}

.btn-icon-more {
   background: none;
   border: none;
   color: var(--color-text-muted);
   cursor: pointer;
   padding: 0;
}

.deal-card-title {
   font-size: var(--font-size-lg);
   font-weight: 700;
   margin: 0 0 4px 0;
   line-height: 1.3;
   display: -webkit-box;
   -webkit-line-clamp: 2;
   -webkit-box-orient: vertical;
   overflow: hidden;
}

.deal-card-address {
   color: var(--color-text-muted);
   font-size: var(--font-size-sm);
   margin-bottom: var(--spacing-lg);
   white-space: nowrap;
   overflow: hidden;
   text-overflow: ellipsis;
}

.deal-metrics {
   margin-top: auto;
   display: flex;
   justify-content: space-between;
   padding-top: var(--spacing-md);
   border-top: 1px solid var(--color-border-subtle);
}

.metric {
   display: flex;
   flex-direction: column;
}

.metric .label {
   font-size: 10px;
   text-transform: uppercase;
   color: var(--color-text-muted);
   font-weight: 600;
}

.metric .value {
   font-weight: 700;
   font-family: var(--font-mono);
   font-size: 14px;
}

.deal-card-footer {
   margin-top: 12px;
   display: flex;
   justify-content: space-between;
   align-items: center;
}

.sim-badge {
   font-size: 10px;
   font-weight: 600;
   display: flex;
   align-items: center;
   gap: 4px;
}

.sim-badge.success { color: var(--color-success); }
.sim-badge.success::before {
   content: '';
   display: block;
   width: 6px;
   height: 6px;
   border-radius: 50%;
   background: var(--color-success);
}

.sim-badge.empty { color: var(--color-text-muted); opacity: 0.7; }

.last-updated {
   font-size: 10px;
   color: var(--color-text-muted);
}

/* Status Colors */
.status-draft { background: var(--color-bg-elevated); color: var(--color-text-muted); }
.status-analyzing { background: rgba(59, 130, 246, 0.1); color: var(--color-info); }
.status-buy { background: rgba(16, 185, 129, 0.1); color: var(--color-success); }
.status-pass { background: var(--color-bg-elevated); color: var(--color-text-secondary); }
.status-uneconomic { background: rgba(239, 68, 68, 0.1); color: var(--color-danger); }

/* Transitions */
.dropdown-fade-enter-active, .dropdown-fade-leave-active {
   transition: all 0.2s ease;
}
.dropdown-fade-enter-from, .dropdown-fade-leave-to {
   opacity: 0;
   transform: translateY(-8px);
}

.fade-slide-enter-active, .fade-slide-leave-active {
  transition: opacity 0.3s ease, transform 0.3s ease;
}

.fade-slide-enter-from {
  opacity: 0;
  transform: translateY(10px);
}
.fade-slide-leave-to {
  opacity: 0;
  transform: translateY(-10px);
}

/* Workbench Container */
.workbench-container {
   height: 100%;
   width: 100%;
   overflow-y: auto;
}

/* Modal and inputs (reused from previous) */
.deal-modal {
  width: 520px;
}

.input-lg {
   font-size: 1.1rem;
   padding: 12px;
}

.btn-close {
   background: none;
   border: none;
   font-size: 24px;
   cursor: pointer;
   color: var(--color-text-muted);
}

.empty-state-large {
   display: flex;
   align-items: center;
   justify-content: center;
   height: 60vh;
}

.empty-content {
   text-align: center;
   max-width: 400px;
}

.empty-icon-circle {
   width: 80px;
   height: 80px;
   background: var(--color-bg-elevated);
   border-radius: 50%;
   display: flex;
   align-items: center;
   justify-content: center;
   margin: 0 auto 24px;
   color: var(--color-industrial-copper);
}

.status-filter-large {
   padding: 8px 16px;
   border-radius: var(--radius-md);
   border: 1px solid var(--color-border);
   background: var(--glass-bg);
   font-size: 14px;
   cursor: pointer;
}
</style>

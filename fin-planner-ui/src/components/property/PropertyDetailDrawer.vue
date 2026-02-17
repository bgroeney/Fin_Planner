<template>
  <div class="drawer-backdrop" @click.self="$emit('close')">
    <aside class="drawer animate-slide-in">
      <div class="drawer-header">
        <div class="header-content">
          <h2 class="drawer-title">{{ property?.address || 'Loading...' }}</h2>
          <span class="type-badge" v-if="property">{{ property.buildingType }}</span>
        </div>
        <button class="btn-close" @click="$emit('close')">
          <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <line x1="18" y1="6" x2="6" y2="18"></line>
            <line x1="6" y1="6" x2="18" y2="18"></line>
          </svg>
        </button>
      </div>

      <div v-if="loading" class="drawer-loading">
        <div class="spinner"></div>
        <span>Loading property details...</span>
      </div>

      <template v-else-if="property">
        <!-- Tabs -->
        <div class="tabs">
          <button 
            v-for="tab in tabs" 
            :key="tab.id"
            class="tab" 
            :class="{ active: activeTab === tab.id }"
            @click="activeTab = tab.id"
          >
            {{ tab.label }}
          </button>
        </div>

        <!-- Tab Content -->
        <div class="drawer-content">
          <!-- Overview Tab -->
          <div v-if="activeTab === 'overview'" class="tab-content">
            <div class="overview-grid">
              <div class="overview-card">
                <span class="overview-label">Current Value</span>
                <span class="overview-value value-large">{{ formatCurrency(latestValuation) }}</span>
              </div>
              <div class="overview-card">
                <span class="overview-label">Purchase Price</span>
                <span class="overview-value">{{ formatCurrency(property.purchasePrice) }}</span>
              </div>
              <div class="overview-card">
                <span class="overview-label">Capital Growth</span>
                <span class="overview-value" :class="capitalGrowth >= 0 ? 'value-positive' : 'value-negative'">
                  {{ capitalGrowth >= 0 ? '+' : '' }}{{ formatCurrency(capitalGrowth) }}
                </span>
              </div>
              <div class="overview-card">
                <span class="overview-label">Total GFA</span>
                <span class="overview-value">{{ property.totalGFA?.toLocaleString() || '—' }} m²</span>
              </div>
              <div class="overview-card full-width">
                <span class="overview-label">Title Reference</span>
                <span class="overview-value">{{ property.titleReference || '—' }}</span>
              </div>
              <div class="overview-card full-width" v-if="property.description">
                <span class="overview-label">Description</span>
                <span class="overview-value text-muted">{{ property.description }}</span>
              </div>
            </div>

            <!-- Valuation History -->
            <div class="section">
              <div class="section-header">
                <h3>Valuation History</h3>
                <button class="btn btn-secondary btn-sm" @click="showAddValuationForm = !showAddValuationForm">
                  {{ showAddValuationForm ? 'Cancel' : '+ Add Valuation' }}
                </button>
              </div>

              <form v-if="showAddValuationForm" class="inline-form" @submit.prevent="addValuation">
                <div class="form-row">
                  <input v-model="newValuation.date" type="date" required />
                  <input v-model.number="newValuation.value" type="number" placeholder="Value" required />
                  <select v-model="newValuation.source" required>
                    <option value="">Source</option>
                    <option value="Agent">Agent</option>
                    <option value="External">External</option>
                    <option value="Internal">Internal</option>
                  </select>
                  <button type="submit" class="btn btn-primary btn-sm" :disabled="addingValuation">Add</button>
                </div>
              </form>

              <div class="valuations-list">
                <div v-for="val in valuations" :key="val.id" class="valuation-item">
                  <div class="valuation-date">{{ formatDate(val.date) }}</div>
                  <div class="valuation-value">{{ formatCurrency(val.value) }}</div>
                  <div class="valuation-source badge">{{ val.source }}</div>
                </div>
                <div v-if="valuations.length === 0" class="empty-state-small">
                  No valuations recorded
                </div>
              </div>
            </div>
          </div>

          <!-- Risk Analysis Tab -->
          <div v-if="activeTab === 'risk'" class="tab-content">
            <div class="risk-intro">
              <p>Run a Monte Carlo simulation on this property to understand its risk profile over time.</p>
            </div>
            <PropertyRiskAnalyzer 
              :initial-data="propertyInputs"
              :auto-run="false"
            />
          </div>

          <!-- Leases Tab -->
          <div v-if="activeTab === 'leases'" class="tab-content">
            <div class="section-header">
              <h3>Lease Profiles</h3>
              <button class="btn btn-secondary btn-sm" @click="showAddLeaseForm = !showAddLeaseForm">
                {{ showAddLeaseForm ? 'Cancel' : '+ Add Lease' }}
              </button>
            </div>

            <form v-if="showAddLeaseForm" class="lease-form" @submit.prevent="addLease">
              <div class="form-group">
                <label>Tenant Name *</label>
                <input v-model="newLease.tenantName" type="text" required />
              </div>
              <div class="form-row-2">
                <div class="form-group">
                  <label>Lease Start *</label>
                  <input v-model="newLease.leaseStart" type="date" required />
                </div>
                <div class="form-group">
                  <label>Lease End *</label>
                  <input v-model="newLease.leaseEnd" type="date" required />
                </div>
              </div>
              <div class="form-row-2">
                <div class="form-group">
                  <label>Current Rent (p.a.) *</label>
                  <input v-model.number="newLease.currentRent" type="number" required />
                </div>
                <div class="form-group">
                  <label>Review Type *</label>
                  <select v-model="newLease.reviewType" required>
                    <option value="">Select</option>
                    <option value="Fixed">Fixed</option>
                    <option value="CPI">CPI</option>
                    <option value="Market">Market</option>
                  </select>
                </div>
              </div>
              <div class="form-row-2">
                <div class="form-group">
                  <label>Option Period</label>
                  <input v-model="newLease.optionPeriod" type="text" placeholder="e.g., 5+5" />
                </div>
                <div class="form-group">
                  <label>Unit Reference</label>
                  <input v-model="newLease.unitReference" type="text" placeholder="e.g., Ground Floor" />
                </div>
              </div>
              <button type="submit" class="btn btn-primary" :disabled="addingLease">
                {{ addingLease ? 'Adding...' : 'Add Lease' }}
              </button>
            </form>

            <div class="leases-list">
              <div v-for="lease in leases" :key="lease.id" class="lease-item" :class="{ inactive: !lease.isActive }">
                <div class="lease-header">
                  <span class="lease-tenant">{{ lease.tenantName }}</span>
                  <span class="lease-status" :class="{ active: lease.isActive }">
                    {{ lease.isActive ? 'Active' : 'Inactive' }}
                  </span>
                </div>
                <div class="lease-details">
                  <div class="lease-detail">
                    <span class="detail-label">Term</span>
                    <span>{{ formatDate(lease.leaseStart) }} — {{ formatDate(lease.leaseEnd) }}</span>
                  </div>
                  <div class="lease-detail">
                    <span class="detail-label">Rent</span>
                    <span>{{ formatCurrency(lease.currentRent) }} p.a.</span>
                  </div>
                  <div class="lease-detail">
                    <span class="detail-label">Review</span>
                    <span>{{ lease.reviewType }} {{ lease.reviewPercentage ? `(${lease.reviewPercentage}%)` : '' }}</span>
                  </div>
                </div>
              </div>
              <div v-if="leases.length === 0" class="empty-state-small">
                No leases recorded
              </div>
            </div>
          </div>

          <!-- Ledger Tab -->
          <div v-if="activeTab === 'ledger'" class="tab-content">
            <div class="section-header">
              <h3>Transaction Ledger</h3>
              <button class="btn btn-secondary btn-sm" @click="showAddLedgerForm = !showAddLedgerForm">
                {{ showAddLedgerForm ? 'Cancel' : '+ Add Entry' }}
              </button>
            </div>

            <form v-if="showAddLedgerForm" class="inline-form" @submit.prevent="addLedgerEntry">
              <div class="form-row-ledger">
                <input v-model="newLedger.date" type="date" required />
                <select v-model="newLedger.type" required>
                  <option value="">Type</option>
                  <option value="Rent_Gross">Rent (Gross)</option>
                  <option value="Outgoing_Recoverable">Outgoings Recoverable</option>
                  <option value="Council_Rates">Council Rates</option>
                  <option value="Water_Rates">Water Rates</option>
                  <option value="Insurance">Insurance</option>
                  <option value="Management_Fee">Management Fee</option>
                  <option value="Repairs_CapEx">Repairs/CapEx</option>
                  <option value="Other_Income">Other Income</option>
                  <option value="Other_Expense">Other Expense</option>
                </select>
                <input v-model.number="newLedger.amount" type="number" placeholder="Amount" required />
                <label class="checkbox-label">
                  <input type="checkbox" v-model="newLedger.isIncome" />
                  Income
                </label>
                <button type="submit" class="btn btn-primary btn-sm" :disabled="addingLedger">Add</button>
              </div>
            </form>

            <div class="ledger-list">
              <div v-for="entry in ledgerEntries" :key="entry.id" class="ledger-item">
                <div class="ledger-date">{{ formatDate(entry.date) }}</div>
                <div class="ledger-type">{{ formatLedgerType(entry.type) }}</div>
                <div class="ledger-amount" :class="entry.isIncome ? 'value-positive' : 'value-negative'">
                  {{ entry.isIncome ? '+' : '-' }}{{ formatCurrency(entry.amount) }}
                </div>
              </div>
              <div v-if="ledgerEntries.length === 0" class="empty-state-small">
                No ledger entries
              </div>
            </div>
          </div>
        </div>
      </template>
    </aside>
  </div>
</template>

<script setup>
import { ref, computed, onMounted, watch } from 'vue';
import api from '../../services/api';
import PropertyRiskAnalyzer from './PropertyRiskAnalyzer.vue';

const props = defineProps({
  propertyId: {
    type: String,
    required: true
  }
});

const emit = defineEmits(['close', 'updated']);

const loading = ref(true);
const property = ref(null);
const valuations = ref([]);
const leases = ref([]);
const ledgerEntries = ref([]);
const activeTab = ref('overview');

const tabs = [
  { id: 'overview', label: 'Overview' },
  { id: 'risk', label: 'Risk Analysis' },
  { id: 'leases', label: 'Leases' },
  { id: 'ledger', label: 'Ledger' }
];

// Form states
const showAddValuationForm = ref(false);
const showAddLeaseForm = ref(false);
const showAddLedgerForm = ref(false);

const addingValuation = ref(false);
const addingLease = ref(false);
const addingLedger = ref(false);

const newValuation = ref({
  date: new Date().toISOString().split('T')[0],
  value: 0,
  source: ''
});

const newLease = ref({
  tenantName: '',
  leaseStart: '',
  leaseEnd: '',
  currentRent: 0,
  reviewType: '',
  optionPeriod: '',
  unitReference: ''
});

const newLedger = ref({
  date: new Date().toISOString().split('T')[0],
  type: '',
  amount: 0,
  isIncome: false
});

const latestValuation = computed(() => {
  if (valuations.value.length > 0) {
    return valuations.value[0].value;
  }
  return property.value?.purchasePrice || 0;
});

const capitalGrowth = computed(() => {
  return latestValuation.value - (property.value?.purchasePrice || 0);
});

// Computed inputs for PropertyRiskAnalyzer
// Maps CommercialProperty fields to the expected simulation input format
const propertyInputs = computed(() => {
  if (!property.value) return {};
  const p = property.value;
  const totalRent = leases.value
      .filter(l => l.isActive)
      .reduce((sum, l) => sum + (l.currentRent || 0), 0);
  
  return {
    propertyValue: latestValuation.value,
    currentValue: latestValuation.value,
    askingPrice: latestValuation.value, // For simulation compatibility
    estimatedGrossRent: totalRent,
    annualRent: totalRent,
    outgoingsEstimate: 0, // TODO: Calculate from ledger if available
    vacancyRatePercent: 5,
    capitalGrowthPercent: 3,
    loanAmount: 0, // TODO: Fetch from linked accounts if available
    interestRatePercent: 6.5,
    holdingPeriodYears: 10,
    discountRate: 8
  };
});

watch(() => props.propertyId, (newId) => {
  if (newId) loadProperty();
}, { immediate: true });

async function loadProperty() {
  try {
    loading.value = true;
    const response = await api.get(`/commercialproperty/${props.propertyId}`);
    property.value = response.data;
    valuations.value = response.data.valuations || [];
    leases.value = response.data.leases || [];
    ledgerEntries.value = response.data.ledgerEntries || [];
  } catch (error) {
    console.error('Failed to load property:', error);
  } finally {
    loading.value = false;
  }
}

async function addValuation() {
  try {
    addingValuation.value = true;
    await api.post(`/commercialproperty/${props.propertyId}/valuations`, newValuation.value);
    await loadProperty();
    showAddValuationForm.value = false;
    newValuation.value = { date: new Date().toISOString().split('T')[0], value: 0, source: '' };
    emit('updated');
  } catch (error) {
    console.error('Failed to add valuation:', error);
  } finally {
    addingValuation.value = false;
  }
}

async function addLease() {
  try {
    addingLease.value = true;
    await api.post(`/commercialproperty/${props.propertyId}/leases`, newLease.value);
    await loadProperty();
    showAddLeaseForm.value = false;
    newLease.value = { tenantName: '', leaseStart: '', leaseEnd: '', currentRent: 0, reviewType: '', optionPeriod: '', unitReference: '' };
    emit('updated');
  } catch (error) {
    console.error('Failed to add lease:', error);
  } finally {
    addingLease.value = false;
  }
}

async function addLedgerEntry() {
  try {
    addingLedger.value = true;
    await api.post(`/commercialproperty/${props.propertyId}/ledger`, newLedger.value);
    await loadProperty();
    showAddLedgerForm.value = false;
    newLedger.value = { date: new Date().toISOString().split('T')[0], type: '', amount: 0, isIncome: false };
    emit('updated');
  } catch (error) {
    console.error('Failed to add ledger entry:', error);
  } finally {
    addingLedger.value = false;
  }
}

function formatCurrency(value) {
  return new Intl.NumberFormat('en-AU', {
    style: 'currency',
    currency: 'AUD',
    minimumFractionDigits: 0,
    maximumFractionDigits: 0
  }).format(value || 0);
}

function formatDate(date) {
  return new Date(date).toLocaleDateString('en-AU', {
    day: 'numeric',
    month: 'short',
    year: 'numeric'
  });
}

function formatLedgerType(type) {
  return type.replace(/_/g, ' ').replace(/([A-Z])/g, ' $1').trim();
}
</script>



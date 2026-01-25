<template>
  <div class="property-hub animate-fade-in">
    <div v-if="loading" class="page-loader-container">
      <MoneyBoxLoader size="lg" text="Loading Property Portfolio..." />
    </div>

    <template v-else>
      <!-- Header Section -->
      <section class="hub-header">
        <div class="header-content">
          <h1 class="hub-title">Property Portfolio</h1>
          <p class="hub-subtitle">Commercial real estate holdings overview</p>
        </div>
        <div class="header-actions">
          <router-link to="/acquisitions" class="btn btn-secondary">
            <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <circle cx="11" cy="11" r="8"></circle>
              <line x1="21" y1="21" x2="16.65" y2="16.65"></line>
            </svg>
            Acquisitions
          </router-link>
          <button class="btn btn-primary" @click="showAddPropertyModal = true">
            <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <line x1="12" y1="5" x2="12" y2="19"></line>
              <line x1="5" y1="12" x2="19" y2="12"></line>
            </svg>
            Add Property
          </button>
        </div>
      </section>

      <!-- Stats Overview -->
      <section class="stats-grid">
        <div class="stat-card stat-primary animate-fade-in-up stagger-1">
          <div class="stat-icon">
            <svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <path d="M3 9l9-7 9 7v11a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2z"></path>
              <polyline points="9 22 9 12 15 12 15 22"></polyline>
            </svg>
          </div>
          <div class="stat-content">
            <span class="stat-label">Total Portfolio Value</span>
            <span class="stat-value value-large">{{ formatCurrency(dashboard.totalValue) }}</span>
            <span class="stat-detail" :class="{ 'value-positive': capitalGain >= 0, 'value-negative': capitalGain < 0 }">
              {{ capitalGain >= 0 ? '+' : '' }}{{ formatCurrency(capitalGain) }} from purchase
            </span>
          </div>
        </div>

        <div class="stat-card animate-fade-in-up stagger-2">
          <div class="stat-icon stat-icon-yield">
            <svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <circle cx="12" cy="12" r="10"></circle>
              <polyline points="12 6 12 12 16 14"></polyline>
            </svg>
          </div>
          <div class="stat-content">
            <span class="stat-label">Net Yield</span>
            <span class="stat-value" :class="yieldClass">{{ dashboard.netYieldPercent.toFixed(2) }}%</span>
            <span class="stat-detail text-muted">{{ formatCurrency(dashboard.netIncome) }} p.a.</span>
          </div>
        </div>

        <div class="stat-card animate-fade-in-up stagger-3">
          <div class="stat-icon stat-icon-leases">
            <svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z"></path>
              <polyline points="14 2 14 8 20 8"></polyline>
              <line x1="16" y1="13" x2="8" y2="13"></line>
              <line x1="16" y1="17" x2="8" y2="17"></line>
            </svg>
          </div>
          <div class="stat-content">
            <span class="stat-label">Active Leases</span>
            <span class="stat-value">{{ dashboard.activeLeases }}</span>
            <span class="stat-detail" :class="{ 'value-warning': dashboard.expiringLeases > 0 }">
              {{ dashboard.expiringLeases }} expiring in 12mo
            </span>
          </div>
        </div>

        <div class="stat-card animate-fade-in-up stagger-4">
          <div class="stat-icon stat-icon-properties">
            <svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <rect x="3" y="3" width="18" height="18" rx="2" ry="2"></rect>
              <line x1="3" y1="9" x2="21" y2="9"></line>
              <line x1="9" y1="21" x2="9" y2="9"></line>
            </svg>
          </div>
          <div class="stat-content">
            <span class="stat-label">Properties</span>
            <span class="stat-value">{{ dashboard.totalProperties }}</span>
            <span class="stat-detail text-muted">
              {{ formatCurrency(dashboard.grossRentalIncome) }} gross income
            </span>
          </div>
        </div>
      </section>

      <!-- Bento Grid Dashboard -->
      <section class="bento-grid">
        <!-- Yield Gauge - Large Left -->
        <div class="bento-card bento-yield card animate-fade-in-up stagger-1">
          <div class="card-header">
            <h3>Net Yield Performance</h3>
            <span class="badge badge-info">Last 12 Months</span>
          </div>
          <YieldGauge 
            :yield-percent="dashboard.netYieldPercent" 
            :target-yield="6.0"
            :gross-income="dashboard.grossRentalIncome"
            :expenses="dashboard.totalExpenses"
          />
        </div>

        <!-- Property Cards - Right Column -->
        <div class="bento-card bento-properties card animate-fade-in-up stagger-2">
          <div class="card-header">
            <h3>Holdings</h3>
            <button class="btn btn-ghost btn-sm" @click="togglePropertyView">
              {{ propertyViewMode === 'cards' ? 'List' : 'Cards' }}
            </button>
          </div>
          <div class="properties-container" :class="propertyViewMode">
            <PropertyCard
              v-for="property in dashboard.properties"
              :key="property.id"
              :property="property"
              @click="openPropertyDetail(property)"
            />
            <div v-if="dashboard.properties.length === 0" class="empty-state">
              <svg width="48" height="48" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5">
                <path d="M3 9l9-7 9 7v11a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2z"></path>
                <polyline points="9 22 9 12 15 12 15 22"></polyline>
              </svg>
              <p>No properties yet</p>
              <button class="btn btn-secondary btn-sm" @click="showAddPropertyModal = true">Add your first property</button>
            </div>
          </div>
        </div>

        <!-- Vacancy Heatmap - Bottom Left -->
        <div class="bento-card bento-vacancy card animate-fade-in-up stagger-3">
          <div class="card-header">
            <h3>Lease Expiry Timeline</h3>
            <span class="badge badge-warning" v-if="dashboard.expiringLeases > 0">
              {{ dashboard.expiringLeases }} at risk
            </span>
          </div>
          <VacancyHeatmap :data="dashboard.vacancyHeatmap" />
        </div>

        <!-- Cashflow Forecast - Bottom Right -->
        <div class="bento-card bento-cashflow card animate-fade-in-up stagger-4">
          <div class="card-header">
            <h3>12-Month Cashflow Forecast</h3>
          </div>
          <CashflowForecast :data="dashboard.cashflowForecast" />
        </div>
      </section>
    </template>

    <!-- Property Detail Drawer -->
    <PropertyDetailDrawer 
      v-if="selectedProperty"
      :property-id="selectedProperty.id"
      @close="selectedProperty = null"
      @updated="fetchDashboard"
    />

    <!-- Add Property Modal -->
    <div v-if="showAddPropertyModal" class="modal-backdrop" @click.self="showAddPropertyModal = false">
      <div class="modal property-modal">
        <div class="modal-header">
          <h2>Add Commercial Property</h2>
          <button class="btn-close" @click="showAddPropertyModal = false">×</button>
        </div>
        <form @submit.prevent="createProperty" class="modal-body">
          <div class="form-group">
            <label>Address *</label>
            <input v-model="newProperty.address" type="text" required placeholder="123 Main Street, Sydney NSW 2000" />
          </div>
          <div class="form-row">
            <div class="form-group">
              <label>Building Type *</label>
              <select v-model="newProperty.buildingType" required>
                <option value="">Select type</option>
                <option value="Office">Office</option>
                <option value="Retail">Retail</option>
                <option value="Industrial">Industrial</option>
                <option value="Mixed">Mixed Use</option>
              </select>
            </div>
            <div class="form-group">
              <label>Total GFA (m²)</label>
              <input v-model.number="newProperty.totalGFA" type="number" min="0" step="0.01" placeholder="1000" />
            </div>
          </div>
          <div class="form-row">
            <div class="form-group">
              <label>Purchase Price *</label>
              <input v-model.number="newProperty.purchasePrice" type="number" min="0" step="0.01" required placeholder="1500000" />
            </div>
            <div class="form-group">
              <label>Purchase Date *</label>
              <input v-model="newProperty.purchaseDate" type="date" required />
            </div>
          </div>
          <div class="form-group">
            <label>Title Reference</label>
            <input v-model="newProperty.titleReference" type="text" placeholder="Lot 1 DP 123456" />
          </div>
          <div class="form-group">
            <label>Description</label>
            <textarea v-model="newProperty.description" rows="2" placeholder="Additional notes about the property"></textarea>
          </div>
          <div class="modal-actions">
            <button type="button" class="btn btn-secondary" @click="showAddPropertyModal = false">Cancel</button>
            <button type="submit" class="btn btn-primary" :disabled="creatingProperty">
              {{ creatingProperty ? 'Creating...' : 'Create Property' }}
            </button>
          </div>
        </form>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue';
import api from '../services/api';
import MoneyBoxLoader from '../components/MoneyBoxLoader.vue';
import YieldGauge from '../components/property/YieldGauge.vue';
import VacancyHeatmap from '../components/property/VacancyHeatmap.vue';
import CashflowForecast from '../components/property/CashflowForecast.vue';
import PropertyCard from '../components/property/PropertyCard.vue';
import PropertyDetailDrawer from '../components/property/PropertyDetailDrawer.vue';

const loading = ref(true);
const dashboard = ref({
  totalValue: 0,
  totalPurchasePrice: 0,
  netYieldPercent: 0,
  grossRentalIncome: 0,
  totalExpenses: 0,
  netIncome: 0,
  totalProperties: 0,
  activeLeases: 0,
  expiringLeases: 0,
  properties: [],
  vacancyHeatmap: [],
  cashflowForecast: []
});

const selectedProperty = ref(null);
const showAddPropertyModal = ref(false);
const creatingProperty = ref(false);
const propertyViewMode = ref('cards');

const newProperty = ref({
  address: '',
  buildingType: '',
  totalGFA: 0,
  purchasePrice: 0,
  purchaseDate: new Date().toISOString().split('T')[0],
  titleReference: '',
  description: ''
});

const capitalGain = computed(() => dashboard.value.totalValue - dashboard.value.totalPurchasePrice);

const yieldClass = computed(() => {
  const y = dashboard.value.netYieldPercent;
  if (y >= 6) return 'value-positive';
  if (y >= 4) return 'value-warning';
  return 'value-negative';
});

function formatCurrency(value) {
  return new Intl.NumberFormat('en-AU', {
    style: 'currency',
    currency: 'AUD',
    minimumFractionDigits: 0,
    maximumFractionDigits: 0
  }).format(value || 0);
}

function togglePropertyView() {
  propertyViewMode.value = propertyViewMode.value === 'cards' ? 'list' : 'cards';
}

function openPropertyDetail(property) {
  selectedProperty.value = property;
}

async function fetchDashboard() {
  try {
    loading.value = true;
    const response = await api.get('/commercialproperty/dashboard');
    dashboard.value = response.data;
  } catch (error) {
    console.error('Failed to load property dashboard:', error);
  } finally {
    loading.value = false;
  }
}

async function createProperty() {
  try {
    creatingProperty.value = true;
    await api.post('/commercialproperty', newProperty.value);
    showAddPropertyModal.value = false;
    newProperty.value = {
      address: '',
      buildingType: '',
      totalGFA: 0,
      purchasePrice: 0,
      purchaseDate: new Date().toISOString().split('T')[0],
      titleReference: '',
      description: ''
    };
    await fetchDashboard();
  } catch (error) {
    console.error('Failed to create property:', error);
    alert('Failed to create property. Please try again.');
  } finally {
    creatingProperty.value = false;
  }
}

onMounted(fetchDashboard);
</script>

<style scoped>
/* =========================================
   Property Hub - Industrial/Brutalist Aesthetic
   ========================================= */

.property-hub {
  max-width: 1400px;
  padding: 0 var(--spacing-md);
}

/* Header */
.hub-header {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  margin-bottom: var(--spacing-xl);
  padding-bottom: var(--spacing-lg);
  border-bottom: 3px solid var(--color-border);
}

.hub-title {
  font-size: var(--font-size-3xl);
  font-weight: 700;
  letter-spacing: -0.03em;
  margin-bottom: var(--spacing-xs);
  background: linear-gradient(135deg, var(--color-text-primary) 0%, var(--color-industrial-copper) 100%);
  -webkit-background-clip: text;
  -webkit-text-fill-color: transparent;
  background-clip: text;
}

.hub-subtitle {
  color: var(--color-text-muted);
  font-size: var(--font-size-base);
}

.header-actions {
  display: flex;
  gap: var(--spacing-md);
}

/* Stats Grid */
.stats-grid {
  display: grid;
  grid-template-columns: repeat(4, 1fr);
  gap: var(--spacing-lg);
  margin-bottom: var(--spacing-xl);
}

@media (max-width: 1100px) {
  .stats-grid {
    grid-template-columns: repeat(2, 1fr);
  }
}

@media (max-width: 600px) {
  .stats-grid {
    grid-template-columns: 1fr;
  }
}

.stat-card {
  display: flex;
  align-items: flex-start;
  gap: var(--spacing-md);
  padding: var(--spacing-lg);
  background: var(--color-bg-card);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-lg);
  transition: all var(--transition-fast);
}

.stat-card:hover {
  border-color: var(--color-industrial-copper);
  box-shadow: var(--shadow-md);
}

.stat-primary {
  background: linear-gradient(135deg, var(--color-bg-card) 0%, rgba(180, 83, 9, 0.05) 100%);
  border-color: var(--color-industrial-copper);
}

.stat-icon {
  width: 48px;
  height: 48px;
  display: flex;
  align-items: center;
  justify-content: center;
  background: var(--color-bg-elevated);
  border-radius: var(--radius-md);
  color: var(--color-industrial-copper);
  flex-shrink: 0;
}

.stat-icon-yield { color: var(--color-success); }
.stat-icon-leases { color: var(--color-info); }
.stat-icon-properties { color: var(--color-accent); }

.stat-content {
  display: flex;
  flex-direction: column;
  gap: var(--spacing-xs);
}

.stat-label {
  font-size: var(--font-size-xs);
  font-weight: 600;
  text-transform: uppercase;
  letter-spacing: var(--letter-spacing-wide);
  color: var(--color-text-muted);
}

.stat-value {
  font-size: var(--font-size-2xl);
  font-weight: 700;
  font-family: var(--font-display);
  color: var(--color-text-primary);
}

.stat-detail {
  font-size: var(--font-size-sm);
  color: var(--color-text-secondary);
}

.value-warning {
  color: var(--color-warning);
}

/* Bento Grid */
.bento-grid {
  display: grid;
  grid-template-columns: 1fr 1fr;
  grid-template-rows: auto auto;
  gap: var(--spacing-lg);
}

@media (max-width: 900px) {
  .bento-grid {
    grid-template-columns: 1fr;
  }
}

.bento-card {
  padding: var(--spacing-lg);
}

.bento-yield {
  grid-row: span 1;
}

.bento-properties {
  grid-row: span 2;
  max-height: 600px;
  overflow-y: auto;
}

.bento-vacancy,
.bento-cashflow {
  min-height: 280px;
}

.card-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: var(--spacing-lg);
}

.card-header h3 {
  font-size: var(--font-size-lg);
  font-weight: 600;
}

.badge {
  padding: var(--spacing-xs) var(--spacing-sm);
  font-size: var(--font-size-xs);
  font-weight: 600;
  border-radius: var(--radius-full);
  text-transform: uppercase;
  letter-spacing: 0.5px;
}

.badge-info {
  background: rgba(2, 132, 199, 0.1);
  color: var(--color-info);
}

.badge-warning {
  background: rgba(217, 119, 6, 0.1);
  color: var(--color-warning);
}

/* Properties Container */
.properties-container {
  display: flex;
  flex-direction: column;
  gap: var(--spacing-md);
}

.properties-container.cards {
  display: grid;
  grid-template-columns: 1fr;
  gap: var(--spacing-md);
}

.empty-state {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  padding: var(--spacing-2xl);
  text-align: center;
  color: var(--color-text-muted);
  gap: var(--spacing-md);
}

.empty-state svg {
  opacity: 0.5;
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

.modal {
  background: var(--color-bg-card);
  border-radius: var(--radius-xl);
  box-shadow: var(--shadow-xl);
  max-width: 560px;
  width: 90%;
  max-height: 90vh;
  overflow-y: auto;
}

.property-modal {
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
  font-size: var(--font-size-xl);
  font-weight: 600;
}

.btn-close {
  background: none;
  border: none;
  font-size: 1.5rem;
  cursor: pointer;
  color: var(--color-text-muted);
  padding: var(--spacing-sm);
  line-height: 1;
}

.btn-close:hover {
  color: var(--color-text-primary);
}

.modal-body {
  padding: var(--spacing-lg);
}

.form-row {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: var(--spacing-md);
}

@media (max-width: 500px) {
  .form-row {
    grid-template-columns: 1fr;
  }
}

.modal-actions {
  display: flex;
  justify-content: flex-end;
  gap: var(--spacing-md);
  margin-top: var(--spacing-lg);
  padding-top: var(--spacing-lg);
  border-top: 1px solid var(--color-border);
}

.btn-sm {
  padding: var(--spacing-xs) var(--spacing-md);
  font-size: var(--font-size-sm);
}

.page-loader-container {
  display: flex;
  justify-content: center;
  align-items: center;
  height: 400px;
}
</style>

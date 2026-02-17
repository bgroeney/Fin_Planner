<template>
  <div class="entities-view animate-fade-in">
    <div class="header-row">
      <div class="header-content">
        <h1>Entity Structure</h1>
        <p class="subtitle">Manage family members, trusts, and companies for tax planning</p>
      </div>
    </div>

    <div v-if="loading" class="loader-container">
      <div class="spinner"></div>
    </div>

    <template v-else>
      <!-- Entity Summary Cards -->
      <div class="entity-grid">
        <!-- Persons Section -->
        <div class="entity-section">
          <div class="section-header">
            <h2>
              <svg class="section-icon" xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M19 21v-2a4 4 0 0 0-4-4H9a4 4 0 0 0-4 4v2"/><circle cx="12" cy="7" r="4"/></svg>
              Family Members
            </h2>
            <button class="btn btn-primary btn-sm" @click="openCreatePerson">
              + Add Person
            </button>
          </div>
          
          <div v-if="persons.length === 0" class="empty-card card">
            <p>No family members added yet.</p>
          </div>
          
          <div v-else class="entity-cards">
            <div v-for="person in persons" :key="person.id" class="entity-card card" @click="viewPerson(person)">
              <div class="entity-avatar person-avatar">{{ getInitials(person.fullName) }}</div>
              <div class="entity-info">
                <h3>{{ person.fullName }}</h3>
                <div class="entity-meta">
                  <span class="tax-rate" :class="getTaxRateClass(person.marginalTaxRate)">
                    {{ (person.marginalTaxRate * 100).toFixed(0) }}% marginal
                  </span>
                  <span v-if="person.superAccounts?.length">
                    {{ person.superAccounts.length }} super account(s)
                  </span>
                </div>
              </div>
              <svg class="entity-arrow" xmlns="http://www.w3.org/2000/svg" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><polyline points="9 18 15 12 9 6"/></svg>
            </div>
          </div>
        </div>

        <!-- Trusts Section -->
        <div class="entity-section">
          <div class="section-header">
            <h2>
              <svg class="section-icon" xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M3 21h18M3 10h18M5 6l7-3 7 3M4 10v11M20 10v11M8 14v3M12 14v3M16 14v3"/></svg>
              Trusts
            </h2>
            <button class="btn btn-primary btn-sm" @click="openCreateTrust">
              + Add Trust
            </button>
          </div>
          
          <div v-if="trusts.length === 0" class="empty-card card">
            <p>No trusts added yet.</p>
          </div>
          
          <div v-else class="entity-cards">
            <div v-for="trust in trusts" :key="trust.id" class="entity-card card" @click="viewTrust(trust)">
              <div class="entity-avatar trust-avatar">{{ getInitials(trust.trustName) }}</div>
              <div class="entity-info">
                <h3>{{ trust.trustName }}</h3>
                <div class="entity-meta">
                  <span class="trust-type">{{ trust.trustType }}</span>
                  <span>{{ trust.beneficiaries?.length || 0 }} beneficiaries</span>
                </div>
              </div>
              <svg class="entity-arrow" xmlns="http://www.w3.org/2000/svg" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><polyline points="9 18 15 12 9 6"/></svg>
            </div>
          </div>
        </div>

        <!-- Companies Section -->
        <div class="entity-section">
          <div class="section-header">
            <h2>
              <svg class="section-icon" xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><rect x="4" y="2" width="16" height="20" rx="2" ry="2"/><path d="M9 22V6h6v16"/><path d="M8 6h.01M16 6h.01M8 10h.01M16 10h.01M8 14h.01M16 14h.01M8 18h.01M16 18h.01"/></svg>
              Companies
            </h2>
            <button class="btn btn-primary btn-sm" @click="openCreateCompany">
              + Add Company
            </button>
          </div>
          
          <div v-if="companies.length === 0" class="empty-card card">
            <p>No companies added yet.</p>
          </div>
          
          <div v-else class="entity-cards">
            <div v-for="company in companies" :key="company.id" class="entity-card card" @click="viewCompany(company)">
              <div class="entity-avatar company-avatar">{{ getInitials(company.companyName) }}</div>
              <div class="entity-info">
                <h3>{{ company.companyName }}</h3>
                <div class="entity-meta">
                  <span class="company-rate">{{ (company.taxRate * 100).toFixed(0) }}% tax rate</span>
                  <span>Franking: {{ formatCurrency(company.frankingAccountBalance) }}</span>
                </div>
                <div v-if="company.division7ALoans?.length" class="loan-warning">
                  ⚠️ {{ company.division7ALoans.length }} Div 7A loan(s)
                </div>
              </div>
              <svg class="entity-arrow" xmlns="http://www.w3.org/2000/svg" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><polyline points="9 18 15 12 9 6"/></svg>
            </div>
          </div>
        </div>
      </div>

      <!-- Tax Optimization CTA -->
      <div class="optimization-cta card" v-if="trusts.length > 0 && persons.length > 0">
        <div class="cta-content">
          <h3>🎯 Optimize Trust Distributions</h3>
          <p>Use our tax optimization engine to distribute trust income efficiently across family members.</p>
        </div>
        <router-link to="/tax-optimization" class="btn btn-accent">
          Open Tax Optimizer
        </router-link>
      </div>
    </template>

    <!-- Create Person Modal -->
    <Teleport to="body">
      <div v-if="showCreatePerson" class="modal-overlay" @click.self="showCreatePerson = false">
        <div class="modal-card">
          <div class="modal-header">
            <h2>Add Family Member</h2>
            <button class="btn-close" @click="showCreatePerson = false">&times;</button>
          </div>
          <form @submit.prevent="createPerson" class="modal-form">
            <div class="form-group">
              <label>Full Name</label>
              <input v-model="newPerson.fullName" type="text" required />
            </div>
            <div class="form-group">
              <label>Date of Birth</label>
              <input v-model="newPerson.dateOfBirth" type="date" required />
            </div>
            <div class="form-group">
              <label>Marginal Tax Rate</label>
              <select v-model="newPerson.marginalTaxRate">
                <option :value="0">0% (Tax-free threshold)</option>
                <option :value="0.19">19%</option>
                <option :value="0.325">32.5%</option>
                <option :value="0.37">37%</option>
                <option :value="0.45">45%</option>
              </select>
            </div>
            <div class="form-group">
              <label class="checkbox-label">
                <input type="checkbox" v-model="newPerson.hasPrivateHealthInsurance" />
                Has private health insurance
              </label>
            </div>
            <div class="modal-actions">
              <button type="button" class="btn btn-secondary" @click="showCreatePerson = false">Cancel</button>
              <button type="submit" class="btn btn-primary" :disabled="saving">
                {{ saving ? 'Saving...' : 'Add Person' }}
              </button>
            </div>
          </form>
        </div>
      </div>
    </Teleport>

    <!-- Create Trust Modal -->
    <Teleport to="body">
      <div v-if="showCreateTrust" class="modal-overlay" @click.self="showCreateTrust = false">
        <div class="modal-card">
          <div class="modal-header">
            <h2>Add Trust</h2>
            <button class="btn-close" @click="showCreateTrust = false">&times;</button>
          </div>
          <form @submit.prevent="createTrust" class="modal-form">
            <div class="form-group">
              <label>Trust Name</label>
              <input v-model="newTrust.trustName" type="text" required />
            </div>
            <div class="form-group">
              <label>ABN (optional)</label>
              <input v-model="newTrust.abn" type="text" />
            </div>
            <div class="form-group">
              <label>Trustee Name</label>
              <input v-model="newTrust.trusteeName" type="text" required />
            </div>
            <div class="form-group">
              <label>Trust Type</label>
              <select v-model="newTrust.trustType">
                <option value="Discretionary">Discretionary (Family Trust)</option>
                <option value="Unit">Unit Trust</option>
                <option value="Hybrid">Hybrid Trust</option>
              </select>
            </div>
            <div class="modal-actions">
              <button type="button" class="btn btn-secondary" @click="showCreateTrust = false">Cancel</button>
              <button type="submit" class="btn btn-primary" :disabled="saving">
                {{ saving ? 'Saving...' : 'Add Trust' }}
              </button>
            </div>
          </form>
        </div>
      </div>
    </Teleport>

    <!-- Create Company Modal -->
    <Teleport to="body">
      <div v-if="showCreateCompany" class="modal-overlay" @click.self="showCreateCompany = false">
        <div class="modal-card">
          <div class="modal-header">
            <h2>Add Company</h2>
            <button class="btn-close" @click="showCreateCompany = false">&times;</button>
          </div>
          <form @submit.prevent="createCompany" class="modal-form">
            <div class="form-group">
              <label>Company Name</label>
              <input v-model="newCompany.companyName" type="text" required />
            </div>
            <div class="form-group">
              <label>ABN (optional)</label>
              <input v-model="newCompany.abn" type="text" />
            </div>
            <div class="form-group">
              <label>ACN (optional)</label>
              <input v-model="newCompany.acn" type="text" />
            </div>
            <div class="form-group">
              <label class="checkbox-label">
                <input type="checkbox" v-model="newCompany.isBaseRateEntity" />
                Base rate entity (25% tax rate)
              </label>
            </div>
            <div class="form-row">
              <div class="form-group">
                <label>Retained Profits</label>
                <input v-model.number="newCompany.retainedProfits" type="number" step="0.01" />
              </div>
              <div class="form-group">
                <label>Franking Balance</label>
                <input v-model.number="newCompany.frankingAccountBalance" type="number" step="0.01" />
              </div>
            </div>
            <div class="form-group">
              <label>Incorporation Date</label>
              <input v-model="newCompany.incorporationDate" type="date" required />
            </div>
            <div class="modal-actions">
              <button type="button" class="btn btn-secondary" @click="showCreateCompany = false">Cancel</button>
              <button type="submit" class="btn btn-primary" :disabled="saving">
                {{ saving ? 'Saving...' : 'Add Company' }}
              </button>
            </div>
          </form>
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
import { formatCurrency } from '../utils/formatters';

const router = useRouter();
const portfolioStore = usePortfolioStore();

const loading = ref(true);
const saving = ref(false);
const persons = ref([]);
const trusts = ref([]);
const companies = ref([]);

const currentPortfolioId = computed(() => portfolioStore.currentPortfolioId);

// Modals
const showCreatePerson = ref(false);
const showCreateTrust = ref(false);
const showCreateCompany = ref(false);

// Form data
const newPerson = ref({
  fullName: '',
  dateOfBirth: '',
  marginalTaxRate: 0.325,
  hasPrivateHealthInsurance: false
});

const newTrust = ref({
  trustName: '',
  abn: '',
  trusteeName: '',
  trustType: 'Discretionary'
});

const newCompany = ref({
  companyName: '',
  abn: '',
  acn: '',
  isBaseRateEntity: true,
  retainedProfits: 0,
  frankingAccountBalance: 0,
  incorporationDate: ''
});



const getInitials = (name) => {
  if (!name) return '?';
  const parts = name.split(' ');
  if (parts.length >= 2) return parts[0][0] + parts[1][0];
  return name.substring(0, 2).toUpperCase();
};

const getTaxRateClass = (rate) => {
  if (rate <= 0.19) return 'tax-low';
  if (rate <= 0.325) return 'tax-medium';
  return 'tax-high';
};

const loadEntities = async () => {
  if (!currentPortfolioId.value) {
    loading.value = false;
    return;
  }
  loading.value = true;
  
  try {
    const [personsRes, trustsRes, companiesRes] = await Promise.all([
      api.get(`/persons?portfolioId=${currentPortfolioId.value}`),
      api.get(`/trusts?portfolioId=${currentPortfolioId.value}`),
      api.get(`/companies?portfolioId=${currentPortfolioId.value}`)
    ]);
    
    persons.value = personsRes.data;
    trusts.value = trustsRes.data;
    companies.value = companiesRes.data;
  } catch (e) {
    console.error('Failed to load entities', e);
  } finally {
    loading.value = false;
  }
};

const openCreatePerson = () => {
  newPerson.value = { fullName: '', dateOfBirth: '', marginalTaxRate: 0.325, hasPrivateHealthInsurance: false };
  showCreatePerson.value = true;
};

const openCreateTrust = () => {
  newTrust.value = { trustName: '', abn: '', trusteeName: '', trustType: 'Discretionary' };
  showCreateTrust.value = true;
};

const openCreateCompany = () => {
  newCompany.value = { companyName: '', abn: '', acn: '', isBaseRateEntity: true, retainedProfits: 0, frankingAccountBalance: 0, incorporationDate: '' };
  showCreateCompany.value = true;
};

const createPerson = async () => {
  saving.value = true;
  try {
    await api.post('/persons', {
      portfolioId: currentPortfolioId.value,
      ...newPerson.value
    });
    showCreatePerson.value = false;
    await loadEntities();
  } catch (e) {
    console.error('Failed to create person', e);
    alert('Failed to create person');
  } finally {
    saving.value = false;
  }
};

const createTrust = async () => {
  saving.value = true;
  try {
    await api.post('/trusts', {
      portfolioId: currentPortfolioId.value,
      ...newTrust.value
    });
    showCreateTrust.value = false;
    await loadEntities();
  } catch (e) {
    console.error('Failed to create trust', e);
    alert('Failed to create trust');
  } finally {
    saving.value = false;
  }
};

const createCompany = async () => {
  saving.value = true;
  try {
    await api.post('/companies', {
      portfolioId: currentPortfolioId.value,
      ...newCompany.value
    });
    showCreateCompany.value = false;
    await loadEntities();
  } catch (e) {
    console.error('Failed to create company', e);
    alert('Failed to create company');
  } finally {
    saving.value = false;
  }
};

const viewPerson = (person) => {
  router.push(`/entities/person/${person.id}`);
};

const viewTrust = (trust) => {
  router.push(`/entities/trust/${trust.id}`);
};

const viewCompany = (company) => {
  router.push(`/entities/company/${company.id}`);
};

// Watch for portfolio changes
watch(currentPortfolioId, () => {
  loadEntities();
});

onMounted(() => {
  if (currentPortfolioId.value) {
    loadEntities();
  } else {
    loading.value = false;
  }
});
</script>

<style scoped>
.entities-view {
  max-width: 1200px;
  margin: 0 auto;
}

.header-row {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: var(--spacing-xl);
  flex-wrap: wrap;
  gap: var(--spacing-md);
}

.portfolio-selector select {
  padding: var(--spacing-sm) var(--spacing-md);
  border-radius: var(--radius-md);
  border: 1px solid var(--color-border);
  background: var(--color-bg-card);
  color: var(--color-text-primary);
  min-width: 200px;
}

.entity-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(350px, 1fr));
  gap: var(--spacing-xl);
  margin-bottom: var(--spacing-xl);
}

.entity-section {
  display: flex;
  flex-direction: column;
  gap: var(--spacing-md);
}

.section-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.section-header h2 {
  display: flex;
  align-items: center;
  gap: var(--spacing-sm);
  font-size: var(--font-size-lg);
  margin: 0;
}

.section-icon {
  font-size: 1.2em;
}

.entity-cards {
  display: flex;
  flex-direction: column;
  gap: var(--spacing-md);
}

.entity-card {
  display: flex;
  align-items: center;
  gap: var(--spacing-md);
  padding: var(--spacing-md);
  cursor: pointer;
  transition: all var(--transition-fast);
  border: 1px solid transparent;
}

.entity-card:hover {
  border-color: var(--color-accent);
  transform: translateX(4px);
}

.entity-avatar {
  width: 48px;
  height: 48px;
  border-radius: var(--radius-full);
  display: flex;
  align-items: center;
  justify-content: center;
  font-weight: 700;
  font-size: var(--font-size-base);
  flex-shrink: 0;
}

.person-avatar {
  background: linear-gradient(135deg, #3b82f6, #1d4ed8);
  color: white;
}

.trust-avatar {
  background: linear-gradient(135deg, #8b5cf6, #6d28d9);
  color: white;
}

.company-avatar {
  background: linear-gradient(135deg, #10b981, #059669);
  color: white;
}

.entity-info {
  flex: 1;
  min-width: 0;
}

.entity-info h3 {
  margin: 0 0 4px;
  font-size: var(--font-size-base);
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.entity-meta {
  display: flex;
  gap: var(--spacing-md);
  font-size: var(--font-size-sm);
  color: var(--color-text-muted);
}

.tax-rate {
  padding: 2px 6px;
  border-radius: var(--radius-sm);
  font-weight: 500;
}

.tax-low {
  background: rgba(16, 185, 129, 0.15);
  color: #10b981;
}

.tax-medium {
  background: rgba(245, 158, 11, 0.15);
  color: #f59e0b;
}

.tax-high {
  background: rgba(239, 68, 68, 0.15);
  color: #ef4444;
}

.trust-type {
  padding: 2px 6px;
  background: rgba(139, 92, 246, 0.15);
  color: #8b5cf6;
  border-radius: var(--radius-sm);
  font-weight: 500;
}

.company-rate {
  padding: 2px 6px;
  background: rgba(16, 185, 129, 0.15);
  color: #10b981;
  border-radius: var(--radius-sm);
  font-weight: 500;
}

.loan-warning {
  margin-top: 4px;
  font-size: var(--font-size-xs);
  color: #f59e0b;
}

.entity-arrow {
  color: var(--color-text-muted);
  font-size: var(--font-size-lg);
}

.empty-card {
  padding: var(--spacing-xl);
  text-align: center;
  color: var(--color-text-muted);
}

.optimization-cta {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: var(--spacing-xl);
  background: linear-gradient(135deg, rgba(139, 92, 246, 0.1), rgba(59, 130, 246, 0.1));
  border: 1px solid rgba(139, 92, 246, 0.3);
}

.cta-content h3 {
  margin: 0 0 var(--spacing-xs);
}

.cta-content p {
  margin: 0;
  color: var(--color-text-secondary);
}

.btn-accent {
  background: linear-gradient(135deg, #8b5cf6, #3b82f6);
  color: white;
  border: none;
  padding: var(--spacing-sm) var(--spacing-lg);
  border-radius: var(--radius-md);
  font-weight: 600;
  cursor: pointer;
  transition: all var(--transition-fast);
}

.btn-accent:hover {
  transform: translateY(-2px);
  box-shadow: 0 4px 12px rgba(139, 92, 246, 0.3);
}

/* Modal Styles */
.modal-overlay {
  position: fixed;
  inset: 0;
  background: rgba(0, 0, 0, 0.7);
  display: flex;
  justify-content: center;
  align-items: center;
  z-index: 1000;
  backdrop-filter: blur(4px);
}

.modal-card {
  background: var(--color-bg-card);
  border-radius: var(--radius-lg);
  box-shadow: var(--shadow-xl);
  width: 100%;
  max-width: 480px;
  max-height: 90vh;
  overflow: auto;
  border: 1px solid var(--color-border);
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
  color: var(--color-text-muted);
  cursor: pointer;
}

.modal-form {
  padding: var(--spacing-lg);
}

.form-group {
  margin-bottom: var(--spacing-lg);
}

.form-group label {
  display: block;
  margin-bottom: var(--spacing-xs);
  font-size: var(--font-size-sm);
  font-weight: 500;
  color: var(--color-text-secondary);
}

.form-group input,
.form-group select {
  width: 100%;
  padding: var(--spacing-sm) var(--spacing-md);
  border-radius: var(--radius-md);
  border: 1px solid var(--color-border);
  background: var(--color-bg-elevated);
  color: var(--color-text-primary);
}

.form-row {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: var(--spacing-md);
}

.checkbox-label {
  display: flex !important;
  align-items: center;
  gap: var(--spacing-sm);
  cursor: pointer;
}

.checkbox-label input {
  width: auto !important;
}

.modal-actions {
  display: flex;
  justify-content: flex-end;
  gap: var(--spacing-md);
  padding-top: var(--spacing-lg);
  border-top: 1px solid var(--color-border);
  margin-top: var(--spacing-lg);
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

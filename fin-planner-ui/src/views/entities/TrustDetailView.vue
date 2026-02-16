<template>
  <div class="trust-detail animate-fade-in" v-if="trust">
    <div class="header-row">
      <div class="header-content">
        <div class="breadcrumb">
          <router-link to="/entities">Entities</router-link>
          <span class="separator">/</span>
          <span class="current">{{ trust.trustName }}</span>
        </div>
        <h1>{{ trust.trustName }}</h1>
        <div class="meta-row">
          <span class="meta-item">🏛️ {{ trust.trustType }}</span>
          <span class="meta-item">🆔 ABN: {{ trust.ABN || 'N/A' }}</span>
          <span class="meta-item">👤 Trustee: {{ trust.trusteeName }}</span>
        </div>
      </div>
      <div class="actions">
        <router-link to="/tax-optimization" class="btn btn-accent mr-sm">🎯 Optimizer</router-link>
        <button class="btn btn-primary" @click="showEditModal = true">Edit Trust</button>
      </div>
    </div>

    <div class="detail-grid">
      <!-- Beneficiaries List -->
      <div class="card section-card">
        <div class="section-header">
          <h2>👥 Beneficiaries</h2>
          <button class="btn btn-secondary btn-sm" @click="openAddBeneficiary">Add Beneficiary</button>
        </div>
        
        <div v-if="trust.beneficiaries?.length === 0" class="empty-state-mini">
          No beneficiaries added to this trust.
        </div>
        
        <div v-else class="beneficiary-grid">
          <div v-for="b in trust.beneficiaries" :key="b.id" class="beneficiary-item" :class="{ inactive: !b.isEligible }">
            <div class="b-header">
              <span class="b-icon">{{ b.personAccountId ? '👤' : '🏢' }}</span>
              <span class="b-name">{{ b.beneficiaryName }}</span>
              <span v-if="!b.isEligible" class="badge badge-error">Ineligible</span>
            </div>
            <div class="b-meta">
              Relationship: {{ b.relationship || 'N/A' }}
            </div>
            <div class="b-actions">
              <button class="btn-icon" @click="toggleEligibility(b)">{{ b.isEligible ? '⏸️' : '▶️' }}</button>
              <button class="btn-icon btn-danger-text" @click="deleteBeneficiary(b.id)">🗑️</button>
            </div>
          </div>
        </div>
      </div>

      <!-- Income & Distributions -->
      <div class="card section-card">
        <div class="section-header">
          <h2>💰 Fiscal Year Summary (FY{{ currentFY }})</h2>
          <button class="btn btn-secondary btn-sm" @click="showIncomeModal = true">Record Income</button>
        </div>

        <div v-if="!currentIncome" class="empty-state-mini">
          No income recorded for FY{{ currentFY }}.
        </div>

        <div v-else class="income-summary">
          <div class="income-grid">
            <div class="income-item">
              <span class="label">Franked Dividends</span>
              <span class="value">{{ formatCurrency(currentIncome.frankedDividends) }}</span>
            </div>
            <div class="income-item">
              <span class="label">Franking Credits</span>
              <span class="value">{{ formatCurrency(currentIncome.frankingCredits) }}</span>
            </div>
            <div class="income-item">
              <span class="label">Rental & Other</span>
              <span class="value">{{ formatCurrency(currentIncome.unfrankedIncome + currentIncome.rentalIncome) }}</span>
            </div>
            <div class="income-item highlight">
              <span class="label">Total Taxable</span>
              <span class="value">{{ formatCurrency(getTotalTaxableIncome(currentIncome)) }}</span>
            </div>
          </div>
        </div>

        <div class="distributions-section mt-xl">
          <h3>📋 Distribution Resolutions</h3>
          <table class="data-table" v-if="distributions.length > 0">
            <thead>
              <tr>
                <th>Beneficiary</th>
                <th class="text-right">Total Dist.</th>
                <th class="text-right">FC Offset</th>
                <th>Status</th>
                <th>Actions</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="d in distributions" :key="d.id">
                <td>{{ d.beneficiary?.beneficiaryName }}</td>
                <td class="text-right font-semibold">{{ formatCurrency(getTotalDistribution(d)) }}</td>
                <td class="text-right text-success">{{ formatCurrency(d.frankingCredits) }}</td>
                <td><span class="status-pill" :class="d.status.toLowerCase()">{{ d.status }}</span></td>
                <td><button class="btn-icon btn-danger-text" @click="deleteDistribution(d.id)">🗑️</button></td>
              </tr>
            </tbody>
          </table>
          <div v-else class="empty-state-mini">No distributions resolved for this FY.</div>
        </div>
      </div>
    </div>

    <!-- Modals -->
    <Teleport to="body">
       <!-- Income Modal -->
       <div v-if="showIncomeModal" class="modal-overlay" @click.self="showIncomeModal = false">
        <div class="modal-card">
          <div class="modal-header">
            <h2>Record Trust Income</h2>
            <button class="btn-close" @click="showIncomeModal = false">&times;</button>
          </div>
          <form @submit.prevent="recordIncome" class="modal-form">
            <div class="form-group">
              <label>Fiscal Year</label>
              <input v-model.number="incomeForm.fiscalYear" type="number" required />
            </div>
            <div class="form-row">
              <div class="form-group">
                <label>Franked Dividends</label>
                <input v-model.number="incomeForm.frankedDividends" type="number" step="0.01" />
              </div>
              <div class="form-group">
                <label>Franking Credits</label>
                <input v-model.number="incomeForm.frankingCredits" type="number" step="0.01" />
              </div>
            </div>
            <div class="form-row">
              <div class="form-group">
                <label>Unfranked & Interest</label>
                <input v-model.number="incomeForm.unfrankedIncome" type="number" step="0.01" />
              </div>
              <div class="form-group">
                <label>Net Rental Income</label>
                <input v-model.number="incomeForm.rentalIncome" type="number" step="0.01" />
              </div>
            </div>
            <div class="form-row">
              <div class="form-group">
                <label>Discount CG (Gross)</label>
                <input v-model.number="incomeForm.discountCapitalGains" type="number" step="0.01" />
              </div>
              <div class="form-group">
                <label>Non-Discount CG</label>
                <input v-model.number="incomeForm.nonDiscountCapitalGains" type="number" step="0.01" />
              </div>
            </div>
            <div class="modal-actions">
              <button type="button" class="btn btn-secondary" @click="showIncomeModal = false">Cancel</button>
              <button type="submit" class="btn btn-primary" :disabled="saving">Save Income</button>
            </div>
          </form>
        </div>
      </div>

      <!-- Add Beneficiary Modal -->
      <div v-if="showBeneficiaryModal" class="modal-overlay" @click.self="showBeneficiaryModal = false">
        <div class="modal-card">
          <div class="modal-header">
            <h2>Add Beneficiary</h2>
            <button class="btn-close" @click="showBeneficiaryModal = false">&times;</button>
          </div>
          <form @submit.prevent="addBeneficiary" class="modal-form">
            <div class="form-group">
                <label>Link Existing Entity</label>
                <select v-model="benLinkType" @change="benLinkForm.id = null">
                    <option value="person">Person</option>
                    <option value="company">Bucket Company</option>
                </select>
            </div>
            <div class="form-group" v-if="benLinkType === 'person'">
                <label>Select Family Member</label>
                <select v-model="benLinkForm.personId">
                    <option v-for="p in availablePersons" :key="p.id" :value="p.id">{{ p.fullName }}</option>
                </select>
            </div>
            <div class="form-group" v-if="benLinkType === 'company'">
                <label>Select Company</label>
                <select v-model="benLinkForm.companyId">
                    <option v-for="c in availableCompanies" :key="c.id" :value="c.id">{{ c.companyName }}</option>
                </select>
            </div>
            <div class="form-group">
                <label>Relationship</label>
                <input v-model="benLinkForm.relationship" type="text" placeholder="e.g. Spouse, Corporate Beneficiary" />
            </div>
            <div class="modal-actions">
              <button type="button" class="btn btn-secondary" @click="showBeneficiaryModal = false">Cancel</button>
              <button type="submit" class="btn btn-primary" :disabled="saving">Add Beneficiary</button>
            </div>
          </form>
        </div>
      </div>
    </Teleport>
  </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue';
import { useRoute } from 'vue-router';
import api from '../../services/api';
import { formatCurrency } from '../../utils/formatters';

const route = useRoute();
const trust = ref(null);
const distributions = ref([]);
const currentIncome = ref(null);
const currentFY = ref(new Date().getMonth() > 5 ? new Date().getFullYear() + 1 : new Date().getFullYear());

const showEditModal = ref(false);
const showIncomeModal = ref(false);
const showBeneficiaryModal = ref(false);

const availablePersons = ref([]);
const availableCompanies = ref([]);
const benLinkType = ref('person');
const benLinkForm = ref({ personId: null, companyId: null, relationship: '' });

const incomeForm = ref({
    fiscalYear: 2025,
    frankedDividends: 0,
    unfrankedIncome: 0,
    discountCapitalGains: 0,
    nonDiscountCapitalGains: 0,
    rentalIncome: 0,
    frankingCredits: 0
});

const saving = ref(false);

const loadData = async () => {
  try {
    const trustRes = await api.get(`/trusts/${route.params.id}`);
    trust.value = trustRes.data;
    
    const [distRes, incRes, personsRes, companiesRes] = await Promise.all([
      api.get(`/trusts/${route.params.id}/distributions?fiscalYear=${currentFY.value}`),
      api.get(`/trusts/${route.params.id}/income/${currentFY.value}`).catch(() => ({ data: null })),
      api.get(`/persons?portfolioId=${trustRes.data.portfolioId}`),
      api.get(`/companies?portfolioId=${trustRes.data.portfolioId}`)
    ]);
    
    distributions.value = distRes.data;
    currentIncome.value = incRes.data;
    availablePersons.value = personsRes.data;
    availableCompanies.value = companiesRes.data;

    if (currentIncome.value) {
        incomeForm.value = { ...currentIncome.value };
    }
  } catch (e) {
    console.error('Failed to load trust data', e);
  }
};

const recordIncome = async () => {
    saving.value = true;
    try {
        await api.post(`/trusts/${trust.value.id}/income`, incomeForm.value);
        showIncomeModal.value = false;
        await loadData();
    } catch (e) {
        alert('Failed to save income');
    } finally {
        saving.value = false;
    }
};

const openAddBeneficiary = () => {
    showBeneficiaryModal.value = true;
};

const addBeneficiary = async () => {
    saving.value = true;
    try {
        const name = benLinkType.value === 'person' 
            ? availablePersons.value.find(p => p.id === benLinkForm.value.personId)?.fullName
            : availableCompanies.value.find(c => c.id === benLinkForm.value.companyId)?.companyName;

        await api.post(`/trusts/${trust.value.id}/beneficiaries`, {
            personAccountId: benLinkForm.value.personId,
            companyAccountId: benLinkForm.value.companyId,
            beneficiaryName: name,
            relationship: benLinkForm.value.relationship,
            isEligible: true
        });
        showBeneficiaryModal.value = false;
        await loadData();
    } catch (e) {
        alert('Failed to add beneficiary');
    } finally {
        saving.value = false;
    }
};

const toggleEligibility = async (ben) => {
    try {
        await api.put(`/trusts/${trust.value.id}/beneficiaries/${ben.id}`, {
            ...ben,
            isEligible: !ben.isEligible
        });
        await loadData();
    } catch (e) {
        alert('Update failed');
    }
};

const deleteBeneficiary = async (id) => {
    if (!confirm('Remove this beneficiary?')) return;
    try {
        await api.delete(`/trusts/${trust.value.id}/beneficiaries/${id}`);
        await loadData();
    } catch (e) {
        alert('Delete failed');
    }
};

const deleteDistribution = async (id) => {
    if (!confirm('Delete this distribution resolution?')) return;
    try {
        await api.delete(`/trusts/${trust.value.id}/distributions/${id}`);
        await loadData();
    } catch (e) {
        alert('Delete failed');
    }
};



const getTotalTaxableIncome = (inc) => {
    return (inc.frankedDividends || 0) + 
           (inc.unfrankedIncome || 0) + 
           (inc.rentalIncome || 0) + 
           ((inc.discountCapitalGains || 0) * 0.5) + 
           (inc.nonDiscountCapitalGains || 0);
};

const getTotalDistribution = (d) => {
    return d.frankedDividends + d.unfrankedIncome + d.discountCapitalGains + d.nonDiscountCapitalGains;
};

onMounted(loadData);
</script>

<style scoped>
.trust-detail { max-width: 1200px; margin: 0 auto; }
.breadcrumb { display: flex; gap: var(--spacing-sm); margin-bottom: var(--spacing-sm); font-size: var(--font-size-sm); color: var(--color-text-muted); }
.breadcrumb a { color: var(--color-accent); }
.meta-row { display: flex; gap: var(--spacing-lg); margin-top: var(--spacing-sm); font-size: var(--font-size-sm); color: var(--color-text-secondary); }
.detail-grid { display: grid; grid-template-columns: 1fr 1.5fr; gap: var(--spacing-xl); margin-top: var(--spacing-xl); }
.section-card { padding: var(--spacing-xl); }
.section-header { display: flex; justify-content: space-between; align-items: center; margin-bottom: var(--spacing-lg); }

.beneficiary-grid { display: flex; flex-direction: column; gap: var(--spacing-md); }
.beneficiary-item { padding: var(--spacing-md); border: 1px solid var(--color-border); border-radius: var(--radius-md); background: var(--color-bg-elevated); }
.beneficiary-item.inactive { opacity: 0.6; grayscale: 1; }
.b-header { display: flex; align-items: center; gap: var(--spacing-sm); margin-bottom: 4px; }
.b-name { font-weight: 600; }
.b-meta { font-size: var(--font-size-xs); color: var(--color-text-muted); }
.b-actions { display: flex; justify-content: flex-end; gap: var(--spacing-xs); margin-top: var(--spacing-sm); }

.income-grid { display: grid; grid-template-columns: 1fr 1fr; gap: var(--spacing-md); }
.income-item { display: flex; flex-direction: column; padding: var(--spacing-md); background: var(--color-bg-elevated); border-radius: var(--radius-md); }
.income-item.highlight { background: linear-gradient(135deg, #1e40af 0%, #3b82f6 100%); color: white; }
.income-item .label { font-size: var(--font-size-xs); opacity: 0.8; }
.income-item .value { font-weight: 700; font-size: var(--font-size-lg); }

.data-table { width: 100%; border-collapse: collapse; }
.data-table th, .data-table td { padding: var(--spacing-md); text-align: left; border-bottom: 1px solid var(--color-border); font-size: var(--font-size-sm); }
.status-pill { padding: 2px 8px; border-radius: var(--radius-full); font-size: var(--font-size-xs); font-weight: 600; text-transform: uppercase; }
.status-pill.draft { background: rgba(245, 158, 11, 0.15); color: #f59e0b; }
.status-pill.resolved { background: rgba(16, 185, 129, 0.15); color: #10b981; }

.text-right { text-align: right; }
.btn-danger-text { color: var(--color-danger); }

/* Reuse modal styles */
.modal-overlay { position: fixed; inset: 0; background: rgba(0,0,0,0.7); display: flex; justify-content: center; align-items: center; z-index: 1000; backdrop-filter: blur(4px); }
.modal-card { background: var(--color-bg-card); border-radius: var(--radius-lg); width: 100%; max-width: 550px; border: 1px solid var(--color-border); }
.modal-header { display: flex; justify-content: space-between; align-items: center; padding: var(--spacing-lg); border-bottom: 1px solid var(--color-border); }
.modal-form { padding: var(--spacing-lg); }
.form-group { margin-bottom: var(--spacing-lg); }
.form-row { display: grid; grid-template-columns: 1fr 1fr; gap: var(--spacing-md); }
.modal-actions { display: flex; justify-content: flex-end; gap: var(--spacing-md); padding-top: var(--spacing-lg); border-top: 1px solid var(--color-border); }
.badge { padding: 2px 6px; border-radius: 4px; font-size: 10px; font-weight: 700; }
.badge-error { background: #fee2e2; color: #ef4444; }
</style>

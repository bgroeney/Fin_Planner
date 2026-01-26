<template>
  <div class="person-detail animate-fade-in" v-if="person">
    <div class="header-row">
      <div class="header-content">
        <div class="breadcrumb">
          <router-link to="/entities">Entities</router-link>
          <span class="separator">/</span>
          <span class="current">{{ person.fullName }}</span>
        </div>
        <h1>{{ person.fullName }}</h1>
        <div class="meta-row">
          <span class="meta-item">🎂 {{ formatDate(person.dateOfBirth) }}</span>
          <span class="meta-item" :class="getTaxRateClass(person.marginalTaxRate)">
            📊 {{ (person.marginalTaxRate * 100).toFixed(1) }}% Effective Marginal Rate
          </span>
          <span class="meta-item">🛡️ {{ person.hasPrivateHealthInsurance ? 'Private Health Insurance' : 'No Private Health' }}</span>
        </div>
      </div>
      <div class="actions">
        <button class="btn btn-primary" @click="showEditModal = true">Edit Profile</button>
      </div>
    </div>

    <div class="detail-grid">
      <!-- Left Column: Financial Summary & Payroll -->
      <div class="main-column">
        <!-- Payroll Income Section -->
        <div class="card section-card">
          <div class="section-header">
            <h2>💼 Payroll Income</h2>
            <button class="btn btn-secondary btn-sm" @click="openAddIncome">Add Income</button>
          </div>
          
          <div v-if="person.payrollIncomes?.length === 0" class="empty-state-mini">
            No payroll records found.
          </div>
          
          <table v-else class="data-table">
            <thead>
              <tr>
                <th>Employer</th>
                <th>FY</th>
                <th class="text-right">Gross Salary</th>
                <th class="text-right">Tax Withheld</th>
                <th class="text-right">Super</th>
                <th class="text-right">Actions</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="pi in person.payrollIncomes" :key="pi.id">
                <td>{{ pi.employer }}</td>
                <td>FY{{ pi.fiscalYear }}</td>
                <td class="text-right font-semibold">{{ formatCurrency(pi.grossSalary) }}</td>
                <td class="text-right text-danger">{{ formatCurrency(pi.taxWithheld) }}</td>
                <td class="text-right text-success">{{ formatCurrency(pi.superContribution) }}</td>
                <td class="text-right">
                  <button class="btn-icon btn-danger-text" @click="deleteIncome(pi.id)">🗑️</button>
                </td>
              </tr>
            </tbody>
          </table>
        </div>

        <!-- Deductions Section -->
        <div class="card section-card mt-xl">
          <div class="section-header">
            <h2>📝 Deductions</h2>
            <button class="btn btn-secondary btn-sm" @click="openAddDeduction">Add Deduction</button>
          </div>
          
          <div v-if="person.deductions?.length === 0" class="empty-state-mini">
            No deductions recorded.
          </div>
          
          <table v-else class="data-table">
            <thead>
              <tr>
                <th>Category</th>
                <th>Description</th>
                <th>FY</th>
                <th class="text-right">Amount</th>
                <th class="text-right">Actions</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="d in person.deductions" :key="d.id">
                <td><span class="badge">{{ d.category }}</span></td>
                <td>{{ d.description }}</td>
                <td>FY{{ d.fiscalYear }}</td>
                <td class="text-right font-semibold text-danger">{{ formatCurrency(d.amount) }}</td>
                <td class="text-right">
                  <button class="btn-icon btn-danger-text" @click="deleteDeduction(d.id)">🗑️</button>
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>

      <!-- Right Column: Super & Tax Position -->
      <div class="side-column">
        <!-- Tax Position Card -->
        <div class="card tax-summary-card gradient-blue">
          <h3>⚡ Tax Position Summary</h3>
          <div class="tax-metric">
            <span class="label">Taxable Income (Est)</span>
            <span class="value">{{ formatCurrency(taxPosition?.taxableIncomeBeforeDistribution) }}</span>
          </div>
          <div class="tax-metric">
            <span class="label">Total Tax (Est)</span>
            <span class="value">{{ formatCurrency(taxPosition?.taxPayableBeforeDistribution) }}</span>
          </div>
          <div class="tax-metric">
            <span class="label">Marginal Bracket</span>
            <span class="value">{{ (taxPosition?.marginalTaxRate * 100).toFixed(1) }}%</span>
          </div>
          <div class="progress-bar">
             <div class="progress-fill" :style="{ width: getTaxProgress(taxPosition?.taxableIncomeBeforeDistribution) + '%' }"></div>
          </div>
        </div>

        <!-- Super Accounts Section -->
        <div class="card section-card mt-xl">
          <div class="section-header">
            <h2>🏦 Superannuation</h2>
            <button class="btn btn-secondary btn-sm" @click="openAddSuper">Add Super</button>
          </div>
          
          <div v-for="superAcc in person.superAccounts" :key="superAcc.id" class="super-item">
            <div class="super-info">
              <div class="fund-name">{{ superAcc.fundName }}</div>
              <div class="fund-meta">{{ superAcc.investmentOption }} • Member {{ superAcc.memberNumber || 'N/A' }}</div>
            </div>
            <div class="super-balance">{{ formatCurrency(superAcc.currentBalance) }}</div>
            <button class="btn-icon" @click="deleteSuper(superAcc.id)">🗑️</button>
          </div>
          
          <div v-if="person.superAccounts?.length === 0" class="empty-state-mini">
            No super accounts linked.
          </div>
        </div>
      </div>
    </div>

    <!-- Modals -->
    <Teleport to="body">
      <!-- Edit Person Modal -->
      <div v-if="showEditModal" class="modal-overlay" @click.self="showEditModal = false">
        <div class="modal-card">
          <div class="modal-header">
            <h2>Edit Profile</h2>
            <button class="btn-close" @click="showEditModal = false">&times;</button>
          </div>
          <form @submit.prevent="updatePerson" class="modal-form">
            <div class="form-group">
              <label>Full Name</label>
              <input v-model="editForm.fullName" type="text" required />
            </div>
            <div class="form-group">
              <label>Date of Birth</label>
              <input v-model="editForm.dateOfBirth" type="date" required />
            </div>
            <div class="form-group">
              <label>Marginal Tax Rate Override</label>
              <select v-model="editForm.marginalTaxRate">
                <option :value="0">0%</option>
                <option :value="0.19">19%</option>
                <option :value="0.30">30%</option>
                <option :value="0.37">37%</option>
                <option :value="0.45">45%</option>
              </select>
            </div>
            <div class="form-group">
              <label class="checkbox-label">
                <input type="checkbox" v-model="editForm.hasPrivateHealthInsurance" />
                Has private health insurance
              </label>
            </div>
            <div class="modal-actions">
              <button type="button" class="btn btn-secondary" @click="showEditModal = false">Cancel</button>
              <button type="submit" class="btn btn-primary" :disabled="saving">Save Changes</button>
            </div>
          </form>
        </div>
      </div>

      <!-- Add Income Modal -->
      <div v-if="showIncomeModal" class="modal-overlay" @click.self="showIncomeModal = false">
        <div class="modal-card">
          <div class="modal-header">
            <h2>Add Payroll Income</h2>
            <button class="btn-close" @click="showIncomeModal = false">&times;</button>
          </div>
          <form @submit.prevent="addIncome" class="modal-form">
            <div class="form-group">
              <label>Employer</label>
              <input v-model="incomeForm.employer" type="text" required />
            </div>
            <div class="form-row">
              <div class="form-group">
                <label>Fiscal Year</label>
                <input v-model.number="incomeForm.fiscalYear" type="number" required />
              </div>
              <div class="form-group">
                <label>Gross Salary</label>
                <input v-model.number="incomeForm.grossSalary" type="number" step="0.01" required />
              </div>
            </div>
            <div class="form-row">
              <div class="form-group">
                <label>Tax Withheld</label>
                <input v-model.number="incomeForm.taxWithheld" type="number" step="0.01" required />
              </div>
              <div class="form-group">
                <label>Super Guarantee</label>
                <input v-model.number="incomeForm.superContribution" type="number" step="0.01" required />
              </div>
            </div>
            <div class="modal-actions">
              <button type="button" class="btn btn-secondary" @click="showIncomeModal = false">Cancel</button>
              <button type="submit" class="btn btn-primary" :disabled="saving">Add Record</button>
            </div>
          </form>
        </div>
      </div>

      <!-- Add Deduction Modal -->
      <div v-if="showDeductionModal" class="modal-overlay" @click.self="showDeductionModal = false">
        <div class="modal-card">
          <div class="modal-header">
            <h2>Add Deduction</h2>
            <button class="btn-close" @click="showDeductionModal = false">&times;</button>
          </div>
          <form @submit.prevent="addDeduction" class="modal-form">
            <div class="form-group">
              <label>Category</label>
              <select v-model="deductionForm.category">
                <option value="WorkRelated">Work Related</option>
                <option value="Investment">Investment Expense</option>
                <option value="Donation">Charitable Donation</option>
                <option value="Other">Other</option>
              </select>
            </div>
            <div class="form-group">
              <label>Description</label>
              <input v-model="deductionForm.description" type="text" required />
            </div>
            <div class="form-row">
              <div class="form-group">
                <label>Fiscal Year</label>
                <input v-model.number="deductionForm.fiscalYear" type="number" required />
              </div>
              <div class="form-group">
                <label>Amount</label>
                <input v-model.number="deductionForm.amount" type="number" step="0.01" required />
              </div>
            </div>
            <div class="modal-actions">
              <button type="button" class="btn btn-secondary" @click="showDeductionModal = false">Cancel</button>
              <button type="submit" class="btn btn-primary" :disabled="saving">Add Record</button>
            </div>
          </form>
        </div>
      </div>
      <!-- Add Super Modal -->
      <div v-if="showSuperModal" class="modal-overlay" @click.self="showSuperModal = false">
        <div class="modal-card">
          <div class="modal-header">
            <h2>Add Superannuation Account</h2>
            <button class="btn-close" @click="showSuperModal = false">&times;</button>
          </div>
          <form @submit.prevent="addSuper" class="modal-form">
            <div class="form-group">
              <label>Fund Name</label>
              <input v-model="superForm.fundName" type="text" required placeholder="e.g. AustralianSuper" />
            </div>
            <div class="form-row">
              <div class="form-group">
                <label>Member Number</label>
                <input v-model="superForm.memberNumber" type="text" />
              </div>
              <div class="form-group">
                <label>Current Balance</label>
                <input v-model.number="superForm.currentBalance" type="number" step="0.01" required />
              </div>
            </div>
            <div class="form-group">
              <label>Investment Option</label>
              <input v-model="superForm.investmentOption" type="text" placeholder="Balanced, High Growth, etc." />
            </div>
            <div class="modal-actions">
              <button type="button" class="btn btn-secondary" @click="showSuperModal = false">Cancel</button>
              <button type="submit" class="btn btn-primary" :disabled="saving">Add Account</button>
            </div>
          </form>
        </div>
      </div>
    </Teleport>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue';
import { useRoute } from 'vue-router';
import api from '../../services/api';

const route = useRoute();
const person = ref(null);
const taxPosition = ref(null);
const loading = ref(true);
const saving = ref(false);

const showEditModal = ref(false);
const showIncomeModal = ref(false);
const showDeductionModal = ref(false);
const showSuperModal = ref(false);

const editForm = ref({});
const incomeForm = ref({ employer: '', fiscalYear: 2025, grossSalary: 0, taxWithheld: 0, superContribution: 0 });
const deductionForm = ref({ category: 'WorkRelated', description: '', fiscalYear: 2025, amount: 0 });
const superForm = ref({ fundName: '', memberNumber: '', currentBalance: 0, investmentOption: 'Balanced', preservationDate: new Date('2050-01-01').toISOString() });

const loadData = async () => {
  try {
    const [personRes, taxRes] = await Promise.all([
      api.get(`/persons/${route.params.id}`),
      api.get(`/persons/${route.params.id}/tax-position`)
    ]);
    person.value = personRes.data;
    taxPosition.value = taxRes.data;
    editForm.value = { ...personRes.data, dateOfBirth: personRes.data.dateOfBirth?.split('T')[0] };
  } catch (e) {
    console.error('Failed to load person data', e);
  } finally {
    loading.value = false;
  }
};

const updatePerson = async () => {
  saving.value = true;
  try {
    await api.put(`/persons/${person.value.id}`, editForm.value);
    showEditModal.value = false;
    await loadData();
  } catch (e) {
    alert('Update failed');
  } finally {
    saving.value = false;
  }
};

const openAddIncome = () => {
  showIncomeModal.value = true;
};

const addIncome = async () => {
  saving.value = true;
  try {
    await api.post(`/persons/${person.value.id}/income`, incomeForm.value);
    showIncomeModal.value = false;
    await loadData();
  } catch (e) {
    alert('Failed to add income');
  } finally {
    saving.value = false;
  }
};

const deleteIncome = async (id) => {
  if (!confirm('Delete this income record?')) return;
  try {
    await api.delete(`/persons/${person.value.id}/income/${id}`);
    await loadData();
  } catch (e) {
    alert('Delete failed');
  }
};

const openAddDeduction = () => {
  showDeductionModal.value = true;
};

const addDeduction = async () => {
  saving.value = true;
  try {
    await api.post(`/persons/${person.value.id}/deductions`, deductionForm.value);
    showDeductionModal.value = false;
    await loadData();
  } catch (e) {
    alert('Failed to add deduction');
  } finally {
    saving.value = false;
  }
};

const deleteDeduction = async (id) => {
  if (!confirm('Delete this deduction record?')) return;
  try {
    await api.delete(`/persons/${person.value.id}/deductions/${id}`);
    await loadData();
  } catch (e) {
    alert('Delete failed');
  }
};

const openAddSuper = () => {
  showSuperModal.value = true;
};

const addSuper = async () => {
  saving.value = true;
  try {
    await api.post(`/persons/${person.value.id}/super`, superForm.value);
    showSuperModal.value = false;
    await loadData();
  } catch (e) {
    alert('Failed to add super account');
  } finally {
    saving.value = false;
  }
};

const deleteSuper = async (id) => {
  if (!confirm('Delete this super account?')) return;
  try {
    await api.delete(`/persons/${person.value.id}/super/${id}`);
    await loadData();
  } catch (e) {
    alert('Delete failed');
  }
};

const formatDate = (dateString) => {
  if (!dateString) return 'N/A';
  return new Date(dateString).toLocaleDateString('en-AU', { day: 'numeric', month: 'short', year: 'numeric' });
};

const formatCurrency = (val) => {
  return new Intl.NumberFormat('en-AU', { style: 'currency', currency: 'AUD' }).format(val || 0);
};

const getTaxRateClass = (rate) => {
  if (rate <= 0.19) return 'tax-low-text';
  if (rate <= 0.325) return 'tax-medium-text';
  return 'tax-high-text';
};

const getTaxProgress = (income) => {
  const max = 200000;
  return Math.min(100, (income / max) * 100);
};

onMounted(loadData);
</script>

<style scoped>
.person-detail {
  max-width: 1200px;
  margin: 0 auto;
}

.breadcrumb {
  display: flex;
  gap: var(--spacing-sm);
  margin-bottom: var(--spacing-sm);
  font-size: var(--font-size-sm);
  color: var(--color-text-muted);
}

.breadcrumb a {
  color: var(--color-accent);
}

.meta-row {
  display: flex;
  gap: var(--spacing-lg);
  margin-top: var(--spacing-sm);
  font-size: var(--font-size-sm);
  color: var(--color-text-secondary);
}

.detail-grid {
  display: grid;
  grid-template-columns: 2fr 1fr;
  gap: var(--spacing-xl);
  margin-top: var(--spacing-xl);
}

.section-card {
  padding: var(--spacing-xl);
}

.section-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: var(--spacing-lg);
}

.data-table {
  width: 100%;
  border-collapse: collapse;
}

.data-table th, .data-table td {
  padding: var(--spacing-md);
  text-align: left;
  border-bottom: 1px solid var(--color-border);
}

.tax-summary-card {
  padding: var(--spacing-xl);
  color: white;
}

.gradient-blue {
  background: linear-gradient(135deg, #1e40af 0%, #3b82f6 100%);
}

.tax-metric {
  display: flex;
  justify-content: space-between;
  margin-top: var(--spacing-md);
}

.tax-metric .label {
  opacity: 0.8;
  font-size: var(--font-size-sm);
}

.tax-metric .value {
  font-weight: 700;
  font-size: var(--font-size-lg);
}

.progress-bar {
  height: 8px;
  background: rgba(255,255,255,0.2);
  border-radius: var(--radius-full);
  margin-top: var(--spacing-lg);
  overflow: hidden;
}

.progress-fill {
  height: 100%;
  background: white;
  border-radius: var(--radius-full);
}

.super-item {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: var(--spacing-md);
  border-bottom: 1px solid var(--color-border);
}

.super-info .fund-name { font-weight: 600; }
.super-info .fund-meta { font-size: var(--font-size-xs); color: var(--color-text-muted); }
.super-balance { font-weight: 700; }

.empty-state-mini {
  padding: var(--spacing-lg);
  text-align: center;
  color: var(--color-text-muted);
  font-style: italic;
}

.badge {
  padding: 2px 8px;
  background: var(--color-bg-elevated);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-full);
  font-size: var(--font-size-xs);
}

.tax-low-text { color: #10b981; }
.tax-medium-text { color: #f59e0b; }
.tax-high-text { color: #ef4444; }

.btn-danger-text { color: var(--color-danger); }
.text-right { text-align: right; }
.font-semibold { font-weight: 600; }

/* Modal Styles Reuse or define in global if missed */
.modal-overlay {
  position: fixed; inset: 0; background: rgba(0,0,0,0.7); display: flex;
  justify-content: center; align-items: center; z-index: 1000; backdrop-filter: blur(4px);
}
.modal-card {
  background: var(--color-bg-card); border-radius: var(--radius-lg); width: 100%;
  max-width: 480px; max-height: 90vh; overflow: auto; border: 1px solid var(--color-border);
}
.modal-header { display: flex; justify-content: space-between; align-items: center; padding: var(--spacing-lg); border-bottom: 1px solid var(--color-border); }
.modal-form { padding: var(--spacing-lg); }
.form-group { margin-bottom: var(--spacing-lg); }
.form-row { display: grid; grid-template-columns: 1fr 1fr; gap: var(--spacing-md); }
.modal-actions { display: flex; justify-content: flex-end; gap: var(--spacing-md); padding-top: var(--spacing-lg); border-top: 1px solid var(--color-border); }
</style>

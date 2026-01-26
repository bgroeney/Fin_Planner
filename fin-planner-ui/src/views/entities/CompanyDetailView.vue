<template>
  <div class="company-detail animate-fade-in" v-if="company">
    <div class="header-row">
      <div class="header-content">
        <div class="breadcrumb">
          <router-link to="/entities">Entities</router-link>
          <span class="separator">/</span>
          <span class="current">{{ company.companyName }}</span>
        </div>
        <h1>{{ company.companyName }}</h1>
        <div class="meta-row">
          <span class="meta-item">🏢 {{ company.isBaseRateEntity ? 'Base Rate Entity (25%)' : 'Full Rate (30%)' }}</span>
          <span class="meta-item">🆔 ABN: {{ company.ABN || 'N/A' }} / ACN: {{ company.ACN || 'N/A' }}</span>
          <span class="meta-item">📅 Inc: {{ formatDate(company.incorporationDate) }}</span>
        </div>
      </div>
      <div class="actions">
        <button class="btn btn-primary" @click="showEditModal = true">Edit Company</button>
      </div>
    </div>

    <div class="detail-grid">
      <!-- Top Row Summary Cards -->
      <div class="summary-cards">
        <div class="card summary-card gradient-emerald">
          <span class="label">Retained Profits</span>
          <span class="value">{{ formatCurrency(company.retainedProfits) }}</span>
        </div>
        <div class="card summary-card gradient-amber">
          <span class="label">Franking Account</span>
          <span class="value">{{ formatCurrency(company.frankingAccountBalance) }}</span>
        </div>
        <div class="card summary-card gradient-blue">
          <span class="label">Active Loans (Div 7A)</span>
          <span class="value">{{ formatCurrency(summary?.totalLoanBalance || 0) }}</span>
          <span class="sub-label">{{ summary?.activeLoans || 0 }} active loan(s)</span>
        </div>
      </div>

      <!-- Warnings Section if any -->
      <div v-if="summary?.division7AWarnings?.length > 0" class="card warning-card mt-xl">
        <h3>⚠️ Division 7A Compliance Warnings</h3>
        <p>The following loans have not met their minimum yearly repayment for the current FY:</p>
        <div v-for="w in summary.division7AWarnings" :key="w.borrowerName" class="warning-item">
          <strong>{{ w.borrowerName }}</strong>: Paid {{ formatCurrency(w.repaidThisYear) }} / Min {{ formatCurrency(w.minimumYearlyRepayment) }} 
          <span class="shortfall">(Shortfall: {{ formatCurrency(w.shortfall) }})</span>
        </div>
      </div>

      <!-- Main Content -->
      <div class="content-grid">
        <!-- Division 7A Loans -->
        <div class="card section-card">
          <div class="section-header">
            <h2>📜 Division 7A Loans</h2>
            <button class="btn btn-secondary btn-sm" @click="showLoanModal = true">New Loan Record</button>
          </div>
          
          <div v-if="company.division7ALoans?.length === 0" class="empty-state-mini">
            No Div 7A loans recorded for this company.
          </div>
          
          <div v-for="loan in company.division7ALoans" :key="loan.id" class="loan-item">
            <div class="loan-main">
              <div class="loan-borrower">Borrower: {{ loan.borrowerName }}</div>
              <div class="loan-meta">Dated {{ formatDate(loan.loanDate) }} • {{ loan.loanTermYears }} year term</div>
              <div class="loan-status">
                <span class="status-pill" :class="loan.status.toLowerCase()">{{ loan.status }}</span>
                <button v-if="loan.status === 'Active'" class="btn btn-secondary btn-xs ml-sm" @click="openRepaymentModal(loan)">Record Repayment</button>
              </div>
            </div>
            <div class="loan-financials">
              <div class="metric">
                <span class="l">Principal</span>
                <span class="v">{{ formatCurrency(loan.principalAmount) }}</span>
              </div>
              <div class="metric">
                <span class="l">Balance</span>
                <span class="v font-bold">{{ formatCurrency(loan.currentBalance) }}</span>
              </div>
              <div class="metric">
                <span class="l">Min Repayment</span>
                <span class="v text-primary">{{ formatCurrency(loan.minimumYearlyRepayment) }}</span>
              </div>
            </div>
          </div>
        </div>

        <!-- Dividend History -->
        <div class="card section-card">
          <div class="section-header">
            <h2>💰 Dividend History</h2>
            <button class="btn btn-secondary btn-sm" @click="showDividendModal = true">Declare Dividend</button>
          </div>

          <table class="data-table" v-if="company.dividends?.length > 0">
            <thead>
              <tr>
                <th>Date</th>
                <th class="text-right">Amount</th>
                <th class="text-right">Franking Credits</th>
                <th>Status</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="div in company.dividends" :key="div.id">
                <td>{{ formatDate(div.paymentDate) }}</td>
                <td class="text-right font-semibold">{{ formatCurrency(div.amount) }}</td>
                <td class="text-right text-success">{{ formatCurrency(div.frankingCredits) }} ({{ div.frankingPercentage }}%)</td>
                <td><span class="status-pill" :class="div.status.toLowerCase()">{{ div.status }}</span></td>
              </tr>
            </tbody>
          </table>
          <div v-else class="empty-state-mini">No dividends recorded.</div>
        </div>
      </div>
    </div>

    <!-- Modals -->
    <Teleport to="body">
        <!-- Edit Company Modal -->
        <div v-if="showEditModal" class="modal-overlay" @click.self="showEditModal = false">
            <div class="modal-card">
                <div class="modal-header">
                    <h2>Edit Company</h2>
                    <button class="btn-close" @click="showEditModal = false">&times;</button>
                </div>
                <form @submit.prevent="updateCompany" class="modal-form">
                    <div class="form-group">
                        <label>Company Name</label>
                        <input v-model="editForm.companyName" type="text" required />
                    </div>
                    <div class="form-row">
                        <div class="form-group">
                            <label>ABN</label>
                            <input v-model="editForm.abn" type="text" />
                        </div>
                        <div class="form-group">
                            <label>ACN</label>
                            <input v-model="editForm.acn" type="text" />
                        </div>
                    </div>
                    <div class="form-group">
                        <label class="checkbox-label">
                            <input type="checkbox" v-model="editForm.isBaseRateEntity" />
                            Base rate entity (lower tax rate)
                        </label>
                    </div>
                    <div class="modal-actions">
                        <button type="button" class="btn btn-secondary" @click="showEditModal = false">Cancel</button>
                        <button type="submit" class="btn btn-primary" :disabled="saving">Save Changes</button>
                    </div>
                </form>
            </div>
        </div>

        <!-- New Loan Modal -->
        <div v-if="showLoanModal" class="modal-overlay" @click.self="showLoanModal = false">
            <div class="modal-card">
                <div class="modal-header">
                    <h2>Record Division 7A Loan</h2>
                    <button class="btn-close" @click="showLoanModal = false">&times;</button>
                </div>
                <form @submit.prevent="createLoan" class="modal-form">
                    <div class="form-group">
                        <label>Borrower Name</label>
                        <input v-model="loanForm.borrowerName" type="text" required placeholder="e.g. John Smith" />
                    </div>
                    <div class="form-row">
                        <div class="form-group">
                            <label>Principal Amount</label>
                            <input v-model.number="loanForm.principalAmount" type="number" step="0.01" required />
                        </div>
                        <div class="form-group">
                            <label>Loan Date</label>
                            <input v-model="loanForm.loanDate" type="date" required />
                        </div>
                    </div>
                    <div class="form-row">
                        <div class="form-group">
                            <label>Interest Rate (%)</label>
                            <input v-model.number="loanForm.interestRate" type="number" step="0.01" placeholder="8.47" />
                            <small>Benchmark for FY25 is 8.47%</small>
                        </div>
                        <div class="form-group">
                            <label>Term (Years)</label>
                            <select v-model.number="loanForm.loanTermYears">
                                <option :value="7">7 Years (Unsecured)</option>
                                <option :value="25">25 Years (Secured)</option>
                            </select>
                        </div>
                    </div>
                    <div class="modal-actions">
                        <button type="button" class="btn btn-secondary" @click="showLoanModal = false">Cancel</button>
                        <button type="submit" class="btn btn-primary" :disabled="saving">Create Loan</button>
                    </div>
                </form>
            </div>
        </div>

        <!-- Loan Repayment Modal -->
        <div v-if="showRepaymentModal" class="modal-overlay" @click.self="showRepaymentModal = false">
            <div class="modal-card">
                <div class="modal-header">
                    <h2>Record Loan Repayment</h2>
                    <button class="btn-close" @click="showRepaymentModal = false">&times;</button>
                </div>
                <form @submit.prevent="recordRepayment" class="modal-form">
                    <div class="form-group">
                        <label>Borrower: {{ selectedLoan?.borrowerName }}</label>
                        <div class="loan-info-mini">
                            Remaining Balance: {{ formatCurrency(selectedLoan?.currentBalance) }}<br/>
                            Min Yearly Repayment: {{ formatCurrency(selectedLoan?.minimumYearlyRepayment) }}
                        </div>
                    </div>
                    <div class="form-row">
                        <div class="form-group">
                            <label>Repayment Amount</label>
                            <input v-model.number="repaymentForm.amount" type="number" step="0.01" required />
                        </div>
                        <div class="form-group">
                            <label>Payment Date</label>
                            <input v-model="repaymentForm.paymentDate" type="date" required />
                        </div>
                    </div>
                    <div class="modal-actions">
                        <button type="button" class="btn btn-secondary" @click="showRepaymentModal = false">Cancel</button>
                        <button type="submit" class="btn btn-primary" :disabled="saving">Record Payment</button>
                    </div>
                </form>
            </div>
        </div>

        <!-- Declare Dividend Modal -->
        <div v-if="showDividendModal" class="modal-overlay" @click.self="showDividendModal = false">
            <div class="modal-card">
                <div class="modal-header">
                    <h2>Declare Dividend</h2>
                    <button class="btn-close" @click="showDividendModal = false">&times;</button>
                </div>
                <form @submit.prevent="declareDividend" class="modal-form">
                    <div class="form-group">
                        <label>Dividend Amount (Net)</label>
                        <input v-model.number="dividendForm.amount" type="number" step="0.01" required />
                    </div>
                    <div class="form-row">
                        <div class="form-group">
                            <label>Declaration Date</label>
                            <input v-model="dividendForm.declarationDate" type="date" required />
                        </div>
                        <div class="form-group">
                            <label>Payment Date</label>
                            <input v-model="dividendForm.paymentDate" type="date" required />
                        </div>
                    </div>
                    <div class="form-group">
                        <label>Franking Percentage (%)</label>
                        <input v-model.number="dividendForm.frankingPercentage" type="number" step="1" max="100" />
                        <small>Max possible credits will be calculated based on {{ company.taxRate * 100 }}% tax rate.</small>
                    </div>
                    <div class="modal-actions">
                        <button type="button" class="btn btn-secondary" @click="showDividendModal = false">Cancel</button>
                        <button type="submit" class="btn btn-primary" :disabled="saving">Declare & Subtract Profits</button>
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
const company = ref(null);
const summary = ref(null);
const loading = ref(true);
const saving = ref(false);

const showEditModal = ref(false);
const showLoanModal = ref(false);
const showRepaymentModal = ref(false);
const showDividendModal = ref(false);

const selectedLoan = ref(null);
const editForm = ref({});
const loanForm = ref({
    borrowerName: '',
    principalAmount: 0,
    interestRate: 0.0847,
    loanDate: new Date().toISOString().split('T')[0],
    loanTermYears: 7,
    isSecured: false
});
const repaymentForm = ref({
    amount: 0,
    paymentDate: new Date().toISOString().split('T')[0]
});
const dividendForm = ref({
    amount: 0,
    declarationDate: new Date().toISOString().split('T')[0],
    paymentDate: new Date().toISOString().split('T')[0],
    frankingPercentage: 100
});

const loadData = async () => {
  try {
    const [compRes, sumRes] = await Promise.all([
        api.get(`/companies/${route.params.id}`),
        api.get(`/companies/${route.params.id}/summary`)
    ]);
    company.value = compRes.data;
    summary.value = sumRes.data;
    editForm.value = { ...compRes.data };
  } catch (e) {
    console.error('Failed to load company data', e);
  } finally {
    loading.value = false;
  }
};

const updateCompany = async () => {
    saving.value = true;
    try {
        await api.put(`/companies/${company.value.id}`, editForm.value);
        showEditModal.value = false;
        await loadData();
    } catch (e) {
        alert('Update failed');
    } finally {
        saving.value = false;
    }
};

const createLoan = async () => {
    saving.value = true;
    try {
        const payload = { ...loanForm.value, interestRate: loanForm.value.interestRate };
        if (payload.interestRate > 1) payload.interestRate = payload.interestRate / 100;

        await api.post(`/companies/${company.value.id}/loans`, payload);
        showLoanModal.value = false;
        await loadData();
    } catch (e) {
        alert(e.response?.data || 'Failed to create loan');
    } finally {
        saving.value = false;
    }
};

const openRepaymentModal = (loan) => {
    selectedLoan.value = loan;
    repaymentForm.value.amount = loan.minimumYearlyRepayment;
    showRepaymentModal.value = true;
};

const recordRepayment = async () => {
    saving.value = true;
    try {
        await api.post(`/companies/${company.value.id}/loans/${selectedLoan.value.id}/repayment`, repaymentForm.value);
        showRepaymentModal.value = false;
        await loadData();
    } catch (e) {
        alert('Repayment failed');
    } finally {
        saving.value = false;
    }
};

const declareDividend = async () => {
    saving.value = true;
    try {
        await api.post(`/companies/${company.value.id}/dividends`, dividendForm.value);
        showDividendModal.value = false;
        await loadData();
    } catch (e) {
        alert(e.response?.data || 'Failed to declare dividend');
    } finally {
        saving.value = false;
    }
};

const formatDate = (dateString) => {
  if (!dateString) return 'N/A';
  return new Date(dateString).toLocaleDateString('en-AU', { day: 'numeric', month: 'short', year: 'numeric' });
};

const formatCurrency = (val) => {
  return new Intl.NumberFormat('en-AU', { style: 'currency', currency: 'AUD', maximumFractionDigits: 0 }).format(val || 0);
};

onMounted(loadData);
</script>

<style scoped>
.company-detail { max-width: 1200px; margin: 0 auto; }
.breadcrumb { display: flex; gap: var(--spacing-sm); margin-bottom: var(--spacing-sm); font-size: var(--font-size-sm); color: var(--color-text-muted); }
.breadcrumb a { color: var(--color-accent); }
.meta-row { display: flex; gap: var(--spacing-lg); margin-top: var(--spacing-sm); font-size: var(--font-size-sm); color: var(--color-text-secondary); }

.summary-cards { display: grid; grid-template-columns: repeat(3, 1fr); gap: var(--spacing-xl); margin-top: var(--spacing-xl); }
.summary-card { padding: var(--spacing-xl); color: white; display: flex; flex-direction: column; }
.summary-card .label { font-size: var(--font-size-sm); opacity: 0.9; }
.summary-card .value { font-size: var(--font-size-2xl); font-weight: 800; }

.gradient-emerald { background: linear-gradient(135deg, #10b981 0%, #059669 100%); }
.gradient-amber { background: linear-gradient(135deg, #f59e0b 0%, #d97706 100%); }
.gradient-blue { background: linear-gradient(135deg, #1e40af 0%, #3b82f6 100%); }

.content-grid { display: grid; grid-template-columns: 1fr 1fr; gap: var(--spacing-xl); margin-top: var(--spacing-xl); }
.section-card { padding: var(--spacing-xl); }
.section-header { display: flex; justify-content: space-between; align-items: center; margin-bottom: var(--spacing-lg); }

.loan-item { padding: var(--spacing-lg); border: 1px solid var(--color-border); border-radius: var(--radius-md); background: var(--color-bg-elevated); margin-bottom: var(--spacing-md); }
.loan-borrower { font-weight: 700; font-size: var(--font-size-base); }
.loan-meta { font-size: var(--font-size-xs); color: var(--color-text-muted); margin: 4px 0; }
.loan-financials { display: flex; justify-content: space-between; margin-top: var(--spacing-md); padding-top: var(--spacing-md); border-top: 1px dotted var(--color-border); }
.loan-financials .metric { display: flex; flex-direction: column; }
.loan-financials .l { font-size: 10px; text-transform: uppercase; color: var(--color-text-muted); }
.loan-financials .v { font-size: var(--font-size-sm); font-weight: 500; }

.data-table { width: 100%; border-collapse: collapse; }
.data-table th, .data-table td { padding: var(--spacing-md); text-align: left; border-bottom: 1px solid var(--color-border); font-size: var(--font-size-sm); }

.status-pill { padding: 2px 8px; border-radius: var(--radius-full); font-size: var(--font-size-xs); font-weight: 600; text-transform: uppercase; }
.status-pill.active { background: rgba(16, 185, 129, 0.15); color: #10b981; }

.text-right { text-align: right; }
.font-bold { font-weight: 800; }

/* Modal Styles */
.modal-overlay { position: fixed; inset: 0; background: rgba(0,0,0,0.7); display: flex; justify-content: center; align-items: center; z-index: 1000; backdrop-filter: blur(4px); }
.modal-card { background: var(--color-bg-card); border-radius: var(--radius-lg); width: 100%; max-width: 480px; border: 1px solid var(--color-border); }
.modal-header { display: flex; justify-content: space-between; align-items: center; padding: var(--spacing-lg); border-bottom: 1px solid var(--color-border); }
.modal-form { padding: var(--spacing-lg); }
.checkbox-label { display: flex; align-items: center; gap: var(--spacing-sm); cursor: pointer; }
.modal-actions { display: flex; justify-content: flex-end; gap: var(--spacing-md); padding-top: var(--spacing-lg); border-top: 1px solid var(--color-border); }

.empty-state-mini { text-align: center; color: var(--color-text-muted); padding: var(--spacing-xl); font-style: italic; }
</style>

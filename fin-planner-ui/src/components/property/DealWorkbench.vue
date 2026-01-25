<template>
  <div v-if="!deal" class="deal-workbench card">
    <div class="workbench-loading">
      <div class="spinner"></div>
      <p>Loading deal...</p>
    </div>
  </div>
  <div v-else class="deal-workbench card">
    <!-- Workbench Header -->
    <div class="workbench-header">
      <div class="deal-info">
        <h2 class="deal-title">{{ deal.name }}</h2>
        <div class="deal-meta">
          <span v-if="deal.address" class="deal-address">{{ deal.address }}</span>
          <span v-if="deal.buildingType" class="deal-type badge">{{ deal.buildingType }}</span>
        </div>
      </div>
      <div class="lifecycle-container">
         <LifecycleStepper 
            :current-status="deal.status" 
            :loading="transitioning"
            @transition="handleLifecycleTransition"
         />
      </div>

    </div>

    <!-- View Toggle Tabs -->
    <div class="view-tabs">

      <button 
        :class="['view-tab', { active: viewMode === 'summary' }]"
        @click="viewMode = 'summary'"
      >
        <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
          <rect x="3" y="3" width="18" height="18" rx="2" ry="2"></rect>
          <line x1="3" y1="9" x2="21" y2="9"></line>
          <line x1="9" y1="21" x2="9" y2="9"></line>
        </svg>
        Assumptions & Inputs
      </button>

      <button 
        :class="['view-tab', { active: viewMode === 'detailed' }]"
        @click="viewMode = 'detailed'"
      >
        <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
          <rect x="3" y="3" width="18" height="18" rx="2" ry="2"></rect>
          <line x1="3" y1="9" x2="21" y2="9"></line>
          <line x1="3" y1="15" x2="21" y2="15"></line>
          <line x1="9" y1="3" x2="9" y2="21"></line>
          <line x1="15" y1="3" x2="15" y2="21"></line>
        </svg>
        Detailed Model
      </button>
      <button 
        :class="['view-tab', { active: viewMode === 'risk' }]"
        @click="viewMode = 'risk'"
      >
        <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
          <path d="M22 12h-4l-3 9L9 3l-3 9H2"/>
        </svg>
        Risk Analysis
      </button>
      <button 
        :class="['view-tab', { active: viewMode === 'documents' }]"
        @click="viewMode = 'documents'"
      >
        <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
           <path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z"></path>
           <polyline points="14 2 14 8 20 8"></polyline>
        </svg>
        Documents
      </button>
    </div>

    <!-- Summary View (Edit Mode) -->
    <div v-if="viewMode === 'summary'" class="workbench-grid">
      <!-- Input Section -->
      <section class="input-section">
        <div class="section-header">
           <h3 class="section-title">Deal Inputs</h3>
           <div class="save-status">
              <span v-if="saving" class="status-saving">
                <span class="spinner-sm"></span> Saving...
              </span>
              <span v-else-if="lastSaved" class="status-saved">
                ✓ Saved
              </span>
           </div>
        </div>
        
        <!-- Acquisition Costs -->
        <div class="input-group">
          <h4 class="group-title">Acquisition</h4>
          <div class="input-row">
            <label>Asking Price</label>
            <div class="input-wrapper">
              <span class="input-prefix">$</span>
              <input type="number" :value="deal.askingPrice" @change="updateField('askingPrice', $event)" />
            </div>
          </div>
          <div class="input-row">
            <label>Stamp Duty Rate</label>
            <div class="input-wrapper">
              <input type="number" step="0.1" :value="deal.stampDutyRate" @change="updateField('stampDutyRate', $event)" />
              <span class="input-suffix">%</span>
            </div>
          </div>
          <div class="input-row">
            <label>Legal Costs</label>
            <div class="input-wrapper">
              <span class="input-prefix">$</span>
              <input type="number" :value="deal.legalCosts" @change="updateField('legalCosts', $event)" />
            </div>
          </div>
          <div class="input-row">
            <label>CapEx Reserve</label>
            <div class="input-wrapper">
              <span class="input-prefix">$</span>
              <input type="number" :value="deal.capExReserve" @change="updateField('capExReserve', $event)" />
            </div>
          </div>
          <div class="input-row calculated">
            <label>Total Cost</label>
            <span class="calculated-value">{{ formatCurrency(totalAcquisitionCost) }}</span>
          </div>
        </div>

        <!-- Income -->
        <div class="input-group">
          <h4 class="group-title">Income</h4>
          <div class="input-row">
            <label>Gross Rent (p.a.)</label>
            <div class="input-wrapper with-variance">
              <span class="input-prefix">$</span>
              <input type="number" :value="deal.estimatedGrossRent" @change="updateField('estimatedGrossRent', $event)" />
              <span class="variance-indicator" title="Uncertainty range">±{{ deal.rentVariancePercent }}%</span>
            </div>
          </div>
          <div class="input-row">
            <label class="flex-label">
               <span>Vacancy / Lease</span>
               <button class="btn-xs-toggle" @click="toggleLeaseMode">{{ deal.leaseDetailsJson ? 'Simple' : 'Detailed' }}</button>
            </label>
            
            <!-- Simple Mode -->
            <div v-if="!leaseDetails" class="input-wrapper with-variance">
              <input type="number" step="0.5" :value="deal.vacancyRatePercent" @change="updateField('vacancyRatePercent', $event)" />
              <span class="input-suffix">%</span>
              <span class="variance-indicator" title="Uncertainty range">±{{ deal.vacancyVariancePercent }}%</span>
            </div>
          </div>
          
          <!-- Detailed Lease Mode -->
          <div v-if="leaseDetails" class="detailed-subgroup">
             <div class="input-row sub-row">
                <label>Remaining Term</label>
                <div class="input-wrapper">
                   <input type="number" :value="leaseDetails.remainingTerm" @change="updateLeaseField('remainingTerm', $event)" class="w-full" />
                   <span class="input-suffix">yrs</span>
                </div>
             </div>
             <div class="input-row sub-row">
                <label>Review Type</label>
                <select :value="leaseDetails.reviewType" @change="updateLeaseField('reviewType', $event)" class="select-sm">
                   <option value="fixed">Fixed %</option>
                   <option value="cpi">CPI / Market</option>
                </select>
             </div>
             <div v-if="leaseDetails.reviewType === 'fixed'" class="input-row sub-row">
                <label>Fixed Increase</label>
                <div class="input-wrapper">
                   <input type="number" step="0.1" :value="leaseDetails.reviewValue" @change="updateLeaseField('reviewValue', $event)" />
                   <span class="input-suffix">%</span>
                </div>
             </div>
             <div class="input-row sub-row">
                <label title="Expected vacancy after lease expiry">Vacancy Shift</label>
                <div class="input-wrapper">
                   <input type="number" :value="leaseDetails.vacancyDurationMonths" @change="updateLeaseField('vacancyDurationMonths', $event)" />
                   <span class="input-suffix">mos</span>
                </div>
             </div>
          </div>
          <div class="input-row">
            <label>Management Fee</label>
            <div class="input-wrapper">
              <input type="number" step="0.5" :value="deal.managementFeePercent" @change="updateField('managementFeePercent', $event)" />
              <span class="input-suffix">%</span>
            </div>
          </div>
          <div class="input-row">
            <label>Outgoings (p.a.)</label>
            <div class="input-wrapper">
              <span class="input-prefix">$</span>
              <input type="number" :value="deal.outgoingsEstimate" @change="updateField('outgoingsEstimate', $event)" />
            </div>
          </div>
          <div class="input-row calculated">
            <label>Net Operating Income</label>
            <span class="calculated-value">{{ formatCurrency(calculatedNOI) }}</span>
          </div>
        </div>

        <!-- Financing -->
        <div class="input-group">
          <h4 class="group-title">Financing</h4>
          <div class="input-row">
            <label class="flex-label">
                <span>Loan Structure</span>
                <button class="btn-xs-toggle" @click="toggleLoanMode">{{ loanDetails ? 'Simple' : 'Multi' }}</button>
            </label>
          </div>

          <!-- Simple Loan Mode -->
          <template v-if="!loanDetails || loanDetails.length === 0">
              <div class="input-row">
                <label>Loan Amount</label>
                <div class="input-wrapper">
                  <span class="input-prefix">$</span>
                  <input type="number" :value="deal.loanAmount" @change="updateField('loanAmount', $event)" />
                </div>
              </div>
              <div class="input-row">
                <label>Interest Rate</label>
                <div class="input-wrapper with-variance">
                  <input type="number" step="0.1" :value="deal.interestRatePercent" @change="updateField('interestRatePercent', $event)" />
                  <span class="input-suffix">%</span>
                  <span class="variance-indicator" title="Uncertainty range">±{{ deal.interestVariancePercent }}%</span>
                </div>
              </div>
          </template>

          <!-- Operations for Multi-Loan -->
          <div v-else class="detailed-subgroup">
             <div v-for="(loan, idx) in loanDetails" :key="idx" class="loan-item">
                <div class="loan-row">
                   <input type="text" v-model="loan.name" placeholder="Tranche A" class="input-xs name-input" @change="persistLoanDetails"/>
                   <button class="btn-icon-xs" @click="removeLoan(idx)" title="Remove">×</button>
                </div>
                <div class="loan-row inputs">
                    <div class="input-wrapper xs">
                       <span class="input-prefix">$</span>
                       <input type="number" v-model.number="loan.amount" @change="persistLoanDetails" />
                    </div>
                    <div class="input-wrapper xs">
                       <input type="number" step="0.1" v-model.number="loan.rate" @change="persistLoanDetails" />
                       <span class="input-suffix">%</span>
                    </div>
                </div>
             </div>
             <button class="btn-add-loan" @click="addLoan">+ Add Split</button>
             
             <!-- Summary -->
             <div class="loan-summary">
                <small>Total: {{ formatCurrency(totalLoanAmount) }} @ {{ weightedAvgRate.toFixed(2) }}%</small>
             </div>
          </div>
          <div class="input-row calculated">
            <label>LVR</label>
            <span class="calculated-value">{{ calculatedLVR.toFixed(1) }}%</span>
          </div>
        </div>

        <!-- Growth & Period -->
        <div class="input-group">
          <h4 class="group-title">Assumptions</h4>
          <div class="input-row">
            <label>Capital Growth</label>
            <div class="input-wrapper with-variance">
              <input type="number" step="0.5" :value="deal.capitalGrowthPercent" @change="updateField('capitalGrowthPercent', $event)" />
              <span class="input-suffix">%</span>
              <span class="variance-indicator" title="Uncertainty range">±{{ deal.capitalGrowthVariancePercent }}%</span>
            </div>
          </div>
          <div class="input-row">
            <label>Discount Rate</label>
            <div class="input-wrapper">
              <input type="number" step="0.1" :value="deal.discountRate || 8" @change="updateField('discountRate', $event)" />
              <span class="input-suffix">%</span>
            </div>
          </div>
          <div class="input-row">
            <label>Interest Variance</label>
            <div class="input-wrapper">
              <input type="number" step="0.1" min="0" :value="deal.interestVariancePercent" @change="updateField('interestVariancePercent', $event)" />
              <span class="input-suffix">±%</span>
            </div>
          </div>
          <div v-if="deal.discountRate < deal.interestRatePercent" class="validation-error">
             ⚠️ Discount ({{deal.discountRate}}%) should be ≥ Interest ({{deal.interestRatePercent}}%)
          </div>
          <div class="input-row">
            <label>Holding Period</label>
            <div class="input-wrapper">
              <input type="number" min="1" max="30" :value="deal.holdingPeriodYears" @change="updateField('holdingPeriodYears', $event)" />
              <span class="input-suffix">years</span>
            </div>
          </div>
        </div>

        <!-- Timing Risk moved to Risk Analysis tab -->
      </section>

      <!-- Results Summary (Read Only) -->
      <section class="results-section">
          <div class="metrics-grid" v-if="simulationResults">
            <div class="metric-card">
              <span class="metric-label">Median NPV</span>
              <span class="metric-value" :class="simulationResults.medianNPV >= 0 ? 'positive' : 'negative'">
                {{ formatCurrency(simulationResults.medianNPV) }}
              </span>
            </div>
            <div class="metric-card">
               <span class="metric-label">Median IRR</span>
               <span class="metric-value">{{ simulationResults.medianIRR.toFixed(1) }}%</span>
            </div>
          </div>
          <div class="no-results" v-else>
             <p>Configure inputs and click "Update & Run" in the header to assess risk.</p>
          </div>
      </section>
    </div>


    <!-- Risk Analysis (Read Only View) -->
    <div v-else-if="viewMode === 'risk'" class="risk-view">
        <div class="alert-banner" v-if="hasUnsavedChanges">
           <span>⚠️ The deal inputs have changed since the last simulation.</span>
           <button class="btn btn-sm btn-primary" @click="handleRunSimulation">Run New Simulation</button>
        </div>

        <div class="risk-actions-bar">
          <button 
            class="btn btn-primary run-simulation-btn full-width" 
            @click="handleRunSimulation"
            :disabled="runningSimulation"
          >
            <span v-if="runningSimulation" class="spinner"></span>
            <svg v-else width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <polygon points="5 3 19 12 5 21 5 3"></polygon>
            </svg>
            {{ runningSimulation ? 'Running...' : 'Update & Run Analysis' }}
          </button>
        </div>

        <PropertyRiskAnalyzer
            :initial-data="simulationInputSource"
            :initial-results="simulationResults"
            :detailed-cashflow="spreadsheetData"
            :hide-run-button="true"
            @update:inputs="handleInputsUpdate"
        />
    </div>


    <!-- Detailed Spreadsheet View -->
    <div v-else-if="viewMode === 'detailed'" class="spreadsheet-view">
      <CashflowSpreadsheet 
        :deal="deal"
        :holding-period="deal.holdingPeriodYears"
        :initial-overrides-json="deal.spreadsheetOverridesJson"
        @apply-changes="handleSpreadsheetChanges"
        @update:overrides="handleOverridesUpdate"
      />
    </div>

    <!-- Documents View -->
    <div v-else-if="viewMode === 'documents'" class="documents-view p-lg">
        <DealDocuments :deal-id="deal.id" />
    </div>

  </div>
</template>

<script setup>
import { ref, computed } from 'vue';
import { formatCurrency as formatCurrencyUtil } from '../../services/monteCarloEngine';
import MonteCarloChart from './MonteCarloChart.vue';
import DecisionMatrix from './DecisionMatrix.vue';
import CashflowSpreadsheet from './CashflowSpreadsheet.vue';
import PropertyRiskAnalyzer from './PropertyRiskAnalyzer.vue';
import LifecycleStepper from './LifecycleStepper.vue';
import DealDocuments from './DealDocuments.vue';

const viewMode = ref('summary'); // Default to Summary for interactive analysis
const spreadsheetData = ref(null);
const transitioning = ref(false);

const props = defineProps({
  deal: { type: Object, required: true },
  simulationResults: { type: Object, default: null },
  runningSimulation: { type: Boolean, default: false }
});

// Save state tracking
const saving = ref(false);
const lastSaved = ref(false);
let saveTimeout = null;

// Watch for deal updates to show "Saving"
// Since parent does the save, we can't know EXACTLY when api call finishes unless we add a prop.
// But we can show immediate feedback on input.
function triggerSaveFeedback() {
   saving.value = true;
   lastSaved.value = false;
   if (saveTimeout) clearTimeout(saveTimeout);
   
   // Fake it for UX responsiveness (optimistic)
   setTimeout(() => {
     saving.value = false;
     lastSaved.value = true;
     saveTimeout = setTimeout(() => lastSaved.value = false, 3000);
   }, 800);
}

const emit = defineEmits(['update', 'run-simulation', 'record-decision']);

// Computed calculations
const totalAcquisitionCost = computed(() => {
  const stampDuty = props.deal.askingPrice * (props.deal.stampDutyRate / 100);
  return props.deal.askingPrice + stampDuty + props.deal.legalCosts + props.deal.capExReserve;
});

const calculatedNOI = computed(() => {
  const effectiveIncome = props.deal.estimatedGrossRent * (1 - props.deal.vacancyRatePercent / 100);
  const managementCost = effectiveIncome * (props.deal.managementFeePercent / 100);
  return effectiveIncome - managementCost - props.deal.outgoingsEstimate;
});

const calculatedLVR = computed(() => {
  return props.deal.askingPrice > 0 ? (props.deal.loanAmount / props.deal.askingPrice) * 100 : 0;
});

const parsedYearlyDCF = computed(() => {
    // Legacy support or fallback if needed, mainly handled by PropertyRiskAnalyzer now
    return null;
});

const simulationInputSource = computed(() => {
    // If we have saved results with a snapshot, use that to show "What produced this graph"
    if (props.simulationResults?.inputsSnapshotJson) {
        try {
            return JSON.parse(props.simulationResults.inputsSnapshotJson);
        } catch (e) {
            console.error("Failed to parse input snapshot", e);
        }
    }
    // Fallback to current deal if no snapshot (e.g. first run)
    return props.deal;
});

// Detect if current deal inputs differ from the snapshot
const hasUnsavedChanges = computed(() => {
    if (!props.simulationResults?.inputsSnapshotJson) return false;
    // Simple check: compare Property Value & Rent as proxy
    //Ideally we compare deep equality but that's heavy.
    // Let's assume hitting "Update" in header syncs it.
    return false; // TODO: Implement dirty check
});

// Methods
function handleInputsUpdate(newInputs) {
    // When inputs change in PropertyRiskAnalyzer, sync them to parent state updates list
    // We update fields individually to trigger save on parent (AcquisitionPlanner) 
    // or batch them if we had a batch update method.
    // For now we map key inputs back to deal structure
    
    // Note: This matches the keys in PropertyDealsController UpdateDealRequest
    const updates = {
       askingPrice: newInputs.propertyValue, // Mapped from propertyValue
       estimatedGrossRent: newInputs.estimatedGrossRent,
       outgoingsEstimate: newInputs.outgoingsEstimate,
       vacancyRatePercent: newInputs.vacancyRatePercent,
       capitalGrowthPercent: newInputs.capitalGrowthPercent,
       capitalGrowthVariancePercent: newInputs.capitalGrowthVariancePercent,
       discountRate: newInputs.discountRate,
       loanAmount: newInputs.loanAmount,
       interestRatePercent: newInputs.interestRatePercent,
       interestVariancePercent: newInputs.interestVariancePercent,
       rentVariancePercent: newInputs.rentVariancePercent,
       holdingPeriodYears: newInputs.holdingPeriodYears,
       timeVarianceEarlyMonths: newInputs.timeVarianceEarly,
       timeVarianceLateMonths: newInputs.timeVarianceLate
    };
    
    emit('update', updates);
}

function handleLifecycleTransition(newStatus) {
    transitioning.value = true;
    // We emit update for 'status'. The parent handles the API call.
    // However, for 'Acquired', we might want special logic.
    // Assuming parent just saves the status update.
    emit('update', { status: newStatus });
    
    // Optimistic toggle
    setTimeout(() => transitioning.value = false, 1000); 
}

function updateField(field, event) {
  triggerSaveFeedback();
  const value = parseFloat(event.target.value) || 0;
  emit('update', { [field]: value });
}

function handleOverridesUpdate(json) {
  // Persist spreadsheet overrides
  triggerSaveFeedback();
  emit('update', { spreadsheetOverridesJson: json });
}

function handleDecision(decision, rationale) {
  emit('record-decision', decision, rationale);
}

function handleSpreadsheetChanges(cashflowData) {
  // Store custom cashflow data for simulation
  spreadsheetData.value = cashflowData;
  
  // If we have detailed cashflows (e.g. from the spreadsheet), ensuring they are passed
  // effectively happens via the :detailed-cashflow propbinding above if we are in Risk View.
  // But if we are in Spreadsheet view and click "Apply", we might want to update the deal state 
  // or trigger a simulation run if applicable.
  
  // Update deal with time variance settings if present
  if (cashflowData.timeVariance) {
    emit('update', {
      timeVarianceEarlyMonths: cashflowData.timeVariance.early,
      timeVarianceLateMonths: cashflowData.timeVariance.late
    });
  }
}

// Watch for deal changes to sync baseline to spreadsheet (if logic existed inside CashflowSpreadsheet)
// Since CashflowSpreadsheet takes :deal prop, it reacts automatically. 
// However, we want to ensure any internal overrides are preserved relative to the new base.
// CashflowSpreadsheet handles this internally via its computed properties (baseValue from prop, override from state).

function handleRunSimulation() {
  emit('run-simulation', spreadsheetData.value);
}

// Parsing Helpers
const leaseDetails = computed(() => {
    try {
        return props.deal.leaseDetailsJson ? JSON.parse(props.deal.leaseDetailsJson) : null;
    } catch { return null; }
});

const loanDetails = computed(() => {
    try {
       return props.deal.loanDetailsJson ? JSON.parse(props.deal.loanDetailsJson) : null;
    } catch { return null; }
});

const totalLoanAmount = computed(() => {
    if (!loanDetails.value) return props.deal.loanAmount;
    return loanDetails.value.reduce((sum, loan) => sum + (loan.amount || 0), 0);
});

const weightedAvgRate = computed(() => {
    if (!loanDetails.value || totalLoanAmount.value === 0) return props.deal.interestRatePercent;
    const weightedSum = loanDetails.value.reduce((sum, loan) => sum + (loan.amount * loan.rate), 0);
    return weightedSum / totalLoanAmount.value;
});

// Logic Methods
function toggleLeaseMode() {
    triggerSaveFeedback();
    if (leaseDetails.value) {
        // Switch to Simple (Clear details)
        emit('update', { leaseDetailsJson: "" });
    } else {
        // Switch to Detailed (Init default)
        const defaultLease = {
            remainingTerm: 3,
            reviewType: 'fixed',
            reviewValue: 3.0,
            vacancyDurationMonths: 3
        };
        emit('update', { leaseDetailsJson: JSON.stringify(defaultLease) });
    }
}

function updateLeaseField(field, event) {
    triggerSaveFeedback();
    const current = { ...leaseDetails.value };
    
    // Handle select vs input
    let val = event.target.value;
    if (event.target.type === 'number') val = parseFloat(val);
    
    current[field] = val;
    emit('update', { leaseDetailsJson: JSON.stringify(current) });
}

function toggleLoanMode() {
    triggerSaveFeedback();
    if (loanDetails.value) {
        // Clear multi-loan
        emit('update', { loanDetailsJson: "" });
    } else {
        // Init multi-loan with current simple values as first tranche
        const defaultLoans = [
            { name: 'Senior Debt', amount: props.deal.loanAmount, rate: props.deal.interestRatePercent }
        ];
        emit('update', { loanDetailsJson: JSON.stringify(defaultLoans) });
    }
}

function addLoan() {
    const current = [...(loanDetails.value || [])];
    current.push({ name: 'Mezzanine', amount: 0, rate: 8.0 });
    persistLoans(current);
}

function removeLoan(idx) {
    const current = [...loanDetails.value];
    current.splice(idx, 1);
    persistLoans(current);
}

function persistLoanDetails() {
    persistLoans(loanDetails.value);
}

function persistLoans(loans) {
    triggerSaveFeedback();
    const json = JSON.stringify(loans);
    
    // Also update the simple aggregate fields for backward compatibility / high-level display
    const total = loans.reduce((sum, l) => sum + (l.amount || 0), 0);
    let weightedRate = 0;
    if (total > 0) {
        weightedRate = loans.reduce((sum, l) => sum + (l.amount * l.rate), 0) / total;
    }
    
    emit('update', { 
        loanDetailsJson: json,
        loanAmount: total,
        interestRatePercent: parseFloat(weightedRate.toFixed(2))
    });
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
.deal-workbench {
  padding: 0;
  overflow: hidden;
}

.workbench-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: var(--spacing-lg);
  border-bottom: 2px solid var(--color-industrial-copper);
  background: linear-gradient(to right, var(--color-bg-card), rgba(180, 83, 9, 0.03));
}

.deal-title {
  font-size: var(--font-size-xl);
  font-weight: 700;
  margin: 0 0 var(--spacing-xs) 0;
}

.deal-meta {
  display: flex;
  align-items: center;
  gap: var(--spacing-sm);
  font-size: var(--font-size-sm);
  color: var(--color-text-muted);
}

.deal-address {
  max-width: 200px;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.run-simulation-btn {
  display: flex;
  align-items: center;
  gap: var(--spacing-sm);
  min-width: 160px;
  justify-content: center;
}

.spinner {
  width: 16px;
  height: 16px;
  border: 2px solid white;
  border-top-color: transparent;
  border-radius: 50%;
  animation: spin 0.8s linear infinite;
}

@keyframes spin {
  to { transform: rotate(360deg); }
}

/* Grid Layout */
.workbench-grid {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: var(--spacing-lg);
  padding: var(--spacing-lg);
}

@media (max-width: 900px) {
  .workbench-grid {
    grid-template-columns: 1fr;
  }
}


.section-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: var(--spacing-lg);
  border-bottom: 1px solid var(--color-border);
  padding-bottom: var(--spacing-sm);
}

.section-header .section-title {
  border-bottom: none;
  margin-bottom: 0;
  padding-bottom: 0;
}

.save-status {
  font-size: var(--font-size-xs);
  display: flex;
  align-items: center;
}

.status-saving {
  color: var(--color-text-muted);
  display: flex;
  align-items: center;
  gap: 6px;
}

.status-saved {
  color: var(--color-success);
  font-weight: 500;
}

.spinner-sm {
  width: 12px;
  height: 12px;
  border: 2px solid var(--color-text-muted);
  border-top-color: transparent;
  border-radius: 50%;
  animation: spin 0.8s linear infinite;
}

/* Input Groups */
.input-group {
  margin-bottom: var(--spacing-lg);
}

.group-title {
  font-size: var(--font-size-xs);
  font-weight: 600;
  text-transform: uppercase;
  letter-spacing: var(--letter-spacing-wide);
  color: var(--color-text-muted);
  margin: 0 0 var(--spacing-sm) 0;
}

.input-row {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: var(--spacing-sm) 0;
  border-bottom: 1px solid var(--color-border-subtle);
}

.input-row label {
  font-size: var(--font-size-sm);
  color: var(--color-text-secondary);
}

.input-wrapper {
  display: flex;
  align-items: center;
  gap: var(--spacing-xs);
}

.input-wrapper input {
  width: 120px;
  text-align: right;
  padding: var(--spacing-xs) var(--spacing-sm);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-md);
  font-family: var(--font-mono);
}

.input-wrapper input:focus {
  border-color: var(--color-industrial-copper);
  outline: none;
}

.input-prefix,
.input-suffix {
  font-size: var(--font-size-sm);
  color: var(--color-text-muted);
}

.variance-indicator {
  font-size: 10px;
  color: var(--color-info);
  background: rgba(59, 130, 246, 0.1);
  padding: 2px 4px;
  border-radius: var(--radius-sm);
  cursor: help;
}

.alert-banner {
  background: rgba(245, 158, 11, 0.1);
  border: 1px solid rgba(245, 158, 11, 0.2);
  color: var(--color-warning); 
  padding: var(--spacing-sm) var(--spacing-md);
  border-radius: var(--radius-md);
  margin-bottom: var(--spacing-lg);
  display: flex;
  justify-content: space-between;
  align-items: center;
  font-size: var(--font-size-sm);
}

.input-row.calculated {
  background: var(--color-bg-elevated);
  padding: var(--spacing-sm);
  margin: var(--spacing-sm) 0 0 0;
  border-radius: var(--radius-md);
  border: none;
}

.calculated-value {
  font-weight: 600;
  font-family: var(--font-mono);
  color: var(--color-text-primary);
}

/* Results Section */
.no-results {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  padding: var(--spacing-2xl);
  text-align: center;
  color: var(--color-text-muted);
  gap: var(--spacing-md);
}

.no-results svg {
  opacity: 0.4;
  animation: pulse 2s ease-in-out infinite;
}

@keyframes pulse {
  0%, 100% { opacity: 0.4; }
  50% { opacity: 0.6; }
}

.hint {
  font-size: var(--font-size-xs);
  color: var(--color-text-muted);
}

/* Metrics Grid */
.metrics-grid {
  display: grid;
  grid-template-columns: repeat(3, 1fr);
  gap: var(--spacing-md);
  margin-bottom: var(--spacing-lg);
}

.metric-card {
  background: var(--color-bg-elevated);
  padding: var(--spacing-md);
  border-radius: var(--radius-md);
  text-align: center;
}

.metric-label {
  display: block;
  font-size: var(--font-size-xs);
  font-weight: 600;
  text-transform: uppercase;
  color: var(--color-text-muted);
  margin-bottom: var(--spacing-xs);
}

.metric-value {
  font-size: var(--font-size-xl);
  font-weight: 700;
  font-family: var(--font-mono);
}

.metric-value.positive { color: var(--color-success); }
.metric-value.negative { color: var(--color-danger); }

/* Percentile Summary */
.percentile-summary {
  display: flex;
  justify-content: space-between;
  margin: var(--spacing-lg) 0;
  padding: var(--spacing-md);
  background: var(--color-bg-elevated);
  border-radius: var(--radius-md);
}

.percentile {
  text-align: center;
}

.percentile-label {
  display: block;
  font-size: var(--font-size-xs);
  color: var(--color-text-muted);
  margin-bottom: var(--spacing-xs);
}

.percentile-value {
  font-weight: 600;
  font-family: var(--font-mono);
}

.percentile-value.positive { color: var(--color-success); }
.percentile-value.negative { color: var(--color-danger); }

/* Status Badges */
.badge {
  padding: 2px 8px;
  border-radius: var(--radius-full);
  font-size: 11px;
  font-weight: 600;
  text-transform: uppercase;
}

.status-draft { background: var(--color-bg-elevated); color: var(--color-text-muted); }
.status-analyzing { background: rgba(59, 130, 246, 0.1); color: var(--color-info); }
.status-buy { background: rgba(16, 185, 129, 0.1); color: var(--color-success); }
.status-pass { background: var(--color-bg-elevated); color: var(--color-text-secondary); }
.status-uneconomic { background: rgba(239, 68, 68, 0.1); color: var(--color-danger); }

/* View Tabs */
.view-tabs {
  display: flex;
  border-bottom: 1px solid var(--color-border);
  padding: 0 var(--spacing-lg);
  background: var(--color-bg-card);
}

.view-tab {
  display: flex;
  align-items: center;
  gap: var(--spacing-xs);
  padding: var(--spacing-md) var(--spacing-lg);
  border: none;
  background: none;
  font-size: var(--font-size-sm);
  font-weight: 500;
  color: var(--color-text-muted);
}

/* Detailed Controls */
.flex-label {
    display: flex;
    align-items: center;
    justify-content: space-between;
    width: 100%;
}

.btn-xs-toggle {
    font-size: 10px;
    padding: 2px 6px;
    border: 1px solid var(--color-border);
    border-radius: 4px;
    background: var(--color-bg-elevated);
    cursor: pointer;
}

.btn-xs-toggle:hover {
    border-color: var(--color-industrial-copper);
    color: var(--color-industrial-copper);
}

.detailed-subgroup {
    background: var(--color-bg-elevated);
    padding: 8px 12px;
    border-radius: 6px;
    margin-bottom: 12px;
    border-left: 2px solid var(--color-industrial-copper);
}

.sub-row {
    border-bottom: 1px solid rgba(0,0,0,0.05) !important;
    padding: 4px 0 !important;
}

.sub-row:last-child {
    border-bottom: none !important;
}

.select-sm {
    padding: 2px 4px;
    font-size: 12px;
    border: 1px solid var(--color-border);
    border-radius: 4px;
}

.loan-item {
    padding: 6px 0;
    border-bottom: 1px solid rgba(0,0,0,0.05);
}

.loan-row {
    display: flex;
    justify-content: space-between;
    align-items: center;
    gap: 8px;
    margin-bottom: 4px;
}

.loan-row.inputs {
   margin-bottom: 0;
}

.input-xs {
    padding: 2px 4px;
    font-size: 11px;
    border: 1px solid var(--color-border);
    border-radius: 3px;
}

.name-input {
    flex: 1;
}

.btn-icon-xs {
    background: none;
    border: none;
    color: var(--color-text-muted);
    cursor: pointer;
    font-size: 14px;
}

.btn-icon-xs:hover {
    color: var(--color-danger);
}

.btn-add-loan {
    width: 100%;
    text-align: center;
    font-size: 11px;
    padding: 4px;
    margin-top: 4px;
    background: none;
    border: 1px dashed var(--color-border);
    border-radius: 4px;
    cursor: pointer;
    color: var(--color-text-muted);
}

.btn-add-loan:hover {
    border-color: var(--color-industrial-copper);
    color: var(--color-industrial-copper);
}

.loan-summary {
    text-align: right;
    margin-top: 6px;
    color: var(--color-text-muted);
    font-size: 11px;
}

.input-wrapper.xs input {
    width: 60px;
    padding: 2px 4px;
    font-size: 11px;
    height: 24px;
}


.view-tab {
  cursor: pointer;
  border-bottom: 2px solid transparent;
  margin-bottom: -1px;
  transition: all var(--transition-fast);
}

.view-tab:hover {
  color: var(--color-text-primary);
}

.view-tab.active {
  color: var(--color-industrial-copper);
  border-bottom-color: var(--color-industrial-copper);
}

.view-tab svg {
  opacity: 0.7;
}

.view-tab.active svg {
  opacity: 1;
}

/* Spreadsheet View */
.spreadsheet-view {
  padding: var(--spacing-lg);
}

/* Risk View */
.risk-view {
    padding: var(--spacing-lg);
}

.risk-actions-bar {
  margin-bottom: var(--spacing-lg);
  display: flex;
  justify-content: flex-end;
}

.validation-error {
    color: var(--color-danger);
    font-size: var(--font-size-xs);
    margin-top: var(--spacing-xs);
    padding: var(--spacing-xs);
    background: rgba(239, 68, 68, 0.1);
    border-radius: var(--radius-sm);
}
.risk-explanation {
    margin-top: var(--spacing-lg);
    background: var(--color-bg-elevated);
    padding: var(--spacing-lg);
    border-radius: var(--radius-md);
}
.risk-explanation h4 {
    margin-top: 0;
}
.risk-explanation ul {
    margin-bottom: 0;
    padding-left: var(--spacing-lg);
}
.text-danger { color: var(--color-danger); }
.text-primary { color: var(--color-industrial-copper); }
.text-success { color: var(--color-success); }

.workbench-loading {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  padding: var(--spacing-2xl);
  min-height: 400px;
  gap: var(--spacing-md);
}

.workbench-loading .spinner {
  width: 48px;
  height: 48px;
  border: 4px solid var(--color-border);
  border-top-color: var(--color-industrial-copper);
}
</style>
